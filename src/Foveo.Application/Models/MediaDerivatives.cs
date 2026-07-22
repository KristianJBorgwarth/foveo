namespace Foveo.Application.Models;

/// <summary>Storage keys of the artifacts produced by <see cref="IMediaProcessor"/>.</summary>
public sealed record MediaDerivatives(string ThumbnailKey, string DisplayKey);
