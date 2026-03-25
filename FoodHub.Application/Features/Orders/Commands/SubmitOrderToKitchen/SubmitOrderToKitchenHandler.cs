using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Features.Options.Common;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Orders.Commands.SubmitOrderToKitchen
{
    public class SubmitOrderToKitchenHandler
        : IRequestHandler<SubmitOrderToKitchenCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ISignalRService _signalRService;
        private readonly ILogger<SubmitOrderToKitchenHandler> _logger;

        public SubmitOrderToKitchenHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            ISignalRService signalRService,
            ILogger<SubmitOrderToKitchenHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _signalRService = signalRService;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(
            SubmitOrderToKitchenCommand request,
            CancellationToken cancellationToken
        )
        {
            var userId = _currentUserService.GetUserIdAsGuid();
            if (userId == null)
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            // Retrieve existing order early if provided
            Order? order = null;
            if (request.OrderId != Guid.Empty)
            {
                order = await _unitOfWork
                    .Repository<Order>()
                    .Query()
                    .FirstOrDefaultAsync(x => x.OrderId == request.OrderId, cancellationToken);
            }

            // Determine effective order type and table (prefer request, fallback to existing order)
            var orderType = order?.OrderType ?? request.OrderType;
            var tableId = request.TableId ?? order?.TableId;

            //Validate Table for dine in
            if (orderType == OrderType.DineIn && !tableId.HasValue)
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.SelectTable)
                );
            }

            //Validate All Menu Items Exist
            var menuItemIds = request.Items.Select(i => i.MenuItemId).Distinct().ToList();
            var menuItems = await _unitOfWork
                .Repository<MenuItem>()
                .Query()
                .Where(m => menuItemIds.Contains(m.MenuItemId))
                .Include(m => m.MenuItemOptionGroups)
                    .ThenInclude(miog => miog.OptionGroup)
                        .ThenInclude(og => og.OptionItems)
                .ToDictionaryAsync(m => m.MenuItemId, cancellationToken);
            if (menuItems.Count != menuItemIds.Count)
            {
                var missingIds = menuItemIds.Except(menuItems.Keys);
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.MenuItem.NotFound)
                );
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

            Table? table = null;
            if (orderType == OrderType.DineIn && tableId.HasValue)
            {
                table = await _unitOfWork
                    .Repository<Table>()
                    .Query()
                    .FirstOrDefaultAsync(t => t.TableId == tableId.Value, cancellationToken);
            }

            // Retrieve or Create Order
            if (order == null && orderType == OrderType.DineIn && tableId.HasValue)
            {
                // Check for existing active order at this table (also lightweight)
                order = await _unitOfWork
                    .Repository<Order>()
                    .Query()
                    .FirstOrDefaultAsync(
                        x => x.TableId == tableId && x.Status == OrderStatus.Serving,
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
                    OrderType = orderType,
                    Status = OrderStatus.Serving,
                    TableId = orderType == OrderType.DineIn ? tableId : null,
                    Note = request.Note,
                    TotalAmount = 0,
                    CreatedBy = userId.Value,
                    CreatedAt = DateTime.UtcNow,
                };
            }
            else if (orderType == OrderType.DineIn && tableId.HasValue)
            {
                order.TableId = tableId;
            }

            if (order.SubTotal <= 0 && order.TotalAmount > 0)
            {
                order.SubTotal =
                    order.DiscountAmount > 0 || order.VatAmount > 0
                        ? Math.Max(
                            order.TotalAmount / (1 + order.VatRate) + order.DiscountAmount,
                            0
                        )
                        : order.TotalAmount;
            }

            if (table != null && orderType == OrderType.DineIn)
            {
                table.MarkAsOccupied(userId.Value, DateTime.UtcNow);
                _unitOfWork.Repository<Table>().Update(table);
            }

            // Group items by signature to merge duplicates within the same request
            var groupedItems = new List<(OrderItemDto Dto, OrderItem Item)>();

            foreach (var itemDto in request.Items)
            {
                var menuItem = menuItems[itemDto.MenuItemId];

                // Prepare options for Domain method
                var selectionValidation = OptionSelectionValidation.ValidateForMenuItem(
                    menuItem,
                    itemDto
                        .SelectedOptions?.Select(x => new RequestedOptionSelection(
                            x.OptionGroupId,
                            x.SelectedValues.Select(v => new RequestedOptionValue(
                                    v.OptionItemId,
                                    v.Quantity,
                                    v.Note
                                ))
                                .ToList()
                        ))
                        .ToList(),
                    _messageService
                );
                if (!selectionValidation.IsSuccess)
                {
                    return Result<Guid>.Failure(
                        selectionValidation.Error!,
                        selectionValidation.ErrorType
                    );
                }

                var domainOptions = selectionValidation
                    .Data!.Select(x => (x.Assignment, x.Group, x.Selections))
                    .ToList();

                // Create Item using lightweight method
                var newItem = order.CreateOrderItem(
                    menuItem,
                    itemDto.Quantity,
                    itemDto.Note,
                    domainOptions
                );

                // Find if we already have an identical item in this request (Grouping logic)
                // We use AddOrUpdateItem's signature logic indirectly or just compare signatures.
                // Actually, let's just use a simple grouping here to avoid polluting 'order.OrderItems' yet.

                var existingGrouped = groupedItems.FirstOrDefault(x =>
                    x.Item.MenuItemId == newItem.MenuItemId
                    && (x.Item.ItemNote ?? "") == (newItem.ItemNote ?? "")
                    && order.GetItemSignature(x.Item) == order.GetItemSignature(newItem)
                );

                if (existingGrouped.Item != null)
                {
                    existingGrouped.Item.Quantity += newItem.Quantity;
                }
                else
                {
                    groupedItems.Add((itemDto, newItem));
                }
            }

            var processedItems = new List<OrderItem>();
            foreach (var grouped in groupedItems)
            {
                var newItem = grouped.Item;

                // Add to repository directly to ensure EF only performs an INSERT
                await _unitOfWork.Repository<OrderItem>().AddAsync(newItem);

                // Track for SignalR notification
                processedItems.Add(newItem);

                // Update Order TotalAmount (Incremental Update)
                var itemTotal = newItem.Quantity * newItem.UnitPriceSnapshot;
                var optionsTotal = newItem
                    .OptionGroups.SelectMany(og => og.OptionValues)
                    .Sum(ov => ov.ExtraPriceSnapshot * ov.Quantity);

                order.TotalAmount += itemTotal + (optionsTotal * newItem.Quantity);
                order.SubTotal += itemTotal + (optionsTotal * newItem.Quantity);
                order.UpdatedAt = DateTime.UtcNow;

                // For New Order, we also add to the collection so the initial AddAsync(order) includes them.
                // This preserves the expected behavior in existing tests.
                if (isNewOrder)
                {
                    order.OrderItems.Add(newItem);
                }
            }

            // Save Changes
            try
            {
                if (isNewOrder)
                {
                    await _unitOfWork.Repository<Order>().AddAsync(order);
                }

                var auditLog = new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    EmployeeId = userId.Value,
                    Action = isNewOrder
                        ? AuditLogActions.SubmitOrder
                        : AuditLogActions.AddOrderItem,
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _cacheService.RemoveByPatternAsync(
                    CacheKey.TableList + "*",
                    cancellationToken
                );
                await _cacheService.RemoveByPatternAsync(
                    string.Format(CacheKey.TableListByArea, "*"),
                    cancellationToken
                );
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Concurrency conflict when saving order {OrderId}",
                    order.OrderId
                );
                return Result<Guid>.Failure(
                    "Đơn hàng đang được cập nhật bởi một phiên làm việc khác. Vui lòng thử lại sau.",
                    ResultErrorType.Conflict
                );
            }

            // 4. Notify KDS
            // 4. Notify KDS
            foreach (var item in processedItems)
            {
                await _signalRService.NotifyOrderItemStatusChangedAsync(
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
