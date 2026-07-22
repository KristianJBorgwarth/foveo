using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Foveo.Application.Contracts;
using Foveo.Infrastructure.Configuration;

namespace Foveo.Infrastructure.Storage;

/// <summary>
/// S3-compatible storage (MinIO). Two clients: an internal one for server-side byte transfer,
/// and a public-endpoint one used only to sign URLs the browser can reach directly.
/// </summary>
public sealed class S3MediaStorage : IMediaStorage, IDisposable
{
    private readonly IAmazonS3 _internal;
    private readonly IAmazonS3 _public;
    private readonly S3Options _options;
    private readonly string _publicScheme;

    public S3MediaStorage(S3Options options)
    {
        _options = options;
        _publicScheme = new Uri(options.PublicEndpoint).Scheme;
        var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
        _internal = Build(credentials, options.InternalEndpoint, options.Region);
        _public = Build(credentials, options.PublicEndpoint, options.Region);
    }

    private static IAmazonS3 Build(AWSCredentials credentials, string endpoint, string region)
        => new AmazonS3Client(credentials, new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = region,
            // SDK v4 adds integrity checksums by default; those extra headers break browser PUTs
            // against a presigned URL, so only calculate a checksum when the operation requires one.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
        });

    public async Task EnsureBucketAsync(CancellationToken ct = default)
    {
        if (await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_internal, _options.BucketName))
            return;

        await _internal.PutBucketAsync(new PutBucketRequest { BucketName = _options.BucketName }, ct);
    }

    public Task<Uri> CreateUploadUrlAsync(string key, string contentType, CancellationToken ct = default)
        => PresignAsync(key, HttpVerb.PUT, contentType);

    public Task<Uri> CreateDownloadUrlAsync(string key, CancellationToken ct = default)
        => PresignAsync(key, HttpVerb.GET, contentType: null);

    private async Task<Uri> PresignAsync(string key, HttpVerb verb, string? contentType)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = verb,
            Expires = DateTime.UtcNow.AddMinutes(_options.PresignMinutes)
        };
        if (contentType is not null)
            request.ContentType = contentType;

        var url = await _public.GetPreSignedURLAsync(request);

        // The SDK forces https when signing; SigV4 signs the host header but not the scheme,
        // so we can safely rewrite the scheme to match the configured public endpoint.
        return new UriBuilder(url) { Scheme = _publicScheme }.Uri;
    }

    public async Task<Stream> OpenReadAsync(string key, CancellationToken ct = default)
    {
        var response = await _internal.GetObjectAsync(_options.BucketName, key, ct);
        return response.ResponseStream;
    }

    public Task PutAsync(string key, Stream content, string contentType, CancellationToken ct = default)
        => _internal.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        }, ct);

    public Task DeleteAsync(string key, CancellationToken ct = default)
        => _internal.DeleteObjectAsync(_options.BucketName, key, ct);

    public void Dispose()
    {
        _internal.Dispose();
        _public.Dispose();
    }
}
