using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Controllers;

public class HomeController : Controller
{
    // Logged-in users go straight to their own home page
    public IActionResult Index()
    {
        string? role = HttpContext.Session.GetString("Role");
        if (role == "Organiser") return RedirectToAction("Index", "Organiser");
        if (role == "Participant") return RedirectToAction("Index", "Participant");
        return View();
    }

    public IActionResult AccessDenied() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
