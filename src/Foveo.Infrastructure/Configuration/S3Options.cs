namespace Foveo.Infrastructure.Configuration;

public sealed class S3Options
{
    public const string SectionName = "S3";

    /// <summary>Cluster-internal endpoint the API uses for server-side byte transfer (fetch original, put derivatives).</summary>
    public string InternalEndpoint { get; init; } = null!;

    /// <summary>Browser-reachable endpoint that presigned URLs are signed against, so guests can PUT/GET directly.</summary>
    public string PublicEndpoint { get; init; } = null!;

    public string AccessKey { get; init; } = null!;
    public string SecretKey { get; init; } = null!;
    public string BucketName { get; init; } = "foveo";
    public string Region { get; init; } = "us-east-1";

    /// <summary>Lifetime of issued presigned URLs.</summary>
    public int PresignMinutes { get; init; } = 60;
}
