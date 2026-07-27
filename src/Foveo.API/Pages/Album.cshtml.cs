using Foveo.Application.Models;
using Foveo.Application.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Foveo.API.Pages;

public sealed class AlbumModel(GalleryService gallery) : PageModel
{
    public GalleryPage Gallery { get; private set; } = default!;
    public GalleryStats Stats { get; private set; } = default!;

    // NB: the route value must not be named "page" — that name is reserved by Razor Pages
    // routing (it identifies the page itself), so asp-route-page never reaches this handler.
    public async Task OnGetAsync(int p = 1)
    {
        var ct = HttpContext.RequestAborted;
        Gallery = await gallery.GetPageAsync(p, GalleryService.DefaultPageSize, ct);
        Stats = await gallery.GetStatsAsync(ct);
    }
}
