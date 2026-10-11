# Pipeline fork — multiple outputs from one decode

Status: **open** (post-MVP candidate; do not implement from the sketch API).  
Severity: medium — real upload → preview + thumbnail pain; workable app workarounds exist today.

## Problem

Callers often need several derivatives from one upload (e.g. `Preview` WebP + `Thumbnail` WebP). Today each `Run` / `RunAsync` is one destination and one encode.

Pain points:

- **Forward-only sources** cannot be rewound. Two runs require the app to buffer the whole encoded source first.
- **Double decode** costs CPU even when the source is seekable.
- **Shrink-on-load** cannot see both targets if each run is independent; the shared largest size is only knowable when branches are planned together.
- Branching from another branch’s **encoded** bytes would re-compress; branches should start from uncompressed pixels at a fork point.

## Requirements (if built)

1. Read the source **exactly once** (sequential / forward-only streams work).
2. Auto-orient (and source-alpha admission) **once**, before the fork.
3. Choose shrink-on-load from the **shared** stages / largest planned output across branches (same spirit as today’s single-run shrink choice).
4. Materialize at the fork point (`CopyMemory` or equivalent). A sequential decode cannot feed two independent graphs. A 1024×1024 RGB frame is ~3MB; that is acceptable internal cost, not a public `byte[]` API.
5. Each branch resizes from the **fork-point image**, never from another branch’s encode.
6. Each branch has its own stages (after the fork), encoding, and destination stream.
7. Return the written `ImageSize` **per branch**, same contract as single-output `Run` / `RunAsync`.
8. Cancellation and `ImageException` behavior should match single-output runs (define partial-write rules explicitly — see open questions).

## Charter fit

| Aspect | Fit |
| --- | --- |
| First product use case (upload → derivatives) | Strong |
| Stream in / stream out; no public `byte[]` | Strong if materialize stays internal |
| App owns HTTP, cache, object storage, persistence | Strong — library only fans out transforms |
| Small public surface / not a filter graph | Risk — fork + branches grow the model |
| “One lazy NetVips graph with a **single encode**” | Conflicts as written — fork is N encodes; charter text would need an explicit carve-out |
| Pipeline as reusable fluent **spec**, streams at `Run` | Conflicts with baking destinations into `.Fork(...)` before run |

Charter already says caching and thumbnail **persistence** stay in the application. Multi-derivative **orchestration** is adjacent but still transform-shaped: one decode, N encodes, N destinations. That belongs in this package only if the API stays as simple as today’s linear pipeline.

## Sketch that should not ship

Stale fluent sketch (also uses pre-settlement `Fit` / floating modifiers):

```csharp
var sizes = await ImagePipeline.Create()
    .Resize(ImageSize.Preview) // shared stages…
    .Fork(
        (b => b.EncodingWebP(85), previewStream),
        (b => b.Resize(ImageSize.Thumbnail).EncodingWebP(85), thumbnailStream))
    .RunAsync(source, cancellationToken); // IReadOnlyList<ImageSize>
```

Problems:

- Destinations inside the fluent chain break “spec then run” and reuse.
- Return type / terminal method diverge from `Run(source, destination)`.
- Easy to slide into a mini orchestration framework.

## Preferred shape direction (not decided)

Keep shared work as an ordinary pipeline fragment; keep branches as ordinary pipeline specs; bind streams only at the multi-run call site. Illustrative only:

```csharp
var shared = ImagePipeline.Create()
    .Resize(ImageSize.Preview, ResizeMode.Pad, r => r.Enlarge(false));

var preview = shared.EncodingWebP(85);
var thumbnail = shared
    .Resize(ImageSize.Thumbnail, ResizeMode.Pad, r => r.Enlarge(false))
    .EncodingWebP(85);

// Hypothetical — name and exact API TBD
var sizes = await ImagePipeline.RunManyAsync(
    source,
    (preview, previewStream),
    (thumbnail, thumbnailStream),
    cancellationToken);
```

Open design questions for that direction: how “shared prefix” is declared vs inferred; whether `RunMany` requires an explicit fork builder; whether branch pipelines must share a common prefix object or only compatible admission options.

## App workarounds today (no library change)

1. If the source is seekable: two `Run` / `RunAsync` calls (double decode; shrink-on-load per run).
2. If not: copy the encoded source to a rewindable buffer once, then two runs.
3. Accept recompression only if deliberately chaining encode → decode (not recommended).

These are enough for MVP. The library feature is an ergonomic + efficiency upgrade, not a blocker.

## Options

1. **Defer** — document workarounds; leave charter “single encode” as-is until a real call site needs one-decode multi-output.
2. **`RunMany` / branch tuples at the call site** — preferred direction above; smallest conceptual add if shared-prefix rules stay strict.
3. **Explicit `.Fork` on the fluent builder** — only if destinations stay out of the builder and the result is still a reusable multi-spec. Higher API novelty.
4. **Expose a decoded/materialized handle** — app runs N pipelines from it. Likely fights “no `byte[]` / no framework” and leaks engine lifetime; avoid unless forced.

## Recommendation

- Capture as **post-MVP**. Do not implement from the sketch.
- If revisited: amend the charter line from “single encode” to “one decode; one encode per destination (linear run or forked run).”
- Prefer call-site stream binding (`RunMany`) over destinations inside fluent `Fork`.
- Decide failure semantics before coding (fail-fast after first branch error vs best-effort all branches + aggregate exception).
- Keep materialization internal; do not add a public decoded-buffer API.

## Open questions

- Partial failure: branch 1 wrote bytes, branch 2 throws — leave partial output, or document “caller deletes” only?
- Must every branch declare encoding, or can a branch inherit a shared encoding?
- Empty fork / single branch — allow (degenerate) or require ≥ 2?
- Shared stages after a conceptual fork — forbidden (recommended) or supported?
- Shrink-on-load when branches disagree on geometry that is not a simple “largest box” (rotate + crop combos).
- Sync `RunMany` vs `RunManyAsync` parity with today’s `Run` / `RunAsync` rules (no fake async around native work).

## Decision log

| Date | Decision |
| --- | --- |
| 2026-10-10 | Documented from design review. Worth considering later; sketch `Fork(...streams...)` rejected; MVP keeps single-destination `Run` / `RunAsync`. |
