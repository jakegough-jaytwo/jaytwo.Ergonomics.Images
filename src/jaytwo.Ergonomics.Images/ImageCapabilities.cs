using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Codecs the loaded libvips can decode, and which output formats it can encode.
/// </summary>
/// <remarks>
/// <see cref="Heic"/> and <see cref="Avif"/> are decode checks. A libheif build can load one of
/// those containers and still be unable to encode it. Use <see cref="Encodes"/> before writing
/// <see cref="ImageOutputFormat.Heic"/> or <see cref="ImageOutputFormat.Avif"/>.
/// </remarks>
public sealed class ImageCapabilities
{
    private readonly bool _jpegEncode;
    private readonly bool _pngEncode;
    private readonly bool _webPEncode;
    private readonly bool _gifEncode;
    private readonly bool _tiffEncode;
    private readonly bool _avifEncode;
    private readonly bool _heicEncode;

    private ImageCapabilities(
        bool jpeg,
        bool png,
        bool webP,
        bool gif,
        bool tiff,
        bool avif,
        bool heic,
        bool jpegEncode,
        bool pngEncode,
        bool webPEncode,
        bool gifEncode,
        bool tiffEncode,
        bool avifEncode,
        bool heicEncode)
    {
        Jpeg = jpeg;
        Png = png;
        WebP = webP;
        Gif = gif;
        Tiff = tiff;
        Avif = avif;
        Heic = heic;
        _jpegEncode = jpegEncode;
        _pngEncode = pngEncode;
        _webPEncode = webPEncode;
        _gifEncode = gifEncode;
        _tiffEncode = tiffEncode;
        _avifEncode = avifEncode;
        _heicEncode = heicEncode;
    }

    /// <summary>Gets a value indicating whether JPEG decode works.</summary>
    public bool Jpeg { get; }

    /// <summary>Gets a value indicating whether PNG decode works.</summary>
    public bool Png { get; }

    /// <summary>Gets a value indicating whether WebP decode works.</summary>
    public bool WebP { get; }

    /// <summary>Gets a value indicating whether GIF decode works.</summary>
    public bool Gif { get; }

    /// <summary>Gets a value indicating whether TIFF decode works.</summary>
    public bool Tiff { get; }

    /// <summary>
    /// Gets a value indicating whether a real AVIF sample decodes.
    /// </summary>
    /// <remarks>Decode only. Encoding is <see cref="Encodes"/> with <see cref="ImageOutputFormat.Avif"/>.</remarks>
    public bool Avif { get; }

    /// <summary>
    /// Gets a value indicating whether a real HEVC HEIC sample decodes.
    /// </summary>
    /// <remarks>Decode only. Encoding is <see cref="Encodes"/> with <see cref="ImageOutputFormat.Heic"/>.</remarks>
    public bool Heic { get; }

    /// <summary>
    /// Reports whether <paramref name="format"/> can be encoded by the loaded libvips.
    /// </summary>
    /// <remarks>
    /// JPEG, PNG, WebP, GIF, and TIFF come from the save operation.
    /// AVIF and HEIC follow the same check: a probe image must encode, and the container brand must be that codec.
    /// </remarks>
    public bool Encodes(ImageOutputFormat format)
    {
        if (!Enum.IsDefined(typeof(ImageOutputFormat), format))
        {
            throw new ArgumentOutOfRangeException(nameof(format));
        }

        switch (format)
        {
            case ImageOutputFormat.Jpeg:
                return _jpegEncode;
            case ImageOutputFormat.Png:
                return _pngEncode;
            case ImageOutputFormat.WebP:
                return _webPEncode;
            case ImageOutputFormat.Gif:
                return _gifEncode;
            case ImageOutputFormat.Tiff:
                return _tiffEncode;
            case ImageOutputFormat.Avif:
                return _avifEncode;
            case ImageOutputFormat.Heic:
                return _heicEncode;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
    }

    internal static ImageCapabilities Probe()
    {
        VipsRuntime.EnsureInitialized();

        // AVIF and HEIC are the same HEIF pipeline. heifload and heifsave exist even when the
        // codec plugin does not, so each codec is a sample: decode pixels, or encode and read the brand.
        return new ImageCapabilities(
            jpeg: HasOperation("jpegload"),
            png: HasOperation("pngload"),
            webP: HasOperation("webpload"),
            gif: HasOperation("gifload"),
            tiff: HasOperation("tiffload"),
            avif: DecodesHeif("jaytwo.Ergonomics.Images.Probes.avif"),
            heic: DecodesHeif("jaytwo.Ergonomics.Images.Probes.heic"),
            jpegEncode: HasOperation("jpegsave"),
            pngEncode: HasOperation("pngsave"),
            webPEncode: HasOperation("webpsave"),
            gifEncode: HasOperation("gifsave"),
            tiffEncode: HasOperation("tiffsave"),
            avifEncode: EncodesHeif(NetVips.Enums.ForeignHeifCompression.Av1, "avif"),
            heicEncode: EncodesHeif(NetVips.Enums.ForeignHeifCompression.Hevc, "heic"));
    }

    private static bool DecodesHeif(string resourceName)
    {
        return HasOperation("heifload") && TryDecodeResource(resourceName);
    }

    private static bool EncodesHeif(NetVips.Enums.ForeignHeifCompression compression, string brand)
    {
        if (!HasOperation("heifsave"))
        {
            return false;
        }

        try
        {
            var pixels = new byte[16 * 16 * 3];
            using var image = NetVips.Image.NewFromMemory(pixels, 16, 16, 3, NetVips.Enums.BandFormat.Uchar);
            using var buffer = new MemoryStream();
            image.HeifsaveStream(buffer, q: 30, compression: compression);
            return HeaderContains(buffer, brand);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool HeaderContains(MemoryStream buffer, string brand)
    {
        if (buffer.Length < brand.Length)
        {
            return false;
        }

        buffer.Position = 0;
        var count = (int)Math.Min(buffer.Length, 64);
        var header = new byte[count];
        var read = buffer.Read(header, 0, count);
        var text = Encoding.ASCII.GetString(header, 0, read);
        return text.Contains(brand, StringComparison.Ordinal);
    }

    private static bool HasOperation(string nickname)
    {
        return NetVips.NetVips.TypeFind("VipsOperation", nickname) != 0;
    }

    private static bool TryDecodeResource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        if (stream is null)
        {
            return false;
        }

        try
        {
            using var image = NetVips.Image.NewFromStream(stream, access: NetVips.Enums.Access.Sequential);

            // Width can come from the container header. Avg forces a pixel decode, so a HEIF
            // loader without that codec's decoder does not count.
            _ = image.Avg();
            return image.Width > 0 && image.Height > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
