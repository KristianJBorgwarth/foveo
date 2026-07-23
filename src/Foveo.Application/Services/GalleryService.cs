using Foveo.Application.Contracts;
using Foveo.Application.Models;

namespace Foveo.Application.Services;

/// <summary>
/// Builds gallery pages. Thumbnails and full media are served by the API itself, so items just
/// carry app-relative URLs — no object-store signing or exposure involved.
/// </summary>
public sealed class GalleryService(IMediaRepository repository)
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

        var items = media
            .Select(m => new GalleryItem(
                m.Id,
                m.Type,
                $"/media/{m.Id:N}/thumb",
                $"/media/{m.Id:N}/display",
                m.UploaderName,
                m.Created))
            .ToList();

        return new GalleryPage(items, page, pageSize, total, totalPages);
    }

    public Task<GalleryStats> GetStatsAsync(CancellationToken ct = default)
        => repository.GetStatsAsync(ct);
}
