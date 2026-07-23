using System.Reflection;
using Foveo.API.Configuration;
using Foveo.API.Extensions;
using Foveo.Application;
using Foveo.Application.Contracts;
using Foveo.Infrastructure;
using Foveo.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;

var configuration = builder.Configuration;
configuration.AddJsonFile("appsettings.json", false, true)
    .AddEnvironmentVariables();

if (env.IsDevelopment())
{
    configuration.AddJsonFile($"appsettings.{Environments.Development}.json", true, true);
    configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), true);
}

builder.AddLogging();
builder.Services.AddObservability();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(configuration);

builder.Services.Configure<WeddingOptions>(configuration.GetSection(WeddingOptions.SectionName));

builder.Services.AddEndpoints();
builder.Services.AddRazorPages();
builder.Services.AddOpenApi();

// Guests attach big videos: raise the multipart cap for the (rare) fallback proxy path.
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.MultipartBodyLengthLimit = long.MaxValue);

var app = builder.Build();

// Single-file SQLite DB, no migrations for this deployment: ensure the schema exists on boot.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
    await db.Database.EnsureCreatedAsync();

    // Best-effort bucket provisioning: don't block the gallery if the store is briefly unavailable.
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await scope.ServiceProvider.GetRequiredService<IMediaStorage>().EnsureBucketAsync();
    }
    catch (Exception ex)
    {
        startupLogger.LogWarning(ex, "Could not ensure the storage bucket on startup; it must exist before uploads work.");
    }
}

app.UseStaticFiles();

app.RegisterMinimalEndpoints();
app.MapRazorPages();

app.MapOpenApi();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Starting application");
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application terminated unexpectedly");
}
finally
{
    logger.LogInformation("Shutting down application");
}
