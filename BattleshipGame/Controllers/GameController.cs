using Microsoft.AspNetCore.Mvc;
using BattleshipGame.Models;
using BattleshipGame.Data;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using BattleshipGame.Hubs;
using Microsoft.EntityFrameworkCore;

namespace BattleshipGame.Controllers
{
    //[Authorize]
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
            var userName = User.Identity?.Name; // Fetch the username
            var gameState = _context.GameStates.FirstOrDefault(g => g.PlayerId == userName);

            GameBoard playerBoard;
            GameBoard trackingBoard;
            string currentTurnPlayerId;

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
                    PlayerId = userName,
                    PlayerBoard = JsonConvert.SerializeObject(playerBoard.Cells),
                    TrackingBoard = JsonConvert.SerializeObject(trackingBoard.Cells),
                    CurrentTurnPlayerId = allGameStates.Count == 0 ? userName : allGameStates[0].PlayerId // Assign turn to first player if first to join
                };

                _context.GameStates.Add(gameState);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("PlayerJoined", userName, allGameStates.Count + 1);
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

            currentTurnPlayerId = gameState.CurrentTurnPlayerId;

            var viewModel = new GameBoardViewModel(playerBoard, trackingBoard, currentTurnPlayerId);
            ViewBag.UserName = userName; // Pass the username to the view using ViewBag
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> StartGame()
        {
            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized();
            }

            var gameState = await _context.GameStates.FirstOrDefaultAsync(g => g.PlayerId == userName);
            if (gameState == null)
            {
                return BadRequest("Game state not found.");
            }

            var opponentGameState = await _context.GameStates.FirstOrDefaultAsync(g => g.PlayerId != userName);
            if (opponentGameState == null)
            {
                return BadRequest("Opponent not found.");
            }

            var random = new Random();
            var startingPlayerId = random.Next(2) == 0 ? userName : opponentGameState.PlayerId;

            gameState.CurrentTurnPlayerId = startingPlayerId;
            opponentGameState.CurrentTurnPlayerId = startingPlayerId;

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("UpdateCurrentTurn", startingPlayerId);
            return Ok();
        }



        [HttpPost]
        public async Task<IActionResult> MakeMove(int x, int y)
        {
            var userName = User.Identity?.Name; // Fetch the username
            var gameState = _context.GameStates.FirstOrDefault(g => g.PlayerId == userName);

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

            if (gameState.CurrentTurnPlayerId != userName)
            {
                return BadRequest("It's not your turn.");
            }

            var opponentGameState = _context.GameStates.FirstOrDefault(g => g.PlayerId != userName);
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

            await _hubContext.Clients.All.SendAsync("ReceiveMove", userName, x, y, hit);
            await _hubContext.Clients.User(opponentGameState.PlayerId).SendAsync("OpponentMove", x, y, hit);

            return Json(new { hit });
        }
    }
}
