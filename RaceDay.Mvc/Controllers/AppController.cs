using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

// Shared code for my controllers: the API client and the "session expired" handling
public abstract class AppController : Controller
{
    protected readonly RaceDayApiClient Api;

    protected AppController(RaceDayApiClient api)
    {
        Api = api;
    }

    // The API session can end (idle timeout) while the MVC session is still alive
    protected static bool IsApiSessionExpired(HttpResponseMessage response)
    {
        return response.StatusCode == HttpStatusCode.Unauthorized;
    }

    protected IActionResult SessionExpired()
    {
        HttpContext.Session.Clear();
        TempData["Error"] = "Your session expired. Please log in again.";
        return RedirectToAction("Login", "Auth");
    }
}
