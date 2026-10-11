using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Admission options for the decoded source, configured on <see cref="ImagePipeline.Create"/>.
/// </summary>
/// <remarks>
/// Not a geometry stage. Auto-orient runs first (unless turned off), then <see cref="SourceAlpha"/>,
/// then ordered stages.
/// </remarks>
public sealed class ImagePipelineSource
{
    private AlphaBehavior _sourceAlpha = AlphaBehavior.Preserve;
    private bool _autoOrient = true;

    internal AlphaBehavior Behavior => _sourceAlpha;

    internal bool AutoOrientEnabled => _autoOrient;

    /// <summary>
    /// Sets whether EXIF/XMP orientation is applied before measuring and geometry.
    /// </summary>
    /// <param name="autoOrient">
    /// True (the default) treats width and height as the visually upright image.
    /// False keeps the stored raster; width and height then apply to that raster.
    /// </param>
    /// <returns>This source configuration.</returns>
    public ImagePipelineSource AutoOrient(bool autoOrient)
    {
        _autoOrient = autoOrient;
        return this;
    }

    /// <summary>
    /// Sets how source transparency is resolved after auto-orient and before geometry.
    /// </summary>
    /// <param name="sourceAlpha">Preserve or flatten. Last call in the configure action wins.</param>
    /// <returns>This source configuration.</returns>
    /// <remarks>
    /// Source admission only — not <see cref="ImageCanvas"/> and not JPEG
    /// <see cref="ImageEncoding.JpegEncoding.AlphaFallbackColor"/>. See <see cref="AlphaBehavior"/>.
    /// </remarks>
    public ImagePipelineSource SourceAlpha(AlphaBehavior sourceAlpha)
    {
        if (sourceAlpha is null)
        {
            throw new ArgumentNullException(nameof(sourceAlpha));
        }

        _sourceAlpha = sourceAlpha;
        return this;
    }
}
