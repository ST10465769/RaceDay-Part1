using System.Net;
using Xunit;

namespace RaceDay.Api.Tests;

// Tests for the image upload endpoints. They only check who is allowed in and
// that bad files are rejected, so no real upload to Azure happens.
public class UploadTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public UploadTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    // A small fake file with no image content type
    private static MultipartFormDataContent FakeFile()
    {
        return new MultipartFormDataContent
        {
            { new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "notes.txt" }
        };
    }

    [Fact]
    public async Task Banner_WhenNotLoggedIn_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/events/1/banner", FakeFile());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Banner_AsParticipant_Returns403()
    {
        var (participant, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Participant");
        var response = await participant.PostAsync("/api/events/1/banner", FakeFile());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Banner_ForAnotherOrganisersEvent_Returns403()
    {
        var (owner, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var (other, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(owner);

        var response = await other.PostAsync($"/api/events/{ev.EventId}/banner", FakeFile());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Banner_WithNonImageFile_Returns400()
    {
        var (organiser, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(organiser);

        var response = await organiser.PostAsync($"/api/events/{ev.EventId}/banner", FakeFile());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProfilePicture_WhenNotLoggedIn_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/users/profile/picture", FakeFile());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Image_WhenNotLoggedIn_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/images/event-banners/test.jpg");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Image_OutsideAllowedFolders_Returns400()
    {
        var (client, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Participant");
        var response = await client.GetAsync("/api/images/secret/file.jpg");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

}

    