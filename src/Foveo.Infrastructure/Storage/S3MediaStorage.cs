using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Foveo.Application.Contracts;
using Foveo.Application.Models;
using Foveo.Infrastructure.Configuration;

namespace Foveo.Infrastructure.Storage;

/// <summary>
/// S3-compatible storage (MinIO), reached only over the internal cluster endpoint. The API
/// streams uploads in and media out, so no presigning, public endpoint, or CORS is involved.
/// </summary>
public sealed class S3MediaStorage : IMediaStorage, IDisposable
{
    private readonly IAmazonS3 _client;
    private readonly S3Options _options;

    public S3MediaStorage(S3Options options)
    {
        _options = options;
        var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
        _client = new AmazonS3Client(credentials, new AmazonS3Config
        {
            ServiceURL = options.Endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = options.Region,
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
        });
    }

    public async Task EnsureBucketAsync(CancellationToken ct = default)
    {
        if (await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_client, _options.BucketName))
            return;

        await _client.PutBucketAsync(new PutBucketRequest { BucketName = _options.BucketName }, ct);
    }

    public async Task PutAsync(string key, Stream content, long contentLength, string contentType, CancellationToken ct = default)
    {
        // SigV4 over HTTP must hash the payload, which needs a seekable stream. A request body isn't
        // seekable, so spool it to a temp file first (flat memory, no OOM even for a 2 GB video).
        // Streams that are already seekable (the processor's derivative files) are sent straight through.
        if (content.CanSeek)
        {
            await PutObjectAsync(key, content, contentType, ct);
            return;
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"foveo-up-{Guid.NewGuid():N}");
        try
        {
            await using (var temp = File.Create(tempPath))
            {
                await content.CopyToAsync(temp, ct);
            }
            await using var seekable = File.OpenRead(tempPath);
            await PutObjectAsync(key, seekable, contentType, ct);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private Task PutObjectAsync(string key, Stream seekableContent, string contentType, CancellationToken ct)
        => _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = seekableContent,
            ContentType = contentType,
            AutoCloseStream = false
        }, ct);

    public async Task<MediaReadResult> OpenReadAsync(string key, long? rangeStart, long? rangeEnd, CancellationToken ct = default)
    {
        if (rangeStart is null && rangeEnd is null)
        {
            var full = await _client.GetObjectAsync(_options.BucketName, key, ct);
            return new MediaReadResult
            {
                Content = full.ResponseStream,
                ContentType = full.Headers.ContentType ?? "application/octet-stream",
                TotalLength = full.Headers.ContentLength,
                RangeStart = 0,
                RangeEnd = full.Headers.ContentLength - 1,
                IsPartial = false
            };
        }

        var meta = await _client.GetObjectMetadataAsync(_options.BucketName, key, ct);
        var total = meta.Headers.ContentLength;
        var start = rangeStart ?? 0;
        var end = rangeEnd ?? total - 1;
        if (end > total - 1) end = total - 1;
        if (start < 0) start = 0;

        var response = await _client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            ByteRange = new ByteRange(start, end)
        }, ct);

        return new MediaReadResult
        {
            Content = response.ResponseStream,
            ContentType = meta.Headers.ContentType ?? "application/octet-stream",
            TotalLength = total,
            RangeStart = start,
            RangeEnd = end,
            IsPartial = true
        };
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
        => _client.DeleteObjectAsync(_options.BucketName, key, ct);

    public void Dispose() => _client.Dispose();
}
