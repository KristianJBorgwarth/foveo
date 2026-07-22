namespace Foveo.Domain.Aggregates;

/// <summary>
/// Lifecycle of an uploaded item. Transitions are one-directional:
/// <see cref="Pending"/> → <see cref="Uploaded"/> → <see cref="Ready"/>, with
/// <see cref="Failed"/> reachable from any non-terminal state.
/// </summary>
public enum MediaStatus
{
    /// <summary>Row created, a presigned upload URL was issued, bytes not yet confirmed in the store.</summary>
    Pending = 0,

    /// <summary>Client confirmed the bytes landed in the store; awaiting thumbnail/derivative processing.</summary>
    Uploaded = 1,

    /// <summary>Thumbnail (and web-friendly display copy where needed) generated; safe to show in the gallery.</summary>
    Ready = 2,

    /// <summary>Processing failed; excluded from the gallery.</summary>
    Failed = 3
}
