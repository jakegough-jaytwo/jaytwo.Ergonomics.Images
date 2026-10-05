# Repo Instructions

## Product charter

Ergonomic still-image transforms on NetVips. It is not an image-processing framework. See `README.md`.

- The primary public API is `ImagePipeline`: a reusable ordered fluent specification ending in sync `Run(source, destination)` or `RunAsync` for streams that need async I/O. Auto-orient is a default preamble. Stages preserve caller order and compile into one lazy NetVips graph with a single encode. `ImageTransforms.Resize` and `ImageTransforms.Rotate` remain thin shortcuts that compile into a pipeline. The positional resize overload fits inside a box and returns the written size. Omit the size to keep the source size.
- Fluent resize uses `.Resize(...)` for the frame and `.Fit(ImageFit)` for `Fit`, `Zoom`, or `Stretch`. Optional `.Aspect`, `.Canvas`, `.Focus`, and `.Enlarge` adjust the preceding resize. Canvas applies with `Fit` and is not part of the mode. On `Zoom` it is only the gap left when `ImageFocus` does not fit in the cover window. `.SourceAlpha` is a policy (last call wins; chain position does not change timing). Output encoding is `.Encoding(ImageEncoding)` where each format type only exposes valid options (quality on JPEG/WebP/AVIF/HEIC; not on PNG/GIF/TIFF). Shortcuts such as `.EncodingWebP(quality)` are allowed. `ImageEncode` remains for `ImageTransforms` shortcuts only. Do not add a general filter graph, text, watermarks, face/object detection, or a standalone blur. Reserve `Crop` for an explicit source rectangle. Edge-pixel extension, if it returns, is a canvas, not a fit mode.
- `ImageSize` is a width and height. `XS` is 128×128, `S` 256×256, `M` 512×512, `L` 1024×1024, `XL` 2048×2048, `XXL` 4096×4096. `Thumbnail` is `S`. `Preview` is `L`. A named size is only the frame. It does not change fit, canvas, metadata, or encode. `ImagePresets.Thumbnail` / `Preview` are pipelines built from the same primitives.
- `ImageRotation.Clockwise` and `CounterClockwise` take a non-negative angle in degrees. `Clockwise(90)` is the exact quarter turn; there is no separate quarter-turn property. `RotationBounds` defaults to `Expand`, the smallest axis-aligned rectangle around the rotated source. New corners use `ImageCanvas` and default to transparent. `Trim` is the largest centered axis-aligned rectangle inside the rotated source. An `ImageAspectRatio` on a trim is that rectangle, not a second fit. Manual rotation runs after auto-orient and clears the orientation tag. Do not add a deskew detector or a focus box on the trim. Do not expose a public stage/plugin model for external analysis in MVP.
- The MVP is this package alone. Do not add a companion package, a blurred canvas, or other content-generated operations.
- Leave a public identify method for a later pass. Canvas, source alpha, and encode alpha fallback stay separate. `AlphaFallbackColor` lives on encodings that cannot store alpha (`ImageEncoding.Jpeg`); it is used only when the output format cannot store remaining alpha. A canvas does not replace it.
- Geometry is in visual pixels. Auto-orientation is on unless the caller turns it off.
- Upscale follows `enlarge`, which defaults to true. `ImageFit.Stretch` always writes the requested size.
- The default metadata policy is `PreserveColorProfileOnly`.
- Stream in, stream out. Do not add APIs that require `byte[]`.
- Do not take a dependency on `NetVips.Native` from the library project. Tests may, behind `UseBundledLibVips`.
- Do not redistribute an HEVC encoder or decoder. HEIC works only when the host libvips/libheif stack has one.
- No fake async around libvips. Native encode stays synchronous. `RunAsync` bridges async source/destination streams with pipes and wires `CancellationToken` to NetVips kill; prefer sync `Run` when both streams allow sync I/O.
- Caching, HTTP, object storage, and thumbnail persistence stay in the application.

## Code style

- File-scoped namespaces. Private fields use an underscore prefix.
- Public surface stays small. Implementation types are `internal`.
- Match the sibling repos: StyleCop warnings are errors, nullable is on, XML docs on the public API.
