using Microsoft.AspNetCore.Mvc;
using BattleshipGame.Models;
using BattleshipGame.Data;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SignalRChat.Hubs;
using System.Threading.Tasks;

namespace BattleshipGame.Controllers
{
    [Authorize]
    public class GameController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHubContext<GameHub> _hubContext;

        public GameController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IHubContext<GameHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Game()
        {
            var userId = _userManager.GetUserId(User);
            var gameState = _context.GameStates.FirstOrDefault(g => g.PlayerId == userId);

            GameBoard playerBoard;
            GameBoard trackingBoard;
            if (gameState == null)
            {
                var allGameStates = _context.GameStates.ToList();
                if (allGameStates.Count >= 2)
                {
                    return BadRequest("The game is full. Only two players are allowed.");
                }

                playerBoard = new GameBoard();
                trackingBoard = new GameBoard();

                ShipGenerator.GenerateShips(playerBoard);

                gameState = new GameState
                {
                    PlayerId = userId,
                    PlayerBoard = JsonConvert.SerializeObject(playerBoard.Cells),
                    TrackingBoard = JsonConvert.SerializeObject(trackingBoard.Cells),
                };

                if (allGameStates.Count == 0)
                {
                    gameState.CurrentTurnPlayerId = userId; // First player to join gets the first turn
                }

                _context.GameStates.Add(gameState);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("PlayerJoined", userId, allGameStates.Count + 1);
            }
            else
            {
                playerBoard = new GameBoard
                {
                    Cells = JsonConvert.DeserializeObject<CellState[,]>(gameState.PlayerBoard)
                };
                trackingBoard = new GameBoard
                {
                    Cells = JsonConvert.DeserializeObject<CellState[,]>(gameState.TrackingBoard)
                };
            }

            var viewModel = new GameBoardViewModel(playerBoard, trackingBoard);
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> MakeMove(int x, int y)
        {
            var userId = _userManager.GetUserId(User);
            var gameState = _context.GameStates.FirstOrDefault(g => g.PlayerId == userId);

            if (gameState == null)
            {
                return BadRequest("Game state not found.");
            }

            var playerBoard = new GameBoard
            {
                Cells = JsonConvert.DeserializeObject<CellState[,]>(gameState.PlayerBoard)
            };
            var trackingBoard = new GameBoard
            {
                Cells = JsonConvert.DeserializeObject<CellState[,]>(gameState.TrackingBoard)
            };

            if (gameState.CurrentTurnPlayerId != userId)
            {
                return BadRequest("It's not your turn.");
            }

            var opponentGameState = _context.GameStates.FirstOrDefault(g => g.PlayerId != userId);
            if (opponentGameState == null)
            {
                return BadRequest("Opponent not found.");
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

            trackingBoard.Cells[x, y] = hit ? CellState.Hit : CellState.Miss;
            gameState.TrackingBoard = JsonConvert.SerializeObject(trackingBoard.Cells);

            gameState.CurrentTurnPlayerId = opponentGameState.PlayerId;

            _context.SaveChanges();

            await _hubContext.Clients.All.SendAsync("ReceiveMove", userId, x, y, hit);
            await _hubContext.Clients.User(opponentGameState.PlayerId).SendAsync("OpponentMove", x, y, hit);

            return Json(new { hit });
        }
    }
}
