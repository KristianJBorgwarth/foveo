using Foveo.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Foveo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<MediaUploadService>();
        services.AddScoped<GalleryService>();
        return services;
    }
}
