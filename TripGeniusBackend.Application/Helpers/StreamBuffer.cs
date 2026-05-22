namespace TripGeniusBackend.Application.Helpers;

public static class StreamBuffer
{
    public static async Task<byte[]?> ReadAllBytesAsync(Stream? stream, CancellationToken cancellationToken = default)
    {
        if (stream is null)
            return null;

        if (stream is MemoryStream memory)
        {
            if (memory.CanSeek)
                memory.Position = 0;
            return memory.ToArray();
        }

        if (stream.CanSeek)
            stream.Position = 0;

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        if (stream.CanSeek)
            stream.Position = 0;

        return bytes;
    }
}
