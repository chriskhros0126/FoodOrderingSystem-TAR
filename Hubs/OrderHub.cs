using Microsoft.AspNetCore.SignalR;

namespace FoodOrderingSystem.Hubs
{
    public class OrderHub : Hub
    {
        public async Task JoinOrderGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveOrderGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
    }
} 