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
    public class SubmitOrderToKitchenCommandHandler : IRequestHandler<SubmitOrderToKitchenCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;

        public SubmitOrderToKitchenCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
        }

        public async Task<Result<Guid>> Handle(SubmitOrderToKitchenCommand request, CancellationToken cancellationToken)
        {
            //Get current user
            var currentIdString = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentIdString) || !Guid.TryParse(currentIdString, out var userId))
            {
                return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn)); 
            }

            //Validate Table for dine in
            if (request.OrderType == OrderType.DineIn)
            {
                //Table cho sprint sau thay tạm bằng table ko tróng
                //var tableExists = await _unitOfWork.Repository<Table>()
                //    .Query()
                //    .AnyAsync(t => t.Id == request.TableId, cancellationToken);

                if (request.TableId != null)
                {
                    return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Order.SelectTable));
                }
            }

            //Validate All Menu Items Exist 
            var menuItemIds = request.Items.Select(i => i.MenuItemId).Distinct().ToList();
            var menuItems = await _unitOfWork.Repository<MenuItem>()
                .Query()
                .Where(m => menuItemIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, cancellationToken);
            if (menuItems.Count != menuItemIds.Count)
            {
                var missingIds = menuItemIds.Except(menuItems.Keys);
                return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.MenuItem.NotFound));
            }

            //Check Out of Stock
            var outOfStockItem = menuItems.Values.Where(x => x.IsOutOfStock).ToList();
            if(outOfStockItem.Any())
            {
                return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.MenuItem.OutOfStock) + 
                    $"{string.Join(", ", outOfStockItem.Select(m => m.Name))}");
            }

            //Gen Order code
            var orderCode = await GenerateOrderCodeAsync(cancellationToken);

            //Create Order entity
            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                OrderCode = orderCode,
                OrderType = request.OrderType,
                Status = OrderStatus.Serving,
                TableId = request.OrderType == OrderType.DineIn ? request.TableId : null,
                Note = request.Note,
                TotalAmount = 0, //calculated after items
                IsPriority = false,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                OrderItems = new List<OrderItem>(),
                OrderAuditLogs = new List<OrderAuditLog>()
            };

            //Merge Duplicate Items and Create Order Items 
            // Group by MenuItemId + Note to merge duplicates
            var groupedItems = request.Items
                .GroupBy(i => new { i.MenuItemId, Note = i.Note ?? string.Empty })
                .Select(g => new
                {
                    g.Key.MenuItemId,
                    Note = string.IsNullOrEmpty(g.Key.Note) ? null : g.Key.Note,
                    TotalQuantity = g.Sum(i => i.Quantity)
                })
                .ToList();
            foreach (var group in groupedItems)
            {
                var menuItem = menuItems[group.MenuItemId];
                // Select price based on order type
                var price = request.OrderType == OrderType.Takeaway
                    ? menuItem.PriceTakeAway
                    : menuItem.PriceDineIn;
                var orderItem = new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    MenuItemId = group.MenuItemId,
                    Quantity = group.TotalQuantity,
                    ItemNote = group.Note,
                    Status = OrderItemStatus.Preparing,
                    CreatedAt = DateTime.UtcNow,
                    //snapshot
                    ItemCodeSnapshot = menuItem.Code,
                    ItemNameSnapshot = menuItem.Name,
                    UnitPriceSnapshot = price,
                    StationSnapshot = menuItem.Station.ToString()
                };
                order.OrderItems.Add(orderItem);
            }

            //Calculate total amount
            order.TotalAmount = order.OrderItems.Sum(x => x.Quantity * x.UnitPriceSnapshot);

            //Audit Log
            var auditLog = new OrderAuditLog
            {
                LogId = Guid.NewGuid(),
                OrderId = order.OrderId,
                EmployeeId = userId,
                Action = "SUBMIT", // ✅ Only one action: SUBMIT (no CREATE or ADD_ITEM)
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Order>().AddAsync(order);
            await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
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
            var lastOrder = await _unitOfWork.Repository<Order>()
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
