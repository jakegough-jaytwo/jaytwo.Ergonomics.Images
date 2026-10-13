using System.Collections.Generic;

namespace jaytwo.Ergonomics.Images.Metadata;

/// <summary>
/// IFD1 — embedded thumbnail tags. Often empty.
/// </summary>
public sealed class ExifIfd1 : ExifIfd
{
    internal static readonly ExifIfd1 Empty = new(EmptyFields);

    internal ExifIfd1(IReadOnlyDictionary<string, string> fields)
        : base(fields)
    {
    }

    /// <summary>Gets thumbnail <c>XResolution</c> as a raw string, when present.</summary>
    public string? XResolution => Get("XResolution");

    /// <summary>Gets thumbnail <c>YResolution</c> as a raw string, when present.</summary>
    public string? YResolution => Get("YResolution");

    /// <summary>Gets thumbnail <c>Compression</c> as a raw string, when present.</summary>
    public string? Compression => Get("Compression");

    /// <summary>Gets thumbnail <c>Orientation</c> as a raw string, when present.</summary>
    public string? Orientation => Get("Orientation");
}
