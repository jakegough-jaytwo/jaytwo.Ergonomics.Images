# M2 — `VipsStreamSource` SeekOrigin.End is not slice-relative

Status: **open** (document only).  
Severity: medium — only matters for seekable streams opened mid-file with trailing bytes after the image.

## Problem

`VipsStreamSource` records `_start = stream.Position` when constructed and maps seeks into the caller’s stream:

| Origin | Behavior today |
| --- | --- |
| `Begin` | `_start + offset` (correct — virtualized to the image slice) |
| `Current` | relative on the underlying stream (OK if already positioned in-slice) |
| `End` | `_stream.Seek(offset, SeekOrigin.End)` — **end of the whole stream**, not end of the image payload |

If a seekable stream is a concatenation (image bytes followed by more data) or the caller positioned at an image that is not flush with `Length`, libvips seeking to “end” can read past the image.

Typical call sites (dedicated file/stream of one image from position 0) are fine.

## Charter fit

Stream in / stream out, no magic. Fixing this is a small correctness patch to the seek adapter, not a new API.

## Options

1. **Virtualize End** — treat End as relative to a known image length, or to `_start`…`Length` only when the payload is known to fill the remainder of the stream. Hard without knowing encoded size up front.
2. **End → fail / unsupported** — return `-1` for `SeekOrigin.End` so libvips falls back to other strategies (if it can). May break some loaders that need End.
3. **Document** — seekable sources should be one image from `_start` to EOF (current practical contract). Reject or ignore mid-file concat.
4. **Prefix / length-limited stream** — wrap with a stream that reports `Length = imageBytes` when the app knows the size (HTTP Content-Length, etc.). App-level, not library magic.

## Recommendation

Leave until a real mid-file / concat call site appears. Prefer option 3 for docs; if we fix in-library, probe whether libvips needs End for WebP/TIFF on seekable sources before choosing 1 vs 2.

## Decision log

| Date | Decision |
| --- | --- |
| 2026-10-10 | Documented; no code change. |
