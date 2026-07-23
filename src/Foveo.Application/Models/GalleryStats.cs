namespace Foveo.Application.Models;

/// <summary>Headline counts for the gallery: ready photos, ready videos, and distinct named guests.</summary>
public sealed record GalleryStats(int Photos, int Videos, int Guests);
