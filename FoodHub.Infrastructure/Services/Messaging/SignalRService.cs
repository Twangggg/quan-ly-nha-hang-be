using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using FoodHub.Infrastructure.Services.Messaging.Hubs;

namespace FoodHub.Infrastructure.Services.Messaging
{
    public class SignalRService : ISignalRService
    {
        private readonly IHubContext<KdsHub> _hubContext;
        private readonly IHubContext<TableStatusHub> _tableStatusHubContext;

        public SignalRService(
            IHubContext<KdsHub> hubContext,
            IHubContext<TableStatusHub> tableStatusHubContext
        )
        {
            _hubContext = hubContext;
            _tableStatusHubContext = tableStatusHubContext;
        }

        // Thông báo khi có món ăn mới vừa được đặt (Submit Order/Add Item)
        public async Task NotifyNewOrderItemAsync(Guid orderId, Guid orderItemId, string station)
        {
            try
            {
                await _hubContext
                    .Clients.Group(station)
                    .SendAsync(
                        "NewOrderItemReceived",
                        new { OrderId = orderId, OrderItemId = orderItemId }
                    );
            }
            catch (Exception ex)
            {
                // Silently log or ignore to prevent process crash
                Console.WriteLine($"SignalR Error in NotifyNewOrderItemAsync: {ex.Message}");
            }
        }

        public async Task NotifyOrderItemStatusChangedAsync(
            Guid orderItemId,
            OrderItemStatus newStatus,
            string station
        )
        {
            try
            {
                // Bắn event kèm ID món ăn và trạng thái mới
                await _hubContext
                    .Clients.Group(station)
                    .SendAsync(
                        "OrderItemStatusChanged",
                        new { OrderItemId = orderItemId, Status = newStatus }
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR Error in NotifyOrderItemStatusChangedAsync: {ex.Message}");
            }
        }

        public async Task NotifyOrderStatusChangedAsync(Guid orderId, string status)
        {
            try
            {
                // Thông báo toàn bộ đơn hàng (ví dụ: đã thanh toán xong)
                await _hubContext.Clients.All.SendAsync(
                    "OrderStatusChanged",
                    new { OrderId = orderId, Status = status }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR Error in NotifyOrderStatusChangedAsync: {ex.Message}");
            }
        }

        public async Task NotifyTableStatusChangedAsync(Guid tableId, string newStatus)
        {
            try
            {
                await _tableStatusHubContext.Clients.All.SendAsync(
                    "TableStatusChanged",
                    new { TableId = tableId, Status = newStatus }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR Error in NotifyTableStatusChangedAsync: {ex.Message}");
            }
        }
    }
}
