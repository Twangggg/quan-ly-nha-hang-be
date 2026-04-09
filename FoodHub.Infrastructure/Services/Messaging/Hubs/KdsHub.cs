using Microsoft.AspNetCore.SignalR;

namespace FoodHub.Infrastructure.Services.Messaging.Hubs
{
    public class KdsHub : Hub
    {
        // Khi một màn hình KDS mở lên, nó sẽ gửi Station Name 
        // để join vào nhóm riêng của trạm đó.
        // Giúp bếp chỉ nhận tin của bếp, bar chỉ nhận tin của bar.
        public async Task JoinStationGroup(string stationName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, stationName);
        }

        public async Task LeaveStationGroup(string stationName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, stationName);
        }
    }
}
