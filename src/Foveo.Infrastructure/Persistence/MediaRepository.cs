using Foveo.Application.Contracts;
using Foveo.Application.Models;
using Foveo.Domain.Aggregates;
using Foveo.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Foveo.Infrastructure.Persistence;

public sealed class MediaRepository(MediaDbContext db) : IMediaRepository
{
    public async Task AddAsync(Media entity, CancellationToken ct = default)
        => await db.Media.AddAsync(entity, ct);

    public Task<Media?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Media.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<Media>> GetReadyPageAsync(int page, int pageSize, CancellationToken ct = default)
        => await db.Media
            .Where(m => m.Status == MediaStatus.Ready)
            .OrderByDescending(m => m.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public Task<int> CountReadyAsync(CancellationToken ct = default)
        => db.Media.CountAsync(m => m.Status == MediaStatus.Ready, ct);

    public async Task<GalleryStats> GetStatsAsync(CancellationToken ct = default)
    {
        var ready = db.Media.Where(m => m.Status == MediaStatus.Ready);
        var photos = await ready.CountAsync(m => m.Type == MediaType.Photo, ct);
        var videos = await ready.CountAsync(m => m.Type == MediaType.Video, ct);
        var guests = await ready
            .Where(m => m.UploaderName != null)
            .Select(m => m.UploaderName)
            .Distinct()
            .CountAsync(ct);
        return new GalleryStats(photos, videos, guests);
    }
}
