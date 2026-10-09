using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

// Tests for capturing and viewing results
public class ResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ResultTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private static CreateResultRequest NewResult(int enrolmentId) => new()
    {
        EnrolmentId = enrolmentId,
        FinishTime = TimeSpan.FromMinutes(225),
        FinishingPosition = 127
    };

    [Fact]
    public async Task Organiser_CanRecordResult_AndParticipantSeesIt()
    {
        var (organiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(organiser);
        var category = await TestHelpers.CreateCategory(organiser, ev.EventId);
        var (participant, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Participant");
        var enrolment = await TestHelpers.Enrol(participant, ev.EventId, category.CategoryId);

        var create = await organiser.PostAsJsonAsync("/api/results", NewResult(enrolment.EnrolmentId));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var mine = await participant.GetFromJsonAsync<List<ResultDto>>("/api/results/mine");
        var result = Assert.Single(mine!);
        Assert.Equal(127, result.FinishingPosition);
        Assert.Equal(ev.EventName, result.EventName);
    }

    [Fact]
    public async Task Participant_CannotRecordResult_Returns403()
    {
        var (organiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(organiser);
        var category = await TestHelpers.CreateCategory(organiser, ev.EventId);
        var (participant, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Participant");
        var enrolment = await TestHelpers.Enrol(participant, ev.EventId, category.CategoryId);

        var response = await participant.PostAsJsonAsync("/api/results", NewResult(enrolment.EnrolmentId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotRecordResult_Returns403()
    {
        var (owner, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var (otherOrganiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(owner);
        var category = await TestHelpers.CreateCategory(owner, ev.EventId);
        var (participant, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Participant");
        var enrolment = await TestHelpers.Enrol(participant, ev.EventId, category.CategoryId);

        var response = await otherOrganiser.PostAsJsonAsync("/api/results", NewResult(enrolment.EnrolmentId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Result_ForUnknownEnrolment_Returns404()
    {
        var (organiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var response = await organiser.PostAsJsonAsync("/api/results", NewResult(99999));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task NotLoggedIn_CannotViewResults_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/results/mine");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}