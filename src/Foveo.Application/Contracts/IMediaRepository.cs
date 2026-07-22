using Foveo.Domain.Aggregates;

namespace Foveo.Application.Contracts;

public interface IMediaRepository : IRepository<Media>
{
    /// <summary>Gallery-ready items, newest first, for the given 1-based page.</summary>
    Task<IReadOnlyList<Media>> GetReadyPageAsync(int page, int pageSize, CancellationToken ct = default);

    /// <summary>Total count of gallery-ready items, for page-count math.</summary>
    Task<int> CountReadyAsync(CancellationToken ct = default);
}
