using Foveo.Application.Contracts;
using Foveo.Application.Models;

namespace Foveo.Application.Services;

/// <summary>
/// Builds gallery pages: reads the ready items for a page and signs fresh GET URLs so the
/// browser fetches thumbnails and full copies straight from the store.
/// </summary>
public sealed class GalleryService(IMediaRepository repository, IMediaStorage storage)
{
    public const int DefaultPageSize = 30;
    public const int MaxPageSize = 100;

    public async Task<GalleryPage> GetPageAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var total = await repository.CountReadyAsync(ct);
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

        var media = await repository.GetReadyPageAsync(page, pageSize, ct);

        var items = new List<GalleryItem>(media.Count);
        foreach (var m in media)
        {
            // ThumbnailKey/DisplayKey are guaranteed non-null for Ready items (invariant of MarkReady).
            var thumbUrl = await storage.CreateDownloadUrlAsync(m.ThumbnailKey!, ct);
            var displayUrl = await storage.CreateDownloadUrlAsync(m.DisplayKey!, ct);
            items.Add(new GalleryItem(m.Id, m.Type, thumbUrl.ToString(), displayUrl.ToString(), m.UploaderName, m.Created));
        }

        return new GalleryPage(items, page, pageSize, total, totalPages);
    }
}
