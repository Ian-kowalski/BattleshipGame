using BattleshipGame.Models;
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
        private static string CurrentTurnPlayerId;

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

                    if (Users.Count == 1)
                    {
                        CurrentTurnPlayerId = Context.ConnectionId; // First player to join gets the first turn
                    }

                    await Clients.All.SendAsync("UpdatePlayers", Users.Values);
                    await Clients.All.SendAsync("UpdateCurrentTurn", CurrentTurnPlayerId);
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
                await Clients.All.SendAsync("UpdatePlayers", Users.Values);
            }

            if (Context.ConnectionId == CurrentTurnPlayerId)
            {
                CurrentTurnPlayerId = Users.Keys.FirstOrDefault(); // Assign turn to another player if the current turn player disconnects
                await Clients.All.SendAsync("UpdateCurrentTurn", CurrentTurnPlayerId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendGameState(string userId, GameState gameState)
        {
            Console.WriteLine($"Sending game state to user {userId}");
            await Clients.User(userId).SendAsync("ReceiveGameState", gameState);
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task SendMove(string user, int x, int y)
        {
            if (Context.ConnectionId != CurrentTurnPlayerId)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "It's not your turn.");
                return;
            }

            await Clients.All.SendAsync("ReceiveMove", user, x, y);
            CurrentTurnPlayerId = Users.Keys.FirstOrDefault(id => id != Context.ConnectionId);
            await Clients.All.SendAsync("UpdateCurrentTurn", CurrentTurnPlayerId);
        }
    }
}
