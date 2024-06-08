using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace SignalRChat.Hubs
{
    public class GameHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> Users = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentQueue<string> PlayerQueue = new ConcurrentQueue<string>(new[] { "Player 1", "Player 2" });

        public override async Task OnConnectedAsync()
        {
            if (Users.Count >= 2)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Game room is full.");
                Context.Abort();
            }
            else
            {
                if (PlayerQueue.TryDequeue(out var player))
                {
                    Users[Context.ConnectionId] = player;
                    await Clients.Caller.SendAsync("ReceiveMessage", "System", $"Welcome {player}!");
                }
                await base.OnConnectedAsync();
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (Users.TryRemove(Context.ConnectionId, out var player))
            {
                PlayerQueue.Enqueue(player);
                await Clients.All.SendAsync("ReceiveMessage", "System", $"{player} has left the game.");
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task SendMove(string user, int x, int y)
        {
            await Clients.All.SendAsync("ReceiveMove", user, x, y);
        }
    }
}
