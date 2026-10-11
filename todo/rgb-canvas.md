# RGB canvas assumption

Status: **enforced at `PlaceOnCanvas`** (throws `ImageException` when color bands ≠ 3).  
Charter: ergonomics, not magic — no silent colorspace conversion on the primary path.

## What the primary path is

`ImageColor` is always three 8-bit channels (R, G, B). Solid and checkerboard plates are built as **three-band RGB** rasters. Content is composited `Over` that plate.

That is the whole model:

```text
RGB(A) image  →  Over  →  RGB plate (ImageColor / checkerboard)
```

The library does **not** invent a colorspace pipeline. If the source is not already three color bands (optional alpha), placing it on a canvas is undefined for this API — and now fails clearly.

## What goes wrong without the check

This is a **band-layout** problem, not an ICC/profile subtlety.

| Source layout | Color bands | Plate | Result without a check |
| --- | --- | --- | --- |
| RGB / sRGB JPEG, PNG, WebP | 3 (+ optional α) | 3-band RGB | Works (primary path) |
| Greyscale PNG/TIFF | 1 (+ optional α) | 3-band RGB | Band mismatch / wrong packing into flatten or `Composite2` |
| Other multi-band (e.g. CMYK TIFF) | ≠ 3 | 3-band RGB | Same class of mismatch |

CMYK and exotic layouts are out of scope for this package’s job (web/app still-image transforms). Greyscale is the realistic non-RGB case. Neither gets a silent convert-to-RGB on load.

## What we enforce today

In `ImageEngine.PlaceOnCanvas`, before building the plate:

- Count color bands (`Bands`, or `Bands - 1` when alpha is present).
- If that count is not `3`, throw `ImageException` with a message that says the plate is RGB and the caller should convert first or omit the canvas.

Early exits that never paint a plate (same size, transparent canvas, no content alpha, etc.) do not run this check — no plate, no RGB requirement.

Documented on `ImageCanvas` remarks and in `README.md` (resize/canvas section).

## Related paths (not enforced the same way yet)

These also assume three-component `ImageColor` backgrounds but are separate knobs:

| Path | When | Options (see below) |
| --- | --- | --- |
| `SourceAlpha` flatten | Before geometry | Same RGB assumption if we ever tighten |
| JPEG `AlphaFallbackColor` | Encode | Same |
| Free-angle rotate corner fill | Expand/Trim invent corners | Uses per-band packing from `ImageColor`; canvas underplate still goes through `PlaceOnCanvas` |

Do not paper over these with load-time `Colourspace(Srgb)`. That would be magic on every run.

## Options consistent with the charter

Ordered from most ergonomic / least magic to most capability.

### 1. Keep primary path RGB-only (current)

**Behavior:** Canvas requires RGB(A). Throw clearly otherwise.

**Caller story:** Most phone/web sources are already RGB. Greyscale scans either omit the canvas (pad/crop without a plate) or convert upstream before calling this library.

**Pros:** Honest; matches `ImageColor`; no hidden work.  
**Cons:** Greyscale + letterbox color needs an extra step outside (or later, option 3).

### 2. Document only (rejected)

Leaving wrong packing as “best effort” failed the ergonomics bar: surprises and native errors are worse than a clear throw.

### 3. Explicit convert-to-RGB step (future, optional)

Add a **named, caller-visible** admission or stage — not an invisible default — for example:

- `Create(s => s.… .ToRgb())` / source bag flag, or
- a small `ImageTransforms` / pipeline helper that converts then runs the usual pipeline.

Semantics would be explicit: “promote/convert this source to three-band RGB so an RGB canvas can apply.” Implementation can use libvips colourspace under that name; the **path** stays visible in the fluent call.

**Pros:** Greyscale (and similar) get a supported on-ramp without changing the default path.  
**Cons:** Another knob; must stay opt-in.

### 4. Greyscale-aware plates (out of MVP)

Build 1-band plates from luma of `ImageColor`, or special-case grey composites.

**Pros:** Greyscale letterbox without convert.  
**Cons:** Two plate models; `ImageColor` is still an RGB API; easy to get wrong with alpha and checkerboard. Not worth it for MVP.

### 5. Silent convert on every load (rejected)

Auto `Colourspace` to sRGB before geometry for all sources.

**Pros:** Canvas “always works.”  
**Cons:** Magic; obscures the real path; changes grey/CMYK outputs without asking. Conflicts with the charter.

## Recommendation

- **Now:** Option 1 (done) — document + throw in `PlaceOnCanvas`.
- **Later, if greyscale + canvas shows up in real call sites:** Option 3 — explicit `ToRgb` (or equivalent) on Create / a shortcut, still defaulting to “already RGB.”
- **Never for this package:** Option 5.

## Test coverage

- RGB + canvas: existing pad/crop/rotate canvas tests.
- Non-RGB + canvas: reject with message mentioning RGB / color bands (`PadCanvasTests` or equivalent).

## Decision log

| Date | Decision |
| --- | --- |
| 2026-10-10 | Enforce RGB at `PlaceOnCanvas`; no silent colourspace convert; todo options recorded here. |
