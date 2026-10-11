# jaytwo.Ergonomics.Images

Resize still images without becoming an image-processing framework.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://mit-license.org/)

Targets `net8.0` and `net6.0`. The processing engine is [NetVips](https://github.com/kleisauke/net-vips) / [libvips](https://www.libvips.org/). The package does not depend on `NetVips.Native` and does not ship an HEVC codec.

> **Pre-1.0.** The API is still settling; minor version bumps may break things until 1.0.

## Contents

- [Installation](#installation)
- [Charter](#charter)
- [Pipeline](#pipeline)
- [Operations](#operations)
- [Resize mode and canvas](#resize-mode-and-canvas)
- [Rotation](#rotation)
- [Alpha: three separate knobs](#alpha-three-separate-knobs)
- [Target](#target)
- [Sizes](#sizes)
- [Options](#options)
- [Orientation](#orientation)
- [Metadata](#metadata)
- [Formats](#formats)
- [Streaming](#streaming)
- [Capabilities](#capabilities)
- [Local development](#local-development)
- [HEIC on Linux](#heic-on-linux)
- [HEIC and HEVC](#heic-and-hevc)
- [Licenses](#licenses)
- [Why this and not the alternatives](#why-this-and-not-the-alternatives)
- [Behavior notes](#behavior-notes)
- [Development](#development)

## Installation

```bash
dotnet add package jaytwo.Ergonomics.Images
```

For local `dotnet run` / `dotnet test` without a system libvips install, also add the bundled native build:

```xml
<PackageReference Include="NetVips.Native" Version="8.18.6" />
```

`NetVips.Native` is enough for JPEG, PNG, WebP, GIF, TIFF, and AVIF. It does not decode HEIC. Do not reference it in a process that also loads a system libvips; the two native builds conflict.

## Charter

Application code keeps asking for the same few operations: fit a photo in a box, fill a frame by cropping the center or painting the bars, and do it without rotating the picture twice or leaking GPS into a public derivative.

This library is that API. It is a thin layer on libvips.

### What it is

- `ImagePipeline`, a reusable ordered fluent specification that runs from `Stream` to `Stream` (`Run` or `RunAsync`) and returns the size written. Compatible stages share one lazy libvips graph and encode once.
- Thin shortcuts: `ImageTransforms.Resize`, `ImageTransforms.Rotate`, and `ImageTransforms.Crop` compile into a pipeline. Pad is the short resize call. `ImageResize` / `ImageRotate` / `ImageCrop` remain option bags for those shortcuts. Shortcuts and the pipeline share `ImageEncoding`.
- Square `ImageSize` frames from 128 to 4096. `Thumbnail` is `S`. `Preview` is `L`. `ImagePresets.ThumbnailWebP` / `PreviewWebP` are pipelines (frame + WebP) built from the same primitives.
- Visual dimensions by default (`Autorot` before geometry)
- A small metadata policy: keep, strip, or keep the color profile only
- `ImageTransforms.Identify` for layered structure + EXIF/XMP/IPTC/ICC bags and common capture/GPS/camera fields
- A capability check that only reports HEIC when a real HEVC sample decodes

### What it is not

- Not face detection, object detection, or a generative fill
- Not a text, watermark, or blend API
- Not a public plugin/stage framework or filter graph
- Not a cache, HTTP handler, or object-store client
- Not `System.Drawing` / GDI+
- Not an HEVC distribution. HEIC shows up only when the host already has a decoder

## Pipeline

Describe operations in order. The chain does not own streams; call `Run` when you have input and output.

```csharp
var pipeline = ImagePipeline.Create()
    .Rotate(ImageRotation.Clockwise(12), r => r.Trim())
    .Resize(ImageSize.M, ResizeMode.Crop)
    .Encoding(ImageEncoding.WebP());

using var source = File.OpenRead(path);
using var destination = File.Create(outputPath);
var size = pipeline.Run(source, destination);
```

Stage options use the same lambda shape as `Create`. Source admission is on `Create`; resize options (`Aspect`, `Focus`, `Enlarge`, `Canvas`) are on `Resize`; rotate options (`Trim`, `Expand`, `Aspect`, `Canvas`) are on `Rotate`:

```csharp
ImagePipeline.Create(s => s.SourceAlpha(AlphaBehavior.Flatten(ImageColor.White)))
    .Resize(ImageSize.Thumbnail, ResizeMode.Crop, r => r.Focus(focus).Enlarge(true))
    .EncodingWebP();
```

`.Resize` sets the frame and optional mode (`Pad`, `Crop`, or `Distort`; defaults to `Pad`). Upscale is off unless `r.Enlarge(true)` / bag `enlarge: true`. `.Encoding` takes a format-specific object (`ImageEncoding.Jpeg`, `WebP`, `Png`, …) so each format only exposes valid options: JPEG has quality and `AlphaFallbackColor`; WebP takes `WebPCompression` (lossy quality via `WebP(80)` / `WebPCompression.Lossy(80)`, or `WebPCompression.Lossless`); PNG has neither. JPEG, lossy WebP, and AVIF default to quality 85. Shortcuts such as `.EncodingJpeg()`, `.EncodingWebP()`, `.EncodingWebP(WebPCompression.Lossless)`, and `.EncodingPng()` are available. Omit the `Create` configure action to auto-orient and preserve source transparency. Metadata and format options live on encoding.

Presets are ordinary pipelines:

```csharp
var thumbnail = ImagePipeline.Preset(ImagePresets.ThumbnailWebP);

thumbnail.Run(input1, output1);
thumbnail.Run(input2, output2);
```

To flatten on a thumbnail-sized pipeline, start from `Create`:

```csharp
var flatThumbnail = ImagePipeline.Create(s => s.SourceAlpha(AlphaBehavior.Flatten(ImageColor.White)))
    .Resize(ImageSize.Thumbnail)
    .EncodingWebP();
```

For ASP.NET request/response bodies (and other streams that require async I/O), use `RunAsync`. Native libvips work stays synchronous on a worker; pipes bridge `ReadAsync` / `WriteAsync`, and the cancellation token kills the native encode:

```csharp
var size = await pipeline.RunAsync(request.Body, response.Body, cancellationToken);
```

Prefer sync `Run` when both streams allow synchronous reads and writes.

## Operations

Output format is an argument because a stream has no filename. Source format is inferred. Shortcuts return the width and height written.

```csharp
using var source = File.OpenRead(path);
using var destination = File.Create(outputPath);

var size = ImageTransforms.Resize(source, destination, ImageEncoding.Jpeg(), 1920, 1080);
```

`size` is the pixels in `destination`. This call pads the whole image inside the box (`ResizeMode.Pad`). A source smaller than 1920×1080 stays at its own size unless you pass `enlarge: true`.

`ImageTransforms.Resize(source, destination, ImageEncoding.Jpeg())` re-encodes at the current size. Auto-orient still runs, so a sideways photo comes back upright.

Passing width, height, or both on that overload pads the image inside that limit and keeps the aspect ratio. One side of a box can come back short. There are no bars and no cover-crop. A missing side is calculated from the source aspect. When neither is passed, the source size is kept.

`ImageResize` is the call that sets a mode, a canvas, or an aspect ratio.

```csharp
ImageTransforms.Resize(source, destination, new ImageResize(ImageEncoding.Jpeg())
{
    Width = 400,
    Height = 300,
    Mode = ResizeMode.Crop,
});

ImageTransforms.Resize(source, destination, new ImageResize(ImageEncoding.Jpeg())
{
    Aspect = ImageAspectRatio.Square,
    Mode = ResizeMode.Crop,
});

ImageTransforms.Resize(source, destination, new ImageResize(ImageEncoding.Jpeg())
{
    Aspect = ImageAspectRatio.Square,
    Canvas = ImageCanvas.SolidBlack,
});
```

## Resize mode and canvas

Magick-style labels: pad inside the frame, crop to fill, or distort to the exact size.

| `ResizeMode` | Result |
| --- | --- |
| `Pad` | Aspect ratio stays. The whole image scales inside the frame (contain). One side can come back short. A canvas is a separate choice and is what makes the output match the frame. |
| `Crop` | Aspect ratio stays. The image scales until the frame is filled, then the overflow is discarded (cover). The center is kept. `ImageFocus` moves that window so a relative rectangle stays inside it. When the rectangle does not fit, cropping stops there and the rest of the frame is canvas. With enlarge on, the output matches the frame. With enlarge off (the default), a window that still has the frame's aspect ratio can be smaller than the frame. A window that stopped early still paints the frame. Not the same as the `.Crop(x, y, width, height)` stage, which extracts an explicit rectangle. |
| `Distort` | Exactly the requested size. Aspect ratio is ignored. Enlarge does not apply. |

A canvas applies with `Pad`. On `Crop`, a canvas is only the remainder when `Focus` does not fit; without `Focus`, Crop rejects a canvas. The output matches the frame when a plate is used. Enlarge stays off by default, so a smaller source does not grow; with a Pad canvas the frame is still painted. Configure canvas on that stage: `.Resize(..., r => r.Canvas(...))` or `.Rotate(..., r => r.Expand().Canvas(...))`. On a rotation, canvas requires Expand (it does not convert Trim to Expand).

Canvas plates are RGB (`ImageColor` is three 8-bit channels). The image on the plate must already be RGB or RGBA. Greyscale and other band layouts throw when a canvas is applied — there is no silent colorspace conversion. Omit the canvas, or convert the source to RGB first (see `todo/rgb-canvas.md`).

How each plate behaves (with `SourceAlpha` left on Preserve):

| Canvas | Invented gaps | Preserved content alpha |
| --- | --- | --- |
| `Transparent` | Fully transparent | Left alone (true holes) |
| `SolidBlack` / `SolidWhite` / `Solid(color)` | That color | Shows that color through content holes |
| `Checkerboard` | Checkerboard | Always composites over a full-frame grid, even when the image already fills the frame |

`ImageFocus` is a rectangle on the upright image. `X`, `Y`, `Width`, and `Height` are fractions from 0 to 1, measured from the top left. The cover window keeps its size and shifts so the rectangle stays inside. `ImageFocus.Point(x, y)` is that same rectangle with no area, and the point stays visible. If the rectangle is larger than the cover window, the crop stops at the rectangle and `Canvas` fills the frame. Omit the canvas and the remainder is transparent.

```csharp
ImageTransforms.Resize(source, destination, new ImageResize(ImageEncoding.Jpeg(), ImageSize.Preview)
{
    Mode = ResizeMode.Crop,
    Focus = new ImageFocus(0.62, 0.18, 0.22, 0.30),
});
```

Face or object detection stays in application code. Map the detector's box into upright relative coordinates, then pass an `ImageFocus`. The focus constrains where the cover window sits; it does not set the window size. To extract a computed rectangle instead, use an explicit `.Crop(x, y, width, height)` stage (same relative coordinates) before or after other stages.

```csharp
// faceBox.X/Y/Width/Height are fractions of the upright image (after the same orient policy you use at Run).
var focus = new ImageFocus(faceBox.X, faceBox.Y, faceBox.Width, faceBox.Height);

var pipeline = ImagePipeline.Create()
    .Resize(ImageSize.Thumbnail, ResizeMode.Crop, r => r
        .Aspect(ImageAspectRatio.Square)
        .Focus(focus))
    .EncodingJpeg();

pipeline.Run(source, destination);
```

```csharp
// Extract a detector box, then pad the result in a thumbnail frame.
var pipeline = ImagePipeline.Create()
    .Crop(faceBox.X, faceBox.Y, faceBox.Width, faceBox.Height)
    .Resize(ImageSize.Thumbnail)
    .EncodingJpeg();

pipeline.Run(source, destination);

// Or the thin shortcut:
ImageTransforms.Crop(source, destination, new ImageCrop(ImageEncoding.Jpeg(), faceBox.X, faceBox.Y, faceBox.Width, faceBox.Height));
```
## Rotation

`ImageTransforms.Rotate` turns the image after auto-orient. Set `AutoOrient = false` on the rotate bag (or `Create(s => s.AutoOrient(false))` on a pipeline) to turn the stored raster instead. Either way, the output orientation tag is cleared, so a viewer does not rotate the result again.

```csharp
ImageTransforms.Rotate(source, destination, new ImageRotate(ImageEncoding.Png())
{
    Rotation = ImageRotation.Clockwise(12),
});

ImageTransforms.Rotate(source, destination, new ImageRotate(ImageEncoding.Png())
{
    Rotation = ImageRotation.CounterClockwise(7.5),
    Bounds = RotationBounds.Trim,
    Aspect = ImageAspectRatio.FourByThree,
});
```

`Clockwise` and `CounterClockwise` take a non-negative number of degrees. `Clockwise(90)`, `Clockwise(180)`, and `Clockwise(270)` are exact quarter turns and do not resample. Any other angle does.

`Bounds` defaults to `Expand`: the smallest axis-aligned rectangle that contains the rotated image. Corners that rectangle adds use `Canvas`, which defaults to transparent. JPEG cannot store that alpha, so `ImageEncoding.Jpeg`’s `AlphaFallbackColor` flattens the corners, white unless you change it. A multiple of 90 degrees adds no corners.

`RotationBounds.Trim` keeps the largest centered axis-aligned rectangle that stays inside the rotated image. `Aspect` on a trim is that same rectangle at the requested ratio, still centered. It is not a second resize. An aspect ratio applies with `Trim`. A canvas applies with `Expand`.

Source admission (auto-orient and source alpha on `Create` or the rotate bag) runs before the turn. Flattening the source does not paint the corners Expand adds; those use `Canvas`.

## Alpha: three separate knobs

Transparency shows up in three places. They are not interchangeable.

| Knob | Where | When | What it does |
| --- | --- | --- |
| `SourceAlpha` (`AlphaBehavior`) | `Create(...)` / option bags | After auto-orient, before geometry | `Preserve` (default) keeps source-pixel holes. `Flatten(color)` composites the source onto a color so geometry sees an opaque raster. |
| `ImageCanvas` | `Resize` / `Rotate` configure / bags | After geometry invents area (or always for checkerboard) | Plate under pad letterboxes, crop-focus remainders, and expand corners. Solid and checkerboard plates also show through preserved content alpha. `Transparent` invents empty gaps only. `Checkerboard` always builds a full-frame preview grid. |
| `AlphaFallbackColor` | `ImageEncoding.Jpeg` only | Encode, last | Formats that cannot store alpha flatten whatever alpha remains (gaps + content holes) onto this color (default white). |

Examples:

- Preserve + `Transparent` + PNG → content holes and pad gaps stay true transparency.
- Preserve + `SolidBlack` + PNG → pad gaps are black, and content holes show black through them.
- `Flatten(White)` + `Transparent` + PNG → source holes are already white; only pad gaps stay transparent.
- Preserve + `Transparent` + JPEG → encode fallback paints remaining alpha (gaps and holes) white unless you change `AlphaFallbackColor`.
- `Checkerboard` is a canvas plate for previewing transparency, not a source-alpha policy.

## Target

The frame comes from the width and height, an `ImageAspectRatio`, or both.

| What is set | Frame |
| --- | --- |
| Width, height, or both | That pixel box. One side on a pad, with no aspect ratio, is completed from the source. |
| Aspect ratio only, `Crop` | Discard overflow until the image has that ratio, at the source size. The center is kept. A focus box moves that window. |
| Aspect ratio only, `Pad` and a canvas | Pad to that ratio at the source size. |
| Aspect ratio and one side | The other side is calculated from the ratio. |
| Aspect ratio and both sides | The sides are a maximum box. The frame is the largest rectangle of that ratio inside the box. |

Named ratios are `Square`, `FourByThree`, `ThreeByFour`, `SixteenByNine`, and `NineBySixteen`. A pair is width and height: `new ImageAspectRatio(3, 2)`. A single number is width divided by height: `new ImageAspectRatio(1.5)`.

## Sizes

`ImageSize` is a width and height. `Resize` returns the size it wrote. The named sizes are square frames. A pad keeps the aspect ratio, so one side can come back short.

| Size | Frame |
| --- | --- |
| `XS` | 128 × 128 |
| `S`, `Thumbnail` | 256 × 256 |
| `M` | 512 × 512 |
| `L`, `Preview` | 1024 × 1024 |
| `XL` | 2048 × 2048 |
| `XXL` | 4096 × 4096 |

`Thumbnail` is `S`. `Preview` is `L`. Any other frame is `new ImageSize(width, height)`. A named size is only the frame. It does not change the mode, the canvas, or the metadata policy.

```csharp
var size = ImageTransforms.Resize(source, destination, ImageEncoding.Jpeg(), ImageSize.Preview);
```

`new ImageResize(ImageEncoding.Jpeg(), ImageSize.L)` sets that frame on the options object.

## Options

```csharp
ImageTransforms.Resize(source, destination, new ImageResize(
    ImageEncoding.WebP(80) with { Metadata = MetadataPolicy.Strip })
{
    Width = 512,
    Height = 512,
    Canvas = ImageCanvas.SolidBlack,
});
```

| Property | Default | Meaning |
| --- | --- | --- |
| `AutoOrient` | `true` | Source admission on `Create` / option bags. Apply orientation before measuring and geometry. |
| `SourceAlpha` | Preserve | Source admission on `Create` / option bags. |
| `Metadata` | `PreserveColorProfileOnly` | On encoding only. See below. |
| `Quality` | see notes | On JPEG, AVIF, and HEIC factories. Defaults are `DefaultJpegQuality` / `DefaultAvifQuality` / `DefaultHeicQuality` (all 85). Range 1-100. PNG, GIF, and TIFF have no quality. |
| `Compression` | lossy 85 | On WebP encoding only. `WebPCompression.Lossy(quality)` or `WebPCompression.Lossless`. `ImageEncoding.WebP(80)` is lossy shorthand. |
| `AlphaFallbackColor` | white | On JPEG encoding only. Used when remaining alpha cannot be stored. |
| `Enlarge` | false | Allow upscaling on Pad/Crop. Distort always writes the requested size. |

Pass encoding overrides only when a default should change. Shortcuts and the pipeline share `ImageEncoding`. Auto-orient is not on encoding.

`ImageTransforms.Resize` / `Rotate` / `Crop` compile into `ImagePipeline` and call `Run`. Prefer the pipeline when order matters (for example crop, then resize) so stages share one native graph.

## Orientation

JPEG and HEIC often store the sensor raster sideways and put the display rotation in EXIF. With auto-orient left on (the default on `Create` / bags), a 4032x3024 file tagged as 90 degrees is treated as 3024x4032.

The rotation is libvips `Autorot` (via thumbnail's rotate, not a hand-rolled EXIF switch). After the pixels move, the orientation tag is reset, so a later viewer does not rotate the derivative again. A caller-requested turn is `ImageTransforms.Rotate`, and it runs after this step.

Turn auto-orient off only when you mean the stored raster:

```csharp
ImagePipeline.Create(s => s.AutoOrient(false))
    .Encoding(ImageEncoding.Jpeg() with { Metadata = MetadataPolicy.Preserve });
```

Width and height then apply to that raster. With `Metadata = Preserve`, a preserved orientation tag is left as it was.

## Metadata

### Identify (read)

```csharp
using var source = File.OpenRead(path);
var meta = ImageTransforms.Identify(source);

meta.Structure.Size;       // stored pixels
meta.Structure.VisualSize; // after EXIF orientation swap
meta.Structure.Format;     // ImageFormat
meta.Exif.Fields;          // libvips EXIF names → plain strings
meta.Xmp.Xml;              // opaque XMP packet, or null
meta.Iptc.Present;
meta.ColorProfile.Present;
meta.CapturedAt;           // EXIF DateTimeOriginal → Digitised → DateTime
meta.Latitude;             // EXIF GPS IFD, decimal degrees
meta.CameraMake;
```

Identify opens the header (typical formats do not force a full pixel decode). It does not auto-orient; use `VisualSize` when you need the upright frame. Same facts can exist in EXIF, XMP, and IPTC; convenience properties resolve from EXIF only in this version. This is read-only — not an EXIF editor.

### Encode policy

| Policy | EXIF / XMP / IPTC | ICC profile |
| --- | --- | --- |
| `Preserve` | kept | kept |
| `Strip` | removed | removed |
| `PreserveColorProfileOnly` | removed | kept |

`PreserveColorProfileOnly` is the default so a thumbnail does not publish camera or GPS tags by accident, and color still renders with the profile.

## Formats

| Format | Decode | Encode | Notes |
| --- | --- | --- | --- |
| JPEG | yes | yes | Remaining alpha is flattened onto `AlphaFallbackColor`. |
| PNG | yes | yes | Alpha kept. `Quality` is ignored. |
| WebP | yes | yes | `WebPCompression` lossy or lossless. |
| AVIF | when the runtime has an AV1 decoder | when it has an AV1 encoder | `NetVips.Native` includes both. |
| GIF | yes | yes | First frame only (same for animated WebP input). |
| TIFF | yes | yes | First page only. |
| HEIC | when the runtime has an HEVC decoder | when it has an HEVC encoder | `NetVips.Native` has neither. |

Animated GIF, animated WebP, and multi-page TIFF are reduced to the first frame on purpose. HEIF is not limited that way: a still HEIC can be stored as a tile grid, and keeping one tile would return a slice of the photo.

## Streaming

```text
source stream -> decode -> auto-orient -> ordered stages -> encode -> destination stream
```

Callers never have to copy the compressed input or the encoded output into a `byte[]`. The work stays in libvips. Thumbnail generation uses `thumbnail_source`, which can shrink JPEG and similar formats while decoding.

`RunAsync` adds pipes on both ends so async-only streams (ASP.NET bodies) never see sync `Read` / `Write`. The native graph still runs synchronously on a worker thread.

Some inputs are not streamed end to end. A center crop that does not enlarge, and a crop with a focus box, decode the oriented image once and then extract. Orientation and some codecs also cause libvips to buffer. The guarantee is only that the API does not force that buffer into managed arrays.

GIF, TIFF, and WebP detection reads a few header bytes. On a non-seekable stream those bytes are replayed; the rest of the file is not copied.

This library does not decide HTTP `Content-Length`, ASP.NET buffering, object-store caching, or where thumbnails are stored.

The first call also sets libvips' operation cache size to 0 for the process. The cache retains source images, which is the wrong default for a service handling unique uploads. If something else in the process depends on that cache, set it again after startup.

## Capabilities

```csharp
if (ImageTransforms.Capabilities.Heic)
{
    // This process can decode HEVC HEIC, not merely load a HEIF container.
}

if (ImageTransforms.Capabilities.Encodes(ImageOutputFormat.Heic))
{
    // This process can encode HEVC HEIC. Decode support does not imply this.
}
```

`Heic` is true only when an embedded HEVC sample actually decodes. A libheif build can load AVIF and still have no HEVC decoder. `Avif` is the same idea with an AV1 sample. JPEG, PNG, WebP, GIF, and TIFF decode is reported from the loader operations.

`Encodes` is the matching question for `ImageOutputFormat`. JPEG, PNG, WebP, GIF, and TIFF come from the save operation. AVIF and HEIC follow the same check: a probe image must encode, and the container brand must be that codec. The probe runs the first time `Capabilities` is read, not on the first `Run`.

## Local development

```bash
dotnet test
```

The test project references `NetVips.Native` unless you turn that off:

```bash
dotnet test -p:UseBundledLibVips=false
```

Use the flag only when system libvips is on the library path and `NetVips.Native` is not loaded. On Windows and macOS, leaving the default (`true`) is the zero-setup path. `NetVips.Native` does not need to support HEIC.

## HEIC on Linux

Ubuntu 26.04 (`resolute`) is the integration image because its `libvips42t64` is 8.18, which matches NetVips 3.2. Ubuntu 24.04's libvips is 8.15 and is too old for this binding.

The runtime image installs a decoder and does not install an HEVC encoder:

```dockerfile
FROM ubuntu:26.04

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        libvips42t64 \
        libheif-plugin-libde265 \
        libheif-plugin-aomdec \
        libheif-plugin-aomenc \
    && rm -rf /var/lib/apt/lists/*
```

`libheif-plugin-libde265` is the HEVC decoder. `libheif-plugin-aomdec` and `libheif-plugin-aomenc` are AV1, so AVIF decode and encode still work when `NetVips.Native` is absent. Do not add `libheif-plugin-x265` unless you have decided to encode HEVC and have reviewed that separately.

Omit `NetVips.Native` in that image. `UseBundledLibVips=false` is how the test project does it. `docker build --target test` builds that stack and runs the suite, including the HEIC fixture.

The stack is:

```text
NetVips
  -> libvips
    -> libheif
      -> libde265 (HEVC decode)
```

## HEIC and HEVC

HEIC photos are usually HEVC (`hvc1`) inside a HEIF container. This package does not contain `libde265`, `x265`, or any other HEVC implementation. Decoding works when the machine's libvips/libheif already has an HEVC decoder. Encoding `ImageOutputFormat.Heic` works only when an HEVC encoder is installed; the Docker test image does not install one.

These install notes are not patent advice. HEVC is patent-encumbered. Whether your use or redistribution needs a license is your decision. Installing `libde265` from Ubuntu does not transfer that decision to this project.

The embedded HEIC probe is a few hundred bytes of bitstream used to answer "can this process decode HEVC?". It is not a codec.

## Licenses

| Piece | Copyright license |
| --- | --- |
| This package | MIT |
| NetVips | MIT |
| libvips | LGPL-2.1-or-later |
| `NetVips.Native` and distro libvips | libvips plus the codecs that build bundles (libjpeg, libpng, libwebp, libaom, and others) |

Copyright licenses and HEVC patents are different things. This README does not make a legal guarantee about either.

## Why this and not the alternatives

**ImageSharp** is pure managed code, so deployment is simple and there is no native stack to get wrong. It also uses more memory and is slower on large resizes. This library exists for backend jobs that want libvips' streaming resize and will accept a native runtime.

**PhotoSauce MagicScaler** has the same product shape: a small, hard-to-misuse resize API with serious attention to orientation, quality, and metadata. Its best path is Windows WIC. This library makes the opposite runtime bet: libvips on Windows, macOS, and Linux, with HEIC left to the host instead of a bundled HEVC decoder.

**Sharp** is the libvips API people already know (`contain`, `cover`, `fill`, gravity). It is Node. The Magick-style modes here are the same set of outcomes: `Pad` (contain, optional canvas), `Crop` (cover, optional focus), `Distort` (fill/stretch).

## Behavior notes

- `netstandard2.1` is not a target. NetVips 3.2 targets `net6.0` and .NET Framework 4.6.2. Sibling ergonomics packages that still multi-target `netstandard2.1` cannot share this one.
- Downscale goes through libvips thumbnail, which applies a mild sharpen and resizes in the image's gamma. There is no linear-light switch in this version.
- A center crop that may enlarge goes through libvips thumbnail. A center crop that does not enlarge, an aspect-ratio crop at the source size, and a crop with a focus box decode the oriented image and then extract. Non-seekable streams take one decode.
- A multiple of 90 degrees uses libvips' quarter-turn rotate. Any other angle resamples. `Trim` floors the centered rectangle, then keeps it one pixel inside that edge so the resampler does not mix the empty corners into the crop.
- libvips prints EXIF strings with a suffix such as ` (value, ASCII, N components, N bytes)` when you read them back. That suffix is how libvips round-trips the field. The bytes are still the original text.
- The HEIC integration fixture is a real HEVC still (`ftypheic` / `mif1` / `hvc1`, camera tag Sony DSLR-A550), not a file from an iPhone camera roll. iPhone photos often add a tile grid and an orientation tag. libheif reassembles grids before this library measures the image, but that container is not what the fixture covers. Drop a licensed iPhone capture in `test/fixtures` if you need that case locked.
- `ImageOutputFormat.Heic` is accepted and then fails clearly when the process has no HEVC encoder. That is expected with `NetVips.Native` and with the Docker test image.

## Development

```bash
dotnet tool restore
dotnet build
dotnet test
```

```bash
make            # clean, build, test, pack a beta nupkg, nuget-check
make test
docker build --target test .
```

The `Makefile` is GNU make. On Windows, run it from Git Bash or WSL, or use the `dotnet` commands above.

## License

[MIT](https://mit-license.org/). See [LICENSE](LICENSE).

---

Made with &hearts; by Jake
