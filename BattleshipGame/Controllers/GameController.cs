using Microsoft.AspNetCore.Mvc;

namespace BattleshipGame.Controllers
{
    public class GameController : Controller
    {
        public IActionResult Game()
        {
            return View();
        }
    }
}
