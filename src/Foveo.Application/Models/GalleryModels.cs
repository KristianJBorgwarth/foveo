using Foveo.Domain.Aggregates;

namespace Foveo.Application.Models;

/// <summary>A single gallery-ready item with freshly-signed URLs for its thumbnail and full display copy.</summary>
public sealed record GalleryItem(
    Guid Id,
    MediaType Type,
    string ThumbnailUrl,
    string DisplayUrl,
    string? UploaderName,
    DateTime CreatedAt);

/// <summary>One page of the gallery, plus the paging math the UI needs to render navigation.</summary>
public sealed record GalleryPage(
    IReadOnlyList<GalleryItem> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages)
{
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
