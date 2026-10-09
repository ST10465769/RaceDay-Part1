using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

// Tests for enrolments, including who is allowed to see and manage them
public class EnrolmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EnrolmentTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var (organiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(organiser);
        var category = await TestHelpers.CreateCategory(organiser, ev.EventId);
        var (participant, participantId, _) = await TestHelpers.RegisterAndLogin(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { EventId = ev.EventId, CategoryId = category.CategoryId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Check the enrolment really links participant, event and category
        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/mine");
        var saved = Assert.Single(mine!);
        Assert.Equal(ev.EventId, saved.EventId);
        Assert.Equal(category.CategoryId, saved.CategoryId);
        Assert.Equal(participantId, saved.ParticipantId);
        Assert.Equal("Pending", saved.Status);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var (organiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(organiser);
        var category = await TestHelpers.CreateCategory(organiser, ev.EventId);

        var response = await organiser.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { EventId = ev.EventId, CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NotLoggedIn_CannotEnrol_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { EventId = 1, CategoryId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotEnrolTwiceForSameEvent_Returns409()
    {
        var (organiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(organiser);
        var category = await TestHelpers.CreateCategory(organiser, ev.EventId);
        var (participant, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Participant");

        await TestHelpers.Enrol(participant, ev.EventId, category.CategoryId);
        var second = await participant.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { EventId = ev.EventId, CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithCategoryFromAnotherEvent_Returns400()
    {
        var (organiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var eventA = await TestHelpers.CreateEvent(organiser);
        var eventB = await TestHelpers.CreateEvent(organiser);
        var categoryOfB = await TestHelpers.CreateCategory(organiser, eventB.EventId);
        var (participant, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { EventId = eventA.EventId, CategoryId = categoryOfB.CategoryId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnEvent()
    {
        var (organiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(organiser);
        var category = await TestHelpers.CreateCategory(organiser, ev.EventId);
        var (participant, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Participant");
        await TestHelpers.Enrol(participant, ev.EventId, category.CategoryId);

        var response = await organiser.GetAsync($"/api/enrolments/event/{ev.EventId}");
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(list!);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments_Returns403()
    {
        var (organiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(organiser);
        var (participant, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Participant");

        var response = await participant.GetAsync($"/api/enrolments/event/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotViewEnrolments_Returns403()
    {
        var (owner, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var (otherOrganiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(owner);

        var response = await otherOrganiser.GetAsync($"/api/enrolments/event/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanConfirmEnrolment_Returns200()
    {
        var (organiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(organiser);
        var category = await TestHelpers.CreateCategory(organiser, ev.EventId);
        var (participant, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Participant");
        var enrolment = await TestHelpers.Enrol(participant, ev.EventId, category.CategoryId);

        var response = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/status",
            new UpdateEnrolmentStatusRequest { Status = "Confirmed" });
        var updated = await response.Content.ReadFromJsonAsync<EnrolmentDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Confirmed", updated!.Status);
    }
}