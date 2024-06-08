using BattleshipGame.Data;
using BattleshipGame.Models;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace BattleshipGame.Hubs
{
    public class GameHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> Users = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentQueue<string> PlayerQueue = new ConcurrentQueue<string>(new[] { "Player 1", "Player 2" });
        private static string CurrentTurnPlayerId;
        private readonly ApplicationDbContext _context;

        public GameHub(ApplicationDbContext context)
        {
            _context = context;
        }

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

        public async Task MakeMove(int x, int y)
        {
            if (Context.ConnectionId != CurrentTurnPlayerId)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "It's not your turn.");
                return;
            }

            var gameState = _context.GameStates.FirstOrDefault(g => g.PlayerId == Context.UserIdentifier);
            if (gameState == null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Game state not found.");
                return;
            }

            var opponentGameState = _context.GameStates.FirstOrDefault(g => g.PlayerId != Context.UserIdentifier);
            if (opponentGameState == null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Opponent not found.");
                return;
            }

            var opponentBoard = new GameBoard
            {
                Cells = JsonConvert.DeserializeObject<CellState[,]>(opponentGameState.PlayerBoard)
            };

            var cellState = opponentBoard.Cells[x, y];
            bool hit = false;
            if (cellState == CellState.Ship)
            {
                opponentBoard.Cells[x, y] = CellState.Hit;
                hit = true;
            }
            else if (cellState == CellState.Empty)
            {
                opponentBoard.Cells[x, y] = CellState.Miss;
            }

            opponentGameState.PlayerBoard = JsonConvert.SerializeObject(opponentBoard.Cells);
            gameState.CurrentTurnPlayerId = opponentGameState.PlayerId;

            _context.SaveChanges();

            CurrentTurnPlayerId = opponentGameState.PlayerId; // Update turn in SignalR

            await Clients.All.SendAsync("ReceiveMove", Context.ConnectionId, x, y, hit);
            await Clients.Client(opponentGameState.PlayerId).SendAsync("OpponentMove", x, y, hit);
            await Clients.All.SendAsync("UpdateCurrentTurn", CurrentTurnPlayerId);
        }
    }
}
