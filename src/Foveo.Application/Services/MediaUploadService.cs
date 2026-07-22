using Foveo.Application.Contracts;
using Foveo.Application.Models;
using Foveo.Domain.Aggregates;
using Foveo.Domain.Common;

namespace Foveo.Application.Services;

/// <summary>
/// Drives the two-step upload: hand out presigned URLs (bytes go browser→store directly),
/// then accept the client's completion callback and enqueue processing.
/// </summary>
public sealed class MediaUploadService(
    IMediaRepository repository,
    IMediaStorage storage,
    IMediaProcessingQueue queue)
{
    public const int MaxItemsPerRequest = 50;

    public async Task<Result<IReadOnlyList<UploadTicket>>> RequestUploadsAsync(
        IReadOnlyList<UploadRequestItem> items,
        string? uploaderName,
        CancellationToken ct = default)
    {
        if (items.Count == 0)
            return Result.Fail<IReadOnlyList<UploadTicket>>(Error.Validation("No files were provided."));

        if (items.Count > MaxItemsPerRequest)
            return Result.Fail<IReadOnlyList<UploadTicket>>(
                Error.Validation($"At most {MaxItemsPerRequest} files can be uploaded at once."));

        var tickets = new List<UploadTicket>(items.Count);
        foreach (var item in items)
        {
            var created = Media.Create(item.FileName, item.ContentType, item.SizeBytes, uploaderName);
            if (!created.Success)
                return Result.Fail<IReadOnlyList<UploadTicket>>(created.Error);

            var media = created.RequiredValue;
            await repository.AddAsync(media, ct);

            var uploadUrl = await storage.CreateUploadUrlAsync(media.StorageKey, media.ContentType, ct);
            tickets.Add(new UploadTicket(media.Id, uploadUrl.ToString(), media.ContentType));
        }

        await repository.SaveChangesAsync(ct);
        return Result.Ok<IReadOnlyList<UploadTicket>>(tickets);
    }

    public async Task<Result> CompleteUploadAsync(Guid mediaId, CancellationToken ct = default)
    {
        var media = await repository.GetByIdAsync(mediaId, ct);
        if (media is null)
            return Result.Fail(Error.NotFound<Media>(mediaId.ToString()));

        var marked = media.MarkUploaded();
        if (!marked.Success)
            return marked;

        await repository.SaveChangesAsync(ct);
        await queue.EnqueueAsync(media.Id, ct);
        return Result.Ok();
    }
}
