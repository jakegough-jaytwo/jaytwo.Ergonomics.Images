# Repo Instructions

## Product charter

Ergonomic still-image transforms on NetVips. It is not an image-processing framework. See `README.md`.

- The public API is `ImageTransforms.Resize` and `ImageTransforms.Rotate`. The positional resize overload fits inside a box and returns the written size. Omit the size to keep the source size. `ImageResize` sets `ImageFit` (`Fit`, `Zoom`, `Stretch`), an optional `ImageAspectRatio`, an optional `ImageCanvas` (`SolidBlack`, `SolidWhite`, `Transparent`, or `Solid(ImageColor)`), an optional `ImageFocus`, and `SourceAlpha` (`Preserve`, `Flatten`, `Checkerboard`). A canvas applies with `Fit` and is not part of the mode. On `Zoom` it is only the gap left when `ImageFocus` does not fit in the cover window. Encode overrides go in `ImageEncode`. Do not add a general filter graph, text, watermarks, face/object detection, or a standalone blur. Reserve `Crop` for an explicit source rectangle. Edge-pixel extension, if it returns, is a canvas, not a fit mode.
- `ImageSize` is a width and height. `XS` is 180×180, `S` 360×360, `M` 720×720, `L` 1440×1440, `XL` 2880×2880. `Thumbnail` is `S`. `Preview` is `M`. A named size is only the frame. It does not change fit, canvas, metadata, or encode.
- `ImageRotation.Clockwise` and `CounterClockwise` take a non-negative angle in degrees. `Clockwise(90)` is the exact quarter turn; there is no separate quarter-turn property. `RotationBounds` defaults to `Expand`, the smallest axis-aligned rectangle around the rotated source. New corners use `ImageCanvas` and default to transparent. `Trim` is the largest centered axis-aligned rectangle inside the rotated source. An `ImageAspectRatio` on a trim is that rectangle, not a second fit. Manual rotation runs after auto-orient and clears the orientation tag. Do not add a pipeline type, a deskew detector, or a focus box on the trim.
- `ImageFocus` applies with `Zoom`. It is a relative rectangle on the upright image, origin at the top left, each edge from 0 to 1. The cover window keeps the frame's aspect and shifts so the rectangle stays inside. Omit it and the center is kept. `ImageFocus.Point` is a zero-area rectangle and only anchors that window. When the rectangle does not fit in the cover window, cropping stops at the rectangle and the leftover frame is `Canvas`, transparent when omitted.
- The MVP is this package alone. Do not add a companion package, a blurred canvas, or other content-generated operations.
- Leave a public identify method for a later pass. Canvas, source alpha, and `ImageEncode.AlphaFallbackColor` stay separate. The fallback is used only when the output format cannot store alpha. A canvas does not replace it.
- Geometry is in visual pixels. Auto-orientation is on unless the caller turns it off.
- Upscale follows `enlarge`, which defaults to true. `ImageFit.Stretch` always writes the requested size.
- The default metadata policy is `PreserveColorProfileOnly`.
- Stream in, stream out. Do not add APIs that require `byte[]`.
- Do not take a dependency on `NetVips.Native` from the library project. Tests may, behind `UseBundledLibVips`.
- Do not redistribute an HEVC encoder or decoder. HEIC works only when the host libvips/libheif stack has one.
- No async wrappers around libvips. The native pipeline is synchronous.
- Caching, HTTP, object storage, and thumbnail persistence stay in the application.

## Code style

- File-scoped namespaces. Private fields use an underscore prefix.
- Public surface stays small. Implementation types are `internal`.
- Match the sibling repos: StyleCop warnings are errors, nullable is on, XML docs on the public API.
