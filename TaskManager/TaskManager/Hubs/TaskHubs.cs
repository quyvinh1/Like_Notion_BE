using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TaskManager.Hubs
{
    [Authorize]
    public class TaskHubs : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User.FindFirst("UserId").Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User-{userId}");
            }
            await base.OnConnectedAsync();
        }
        public async Task JoinPageRoom(string pageId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Page-{pageId}");
        }
        public async Task LeavePageRoom(string pageId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Page-{pageId}");
        }
    }
}
