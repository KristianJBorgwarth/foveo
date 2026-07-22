using Foveo.Domain.Abstractions;
using Foveo.Domain.Common;

namespace Foveo.Domain.Aggregates;

/// <summary>
/// A single uploaded photo or video and its derived artifacts (thumbnail, web-friendly display copy).
/// The raw bytes live in object storage; this aggregate owns only the keys and lifecycle.
/// </summary>
public sealed class Media : Entity
{
    public const long MaxPhotoBytes = 50L * 1024 * 1024;      // 50 MB
    public const long MaxVideoBytes = 2L * 1024 * 1024 * 1024; // 2 GB

    /// <summary>Content types we accept, mapped to their kind and the canonical extension used for storage keys.</summary>
    private static readonly IReadOnlyDictionary<string, (MediaType Type, string Extension)> Supported =
        new Dictionary<string, (MediaType, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = (MediaType.Photo, ".jpg"),
            ["image/png"]  = (MediaType.Photo, ".png"),
            ["image/webp"] = (MediaType.Photo, ".webp"),
            ["image/gif"]  = (MediaType.Photo, ".gif"),
            ["image/heic"] = (MediaType.Photo, ".heic"),
            ["image/heif"] = (MediaType.Photo, ".heif"),
            ["video/mp4"]  = (MediaType.Video, ".mp4"),
            ["video/quicktime"] = (MediaType.Video, ".mov"),
            ["video/webm"] = (MediaType.Video, ".webm")
        };

    public string OriginalFileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public MediaType Type { get; private set; }
    public MediaStatus Status { get; private set; }

    /// <summary>Key of the original, untouched upload in the object store.</summary>
    public string StorageKey { get; private set; } = null!;

    /// <summary>Key of the generated thumbnail. Null until processing completes.</summary>
    public string? ThumbnailKey { get; private set; }

    /// <summary>
    /// Key of the browser-friendly derivative served on tap — a transcoded JPEG/WebP for HEIC,
    /// or a web-MP4 for video. For already-web-friendly photos this equals <see cref="StorageKey"/>.
    /// Null until processing completes.
    /// </summary>
    public string? DisplayKey { get; private set; }

    public long SizeBytes { get; private set; }

    /// <summary>Optional name the guest typed when uploading.</summary>
    public string? UploaderName { get; private set; }

    private Media() { } // EF

    private Media(Guid id) : base(id) { }

    public static Result<Media> Create(string originalFileName, string contentType, long sizeBytes, string? uploaderName)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            return Result.Fail<Media>(Error.Validation("A file name is required."));

        if (string.IsNullOrWhiteSpace(contentType) || !Supported.TryGetValue(contentType.Trim(), out var kind))
            return Result.Fail<Media>(Error.Validation($"Unsupported content type '{contentType}'."));

        var max = kind.Type == MediaType.Video ? MaxVideoBytes : MaxPhotoBytes;
        if (sizeBytes <= 0 || sizeBytes > max)
            return Result.Fail<Media>(Error.Validation($"Size must be between 1 byte and {max} bytes."));

        var id = Guid.NewGuid();
        var media = new Media(id)
        {
            OriginalFileName = originalFileName.Trim(),
            ContentType = contentType.Trim().ToLowerInvariant(),
            Type = kind.Type,
            Status = MediaStatus.Pending,
            StorageKey = $"originals/{id}{kind.Extension}",
            SizeBytes = sizeBytes,
            UploaderName = string.IsNullOrWhiteSpace(uploaderName) ? null : uploaderName.Trim()
        };
        media.SetCreated();
        return media;
    }

    /// <summary>The client confirmed the bytes landed in the store.</summary>
    public Result MarkUploaded()
    {
        if (Status != MediaStatus.Pending)
            return Result.Fail(InvalidTransition(MediaStatus.Uploaded));

        Status = MediaStatus.Uploaded;
        SetLastModified();
        return Result.Ok();
    }

    /// <summary>Processing produced a thumbnail and a display copy; the item may now appear in the gallery.</summary>
    public Result MarkReady(string thumbnailKey, string displayKey)
    {
        if (Status != MediaStatus.Uploaded)
            return Result.Fail(InvalidTransition(MediaStatus.Ready));

        if (string.IsNullOrWhiteSpace(thumbnailKey) || string.IsNullOrWhiteSpace(displayKey))
            return Result.Fail(Error.Validation("Thumbnail and display keys are required to mark ready."));

        ThumbnailKey = thumbnailKey;
        DisplayKey = displayKey;
        Status = MediaStatus.Ready;
        SetLastModified();
        return Result.Ok();
    }

    /// <summary>Processing failed; the item is hidden from the gallery.</summary>
    public Result MarkFailed()
    {
        if (Status is MediaStatus.Ready or MediaStatus.Failed)
            return Result.Fail(InvalidTransition(MediaStatus.Failed));

        Status = MediaStatus.Failed;
        SetLastModified();
        return Result.Ok();
    }

    private Error InvalidTransition(MediaStatus target) =>
        Error.BadRequest("media.invalid.transition", $"Cannot move media {Id} from {Status} to {target}.");
}
