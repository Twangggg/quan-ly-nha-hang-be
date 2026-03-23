using Microsoft.AspNetCore.SignalR;

namespace FoodHub.Infrastructure.Services.Hubs
{
    public class BillingHub : Hub
    {
        // Hub dành riêng cho thông báo thanh toán và quản lý hóa đơn.
        // Frontend kết nối vào: ws://localhost:5133/hubs/billing
        // Tùy chọn: Bạn có thể thêm các method JoinGroup theo OrderId nếu cần.
    }
}
