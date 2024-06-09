using BattleshipGame.Data;
using BattleshipGame.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<GameHub> _logger;

        public GameHub(ApplicationDbContext context, ILogger<GameHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);

            if (Users.Count >= 2)
            {
                _logger.LogWarning("Connection attempt rejected, game room is full: {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Game room is full.");
                Context.Abort();
            }
            else
            {
                await base.OnConnectedAsync();
                _logger.LogInformation("Client successfully connected: {ConnectionId}", Context.ConnectionId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);

            if (Users.TryRemove(Context.ConnectionId, out var player))
            {
                PlayerQueue.Enqueue(player);
                await Clients.All.SendAsync("ReceiveMessage", "System", $"{player} has left the game.");
                await Clients.All.SendAsync("UpdatePlayers", Users.Values);

                _logger.LogInformation("Player removed: {Player}", player);
            }

            if (Context.ConnectionId == CurrentTurnPlayerId)
            {
                CurrentTurnPlayerId = Users.Keys.FirstOrDefault(); // Assign turn to another player if the current turn player disconnects
                await Clients.All.SendAsync("UpdateCurrentTurn", CurrentTurnPlayerId);
                _logger.LogInformation("Turn reassigned to: {CurrentTurnPlayerId}", CurrentTurnPlayerId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task RegisterPlayer(string userName)
        {
            _logger.LogInformation("Registering player: {UserName}, ConnectionId: {ConnectionId}", userName, Context.ConnectionId);

            if (Users.Count >= 2)
            {
                _logger.LogWarning("Registration failed, game room is full: {UserName}, ConnectionId: {ConnectionId}", userName, Context.ConnectionId);
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Game room is full.");
                Context.Abort();
                return;
            }

            if (PlayerQueue.TryDequeue(out var player))
            {
                Users[Context.ConnectionId] = userName;
                await Clients.All.SendAsync("ReceiveMessage", "System", $"Welcome {userName}!");
                await Clients.All.SendAsync("UpdatePlayers", Users.Values);
                await Clients.All.SendAsync("PlayerJoined", Context.ConnectionId, Users.Count);

                _logger.LogInformation("Player registered: {UserName}, PlayerType: {Player}", userName, player);

                if (Users.Count == 1)
                {
                    CurrentTurnPlayerId = Context.ConnectionId; // First player to join gets the first turn
                    _logger.LogInformation("First player to join, assigned turn: {UserName}", userName);
                }
            }
        }

        public async Task StartGame()
        {
            var userId = Context.UserIdentifier;
            _logger.LogInformation("Starting game for user: {UserId}", userId);

            var gameState = _context.GameStates.FirstOrDefault(g => g.PlayerId == userId);

            if (gameState == null)
            {
                _logger.LogError("Game state not found for user: {UserId}", userId);
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Game state not found.");
                return;
            }

            var opponentGameState = _context.GameStates.FirstOrDefault(g => g.PlayerId != userId);
            if (opponentGameState == null)
            {
                _logger.LogError("Opponent not found for user: {UserId}", userId);
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Opponent not found.");
                return;
            }

            // Determine starting player using coin flip
            var random = new Random();
            var startingPlayerId = random.Next(2) == 0 ? userId : opponentGameState.PlayerId;

            gameState.CurrentTurnPlayerId = startingPlayerId;
            opponentGameState.CurrentTurnPlayerId = startingPlayerId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Game started, starting player: {StartingPlayerId}", startingPlayerId);
            await Clients.All.SendAsync("UpdateCurrentTurn", startingPlayerId);
            await Clients.All.SendAsync("ReceiveMessage", "System", "The game has started!");
        }

        public async Task MakeMove(string user, int x, int y)
        {
            _logger.LogInformation("Move made by user: {user} at ({X}, {Y})", user, x, y);

            var gameState = await _context.GameStates.FirstOrDefaultAsync(g => g.PlayerId == user);
            if (gameState == null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Game state not found.");
                return;
            }

            if (gameState.CurrentTurnPlayerId != user)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "It's not your turn.");
                return;
            }

            var opponentGameState = await _context.GameStates.FirstOrDefaultAsync(g => g.PlayerId != user);
            if (opponentGameState == null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Opponent not found.");
                return;
            }

            var opponentBoard = JsonConvert.DeserializeObject<CellState[,]>(opponentGameState.PlayerBoard);
            var cellState = opponentBoard[x, y];
            bool hit = false;

            if (cellState == CellState.Ship)
            {
                opponentBoard[x, y] = CellState.Hit;
                hit = true;
            }
            else if (cellState == CellState.Empty)
            {
                opponentBoard[x, y] = CellState.Miss;
            }

            opponentGameState.PlayerBoard = JsonConvert.SerializeObject(opponentBoard);
            gameState.CurrentTurnPlayerId = opponentGameState.PlayerId;
            await _context.SaveChangesAsync();

            CurrentTurnPlayerId = opponentGameState.PlayerId;

            await Clients.All.SendAsync("ReceiveMove", user, x, y, hit);
            await Clients.Client(opponentGameState.PlayerId).SendAsync("OpponentMove", x, y, hit);
            await Clients.All.SendAsync("UpdateCurrentTurn", CurrentTurnPlayerId);
        }

        public async Task SendMessage(string userName, string message)
        {
            _logger.LogInformation("Message sent by user: {UserName} - {Message}", userName, message);

            await Clients.All.SendAsync("ReceiveMessage", userName, message);
        }

        public async Task SendMove(string userName, int x, int y)
        {
            _logger.LogInformation("Move sent by user: {UserName} - ({X}, {Y})", userName, x, y);
            await MakeMove(userName, x, y);
        }
    }
}
