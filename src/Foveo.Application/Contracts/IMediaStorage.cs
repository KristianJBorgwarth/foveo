using Foveo.Application.Models;

namespace Foveo.Application.Contracts;

/// <summary>
/// Object-store boundary. All byte traffic flows through the API: uploads are streamed in and
/// stored here; thumbnails/media are streamed back out. MinIO stays entirely internal.
/// </summary>
public interface IMediaStorage
{
    /// <summary>Create the backing bucket if it does not already exist. Idempotent; called on startup.</summary>
    Task EnsureBucketAsync(CancellationToken ct = default);

    /// <summary>Store an object by streaming <paramref name="content"/> straight through to the store.</summary>
    Task PutAsync(string key, Stream content, long contentLength, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Open an object for reading. When a byte range is given, returns just that slice (HTTP 206 semantics);
    /// otherwise the whole object. The caller owns disposing <see cref="MediaReadResult.Content"/>.
    /// </summary>
    Task<MediaReadResult> OpenReadAsync(string key, long? rangeStart, long? rangeEnd, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);
}
