// AdminController.cs
using Microsoft.AspNetCore.Mvc;
using BattleshipGame.Data; // Include your data context namespace
using System.Linq;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Administrator")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Admin dashboard action
    public IActionResult Dashboard()
    {
        var users = _context.Users.ToList(); // Retrieve all users from the database
        return View(users); // Pass the list of users to the view
    }

    // Other admin actions...
}
