using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

// Category endpoints. Both roles can view categories.
// Only the Organiser who owns the event can add, change or delete its categories.
[ApiController]
[Route("api")]
[Tags("Categories")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    // GET /api/events/1/categories
    [HttpGet("events/{eventId:int}/categories")]
    [SessionAuthorize]
    [SwaggerOperation(
        Summary = "List the categories of an event",
        Description = "Both roles can view categories, for example Under 20, Senior or 10km.")]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
        {
            return NotFound(new { message = "Event does not exist." });
        }

        var categories = await _db.Categories
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.CategoryName)
            .Select(c => ToDto(c))
            .ToListAsync();

        return Ok(categories);
    }

    // POST /api/events/1/categories
    [HttpPost("events/{eventId:int}/categories")]
    [SessionAuthorize(UserRoles.Organiser)]
    [SwaggerOperation(
        Summary = "Add a category to an event",
        Description = "Organiser only, and only for events they own.")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> CreateCategory(int eventId, CreateCategoryRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null)
        {
            return NotFound(new { message = "Event does not exist." });
        }

        if (ev.OrganiserId != HttpContext.Session.GetUserId())
        {
            return NotOwner();
        }

        var category = new Category
        {
            EventId = eventId,
            CategoryName = request.CategoryName.Trim(),
            Description = request.Description
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToDto(category));
    }

    // PUT /api/categories/3
    [HttpPut("categories/{id:int}")]
    [SessionAuthorize(UserRoles.Organiser)]
    [SwaggerOperation(
        Summary = "Update a category",
        Description = "Organiser only, and only if they own the event the category belongs to.")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, CreateCategoryRequest request)
    {
        // Include(Event) so I can check who owns the event
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
        {
            return NotFound(new { message = "Category does not exist." });
        }

        if (category.Event.OrganiserId != HttpContext.Session.GetUserId())
        {
            return NotOwner();
        }

        category.CategoryName = request.CategoryName.Trim();
        category.Description = request.Description;
        await _db.SaveChangesAsync();

        return Ok(ToDto(category));
    }

    // DELETE /api/categories/3
    [HttpDelete("categories/{id:int}")]
    [SessionAuthorize(UserRoles.Organiser)]
    [SwaggerOperation(
        Summary = "Delete a category",
        Description = "Organiser only, and only for their own events. A category that already has enrolments can't be deleted.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
        {
            return NotFound(new { message = "Category does not exist." });
        }

        if (category.Event.OrganiserId != HttpContext.Session.GetUserId())
        {
            return NotOwner();
        }

        // Participants already chose this category, so deleting it would break their enrolments
        if (await _db.Enrolments.AnyAsync(e => e.CategoryId == id))
        {
            return Conflict(new { message = "This category already has enrolments and can't be deleted." });
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static CategoryDto ToDto(Category c)
    {
        return new CategoryDto
        {
            CategoryId = c.CategoryId,
            EventId = c.EventId,
            CategoryName = c.CategoryName,
            Description = c.Description
        };
    }

    private ObjectResult NotOwner()
    {
        return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only change categories of your own events." });
    }
}