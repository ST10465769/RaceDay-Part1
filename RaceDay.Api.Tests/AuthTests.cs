using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

// Tests for registration, login, logout and protected profile access
public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private static RegisterRequest NewUser(string role = "Participant") => new()
    {
        FirstName = "Test",
        LastName = "User",
        Email = $"{Guid.NewGuid():N}@test.com",
        Password = "Pass1234",
        Role = role
    };

    [Fact]
    public async Task Register_WithValidDetails_Returns201()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_ResponseDoesNotContainPassword()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser());
        string body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Pass1234", body);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithSameEmailTwice_Returns409()
    {
        var client = _factory.CreateClient();
        var user = NewUser();
        await client.PostAsJsonAsync("/api/auth/register", user);
        var second = await client.PostAsJsonAsync("/api/auth/register", user);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_Returns400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser("Admin"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_Returns200()
    {
        var client = _factory.CreateClient();
        var user = NewUser();
        await client.PostAsJsonAsync("/api/auth/register", user);

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = user.Email, Password = user.Password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var user = NewUser();
        await client.PostAsJsonAsync("/api/auth/register", user);

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = user.Email, Password = "WrongPass99" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "nobody@test.com", Password = "Pass1234" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Profile_WhenNotLoggedIn_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/users/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WhenLoggedIn_ReturnsOwnProfile()
    {
        var (client, userId, email) = await TestHelpers.RegisterAndLogin(_factory, "Participant");

        var response = await client.GetAsync("/api/users/profile");
        var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(userId, profile!.UserId);
        Assert.Equal(email, profile.Email);
        Assert.Equal("Participant", profile.Role);
    }

    [Fact]
    public async Task Profile_AfterLogout_Returns401()
    {
        var (client, _, _) = await TestHelpers.RegisterAndLogin(_factory, "Organiser");

        await client.PostAsync("/api/auth/logout", null);
        var response = await client.GetAsync("/api/users/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}