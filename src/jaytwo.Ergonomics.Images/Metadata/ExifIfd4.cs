using System.Collections.Generic;

namespace jaytwo.Ergonomics.Images.Metadata;

/// <summary>
/// IFD4 — interoperability tags. Often empty.
/// </summary>
public sealed class ExifIfd4 : ExifIfd
{
    internal static readonly ExifIfd4 Empty = new(EmptyFields);

    internal ExifIfd4(IReadOnlyDictionary<string, string> fields)
        : base(fields)
    {
    }

    /// <summary>Gets <c>InteroperabilityIndex</c>, when present.</summary>
    public string? InteroperabilityIndex => Get("InteroperabilityIndex");

    /// <summary>Gets <c>InteroperabilityVersion</c>, when present.</summary>
    public string? InteroperabilityVersion => Get("InteroperabilityVersion");
}
