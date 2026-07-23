namespace Foveo.Application.Models;

/// <summary>A readable object slice plus the metadata needed to write a correct HTTP (200 or 206) response.</summary>
public sealed class MediaReadResult
{
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }

    /// <summary>Total size of the whole object, regardless of range.</summary>
    public required long TotalLength { get; init; }

    /// <summary>Inclusive start of the returned slice.</summary>
    public required long RangeStart { get; init; }

    /// <summary>Inclusive end of the returned slice.</summary>
    public required long RangeEnd { get; init; }

    public bool IsPartial { get; init; }

    /// <summary>Number of bytes in this slice.</summary>
    public long ContentLength => RangeEnd - RangeStart + 1;
}
