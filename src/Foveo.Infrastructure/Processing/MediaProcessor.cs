using Foveo.Application.Contracts;
using Foveo.Application.Models;
using Foveo.Domain.Aggregates;
using Foveo.Domain.Common;
using ImageMagick;
using Microsoft.Extensions.Logging;

namespace Foveo.Infrastructure.Processing;

/// <summary>
/// Generates a thumbnail and a browser-friendly display copy for an uploaded item.
/// Magick.NET handles all still images (including native HEIC decode); ffmpeg handles video.
/// Standard photos reuse the original as their display copy; HEIC is transcoded to JPEG.
/// </summary>
public sealed class MediaProcessor(IMediaStorage storage, ILogger<MediaProcessor> logger) : IMediaProcessor
{
    private const uint ThumbnailMax = 500;
    private const uint DisplayMax = 2560;
    private const int VideoDisplayWidth = 1280;

    public async Task<Result<MediaDerivatives>> ProcessAsync(Media media, CancellationToken ct = default)
    {
        var workDir = Path.Combine(Path.GetTempPath(), $"foveo-{media.Id:N}");
        Directory.CreateDirectory(workDir);
        try
        {
            var originalPath = Path.Combine(workDir, $"original{Path.GetExtension(media.StorageKey)}");
            await DownloadAsync(media.StorageKey, originalPath, ct);

            var thumbnailKey = $"thumbnails/{media.Id:N}.jpg";
            var thumbPath = Path.Combine(workDir, "thumb.jpg");

            var displayKey = media switch
            {
                { Type: MediaType.Video } => await ProcessVideoAsync(media, originalPath, workDir, thumbPath, ct),
                { Type: MediaType.Photo } when IsHeic(media.ContentType)
                    => await ProcessHeicAsync(media, originalPath, workDir, thumbPath, ct),
                _ => await ProcessStandardPhotoAsync(media, originalPath, thumbPath, ct)
            };

            await UploadAsync(thumbnailKey, thumbPath, "image/jpeg", ct);
            return Result.Ok(new MediaDerivatives(thumbnailKey, displayKey));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process media {MediaId}", media.Id);
            return Result.Fail<MediaDerivatives>(Error.Failure($"Processing failed for {media.Id}."));
        }
        finally
        {
            TryCleanup(workDir);
        }
    }

    private async Task<string> ProcessStandardPhotoAsync(Media media, string originalPath, string thumbPath, CancellationToken ct)
    {
        // jpeg/png/webp/gif are already browser-friendly: reuse the original as the display copy.
        await WriteResizedJpegAsync(originalPath, thumbPath, ThumbnailMax, quality: 80, ct);
        return media.StorageKey;
    }

    private async Task<string> ProcessHeicAsync(Media media, string originalPath, string workDir, string thumbPath, CancellationToken ct)
    {
        // Browsers can't render HEIC, so produce a JPEG display copy alongside the thumbnail.
        var displayKey = $"display/{media.Id:N}.jpg";
        var displayPath = Path.Combine(workDir, "display.jpg");
        await WriteResizedJpegAsync(originalPath, displayPath, DisplayMax, quality: 88, ct);
        await UploadAsync(displayKey, displayPath, "image/jpeg", ct);

        await WriteResizedJpegAsync(originalPath, thumbPath, ThumbnailMax, quality: 80, ct);
        return displayKey;
    }

    private async Task<string> ProcessVideoAsync(Media media, string originalPath, string workDir, string thumbPath, CancellationToken ct)
    {
        // Poster frame ~1s in, scaled to thumbnail size.
        await Ffmpeg.RunAsync(
            $"-y -ss 00:00:01 -i \"{originalPath}\" -frames:v 1 -vf \"scale='min({ThumbnailMax},iw)':-2\" -q:v 3 \"{thumbPath}\"", ct);

        if (media.ContentType == "video/mp4")
            return media.StorageKey; // already web-friendly, no transcode

        var displayKey = $"display/{media.Id:N}.mp4";
        var displayPath = Path.Combine(workDir, "display.mp4");
        await Ffmpeg.RunAsync(
            $"-y -i \"{originalPath}\" -vf \"scale='min({VideoDisplayWidth},iw)':-2\" " +
            $"-c:v libx264 -preset veryfast -crf 23 -c:a aac -movflags +faststart \"{displayPath}\"", ct);
        await UploadAsync(displayKey, displayPath, "video/mp4", ct);
        return displayKey;
    }

    private static async Task WriteResizedJpegAsync(string sourcePath, string destPath, uint maxDimension, uint quality, CancellationToken ct)
    {
        using var image = new MagickImage(sourcePath);
        image.AutoOrient();                                   // honour EXIF rotation from phones
        image.Resize(new MagickGeometry(maxDimension, maxDimension) { Greater = true }); // shrink-only, keep aspect
        image.Format = MagickFormat.Jpeg;
        image.Quality = quality;
        image.Strip();                                        // drop metadata
        await image.WriteAsync(destPath, ct);
    }

    private static bool IsHeic(string contentType)
        => contentType is "image/heic" or "image/heif";

    private async Task DownloadAsync(string key, string destinationPath, CancellationToken ct)
    {
        await using var source = await storage.OpenReadAsync(key, ct);
        await using var destination = File.Create(destinationPath);
        await source.CopyToAsync(destination, ct);
    }

    private async Task UploadAsync(string key, string path, string contentType, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        await storage.PutAsync(key, stream, contentType, ct);
    }

    private void TryCleanup(string workDir)
    {
        try
        {
            Directory.Delete(workDir, recursive: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not clean up work dir {WorkDir}", workDir);
        }
    }
}
