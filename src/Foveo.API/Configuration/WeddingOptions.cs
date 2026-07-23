namespace Foveo.API.Configuration;

/// <summary>Per-deployment personalisation for the gallery chrome (names, date, intro text).</summary>
public sealed class WeddingOptions
{
    public const string SectionName = "Wedding";

    public string CoupleFirst { get; init; } = "Cecilie";
    public string CoupleSecond { get; init; } = "Mads";
    public string EventDate { get; init; } = "25 · 07 · 2026";
    public string Venue { get; init; } = "Damgaard";
    public string Tagline { get; init; } = "Bryllupsalbum";

    /// <summary>Two-letter monogram for the footer mark, derived from the couple's initials.</summary>
    public string Monogram =>
        $"{Initial(CoupleFirst)}{Initial(CoupleSecond)}";

    private static char Initial(string name) =>
        string.IsNullOrWhiteSpace(name) ? '·' : char.ToUpperInvariant(name.Trim()[0]);
}
