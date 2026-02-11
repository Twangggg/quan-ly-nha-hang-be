using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.OrderItems.Commands.AddOrderItem;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.OrderItems.Commands.UpdateOrderItem
{
    public class UpdateOrderItemHandler : IRequestHandler<UpdateOrderItemCommand, Result<UpdateOrderItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateOrderItemCommand> _logger;


        public UpdateOrderItemHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            IMapper mapper,
            ILogger<UpdateOrderItemCommand> logger)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<Result<UpdateOrderItemResponse>> Handle(UpdateOrderItemCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<UpdateOrderItemResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn), ResultErrorType.Unauthorized);
            }

            var order = await _unitOfWork.Repository<Domain.Entities.Order>()
                .Query()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OptionGroups)
                        .ThenInclude(og => og.OptionValues)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result<UpdateOrderItemResponse>.Failure(_messageService.GetMessage(MessageKeys.Order.NotFound), ResultErrorType.NotFound);
            }

            var incomingItems = request.Items ?? new List<UpdateOrderItemDto>();

            var itemsToRemove = order.OrderItems
                .Where(oi => oi.Status != OrderItemStatus.Cancelled &&
                !incomingItems.Any(ii => ii.OrderItemId == oi.OrderItemId))
                .ToList();

            foreach (var item in itemsToRemove)
            {
                item.Status = OrderItemStatus.Cancelled;
                item.UpdatedAt = DateTime.UtcNow;
            }

            foreach (var incomingItem in incomingItems)
            {
                var existingItem = order.OrderItems.FirstOrDefault(i => i.OrderItemId == incomingItem.OrderItemId);

                if (existingItem != null)
                {
                    // Update existing
                    existingItem.Quantity = incomingItem.Quantity;
                    existingItem.ItemNote = incomingItem.ItemNote;
                    existingItem.UpdatedAt = DateTime.UtcNow;

                    await ProcessOptionsAsync(existingItem, incomingItem.SelectedOptions, cancellationToken);
                }
                else
                {
                    // Add new item
                    var menuItem = await _unitOfWork.Repository<MenuItem>().GetByIdAsync(incomingItem.MenuItemId);
                    if (menuItem == null)
                    {
                        return Result<UpdateOrderItemResponse>.Failure(_messageService.GetMessage(MessageKeys.MenuItem.NotFound));
                    }

                    var price = order.OrderType == Domain.Enums.OrderType.Takeaway
                        ? menuItem.PriceTakeAway
                        : menuItem.PriceDineIn;

                    var newItem = new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        OrderId = order.OrderId,
                        MenuItemId = incomingItem.MenuItemId,
                        Quantity = incomingItem.Quantity,
                        ItemNote = incomingItem.ItemNote,
                        CreatedAt = DateTime.UtcNow,
                        Status = OrderItemStatus.Preparing,
                        ItemNameSnapshot = menuItem.Name,
                        ItemCodeSnapshot = menuItem.Code,
                        UnitPriceSnapshot = price,
                        StationSnapshot = menuItem.Station.ToString()
                    };

                    await ProcessOptionsAsync(newItem, incomingItem.SelectedOptions, cancellationToken);
                    order.OrderItems.Add(newItem);
                }
            }

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
            order.UpdatedAt = DateTime.UtcNow;

            var auditLog = new OrderAuditLog
            {
                LogId = Guid.NewGuid(),
                OrderId = order.OrderId,
                EmployeeId = auditorId,
                Action = AuditLogActions.UpdateOrderItem,
                CreatedAt = DateTime.UtcNow,
                ChangeReason = request.Reason,
                NewValue = "{\"action\": \"Updated Order Items Sync\"}"
            };

            await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
            _unitOfWork.Repository<Domain.Entities.Order>().Update(order);

            try
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while updating order items for OrderId {OrderId}", request.OrderId);
                return Result<UpdateOrderItemResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
            }

            var response = _mapper.Map<UpdateOrderItemResponse>(order);
            return Result<UpdateOrderItemResponse>.Success(response);
        }

        private async Task ProcessOptionsAsync(OrderItem item, List<OrderItemOptionGroupDto>? selectedOptions, CancellationToken cancellationToken)
        {
            // Clear existing options
            item.OptionGroups.Clear();

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
