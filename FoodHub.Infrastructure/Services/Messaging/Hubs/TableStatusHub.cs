using Microsoft.AspNetCore.SignalR;

namespace FoodHub.Infrastructure.Services.Messaging.Hubs
{
    /// <summary>
    /// Hub dành riêng cho sơ đồ bàn.
    /// Frontend kết nối vào: ws://{host}/hubs/table-status
    /// Lắng nghe event "TableStatusChanged" để cập nhật trạng thái bàn theo thời gian thực.
    /// </summary>
    public class TableStatusHub : Hub
    {
    }
}
