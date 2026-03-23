using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Enums;
using FoodHub.Infrastructure.Services.Hubs;
using FoodHub.Infrastructure.Services.Messaging.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FoodHub.Infrastructure.Services
{
    public class SignalRService : ISignalRService
    {
        private readonly IHubContext<KdsHub> _hubContext;
        private readonly IHubContext<BillingHub> _billingHubContext;

        public SignalRService(
            IHubContext<KdsHub> hubContext,
            IHubContext<BillingHub> billingHubContext
        )
        {
            _hubContext = hubContext;
            _billingHubContext = billingHubContext;
        }

        // Thông báo khi có món ăn mới vừa được đặt (Submit Order/Add Item)
        public async Task NotifyNewOrderItemAsync(Guid orderId, Guid orderItemId, string station)
        {
            await _hubContext
                .Clients.Group(station)
                .SendAsync(
                    "NewOrderItemReceived",
                    new { OrderId = orderId, OrderItemId = orderItemId }
                );
        }

        public async Task NotifyOrderItemStatusChangedAsync(
            Guid orderItemId,
            OrderItemStatus newStatus,
            string station
        )
        {
            // Bắn event kèm ID món ăn và trạng thái mới
            await _hubContext
                .Clients.Group(station)
                .SendAsync(
                    "OrderItemStatusChanged",
                    new { OrderItemId = orderItemId, Status = newStatus }
                );
        }

        public async Task NotifyOrderStatusChangedAsync(Guid orderId, string status)
        {
            // Thông báo toàn bộ đơn hàng (ví dụ: đã thanh toán xong) cho KDS
            await _hubContext.Clients.All.SendAsync(
                "OrderStatusChanged",
                new { OrderId = orderId, Status = status }
            );

            // Bắn tín hiệu sang màn hình Thanh Toán (Frontend)
            await _billingHubContext.Clients.All.SendAsync(
                "OrderStatusChanged",
                new { OrderId = orderId, Status = status }
            );
        }
    }
}
