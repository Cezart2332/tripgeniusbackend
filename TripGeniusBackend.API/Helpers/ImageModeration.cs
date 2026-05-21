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

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        var result = await moderation.CheckImageAsync(
            new MemoryStream(bytes),
            file.ContentType,
            cancellationToken);
        if (result.IsBlocked)
            return (null, new BadRequestObjectResult(new { message = result.Reason }));

        return (new MemoryStream(bytes), null);
    }
}
