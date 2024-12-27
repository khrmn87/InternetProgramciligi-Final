using Microsoft.AspNetCore.SignalR;

namespace Webapplication.Hubs
{
    public class FileHub : Hub
    {
        public async Task UpdateStorageInfo(string userId, double totalSize)
        {
            await Clients.Group("Admins").SendAsync("ReceiveStorageUpdate", totalSize);
            await Clients.User(userId).SendAsync("ReceiveUserStorageUpdate", totalSize);
        }

        public async Task UpdateStorageLimit(string userId, double newLimit)
        {
            await Clients.User(userId).SendAsync("ReceiveStorageLimitUpdate", newLimit);
        }

        public async Task UpdateSystemStorageInfo(double totalSize)
        {
            await Clients.Group("Admins").SendAsync("ReceiveSystemStorageUpdate", totalSize);
        }

        public override async Task OnConnectedAsync()
        {
            if (Context.User.IsInRole("Admin"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }
            await base.OnConnectedAsync();
        }
    }
} 