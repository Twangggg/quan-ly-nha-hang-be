using Microsoft.AspNetCore.SignalR;

namespace FoodHub.WebAPI.Presentation.Hubs
{
    public class KdsHub : Hub
    {
        // Hàm này giúp màn hình Bếp hoặc Bar đăng ký nhận tin nhắn theo cụm (Group)
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
