using FoodHub.Application.Common.Models;
using FoodHub.Domain.Entities;
using FoodHub.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FoodHub.Application.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.OrderItems.Commands.AddOrderItem
{
    public class AddOrderItemHandler : IRequestHandler<AddOrderItemCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;

        public AddOrderItemHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
        }

        public async Task<Result<Guid>> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn));
            }

            var order = await _unitOfWork.Repository<Domain.Entities.Order>()
                .Query()
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.OptionGroups)
                        .ThenInclude(og => og.OptionValues)
                .FirstOrDefaultAsync(x => x.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Order.NotFound),
                    ResultErrorType.NotFound);
            }

            if (order.Status == OrderStatus.Completed)
            {
                return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Order.InvalidAction));
            }

            var menuItem = await _unitOfWork.Repository<MenuItem>()
                .Query()
                .Include(m => m.OptionGroups)
                    .ThenInclude(og => og.OptionItems)
                .FirstOrDefaultAsync(x => x.MenuItemId == request.MenuItemId, cancellationToken);

            if (menuItem == null)
            {
                return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.MenuItem.NotFound));
            }

            if (menuItem.IsOutOfStock)
            {
                return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.MenuItem.OutOfStock));
            }

            // Create simplified option signature for comparison (using only OptionItemIds)
            var requestSignature = request.SelectedOptions == null ? string.Empty :
                string.Join("|", request.SelectedOptions
                    .SelectMany(og => og.SelectedValues)
                    .OrderBy(v => v.OptionItemId)
                    .Select(v => $"{v.OptionItemId}x{v.Quantity}")
                );

            var existingItem = order.OrderItems.FirstOrDefault(x =>
                x.MenuItemId == request.MenuItemId &&
                x.Status == OrderItemStatus.Preparing &&
                (x.ItemNote ?? string.Empty) == (request.Note ?? string.Empty) &&
                GetItemSignature(x) == requestSignature);

            if (existingItem != null)
            {
                if (request.Reason == null)
                {
                    return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Order.ReasonRequired),
                        ResultErrorType.BadRequest);
                }
                existingItem.Quantity += request.Quantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var price = order.OrderType == OrderType.Takeaway
                                ? menuItem.PriceTakeAway
                                : menuItem.PriceDineIn;

                var newItem = new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    OrderId = request.OrderId,
                    MenuItemId = request.MenuItemId,
                    Quantity = request.Quantity,
                    ItemNote = request.Note,
                    CreatedAt = DateTime.UtcNow,
                    Status = OrderItemStatus.Preparing,
                    ItemNameSnapshot = menuItem.Name,
                    ItemCodeSnapshot = menuItem.Code,
                    UnitPriceSnapshot = price,
                    StationSnapshot = menuItem.Station.ToString()
                };

                await ProcessOptionsAsync(newItem, request.SelectedOptions, cancellationToken);

                order.OrderItems.Add(newItem);
            }

            // Recalculate Total including options
            order.TotalAmount = order.OrderItems
                .Where(x => x.Status != OrderItemStatus.Cancelled && x.Status != OrderItemStatus.Rejected)
                .Sum(item =>
                {
                    var itemTotal = item.Quantity * item.UnitPriceSnapshot;
                    var optionsTotal = item.OptionGroups?
                        .SelectMany(og => og.OptionValues)
                        .Sum(ov => ov.ExtraPriceSnapshot * ov.Quantity) ?? 0;
                    return itemTotal + (optionsTotal * item.Quantity);
                });

            // Audit Log
            var auditLog = new OrderAuditLog
            {
                LogId = Guid.NewGuid(),
                OrderId = order.OrderId,
                EmployeeId = userId,
                Action = AuditLogActions.AddOrderItem,
                ChangeReason = request.Reason,
                CreatedAt = DateTime.UtcNow,
            };

            await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            return Result<Guid>.Success(order.OrderId);
        }

        private string GetItemSignature(OrderItem item)
        {
            if (item.OptionGroups == null || !item.OptionGroups.Any()) return string.Empty;

            var allValues = item.OptionGroups
                .SelectMany(og => og.OptionValues)
                .Where(ov => ov.OptionItemId.HasValue)
                .OrderBy(ov => ov.OptionItemId)
                .Select(ov => $"{ov.OptionItemId}x{ov.Quantity}");

            return string.Join("|", allValues);
        }

        private async Task ProcessOptionsAsync(OrderItem item, List<OrderItemOptionGroupDto>? selectedOptions, CancellationToken cancellationToken)
        {
            if (selectedOptions == null || !selectedOptions.Any()) return;

            var optionGroupIds = selectedOptions.Select(og => og.OptionGroupId).ToList();
            var optionItemIds = selectedOptions.SelectMany(og => og.SelectedValues).Select(v => v.OptionItemId).ToList();

            var optionGroups = await _unitOfWork.Repository<OptionGroup>()
                .Query()
                .Where(og => optionGroupIds.Contains(og.OptionGroupId))
                .ToDictionaryAsync(og => og.OptionGroupId, cancellationToken);

            var optionItems = await _unitOfWork.Repository<OptionItem>()
                .Query()
                .Where(oi => optionItemIds.Contains(oi.OptionItemId))
                .ToDictionaryAsync(oi => oi.OptionItemId, cancellationToken);

            foreach (var optionGroupDto in selectedOptions)
            {
                if (optionGroups.TryGetValue(optionGroupDto.OptionGroupId, out var ogDef))
                {
                    var orderItemOptionGroup = new OrderItemOptionGroup
                    {
                        OrderItemOptionGroupId = Guid.NewGuid(),
                        OrderItemId = item.OrderItemId,
                        GroupNameSnapshot = ogDef.Name,
                        GroupTypeSnapshot = ogDef.OptionType.ToString(),
                        IsRequiredSnapshot = ogDef.IsRequired,
                        CreatedAt = DateTime.UtcNow
                    };

                    foreach (var valueDto in optionGroupDto.SelectedValues)
                    {
                        if (optionItems.TryGetValue(valueDto.OptionItemId, out var oiDef))
                        {
                            var orderItemOptionValue = new OrderItemOptionValue
                            {
                                OrderItemOptionValueId = Guid.NewGuid(),
                                OrderItemOptionGroupId = orderItemOptionGroup.OrderItemOptionGroupId,
                                OptionItemId = valueDto.OptionItemId,
                                LabelSnapshot = oiDef.Label,
                                ExtraPriceSnapshot = oiDef.ExtraPrice,
                                Quantity = valueDto.Quantity,
                                Note = valueDto.Note,
                                CreatedAt = DateTime.UtcNow
                            };
                            orderItemOptionGroup.OptionValues.Add(orderItemOptionValue);
                        }
                    }
                    item.OptionGroups.Add(orderItemOptionGroup);
                }
            }
        }
    }
}
