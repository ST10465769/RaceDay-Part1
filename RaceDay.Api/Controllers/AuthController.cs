using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;          // This connects to your RaceDayDbContext
using RaceDay.Api.Models;        // This connects to your AppUser class!
using RaceDay.Api.Services;      // This connects to your IPasswordHasher
using RaceDay.Contracts.Auth;    // This connects to your DTOs (RegisterRequest, UserResponse)

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // Dependency Injection: We need the database and the password hasher.
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public AuthController(RaceDayDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request)
    {
        // Step 1: Check if the email is already in the database.
        // We use _db.Users because that's what you named the DbSet in RaceDayDbContext.cs
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
        {
            return Conflict("Email already registered.");
        }

        // Step 2: Create a new AppUser entity.
        // IMPORTANT: Changed "new User" to "new AppUser" to match your Models folder.
        var user = new AppUser
        {
            Email = request.Email,

            // Hashing the password before saving
            PasswordHash = _passwordHasher.Hash(request.Password),

            FirstName = request.FirstName,
            LastName = request.LastName,

            // Default role for security
            Role = "Participant"
        };

        // Step 3: Save the new user to the database.
        // We add it to _db.Users (the DbSet) and then save.
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Step 4: Map the AppUser entity to a UserResponse DTO.
        var response = new UserResponse
        {
            Id = user.UserId,  // <--- Fixed! Matches your AppUser.cs property
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
