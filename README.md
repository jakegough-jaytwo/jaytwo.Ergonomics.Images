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
- [Fit and canvas](#fit-and-canvas)
- [Rotation](#rotation)
- [Source alpha](#source-alpha)
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
- Thin shortcuts: `ImageTransforms.Resize` and `ImageTransforms.Rotate` compile into a pipeline. Fit is the short resize call. `ImageResize` / `ImageRotate` remain option bags for those shortcuts.
- Square `ImageSize` frames from 128 to 4096. `Thumbnail` is `S`. `Preview` is `L`. `ImagePresets` are pipelines built from the same primitives.
- Visual dimensions by default (`Autorot` before geometry)
- A small metadata policy: keep, strip, or keep the color profile only
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
    .Rotate(ImageRotation.Clockwise(12))
    .Trim()
    .Resize(ImageSize.M)
    .Fit(ImageFit.Zoom)
    .Encoding(ImageEncoding.WebP());

using var source = File.OpenRead(path);
using var destination = File.Create(outputPath);
var size = pipeline.Run(source, destination);
```

`.Resize` sets the frame. `.Fit(ImageFit)` sets the mode (`Fit`, `Zoom`, or `Stretch`). `.Encoding` takes a format-specific object (`ImageEncoding.Jpeg`, `WebP`, `Png`, …) so each format only exposes valid options: JPEG/WebP have quality, JPEG has `AlphaFallbackColor`, PNG has neither. Defaults are JPEG quality 80 and WebP quality 60. Shortcuts such as `.EncodingJpeg()`, `.EncodingWebP()`, and `.EncodingPng()` are available. `.SourceAlpha` is a policy: last call wins, and its place in the chain does not change when it runs. Auto-orient is a default preamble.

Presets are ordinary pipelines:

```csharp
var thumbnail = ImagePipeline.Preset(ImagePresets.Thumbnail)
    .SourceAlpha(AlphaBehavior.Preserve);

thumbnail.Run(input1, output1);
thumbnail.Run(input2, output2);
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

var size = ImageTransforms.Resize(source, destination, ImageOutputFormat.Jpeg, 1920, 1080, enlarge: false);
```

`size` is the pixels in `destination`. This call fits the whole image inside the box. `enlarge: false` leaves a source smaller than 1920x1080 at its own size. The default call, without that flag, may enlarge.

`ImageTransforms.Resize(source, destination, ImageOutputFormat.Jpeg)` re-encodes at the current size. Auto-orient still runs, so a sideways photo comes back upright.

Passing width, height, or both on that overload fits the image inside that limit and keeps the aspect ratio. One side of a box can come back short. There are no bars and no crop. A missing side is calculated from the source aspect. When neither is passed, the source size is kept.

`ImageResize` is the call that sets a fit, a canvas, or an aspect ratio.

```csharp
ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Jpeg)
{
    Width = 400,
    Height = 300,
    Fit = ImageFit.Zoom,
});

ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Jpeg)
{
    Aspect = ImageAspectRatio.Square,
    Fit = ImageFit.Zoom,
});

ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Jpeg)
{
    Aspect = ImageAspectRatio.Square,
    Canvas = ImageCanvas.SolidBlack,
});
```

## Fit and canvas

| `ImageFit` | Result |
| --- | --- |
| `Fit` | Aspect ratio stays. The whole image scales inside the frame. One side can come back short. A canvas is a separate choice and is what makes the output match the frame. |
| `Zoom` | Aspect ratio stays. The image scales until the frame is filled, then the overflow is discarded. The center is kept. `ImageFocus` moves that window so a relative rectangle stays inside it. When the rectangle does not fit, cropping stops there and the rest of the frame is canvas. With enlarge left on, the output matches the frame. With enlarge off, a window that still has the frame's aspect ratio can be smaller than the frame. A window that stopped early still paints the frame. |
| `Stretch` | Exactly the requested size. Aspect ratio is ignored. |

A canvas applies with `Fit`. `SolidBlack`, `SolidWhite`, and `Solid(ImageColor)` paint the gap. `Transparent` leaves it empty. The output matches the frame. `enlarge: false` keeps a smaller source from growing and still paints an exact frame. `Stretch` does not take `enlarge: false`.

`ImageFocus` is a rectangle on the upright image. `X`, `Y`, `Width`, and `Height` are fractions from 0 to 1, measured from the top left. The cover window keeps its size and shifts so the rectangle stays inside. `ImageFocus.Point(x, y)` is that same rectangle with no area, and the point stays visible. If the rectangle is larger than the cover window, the crop stops at the rectangle and `Canvas` fills the frame. Omit the canvas and the gap is transparent.

```csharp
ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Jpeg, ImageSize.Preview)
{
    Fit = ImageFit.Zoom,
    Focus = new ImageFocus(0.62, 0.18, 0.22, 0.30),
});
```

## Rotation

`ImageTransforms.Rotate` turns the image after auto-orient. Set `AutoOrient` to false to turn the stored raster instead. Either way, the output orientation tag is cleared, so a viewer does not rotate the result again.

```csharp
ImageTransforms.Rotate(source, destination, new ImageRotate(ImageOutputFormat.Png)
{
    Rotation = ImageRotation.Clockwise(12),
});

