using Foveo.Application.Models;
using Foveo.Application.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Foveo.API.Pages;

public sealed class IndexModel(GalleryService gallery) : PageModel
{
    public GalleryPage Gallery { get; private set; } = default!;

    public async Task OnGetAsync(int page = 1)
        => Gallery = await gallery.GetPageAsync(page, GalleryService.DefaultPageSize, HttpContext.RequestAborted);
}
