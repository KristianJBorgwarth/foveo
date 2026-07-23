namespace Foveo.Infrastructure.Configuration;

public sealed class S3Options
{
    public const string SectionName = "S3";

    /// <summary>Cluster-internal endpoint. MinIO is never exposed to browsers — the API is the only client.</summary>
    public string Endpoint { get; init; } = null!;

    public string AccessKey { get; init; } = null!;
    public string SecretKey { get; init; } = null!;
    public string BucketName { get; init; } = "foveo";
    public string Region { get; init; } = "us-east-1";
}
