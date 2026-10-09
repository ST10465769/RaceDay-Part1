using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

// Starts my real API in memory for the tests, but swaps SQL Server for an
// in-memory database so the tests never touch my real RaceDayDb.
public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server setup that Program.cs registered
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<RaceDayDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }
}

// Small helper methods so each test stays short and easy to read
public static class TestHelpers
{
    public const string Password = "Pass1234";

    // Each client has its own cookie jar, so it keeps its own session
    public static async Task<(HttpClient Client, int UserId, string Email)> RegisterAndLogin(RaceDayFactory factory, string role)
    {
        var client = factory.CreateClient();
        string email = $"{Guid.NewGuid():N}@test.com"; // unique email so tests never clash

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Test",
            LastName = role,
            Email = email,
            Password = Password,
            Role = role
        });
        register.EnsureSuccessStatusCode();
        var auth = (await register.Content.ReadFromJsonAsync<AuthResponse>())!;

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = Password });
        login.EnsureSuccessStatusCode();

        return (client, auth.UserId, email);
    }

    public static CreateEventRequest NewEventRequest()
    {
        return new CreateEventRequest
        {
            EventName = "Test Marathon",
            Description = "Event made by a test",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Johannesburg",
            Distance = 10,
            EventType = "Run"
        };
    }

    public static async Task<EventDto> CreateEvent(HttpClient organiser)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", NewEventRequest());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    public static async Task<CategoryDto> CreateCategory(HttpClient organiser, int eventId)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new CreateCategoryRequest { CategoryName = "Senior" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    public static async Task<EnrolmentDto> Enrol(HttpClient participant, int eventId, int categoryId)
    {
        var response = await participant.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { EventId = eventId, CategoryId = categoryId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EnrolmentDto>())!;
    }
}