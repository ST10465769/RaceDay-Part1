using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

// Shows images from the private Azure container. MVC never talks to Azure:
// it asks my API, and the API reads the blob.
[SessionRole] // any logged-in user
public class ImagesController : AppController
{
    public ImagesController(RaceDayApiClient api) : base(api) { }

    [HttpGet("Images/{*blobName}")]
    public async Task<IActionResult> Get(string blobName)
    {
        var response = await Api.SendAsync(HttpMethod.Get, "api/images/" + blobName);
        if (!response.IsSuccessStatusCode)
        {
            return NotFound();
        }

        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        string contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

        Response.Headers.CacheControl = "private, max-age=3600";
        return File(bytes, contentType);
    }
}