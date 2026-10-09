using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

// Enrolment endpoints. An Enrolment links the Participant, the Event and the chosen Category.
[ApiController]
[Route("api/enrolments")]
[Tags("Enrolments")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // POST /api/enrolments
    [HttpPost]
    [SessionAuthorize(UserRoles.Participant)]
    [SwaggerOperation(
        Summary = "Enter an event",
        Description = "Participant only. Picks an event and one of its categories. The new enrolment starts as Pending.")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentDto>> Enrol(CreateEnrolmentRequest request)
    {
        int userId = HttpContext.Session.GetUserId();

        if (!await _db.Events.AnyAsync(e => e.EventId == request.EventId))
        {
            return NotFound(new { message = "Event does not exist." });
        }

        // The category must belong to the event the participant chose
        bool categoryOk = await _db.Categories.AnyAsync(c => c.CategoryId == request.CategoryId && c.EventId == request.EventId);
        if (!categoryOk)
        {
            return BadRequest(new { message = "That category does not belong to this event." });
        }

        // A participant can only enter the same event once (same rule as the unique index)
        bool alreadyEnrolled = await _db.Enrolments.AnyAsync(e => e.ParticipantId == userId && e.EventId == request.EventId);
        if (alreadyEnrolled)
        {
            return Conflict(new { message = "You are already enrolled for this event." });
        }

        var enrolment = new Enrolment
        {
            ParticipantId = userId, // taken from the session, not from the request
            EventId = request.EventId,
            CategoryId = request.CategoryId
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        var dto = await EnrolmentQuery().FirstAsync(e => e.EnrolmentId == enrolment.EnrolmentId);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    // GET /api/enrolments/mine
    [HttpGet("mine")]
    [SessionAuthorize(UserRoles.Participant)]
    [SwaggerOperation(
        Summary = "View my enrolments",
        Description = "Participant only. Returns the events the logged-in Participant has entered, with their status.")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<EnrolmentDto>>> GetMyEnrolments()
    {
        int userId = HttpContext.Session.GetUserId();
        var enrolments = await EnrolmentQuery()
            .Where(e => e.ParticipantId == userId)
            .OrderByDescending(e => e.EnrolmentDate)
            .ToListAsync();
        return Ok(enrolments);
    }

    // GET /api/enrolments/event/1
    [HttpGet("event/{eventId:int}")]
    [SessionAuthorize(UserRoles.Organiser)]
    [SwaggerOperation(
        Summary = "View the enrolments for one of my events",
        Description = "Organiser only, and only for events they own.")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EnrolmentDto>>> GetEventEnrolments(int eventId)
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

        var enrolments = await EnrolmentQuery()
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.ParticipantName)
            .ToListAsync();
        return Ok(enrolments);
    }

    // PUT /api/enrolments/1/status
    [HttpPut("{id:int}/status")]
    [SessionAuthorize(UserRoles.Organiser)]
    [SwaggerOperation(
        Summary = "Change the status of an enrolment",
        Description = "Organiser only, and only for events they own. Status must be Pending, Confirmed or Cancelled.")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrolmentDto>> UpdateStatus(int id, UpdateEnrolmentStatusRequest request)
    {
        var enrolment = await _db.Enrolments.Include(e => e.Event).FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment == null)
        {
            return NotFound(new { message = "Enrolment does not exist." });
        }

        if (enrolment.Event.OrganiserId != HttpContext.Session.GetUserId())
        {
            return NotOwner();
        }

        string? status = EnrolmentStatuses.All.FirstOrDefault(s => s.Equals(request.Status.Trim(), StringComparison.OrdinalIgnoreCase));
        if (status == null)
        {
            return BadRequest(new { message = "Status must be Pending, Confirmed or Cancelled." });
        }

        enrolment.Status = status;
        await _db.SaveChangesAsync();

        return Ok(await EnrolmentQuery().FirstAsync(e => e.EnrolmentId == id));
    }

    // Builds the EnrolmentDto from the database so I don't repeat the mapping
    private IQueryable<EnrolmentDto> EnrolmentQuery()
    {
        return _db.Enrolments.Select(e => new EnrolmentDto
        {
            EnrolmentId = e.EnrolmentId,
            EventId = e.EventId,
            EventName = e.Event.EventName,
            CategoryId = e.CategoryId,
            CategoryName = e.Category.CategoryName,
            ParticipantId = e.ParticipantId,
            ParticipantName = e.Participant.FirstName + " " + e.Participant.LastName,
            Status = e.Status,
            EnrolmentDate = e.EnrolmentDate
        });
    }

    private ObjectResult NotOwner()
    {
        return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only manage enrolments for your own events." });
    }
}