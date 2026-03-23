using System.Text.Json;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class OrderAuditLog
    {
        public Guid LogId { get; set; }
        public Guid OrderId { get; set; }
        public Guid EmployeeId { get; set; }

        public string Action { get; set; } = null!;

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? ChangeReason { get; set; }

        public DateTime CreatedAt { get; set; }
        public virtual Order Order { get; set; } = null!;
        public virtual Employee Employee { get; set; } = null!;

        public static OrderAuditLog CreateOrderCreated(
            Guid orderId,
            Guid employeeId,
            string orderCode,
            OrderType orderType,
            Guid? tableId
        )
        {
            return Create(
                orderId,
                employeeId,
                OrderAuditActions.CreateOrder,
                newValue: new
                {
                    orderCode,
                    orderType = orderType.ToString(),
                    tableId,
                }
            );
        }

        public static OrderAuditLog CreateOrderCancelled(
            Guid orderId,
            Guid employeeId,
            string? reason
        )
        {
            return Create(
                orderId,
                employeeId,
                OrderAuditActions.CancelOrder,
                changeReason: reason,
                newValue: new { status = OrderStatus.Cancelled.ToString() }
            );
        }

        public static OrderAuditLog CreateOrderItemAdded(
            Guid orderId,
            Guid employeeId,
            Guid orderItemId,
            bool isNew,
            int quantity,
            string? reason
        )
        {
            return Create(
                orderId,
                employeeId,
                OrderAuditActions.AddOrderItem,
                changeReason: reason,
                newValue: new
                {
                    orderItemId,
                    isNew,
                    quantity,
                }
            );
        }

        public static OrderAuditLog CreateKdsStartCooking(Guid orderId, Guid employeeId)
        {
            return Create(
                orderId,
                employeeId,
                OrderAuditActions.KdsStartCooking,
                oldValue: OrderItemStatus.Preparing.ToString(),
                newValue: OrderItemStatus.Cooking.ToString()
            );
        }

        private static OrderAuditLog Create(
            Guid orderId,
            Guid employeeId,
            string action,
            object? oldValue = null,
            object? newValue = null,
            string? changeReason = null
        )
        {
            return new OrderAuditLog
            {
                LogId = Guid.NewGuid(),
                OrderId = orderId,
                EmployeeId = employeeId,
                Action = action,
                OldValue = Serialize(oldValue),
                NewValue = Serialize(newValue),
                ChangeReason = changeReason,
                CreatedAt = DateTime.UtcNow,
            };
        }

        private static string? Serialize(object? value)
        {
            return value is null ? null : JsonSerializer.Serialize(value);
        }
    }
}
