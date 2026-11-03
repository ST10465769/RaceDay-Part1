using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Mvc.Services;

// Every call from the MVC app to my Part 2 API goes through this class.
// The MVC project never touches SQL Server or Azure directly.
public class RaceDayApiClient
{
    private readonly IHttpClientFactory _factory;
    private readonly IHttpContextAccessor _httpContextAccessor;

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
    // Without it the API answers 401 because it doesn't know who is calling.
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

    // If the API is switched off I return a friendly 503 instead of crashing the page
    private static HttpResponseMessage ApiUnavailable()
    {
        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent(
                "{\"message\":\"The RaceDay API could not be reached. Please try again later.\"}",
                System.Text.Encoding.UTF8, "application/json")
        };
    }

    // Sends a request to the API. A body is turned into JSON.
    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null)
    {
        try
        {
            var client = _factory.CreateClient("RaceDayApi");
            using var request = CreateRequest(method, url);

            if (body != null)
            {
                request.Content = JsonContent.Create(body, body.GetType());
            }

            return await client.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ApiUnavailable();
        }
    }

    // Sends an image to the API as multipart/form-data. The API uploads it to Azure.
    public async Task<HttpResponseMessage> UploadFileAsync(string url, IFormFile file)
    {
        try
        {
            var client = _factory.CreateClient("RaceDayApi");
            using var request = CreateRequest(HttpMethod.Post, url);
            using var form = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();

            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(fileContent, "file", file.FileName);
            request.Content = form;

            return await client.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ApiUnavailable();
        }
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
            // the body wasn't JSON, so I use the generic messages below
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Please log in again.",
            HttpStatusCode.Forbidden => "You are not allowed to do that.",
            HttpStatusCode.NotFound => "That item could not be found.",
            _ => "Something went wrong. Please check your details and try again."
        };
    }

    // After login the API sends its own session cookie. I keep only name=value
    // so I can send it back on later requests.
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