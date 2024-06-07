using Microsoft.AspNetCore.SignalR;

namespace SignalRChat.Hubs
{
    public class GameHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task SendMove(string user, int x, int y)
        {
            await Clients.Others.SendAsync("ReceiveMove", user, x, y);
        }
    }
}