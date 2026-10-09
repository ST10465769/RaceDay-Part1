using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

// Tests for event management and role enforcement
public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201()
    {
        var (organiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var response = await organiser.PostAsJsonAsync("/api/events", TestHelpers.NewEventRequest());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var (participant, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Participant");
        var response = await participant.PostAsJsonAsync("/api/events", TestHelpers.NewEventRequest());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NotLoggedIn_CannotCreateEvent_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/events", TestHelpers.NewEventRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewEvents_Returns200()
    {
        var (organiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        await TestHelpers.CreateEvent(organiser);
        var (participant, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Participant");

        var response = await participant.GetAsync("/api/events");
        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(events!);
    }

    [Fact]
    public async Task Create_WithInvalidEventType_Returns400()
    {
        var (organiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var request = TestHelpers.NewEventRequest();
        request.EventType = "Swim";

        var response = await organiser.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent_Returns200()
    {
        var (organiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(organiser);

        var update = new UpdateEventRequest
        {
            EventName = "Renamed Marathon",
            Description = "Updated",
            EventDate = DateTime.UtcNow.AddDays(40),
            Location = "Pretoria",
            Distance = 21,
            EventType = "Walk"
        };
        var response = await organiser.PutAsJsonAsync($"/api/events/{ev.EventId}", update);
        var updated = await response.Content.ReadFromJsonAsync<EventDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Renamed Marathon", updated!.EventName);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_Returns403()
    {
        var (owner, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var (otherOrganiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(owner);

        var update = new UpdateEventRequest
        {
            EventName = "Hijacked",
            Description = "x",
            EventDate = DateTime.UtcNow.AddDays(5),
            Location = "x",
            Distance = 5,
            EventType = "Run"
        };
        var response = await otherOrganiser.PutAsJsonAsync($"/api/events/{ev.EventId}", update);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent_Returns204()
    {
        var (organiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{ev.EventId}");
        var afterwards = await organiser.GetAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotDeleteEvent_Returns403()
    {
        var (organiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(organiser);
        var (participant, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Participant");

        var response = await participant.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}