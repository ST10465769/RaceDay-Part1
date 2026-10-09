using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

// Profile endpoints from my Part 1 plan. Both roles can use them, but only
// for their own account: the UserId comes from the session, never from the URL.
[ApiController]
[Route("api/users")]
[Tags("Profile")]
[SessionAuthorize] // any logged-in user, no role restriction
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db)
    {
        _db = db;
    }

    // GET /api/users/profile
    [HttpGet("profile")]
    [SwaggerOperation(
        Summary = "View my profile",
        Description = "Returns the profile of the logged-in user. The password hash is never returned.")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileDto>> GetProfile()
    {
        // The filter already checked the session, so UserId is there
        int userId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(ToDto(user));
    }

    // PUT /api/users/profile
    [HttpPut("profile")]
    [SwaggerOperation(
        Summary = "Update my profile",
        Description = "Updates the first name, last name, phone number and profile picture of the logged-in user.")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileDto>> UpdateProfile(UpdateProfileRequest request)
    {
        int userId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        user.ProfilePictureUrl = request.ProfilePictureUrl;

        await _db.SaveChangesAsync();
        return Ok(ToDto(user));
    }

    // Turns an AppUser into a ProfileDto (no password hash)
    private static ProfileDto ToDto(AppUser user)
    {
        return new ProfileDto
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Role = user.Role
        };
    }
}