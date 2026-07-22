using Foveo.Application.Contracts;
using Foveo.Infrastructure.Configuration;
using Foveo.Infrastructure.Persistence;
using Foveo.Infrastructure.Persistence.Context;
using Foveo.Infrastructure.Processing;
using Foveo.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Foveo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var s3 = configuration.GetSection(S3Options.SectionName).Get<S3Options>()
                 ?? throw new InvalidOperationException($"Missing '{S3Options.SectionName}' configuration section.");
        services.AddSingleton(s3);
        services.AddSingleton<IMediaStorage, S3MediaStorage>();

        services.AddSingleton<IMediaProcessingQueue, ChannelMediaProcessingQueue>();
        services.AddScoped<IMediaProcessor, MediaProcessor>();
        services.AddHostedService<MediaProcessingWorker>();

        var connectionString = configuration.GetConnectionString("Media") ?? "Data Source=foveo.db";
        services.AddDbContext<MediaDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IMediaRepository, MediaRepository>();

        return services;
    }
}
