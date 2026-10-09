using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

// Event endpoints from my Part 1 plan.
// Both roles can view events, only Organisers can create, update or delete them.
[ApiController]
[Route("api/events")]
[Tags("Events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // GET /api/events?eventType=Run&fromDate=2026-01-01
    [HttpGet]
    [SessionAuthorize]
    [SwaggerOperation(
        Summary = "List all events",
        Description = "Both roles can view events. Optional filters: eventType (Run, Walk or Cycle) and fromDate.")]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<EventDto>>> GetEvents([FromQuery] string? eventType, [FromQuery] DateTime? fromDate)
    {
        var query = EventQuery();

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            string? type = NormaliseEventType(eventType);
            if (type == null)
            {
                return BadRequest(new { message = "eventType must be Run, Walk or Cycle." });
            }
            query = query.Where(e => e.EventType == type);
        }

        if (fromDate.HasValue)
        {
            DateTime from = fromDate.Value.Date;
            query = query.Where(e => e.EventDate >= from);
        }

        return Ok(await query.OrderBy(e => e.EventDate).ToListAsync());
    }

    // GET /api/events/mine
    [HttpGet("mine")]
    [SessionAuthorize(UserRoles.Organiser)]
    [SwaggerOperation(
        Summary = "List my events",
        Description = "Organiser only. Returns the events created by the logged-in Organiser.")]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<EventDto>>> GetMyEvents()
    {
        int userId = HttpContext.Session.GetUserId();
        var events = await EventQuery()
            .Where(e => e.OrganiserId == userId)
            .OrderBy(e => e.EventDate)
            .ToListAsync();
        return Ok(events);
    }

    // GET /api/events/5
    [HttpGet("{id:int}")]
    [SessionAuthorize]
    [SwaggerOperation(
        Summary = "Get one event",
        Description = "Returns the event details, its categories and the number of enrolments.")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetEvent(int id)
    {
        var ev = await EventQuery().FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null)
        {
            return NotFound(new { message = "Event does not exist." });
        }
        return Ok(ev);
    }

    // POST /api/events
    [HttpPost]
    [SessionAuthorize(UserRoles.Organiser)]
    [SwaggerOperation(
        Summary = "Create an event",
        Description = "Organiser only. The logged-in Organiser becomes the owner of the event.")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventDto>> CreateEvent(CreateEventRequest request)
    {
        string? type = NormaliseEventType(request.EventType);
        if (type == null)
        {
            return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });
        }

        var ev = new Event
        {
            EventName = request.EventName.Trim(),
            Description = request.Description,
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            Distance = request.Distance,
            EventType = type,
            BannerImageUrl = request.BannerImageUrl,
            OrganiserId = HttpContext.Session.GetUserId() // owner comes from the session, not the request
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        var dto = await EventQuery().FirstAsync(e => e.EventId == ev.EventId);
        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, dto);
    }

    // PUT /api/events/5
    [HttpPut("{id:int}")]
    [SessionAuthorize(UserRoles.Organiser)]
    [SwaggerOperation(
        Summary = "Update an event",
        Description = "Organiser only, and only for events they own.")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> UpdateEvent(int id, UpdateEventRequest request)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null)
        {
            return NotFound(new { message = "Event does not exist." });
        }

        // An Organiser may only change their own events
        if (ev.OrganiserId != HttpContext.Session.GetUserId())
        {
            return NotOwner();
        }

        string? type = NormaliseEventType(request.EventType);
        if (type == null)
        {
            return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });
        }

        ev.EventName = request.EventName.Trim();
        ev.Description = request.Description;
        ev.EventDate = request.EventDate;
        ev.Location = request.Location.Trim();
        ev.Distance = request.Distance;
        ev.EventType = type;
        ev.BannerImageUrl = request.BannerImageUrl;

        await _db.SaveChangesAsync();
        return Ok(await EventQuery().FirstAsync(e => e.EventId == id));
    }

    // DELETE /api/events/5
    [HttpDelete("{id:int}")]
    [SessionAuthorize(UserRoles.Organiser)]
    [SwaggerOperation(
        Summary = "Delete an event",
        Description = "Organiser only, and only for events they own. Its categories and enrolments are deleted too.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null)
        {
            return NotFound(new { message = "Event does not exist." });
        }

        if (ev.OrganiserId != HttpContext.Session.GetUserId())
        {
            return NotOwner();
        }

        // My plan says deleting an event also removes its categories and enrolments.
        // I load them so EF deletes them in the right order in one save.
        var enrolments = await _db.Enrolments.Where(e => e.EventId == id).ToListAsync();
        var categories = await _db.Categories.Where(c => c.EventId == id).ToListAsync();
        _db.Enrolments.RemoveRange(enrolments);
        _db.Categories.RemoveRange(categories);
        _db.Events.Remove(ev);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Builds the EventDto straight from the database so I don't repeat the mapping
    private IQueryable<EventDto> EventQuery()
    {
        return _db.Events.Select(e => new EventDto
        {
            EventId = e.EventId,
            EventName = e.EventName,
            Description = e.Description ?? string.Empty,
            EventDate = e.EventDate,
            Location = e.Location,
            Distance = e.Distance,
            EventType = e.EventType,
            BannerImageUrl = e.BannerImageUrl,
            OrganiserId = e.OrganiserId,
            OrganiserName = e.Organiser.FirstName + " " + e.Organiser.LastName,
            EnrolmentCount = e.Enrolments.Count,
            Categories = e.Categories.Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                EventId = c.EventId,
                CategoryName = c.CategoryName,
                Description = c.Description
            }).ToList()
        });
    }

    // Returns "Run", "Walk" or "Cycle" (ignoring capital letters), or null if it's not valid
    private static string? NormaliseEventType(string value)
    {
        return EventTypes.All.FirstOrDefault(t => t.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    // 403 for a logged-in Organiser who doesn't own this event
    private ObjectResult NotOwner()
    {
        return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only change your own events." });
    }
}