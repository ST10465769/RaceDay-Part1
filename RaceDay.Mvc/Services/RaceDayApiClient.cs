using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Mvc.Services;

// All calls from the MVC app to my Part 2 API go through this class.
// The MVC project never touches SQL Server or Azure directly.
public class RaceDayApiClient
{
    private readonly IHttpClientFactory _factory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // The API returns camelCase JSON, so I ignore capital letters when reading it
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RaceDayApiClient(IHttpClientFactory factory, IHttpContextAccessor accessor)
    {
        _factory = factory;
        _httpContextAccessor = accessor;
    }

    // Builds a request and adds the API session cookie if the user is logged in.
    // Without this cookie the API would answer 401 because it wouldn't know who is calling.
    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);

        string? apiCookie = _httpContextAccessor.HttpContext?.Session.GetString("ApiSessionCookie");
        if (!string.IsNullOrWhiteSpace(apiCookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", apiCookie);
        }

        return request;
    }

    // Sends a request to the API. body is turned into JSON when it isn't null.
    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null)
    {
        var client = _factory.CreateClient("RaceDayApi");
        using var request = CreateRequest(method, url);

        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }

    // Reads the JSON of a successful response into an object (null if the call failed)
    public async Task<T?> ReadAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        string json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    // My API sends errors like { "message": "Email already registered." }.
    // This pulls out just the message so the user never sees raw JSON.
    public async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? "Something went wrong.";
            }
        }
        catch
        {
            // the body wasn't JSON, so I fall through to the generic messages below
        }

        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "Please log in again.",
            System.Net.HttpStatusCode.Forbidden => "You are not allowed to do that.",
            System.Net.HttpStatusCode.NotFound => "That item could not be found.",
            _ => "Something went wrong. Please check your details and try again."
        };
    }

    // After login the API sends back its own session cookie.
    // I keep only the name=value part so I can send it back on later requests.
    public static string? ExtractSessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return null;
        }

        string? sessionHeader = values.FirstOrDefault(v =>
            v.StartsWith(".AspNetCore.Session=", StringComparison.OrdinalIgnoreCase));

        return sessionHeader?.Split(';', 2)[0];
    }
}