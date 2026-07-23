using Foveo.API.Common;
using Foveo.Application.Contracts;
using Foveo.Domain.Aggregates;
using Microsoft.Net.Http.Headers;

namespace Foveo.API.Endpoints;

/// <summary>
/// Serves media bytes through the API from the internal store. Thumbnails and photos stream whole;
/// video honours Range requests so playback can seek. MinIO is never exposed to the browser.
/// </summary>
internal sealed class MediaContentEndpoints : IEndpoint
{
    public void MapRoutes(IEndpointRouteBuilder rtb)
    {
        var grp = rtb.MapGroup("/media/{id:guid}").WithTags("Media");

        grp.MapGet("/thumb", (Guid id, HttpContext ctx, IMediaRepository repo, IMediaStorage storage) =>
            ServeAsync(id, m => m.ThumbnailKey, ctx, repo, storage));

        grp.MapGet("/display", (Guid id, HttpContext ctx, IMediaRepository repo, IMediaStorage storage) =>
            ServeAsync(id, m => m.DisplayKey, ctx, repo, storage));
    }

    private static async Task ServeAsync(
        Guid id,
        Func<Media, string?> keySelector,
        HttpContext ctx,
        IMediaRepository repo,
        IMediaStorage storage)
    {
        var ct = ctx.RequestAborted;
        var media = await repo.GetByIdAsync(id, ct);
        var key = media is { Status: MediaStatus.Ready } ? keySelector(media) : null;
        if (key is null)
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var (rangeStart, rangeEnd) = ParseRange(ctx.Request.Headers.Range.ToString());
        var result = await storage.OpenReadAsync(key, rangeStart, rangeEnd, ct);

        var response = ctx.Response;
        response.Headers.AcceptRanges = "bytes";
        response.Headers.CacheControl = "public, max-age=31536000, immutable";
        response.ContentType = result.ContentType;
        response.ContentLength = result.ContentLength;

        if (result.IsPartial)
        {
            response.StatusCode = StatusCodes.Status206PartialContent;
            response.Headers[HeaderNames.ContentRange] =
                $"bytes {result.RangeStart}-{result.RangeEnd}/{result.TotalLength}";
        }

        await using var content = result.Content;
        await content.CopyToAsync(response.Body, ct);
    }

    /// <summary>Parses a single "bytes=start-end" range; either bound may be omitted. Ignores multi-range.</summary>
    private static (long? Start, long? End) ParseRange(string? header)
    {
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
            return (null, null);

        var spec = header["bytes=".Length..];
        if (spec.Contains(','))
            return (null, null);

        var dash = spec.IndexOf('-');
        if (dash < 0)
            return (null, null);

        long? start = long.TryParse(spec[..dash], out var s) ? s : null;
        long? end = long.TryParse(spec[(dash + 1)..], out var e) ? e : null;
        return (start, end);
    }
}
