using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace FoodHub.Application.Features.Orders.Commands.SubmitOrderToKitchen
{
    public class SubmitOrderToKitchenHandler
        : IRequestHandler<SubmitOrderToKitchenCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ISignalRService _signalRService;

        public SubmitOrderToKitchenHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ISignalRService signalRService
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _signalRService = signalRService;
        }

        public async Task<Result<Guid>> Handle(
            SubmitOrderToKitchenCommand request,
            CancellationToken cancellationToken
        )
        {
            //Get current user
            var currentIdString = _currentUserService.UserId;
            if (
                string.IsNullOrEmpty(currentIdString)
                || !Guid.TryParse(currentIdString, out var userId)
            )
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            //Validate Table for dine in
            if (request.OrderType == OrderType.DineIn)
            {
                if (!request.TableId.HasValue)
                {
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(MessageKeys.Order.SelectTable)
                    );
                }
            }

            //Validate All Menu Items Exist
            var menuItemIds = request.Items.Select(i => i.MenuItemId).Distinct().ToList();
            var menuItems = await _unitOfWork
                .Repository<MenuItem>()
                .Query()
                .Where(m => menuItemIds.Contains(m.MenuItemId))
                .ToDictionaryAsync(m => m.MenuItemId, cancellationToken);
            if (menuItems.Count != menuItemIds.Count)
            {
                var missingIds = menuItemIds.Except(menuItems.Keys);
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.MenuItem.NotFound)
                );
            }

            // Validate Options Exist (if provided)
            var allOptionGroupIds = request
                .Items.Where(i => i.SelectedOptions != null)
                .SelectMany(i => i.SelectedOptions!)
                .Select(og => og.OptionGroupId)
                .Distinct()
                .ToList();
            var allOptionItemIds = request
                .Items.Where(i => i.SelectedOptions != null)
                .SelectMany(i => i.SelectedOptions!)
                .SelectMany(og => og.SelectedValues)
                .Select(v => v.OptionItemId)
                .Distinct()
                .ToList();
            Dictionary<Guid, OptionGroup> optionGroups = new();
            Dictionary<Guid, OptionItem> optionItems = new();
            if (allOptionGroupIds.Any())
            {
                optionGroups = await _unitOfWork
                    .Repository<OptionGroup>()
                    .Query()
                    .Where(og => allOptionGroupIds.Contains(og.OptionGroupId))
                    .ToDictionaryAsync(og => og.OptionGroupId, cancellationToken);
                if (optionGroups.Count != allOptionGroupIds.Count)
                {
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(MessageKeys.OptionGroup.NotFound)
                    );
                }
            }
            if (allOptionItemIds.Any())
            {
                optionItems = await _unitOfWork
                    .Repository<OptionItem>()
                    .Query()
                    .Where(oi => allOptionItemIds.Contains(oi.OptionItemId))
                    .ToDictionaryAsync(oi => oi.OptionItemId, cancellationToken);
                if (optionItems.Count != allOptionItemIds.Count)
                {
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(MessageKeys.OptionItem.NotFound)
                    );
                }
            }

            //Check Out of Stock
            var outOfStockItem = menuItems.Values.Where(x => x.IsOutOfStock).ToList();
            if (outOfStockItem.Any())
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.MenuItem.OutOfStock)
                        + $"{string.Join(", ", outOfStockItem.Select(m => m.Name))}"
                );
            }

            // 1. Retrieve or Create Order
            Order? order = null;

            if (request.OrderId != Guid.Empty)
            {
                order = await _unitOfWork
                    .Repository<Order>()
                    .Query()
                    .Include(x => x.OrderItems)
                        .ThenInclude(oi => oi.OptionGroups)
                            .ThenInclude(og => og.OptionValues)
                    .FirstOrDefaultAsync(x => x.OrderId == request.OrderId, cancellationToken);
            }

            if (order == null && request.OrderType == OrderType.DineIn && request.TableId.HasValue)
            {
                // Check for existing active order at this table
                order = await _unitOfWork
                    .Repository<Order>()
                    .Query()
                    .Include(x => x.OrderItems)
                        .ThenInclude(oi => oi.OptionGroups)
                            .ThenInclude(og => og.OptionValues)
                    .FirstOrDefaultAsync(
                        x => x.TableId == request.TableId && x.Status == OrderStatus.Serving,
                        cancellationToken
                    );
            }

            bool isNewOrder = false;
            if (order == null)
            {
                isNewOrder = true;
                var orderCode = await GenerateOrderCodeAsync(cancellationToken);
                order = new Order
                {
                    OrderId = Guid.NewGuid(),
                    OrderCode = orderCode,
                    OrderType = request.OrderType,
                    Status = OrderStatus.Serving,
                    TableId = request.OrderType == OrderType.DineIn ? request.TableId : null,
                    Note = request.Note,
                    TotalAmount = 0,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                };
            }

            // 2. Add/Update Items using Domain Logic
            var processedItems = new List<OrderItem>();
            foreach (var itemDto in request.Items)
            {
                var menuItem = menuItems[itemDto.MenuItemId];

                // Prepare options for Domain method
                var domainOptions =
                    new List<(
                        OptionGroup Group,
                        List<(OptionItem Item, int Quantity, string? Note)> Selections
                    )>();
                if (itemDto.SelectedOptions != null)
                {
                    foreach (var optDto in itemDto.SelectedOptions)
                    {
                        if (optionGroups.TryGetValue(optDto.OptionGroupId, out var og))
                        {
                            var selections = optDto
                                .SelectedValues.Where(v => optionItems.ContainsKey(v.OptionItemId))
                                .Select(v => (optionItems[v.OptionItemId], v.Quantity, v.Note))
                                .ToList();
                            domainOptions.Add((og, selections));
                        }
                    }
                }

                var result = order.AddOrUpdateItem(
                    menuItem,
                    itemDto.Quantity,
                    itemDto.Note,
                    domainOptions
                );
                processedItems.Add(result.Item);
            }

            // 3. Save Changes
            if (isNewOrder)
            {
                await _unitOfWork.Repository<Order>().AddAsync(order);
            }
            else
            {
                _unitOfWork.Repository<Order>().Update(order);
            }

            var auditLog = new OrderAuditLog
            {
                LogId = Guid.NewGuid(),
                OrderId = order.OrderId,
                EmployeeId = userId,
                Action = isNewOrder ? AuditLogActions.SubmitOrder : AuditLogActions.AddOrderItem,
                CreatedAt = DateTime.UtcNow,
            };
            await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // 4. Notify KDS
            foreach (var item in processedItems)
            {
                _ = _signalRService.NotifyOrderItemStatusChangedAsync(
                    item.OrderItemId,
                    item.Status,
                    item.StationSnapshot
                );
            }

            return Result<Guid>.Success(order.OrderId);
        }

        /// <summary>
        /// Generate unique order code in format: ORD-yyyyMMdd-xxxx
        /// Thread-safe sequential numbering per day
        /// </summary>
        private async Task<string> GenerateOrderCodeAsync(CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;
            var dateString = today.ToString("yyyyMMdd");
            var prefix = $"ORD-{dateString}-";
            // Get last order code for today
            var lastOrder = await _unitOfWork
                .Repository<Order>()
                .Query()
                .Where(o => o.OrderCode.StartsWith(prefix))
                .OrderByDescending(o => o.OrderCode)
                .FirstOrDefaultAsync(cancellationToken);
            int sequenceNumber = 1;
            if (lastOrder != null)
            {
                var parts = lastOrder.OrderCode.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSequence))
                {
                    sequenceNumber = lastSequence + 1;
                }
            }
            return $"{prefix}{sequenceNumber:D4}";
        }
    }
}
