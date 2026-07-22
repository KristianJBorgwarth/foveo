namespace Foveo.Application.Contracts;

/// <summary>
/// Object-store boundary. The API is control-plane only: raw upload/download bytes flow
/// directly between the browser and the store via presigned URLs. Server-side byte access
/// (<see cref="OpenReadAsync"/>/<see cref="PutAsync"/>) exists solely for derivative processing.
/// </summary>
public interface IMediaStorage
{
    /// <summary>Create the backing bucket if it does not already exist. Idempotent; called on startup.</summary>
    Task EnsureBucketAsync(CancellationToken ct = default);

    /// <summary>Presigned PUT URL the browser uses to upload raw bytes straight to the store.</summary>
    Task<Uri> CreateUploadUrlAsync(string key, string contentType, CancellationToken ct = default);

    /// <summary>Presigned GET URL the browser uses to fetch an object straight from the store.</summary>
    Task<Uri> CreateDownloadUrlAsync(string key, CancellationToken ct = default);

    /// <summary>Open an object's bytes for server-side processing (fetching the original).</summary>
    Task<Stream> OpenReadAsync(string key, CancellationToken ct = default);

    /// <summary>Store a server-generated derivative (thumbnail or display copy).</summary>
    Task PutAsync(string key, Stream content, string contentType, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);
}
