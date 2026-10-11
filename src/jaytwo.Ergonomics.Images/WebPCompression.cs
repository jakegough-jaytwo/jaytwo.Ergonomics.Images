using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// How WebP is compressed: lossy quality or lossless.
/// </summary>
/// <remarks>
/// Mutually exclusive modes, like <see cref="ImageCanvas"/>.
/// Use <see cref="Lossy"/> for a quality from 1 to 100, or <see cref="Lossless"/>.
/// </remarks>
public abstract record WebPCompression
{
    private WebPCompression()
    {
    }

    /// <summary>Gets lossless WebP compression.</summary>
    public static WebPCompression Lossless { get; } = new LosslessMode();

    /// <summary>Lossy WebP at <paramref name="quality"/>.</summary>
    /// <param name="quality">Lossy quality from 1 to 100. Defaults to <see cref="ImageEncoding.DefaultWebPQuality"/>.</param>
    /// <returns>A lossy compression.</returns>
    public static WebPCompression Lossy(int quality = ImageEncoding.DefaultWebPQuality)
    {
        if (quality is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be from 1 to 100.");
        }

        return new LossyMode(quality);
    }

    internal sealed record LossyMode(int Quality) : WebPCompression;

    internal sealed record LosslessMode : WebPCompression;
}
