using Microsoft.AspNetCore.Mvc;
using BattleshipGame.Models;

namespace BattleshipGame.Controllers
{
    public class GameController : Controller
    {
        public IActionResult Game()
        {
            GameBoard gameBoard = new GameBoard();
            ShipGenerator.GenerateShips(gameBoard);
            GameBoardViewModel viewModel = new GameBoardViewModel(gameBoard);
            return View(viewModel);
        }
    }
}
