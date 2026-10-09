using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

// Result endpoints. The Organiser records finish times and positions after the event.
// Participants can only view their own results.
[ApiController]
[Route("api/results")]
[Tags("Results")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // POST /api/results
    [HttpPost]
    [SessionAuthorize(UserRoles.Organiser)]
    [SwaggerOperation(
        Summary = "Record a result",
        Description = "Organiser only, and only for enrolments in their own events. FinishTime is written like 03:45:22.")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultDto>> CreateResult(CreateResultRequest request)
    {
        var enrolment = await _db.Enrolments.Include(e => e.Event).FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);
        if (enrolment == null)
        {
            return NotFound(new { message = "Enrolment does not exist." });
        }

        // Check the Organiser owns the event before saving the result
        if (enrolment.Event.OrganiserId != HttpContext.Session.GetUserId())
        {
            return NotOwner();
        }

        if (request.FinishTime <= TimeSpan.Zero)
        {
            return BadRequest(new { message = "FinishTime must be greater than zero." });
        }

        // One enrolment can only have one result
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == request.EnrolmentId))
        {
            return Conflict(new { message = "A result was already recorded for this enrolment." });
        }

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            FinishTime = request.FinishTime,
            FinishingPosition = request.FinishingPosition
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var dto = await ToDtos(_db.Results.Where(r => r.ResultId == result.ResultId)).FirstAsync();
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    // GET /api/results/mine
    [HttpGet("mine")]
    [SessionAuthorize(UserRoles.Participant)]
    [SwaggerOperation(
        Summary = "View my results",
        Description = "Participant only. Returns the logged-in Participant's race history.")]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<ResultDto>>> GetMyResults()
    {
        int userId = HttpContext.Session.GetUserId();
        var results = await ToDtos(_db.Results.Where(r => r.Enrolment.ParticipantId == userId))
            .OrderByDescending(r => r.EventDate)
            .ToListAsync();
        return Ok(results);
    }

    // GET /api/results/event/1
    [HttpGet("event/{eventId:int}")]
    [SessionAuthorize(UserRoles.Organiser)]
    [SwaggerOperation(
        Summary = "View the results of one of my events",
        Description = "Organiser only, and only for events they own. Sorted by finishing position.")]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ResultDto>>> GetEventResults(int eventId)
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

        var results = await ToDtos(_db.Results.Where(r => r.Enrolment.EventId == eventId))
            .OrderBy(r => r.FinishingPosition)
            .ToListAsync();
        return Ok(results);
    }

    // Turns a set of Results into ResultDtos with the event, category and participant names
    private static IQueryable<ResultDto> ToDtos(IQueryable<Result> results)
    {
        return results.Select(r => new ResultDto
        {
            ResultId = r.ResultId,
            EnrolmentId = r.EnrolmentId,
            EventName = r.Enrolment.Event.EventName,
            EventDate = r.Enrolment.Event.EventDate,
            CategoryName = r.Enrolment.Category.CategoryName,
            ParticipantName = r.Enrolment.Participant.FirstName + " " + r.Enrolment.Participant.LastName,
            FinishTime = r.FinishTime,
            FinishingPosition = r.FinishingPosition
        });
    }

    private ObjectResult NotOwner()
    {
        return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only record results for your own events." });
    }
}