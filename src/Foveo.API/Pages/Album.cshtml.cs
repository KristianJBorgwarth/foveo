using Foveo.Application.Models;
using Foveo.Application.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Foveo.API.Pages;

public sealed class AlbumModel(GalleryService gallery) : PageModel
{
    public GalleryPage Gallery { get; private set; } = default!;
    public GalleryStats Stats { get; private set; } = default!;

    public async Task OnGetAsync(int page = 1)
    {
        var ct = HttpContext.RequestAborted;
        Gallery = await gallery.GetPageAsync(page, GalleryService.DefaultPageSize, ct);
        Stats = await gallery.GetStatsAsync(ct);
    }
}
