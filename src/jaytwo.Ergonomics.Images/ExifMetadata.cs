using System.Collections.Generic;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// EXIF bag from the source image.
/// </summary>
/// <remarks>
/// <see cref="Fields"/> uses libvips field names (for example <c>exif-ifd0-Make</c>).
/// Values are unwrapped plain strings. Prefer <see cref="ImageMetadata"/> convenience
/// properties for capture time, GPS, and camera.
/// </remarks>
public sealed class ExifMetadata
{
    internal ExifMetadata(bool present, IReadOnlyDictionary<string, string> fields)
    {
        Present = present;
        Fields = fields;
    }

    /// <summary>Gets a value indicating whether an EXIF blob or expanded EXIF fields are present.</summary>
    public bool Present { get; }

    /// <summary>Gets expanded EXIF string fields keyed by libvips names.</summary>
    public IReadOnlyDictionary<string, string> Fields { get; }
}
