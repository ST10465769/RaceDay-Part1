using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

// Handles register, login and logout. These are the endpoints from my Part 1 plan.
[ApiController]
[Route("api/auth")]
[Tags("Auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    // The database and the hasher are injected by ASP.NET (dependency injection)
    public AuthController(RaceDayDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    [SwaggerOperation(
        Summary = "Register a new account",
        Description = "Creates an Organiser or Participant account. The password is hashed before it is saved.")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        // The role has to be exactly Organiser or Participant (ignoring capital letters)
        string role;
        if (string.Equals(request.Role.Trim(), UserRoles.Organiser, StringComparison.OrdinalIgnoreCase))
        {
            role = UserRoles.Organiser;
        }
        else if (string.Equals(request.Role.Trim(), UserRoles.Participant, StringComparison.OrdinalIgnoreCase))
        {
            role = UserRoles.Participant;
        }
        else
        {
            return BadRequest(new { message = "Role must be Organiser or Participant." });
        }

        // I lower-case the email so "A@x.com" and "a@x.com" count as the same account
        string email = request.Email.Trim().ToLower();

        // 409 Conflict if someone already registered with this email
        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict(new { message = "Email already registered." });
        }

        var user = new AppUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password), // only the hash is saved
            Role = role,
            PhoneNumber = request.PhoneNumber
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // The response has no password or hash in it
        return StatusCode(StatusCodes.Status201Created, ToResponse(user, "Registration successful."));
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Log in",
        Description = "Checks the email and password, then stores the UserId and Role in the session.")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        string email = request.Email.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // Same message for "no such email" and "wrong password",
        // so nobody can use the API to find out which emails are registered
        if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // This is the session: the server remembers who is logged in and their role
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);

        return Ok(ToResponse(user, "Login successful."));
    }

    // POST /api/auth/logout
    [HttpPost("logout")]
    [SwaggerOperation(
        Summary = "Log out",
        Description = "Clears the session so protected endpoints can't be used until the user logs in again.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }

    // Turns an AppUser into the AuthResponse DTO (never includes the password hash)
    private static AuthResponse ToResponse(AppUser user, string message)
    {
        return new AuthResponse
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            Message = message
        };
    }
}