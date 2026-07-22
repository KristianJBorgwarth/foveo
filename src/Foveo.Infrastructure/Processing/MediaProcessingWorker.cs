using Foveo.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Foveo.Infrastructure.Processing;

/// <summary>
/// Drains the processing queue: for each uploaded item, generate derivatives and move the
/// aggregate to Ready (or Failed). Runs one item at a time — fine at wedding scale.
/// </summary>
public sealed class MediaProcessingWorker(
    IMediaProcessingQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<MediaProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var mediaId in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await ProcessOneAsync(mediaId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error processing media {MediaId}", mediaId);
            }
        }
    }

    private async Task ProcessOneAsync(Guid mediaId, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMediaRepository>();
        var processor = scope.ServiceProvider.GetRequiredService<IMediaProcessor>();

        var media = await repository.GetByIdAsync(mediaId, ct);
        if (media is null)
        {
            logger.LogWarning("Media {MediaId} not found for processing", mediaId);
            return;
        }

        var result = await processor.ProcessAsync(media, ct);
        if (result.Success)
        {
            var derivatives = result.RequiredValue;
            media.MarkReady(derivatives.ThumbnailKey, derivatives.DisplayKey);
        }
        else
        {
            media.MarkFailed();
            logger.LogError("Processing failed for media {MediaId}: {Error}", mediaId, result.Error?.Description);
        }

        await repository.SaveChangesAsync(ct);
    }
}
