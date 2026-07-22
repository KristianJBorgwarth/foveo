namespace Foveo.Application.Contracts;

/// <summary>
/// In-process hand-off from the upload endpoint to the background processing worker.
/// Backed by a bounded channel; survives only as long as the process does (acceptable at
/// wedding scale — a missed item can be re-completed).
/// </summary>
public interface IMediaProcessingQueue
{
    ValueTask EnqueueAsync(Guid mediaId, CancellationToken ct = default);

    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken ct = default);
}
