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
        private readonly IHubContext<TableStatusHub> _tableStatusHubContext;

        public SignalRService(
            IHubContext<KdsHub> hubContext,
            IHubContext<BillingHub> billingHubContext,
            IHubContext<TableStatusHub> tableStatusHubContext
        )
        {
            _hubContext = hubContext;
            _billingHubContext = billingHubContext;
            _tableStatusHubContext = tableStatusHubContext;
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

        public async Task NotifyKdsItemUpdatedAsync(string station, object kdsItem)
        {
            try
            {
                await _hubContext.Clients.Group(station).SendAsync("KdsItemUpdated", kdsItem);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR Error in NotifyKdsItemUpdatedAsync: {ex.Message}");
            }
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

        /// <summary>
        /// Thông báo cho nhân viên khi lịch ca làm việc thay đổi.
        /// Frontend lắng nghe event "ShiftAssignmentChanged" trong group "{employeeId}".
        /// </summary>
        public async Task NotifyShiftAssignmentAsync(
            Guid employeeId,
            string shiftName,
            DateOnly assignedDate,
            bool isCancelled)
        {
            try
            {
                await _hubContext.Clients.Group(employeeId.ToString()).SendAsync(
                    "ShiftAssignmentChanged",
                    new
                    {
                        ShiftName = shiftName,
                        AssignedDate = assignedDate.ToString("yyyy-MM-dd"),
                        IsCancelled = isCancelled
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR Error in NotifyShiftAssignmentAsync: {ex.Message}");
            }
        }
        public async Task NotifyTableStatusChangedAsync(Guid tableId, string newStatus)
        {
            // Bắn event tới tất cả client đang xem sơ đồ bàn
            await _tableStatusHubContext.Clients.All.SendAsync(
                "TableStatusChanged",
                new { TableId = tableId, Status = newStatus }
            );
        }
    }
}
