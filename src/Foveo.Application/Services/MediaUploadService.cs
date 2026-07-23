using Foveo.Application.Contracts;
using Foveo.Domain.Aggregates;
using Foveo.Domain.Common;

namespace Foveo.Application.Services;

/// <summary>
/// Accepts an uploaded file: streams the bytes into the store, records the aggregate, and
/// queues it for thumbnail/derivative processing. One request, no client round-trips.
/// </summary>
public sealed class MediaUploadService(
    IMediaRepository repository,
    IMediaStorage storage,
    IMediaProcessingQueue queue)
{
    public async Task<Result<Guid>> StoreUploadAsync(
        string fileName,
        string contentType,
        long sizeBytes,
        string? uploaderName,
        Stream content,
        CancellationToken ct = default)
    {
        var created = Media.Create(fileName, contentType, sizeBytes, uploaderName);
        if (!created.Success)
            return Result.Fail<Guid>(created.Error);

        var media = created.RequiredValue;

        // Stream straight to the store first — if this fails we never persist an orphan row.
        await storage.PutAsync(media.StorageKey, content, sizeBytes, media.ContentType, ct);

        var marked = media.MarkUploaded();
        if (!marked.Success)
            return Result.Fail<Guid>(marked.Error);

        await repository.AddAsync(media, ct);
        await repository.SaveChangesAsync(ct);
        await queue.EnqueueAsync(media.Id, ct);

        return Result.Ok(media.Id);
    }
}
