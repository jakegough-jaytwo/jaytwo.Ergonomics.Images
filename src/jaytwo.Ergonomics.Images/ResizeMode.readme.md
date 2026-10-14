# ResizeMode naming

How this library names fit behaviors, what peers do, and why the public enum is
**Fit / Pad / Crop / Stretch**.

Public API: [`ResizeMode.cs`](ResizeMode.cs). Product rules: [`AGENTS.md`](../../../AGENTS.md), [`README.md`](../../../README.md).

## The four behaviors

| Intent | Output size (both W and H set) | Aspect |
| --- | --- | --- |
| Scale inside the box | At most the frame (one side may be short) | Kept |
| Letterbox to the frame | Always exact W×H | Kept |
| Cover the frame (discard overflow) | Always exact W×H | Kept |
| Stretch to the frame | Always exact W×H | Ignored |

**Rule:** mode picks size policy. `ImageCanvas` is paint only (bars / underfill). Setting a canvas color must not change whether the result is 100×50 or 100×100.

## What we ship

| `ResizeMode` | Behavior | Sharp peer | Typical CDN peer |
| --- | --- | --- | --- |
| `Fit` | ≤ frame; no canvas | `inside` | Cloudinary `c_fit`, imgix `clip`, Cloudflare `contain` / `scale-down`, imgproxy `fit` |
| `Pad` | Exact frame; letterbox; canvas defaults transparent | `contain` | Cloudinary `c_pad`, Cloudflare `pad`, ImageSharp `Pad` |
| `Crop` | Exact frame; cover; focus optional | `cover` | imgix `fit=crop`, ImageSharp `Crop`, Cloudinary `c_fill` |
| `Stretch` | Exact frame; ignore aspect | `fill` | imgproxy `force`, ImageSharp `Stretch`, Cloudinary `c_scale` |

Default mode is **`Fit`** (short `ImageTransforms.Resize(w, h)` and presets). Extract stage `.Crop(x, y, w, h)` is unrelated: it cuts a relative rectangle; it is not `ResizeMode.Crop`.

## Why not Magick’s compose model

ImageMagick / GraphicsMagick / Magick.NET do **not** ship a three-value resize enum.

| Outcome | Magick |
| --- | --- |
| Fit inside | Default `-resize WxH` |
| Stretch | `-resize WxH!` / `IgnoreAspectRatio` |
| Cover | `-resize WxH^` then crop / extent |
| Letterbox | `-resize` **then** `-extent` (second op) |

Letterbox is composition (`Extent`), not a resize mode. An earlier design mirrored that by making “Pad” mean fit-inside and treating canvas as the silent second step. That made **output size depend on whether a canvas was set** — the bug we refused to keep.

ffmpeg and libvips `vipsthumbnail` follow the same split: fit / force / crop-to-fill are resize flags; pad/embed is a separate operator.

Going Magick-pure (`IgnoreAspectRatio` + an `Extent` pipeline stage) would still need a first-class cover story and would push letterbox into two stages. This package’s charter is one ordered fluent resize with a clear mode.

## Why not Sharp/CSS names alone

Sharp’s `fit` is the cleanest *size-policy* enum in the libvips world:

| Sharp | Size | Our name |
| --- | --- | --- |
| `inside` | ≤ frame | Fit |
| `contain` | Exact letterbox | Pad |
| `cover` | Exact cover | Crop |
| `fill` | Stretch | Stretch |

CSS `object-fit` is close (`contain` / `cover` / `fill`) but has no `inside`. Sharp invented `inside` / `outside` for that gap.

We did **not** adopt Sharp’s spellings as the public enum because the same English words mean opposite things across vendors:

| Word | Letterbox | Cover | Stretch |
| --- | --- | --- | --- |
| **fill** | imgix `fit=fill` | Cloudinary / imgproxy `fill` | Sharp / AWS `fill` |
| **contain** | Sharp / AWS (exact letterbox) | — | Cloudflare `contain` = fit inside (≤ frame) |

Backend engineers who “know Magick” also do not get Magick’s pad/crop/distort labels for free — Magick never used that trio as a resize enum. ImageSharp’s `Pad` already means letterbox (exact), which collided with our old Magick-ish `Pad` (fit inside).

## Industry snapshot (research)

Surveyed: ImageMagick, GraphicsMagick, libvips/`vipsthumbnail`, ffmpeg, netpbm; Cloudinary, imgix, Cloudflare Images, Thumbor, imgproxy, AWS Dynamic Image Transformation (Sharp), ImageKit, Uploadcare, Fastly IO, Akamai IVM; Sharp, Magick.NET, ImageSharp, Pillow, pyvips, Go imaging/bimg/govips, Thumbnailator, Intervention Image.

Patterns:

1. **Compose tools** (Magick, ffmpeg, libvips thumbnail): fit-inside is default resize; letterbox is pad/extent/embed.
2. **Named-fit APIs** (Sharp, ImageSharp, Pillow ImageOps, Cloudflare, Cloudinary, AWS SIH): fit-inside vs letterbox are different modes; background is a color param.
3. **`fill` is poisoned** — never use it as a public mode name without a peer table.
4. **Unambiguous labels** across the survey: **fit**, **pad**, **crop** (as cover), **stretch** / **force**.

## How we settled

1. Split fit-inside from letterbox so canvas cannot change output dimensions (H3).
2. Prefer Sharp’s four-way *size policy*, not Magick’s two-step compose.
3. Avoid `fill` and `contain` as public names because CDN/Sharp disagree on them.
4. Choose **Fit / Pad / Crop / Stretch**:
   - Fit — common “fit in the box” language
   - Pad — letterbox everywhere that matters (Cloudinary, Cloudflare, ImageSharp)
   - Crop — “crop the image to fill the frame” (cover); extract `.Crop` stays a different API
   - Stretch — unambiguous force-size (vs Sharp’s stretch named `fill`)

Rejected alternatives:

- Magick `Pad` / `Crop` / `Distort` only — canvas as size switch; no clear ≤-frame name
- Sharp `Inside` / `Contain` / `Cover` / `Fill` — clearest peer map, but `Contain`/`Fill` collide with CDNs
- `Fit` / `Pad` / `Fill` / `Stretch` — `Fill` still collides (Sharp stretch vs Cloudinary cover)
- `IgnoreAspectRatio` + `Extent` stage — Magick-faithful, worse ergonomics, cover still missing

## Crop vs `.Crop`

`ResizeMode.Crop` covers the frame (may discard edges).  
`ImagePipeline.Crop` / `ImageTransforms.Crop` / `ImageCrop` extracts an explicit relative rectangle.

Same English word, intentional. Docs cross-link; no `Extract` rename for MVP.

## Changelog note (pre-1.0)

| Before | After |
| --- | --- |
| `Pad` = fit inside; canvas made it letterbox | `Fit` = fit inside; `Pad` = always letterbox |
| `Crop` = cover; underfill could be smaller than the frame | `Crop` = cover; both dims → always the frame |
| `Distort` | `Stretch` |

Default short resize and presets remain fit-inside (`Fit`).
