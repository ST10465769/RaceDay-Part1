using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

// Register, login and logout. All the real work is done by my Part 2 API.
public class AuthController : AppController
{
    public AuthController(RaceDayApiClient api) : base(api) { }

    [HttpGet]
    public IActionResult Register() => View(new RegisterVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var request = new RaceDay.Contracts.RegisterRequest
        {
            FirstName = vm.FirstName.Trim(),
            LastName = vm.LastName.Trim(),
            Email = vm.Email.Trim(),
            Password = vm.Password,
            Role = vm.Role,
            PhoneNumber = string.IsNullOrWhiteSpace(vm.PhoneNumber) ? null : vm.PhoneNumber
        };

        var response = await Api.SendAsync(HttpMethod.Post, "api/auth/register", request);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await Api.ReadErrorAsync(response));
            return View(vm);
        }

        TempData["Success"] = "Registration successful. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login() => View(new LoginVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var response = await Api.SendAsync(HttpMethod.Post, "api/auth/login",
            new RaceDay.Contracts.LoginRequest { Email = vm.Email.Trim(), Password = vm.Password });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await Api.ReadErrorAsync(response));
            return View(vm);
        }

        var auth = await Api.ReadAsync<AuthResponse>(response);
        string? apiCookie = RaceDayApiClient.ExtractSessionCookie(response);
        if (auth == null || apiCookie == null)
        {
            ModelState.AddModelError("", "Login worked but the session could not be read from the API.");
            return View(vm);
        }

        // The MVC session remembers who the user is. The API cookie is kept too,
        // so every later API call can prove the user is logged in.
        HttpContext.Session.SetString("ApiSessionCookie", apiCookie);
        HttpContext.Session.SetInt32("UserId", auth.UserId);
        HttpContext.Session.SetString("Role", auth.Role);
        HttpContext.Session.SetString("DisplayName", auth.FirstName);

        return auth.Role == "Organiser"
            ? RedirectToAction("Index", "Organiser")
            : RedirectToAction("Index", "Participant");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await Api.SendAsync(HttpMethod.Post, "api/auth/logout");
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}