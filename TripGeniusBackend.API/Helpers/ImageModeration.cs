using Microsoft.AspNetCore.Mvc;
using TripGeniusBackend.Application.Interfaces.Services;

namespace TripGeniusBackend.API.Helpers;

public static class ImageModeration
{
    public static async Task<(MemoryStream? Stream, IActionResult? Rejection)> ValidateUploadAsync(
        IFormFile? file,
        IContentModerationService moderation,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return (null, null);

        var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        stream.Position = 0;

        var result = await moderation.CheckImageAsync(stream, file.ContentType, cancellationToken);
        if (result.IsBlocked)
        {
            await stream.DisposeAsync();
            return (null, new BadRequestObjectResult(new { message = result.Reason }));
        }

        stream.Position = 0;
        return (stream, null);
    }
}
