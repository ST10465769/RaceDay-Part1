using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Filters;
using RaceDay.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

// The Azure container is private, so images are read through this endpoint.
// Any logged-in user can view images (event banners and profile pictures).
[ApiController]
[Route("api/images")]
[Tags("Images")]
[SessionAuthorize]
public class ImagesController : ControllerBase
{
    private readonly BlobStorageService _blobStorage;

    public ImagesController(BlobStorageService blobStorage)
    {
        _blobStorage = blobStorage;
    }

    // GET /api/images/event-banners/abc.jpg
    [HttpGet("{*blobName}")]
    [SwaggerOperation(
        Summary = "Get an uploaded image",
        Description = "Streams an event banner or profile picture from the private Azure container.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(string blobName)
    {
        // Stops people asking for any other blob in the container
        if (!BlobStorageService.IsAllowedBlobName(blobName))
        {
            return BadRequest(new { message = "That image path is not allowed." });
        }

        try
        {
            var image = await _blobStorage.DownloadAsync(blobName);
            if (image == null)
            {
                return NotFound(new { message = "Image not found." });
            }

            Response.Headers.CacheControl = "private, max-age=3600";
            return File(image.Value.Content, image.Value.ContentType);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "The image could not be loaded." });
        }
    }
}