ImageTransforms.Rotate(source, destination, new ImageRotate(ImageOutputFormat.Png)
{
    Rotation = ImageRotation.CounterClockwise(7.5),
    Bounds = RotationBounds.Trim,
    Aspect = ImageAspectRatio.FourByThree,
});
```

`Clockwise` and `CounterClockwise` take a non-negative number of degrees. `Clockwise(90)`, `Clockwise(180)`, and `Clockwise(270)` are exact quarter turns and do not resample. Any other angle does.

`Bounds` defaults to `Expand`: the smallest axis-aligned rectangle that contains the rotated image. Corners that rectangle adds use `Canvas`, which defaults to transparent. JPEG cannot store that alpha, so `ImageEncoding.Jpeg`’s `AlphaFallbackColor` flattens the corners, white unless you change it. A multiple of 90 degrees adds no corners.

`RotationBounds.Trim` keeps the largest centered axis-aligned rectangle that stays inside the rotated image. `Aspect` on a trim is that same rectangle at the requested ratio, still centered. It is not a second resize. An aspect ratio applies with `Trim`. A canvas applies with `Expand`.

`SourceAlpha` is resolved on the upright image before the turn. A flattened or checkerboard source does not paint the corners Expand adds.

## Source alpha

`SourceAlpha` is transparency inside the source. It defaults to `Preserve`. `Flatten(ImageColor.White)` composites that transparency onto a color. `Checkerboard()` paints an 8-pixel white and gray grid under it, after the resize.

The canvas stays separate. A transparent canvas can surround a checkerboard, and a black canvas can show through preserved source transparency.

`AlphaFallbackColor` on JPEG encoding runs last, and only when the output cannot store alpha. JPEG flattens whatever remains onto that color, which defaults to white.

## Target

The frame comes from the width and height, an `ImageAspectRatio`, or both.

| What is set | Frame |
| --- | --- |
| Width, height, or both | That pixel box. One side on a fit, with no aspect ratio, is completed from the source. |
| Aspect ratio only, `Zoom` | Discard overflow until the image has that ratio, at the source size. The center is kept. A focus box moves that window. |
| Aspect ratio only, `Fit` and a canvas | Pad to that ratio at the source size. |
| Aspect ratio and one side | The other side is calculated from the ratio. |
| Aspect ratio and both sides | The sides are a maximum box. The frame is the largest rectangle of that ratio inside the box. |

Named ratios are `Square`, `FourByThree`, `ThreeByFour`, `SixteenByNine`, and `NineBySixteen`. A pair is width and height: `new ImageAspectRatio(3, 2)`. A single number is width divided by height: `new ImageAspectRatio(1.5)`.

## Sizes

`ImageSize` is a width and height. `Resize` returns the size it wrote. The named sizes are square frames. A fit keeps the aspect ratio, so one side can come back short.

| Size | Frame |
| --- | --- |
| `XS` | 128 × 128 |
| `S`, `Thumbnail` | 256 × 256 |
| `M` | 512 × 512 |
| `L`, `Preview` | 1024 × 1024 |
| `XL` | 2048 × 2048 |
| `XXL` | 4096 × 4096 |

`Thumbnail` is `S`. `Preview` is `L`. Any other frame is `new ImageSize(width, height)`. A named size is only the frame. It does not change the fit, the canvas, or the metadata policy.

```csharp
var size = ImageTransforms.Resize(source, destination, ImageOutputFormat.Jpeg, ImageSize.Preview);
```

`new ImageResize(format, ImageSize.L)` sets that frame on the options object.

## Options

```csharp
ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.WebP)
{
    Width = 512,
    Height = 512,
    Canvas = ImageCanvas.SolidBlack,
    Encode = new ImageEncode
    {
        Metadata = MetadataPolicy.Strip,
        Quality = 80
    }
});
```

| Property | Default | Meaning |
| --- | --- | --- |
| `AutoOrient` | `true` | Apply orientation metadata before measuring and resizing. |
| `Metadata` | `PreserveColorProfileOnly` | See below. |
| `Quality` | see notes | `ImageEncoding.Jpeg` defaults to 80, `WebP` to 60, AVIF/HEIC to 85. Range 1-100. PNG has no quality. |
| `AlphaFallbackColor` | white | On JPEG encoding only. Used when remaining alpha cannot be stored. |

Pass `encode` only when a default should change.

`ImageTransforms.Resize` / `Rotate` compile into `ImagePipeline` and call `Run`. Prefer the pipeline when order matters (for example rotate, then resize) so stages share one native graph.

## Orientation

JPEG and HEIC often store the sensor raster sideways and put the display rotation in EXIF. With `AutoOrient` left on, a 4032x3024 file tagged as 90 degrees is treated as 3024x4032.

The rotation is libvips `Autorot` (via thumbnail's rotate, not a hand-rolled EXIF switch). After the pixels move, the orientation tag is reset, so a later viewer does not rotate the derivative again. A caller-requested turn is `ImageTransforms.Rotate`, and it runs after this step.

Set `AutoOrient` to false only when you mean the stored raster. Width and height then apply to that raster, and a preserved orientation tag is left as it was.

## Metadata

| Policy | EXIF / XMP / IPTC | ICC profile |
| --- | --- | --- |
| `Preserve` | kept | kept |
| `Strip` | removed | removed |
| `PreserveColorProfileOnly` | removed | kept |

`PreserveColorProfileOnly` is the default so a thumbnail does not publish camera or GPS tags by accident, and color still renders with the profile.

This is not an EXIF editor. There is no typed `CapturedAt` / `Latitude` facade in this version.

## Formats

| Format | Decode | Encode | Notes |
| --- | --- | --- | --- |
| JPEG | yes | yes | Remaining alpha is flattened onto `AlphaFallbackColor`. |
| PNG | yes | yes | Alpha kept. `Quality` is ignored. |
| WebP | yes | yes | |
| AVIF | when the runtime has an AV1 decoder | when it has an AV1 encoder | `NetVips.Native` includes both. |
| GIF | yes | yes | First frame only. |
| TIFF | yes | yes | First page only. |
| HEIC | when the runtime has an HEVC decoder | when it has an HEVC encoder | `NetVips.Native` has neither. |

Animated GIF and multi-page TIFF are reduced to the first frame on purpose. HEIF is not limited that way: a still HEIC can be stored as a tile grid, and keeping one tile would return a slice of the photo.

## Streaming

```text
source stream -> decode -> auto-orient -> ordered stages -> encode -> destination stream
```

Callers never have to copy the compressed input or the encoded output into a `byte[]`. The work stays in libvips. Thumbnail generation uses `thumbnail_source`, which can shrink JPEG and similar formats while decoding.

`RunAsync` adds pipes on both ends so async-only streams (ASP.NET bodies) never see sync `Read` / `Write`. The native graph still runs synchronously on a worker thread.

Some inputs are not streamed end to end. A center zoom that does not enlarge, and a zoom with a focus box, decode the oriented image once and then extract. Orientation and some codecs also cause libvips to buffer. The guarantee is only that the API does not force that buffer into managed arrays.

GIF and TIFF detection reads a few header bytes. On a non-seekable stream those bytes are replayed; the rest of the file is not copied.

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

`Encodes` is the matching question for `ImageOutputFormat`. JPEG, PNG, WebP, GIF, and TIFF come from the save operation. AVIF and HEIC follow the same check: a probe image must encode, and the container brand must be that codec. The probe runs the first time `Capabilities` is read, not on the first `Render`.

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

**Sharp** is the libvips API people already know (`contain`, `cover`, `fill`, gravity). It is Node. The resize here is the same set of outcomes: fit inside, zoom to the center or a focus box, or pad the gap.

## Behavior notes

- `netstandard2.1` is not a target. NetVips 3.2 targets `net6.0` and .NET Framework 4.6.2. Sibling ergonomics packages that still multi-target `netstandard2.1` cannot share this one.
- Downscale goes through libvips thumbnail, which applies a mild sharpen and resizes in the image's gamma. There is no linear-light switch in this version.
- A center zoom that may enlarge goes through libvips thumbnail. A center zoom that does not enlarge, an aspect-ratio zoom at the source size, and a zoom with a focus box decode the oriented image and then extract. Non-seekable streams take one decode.
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
