# M4 — Focus `BackOff` can abandon the frame aspect

Status: **open** (document only).  
Severity: medium — only when the focus rectangle is larger than any cover window of the requested frame ratio inside the source.

## Problem

On `ResizeMode.Crop` with `ImageFocus`, `FocusMath` builds a cover window at the frame’s width/height ratio, then shifts it so the focus stays inside.

If the focus span is larger than that cover window (`UnderfillsFrame`), `BackOff` grows the window to include the focus. When that grown window cannot fit in the source **while keeping** `widthOverHeight`, width/height are clamped to the source. The crop window’s aspect can then diverge from the frame ratio.

`Finish` still letterboxes/paints to the requested frame size when underfilling, so **output dimensions stay correct**. What changes is which source pixels are kept: not a pure cover crop of the frame aspect.

## When it shows up

- Focus box is most or all of a source whose aspect differs strongly from the frame.
- Existing tests such as whole-image focus + canvas already exercise the underfill path; aspect abandonment is the clamp edge inside `BackOff`.

## Charter fit

Ergonomics: document the behavior (focus wins over strict cover aspect when the box cannot fit). Silent “fix” by shrinking focus would surprise callers who asked to keep that rectangle.

## Options

1. **Document as intentional** — when focus cannot fit a cover window, the crop may not preserve frame aspect; canvas paints the frame. Matches “focus stops the crop.”
2. **Clamp focus to a max cover window** — never abandon aspect; may clip the requested focus (change of contract).
3. **Fail fast** — throw if backoff would break aspect. Honest, harsher API.
4. **Expose a flag** — `FocusStrictAspect` vs current backoff (extra knob; defer).

## Recommendation

Prefer option 1 unless product feedback wants fail-fast or strict aspect. Add a README / `ImageFocus` remarks sentence when documenting; no engine change required for MVP.

## Decision log

| Date | Decision |
| --- | --- |
| 2026-10-10 | Documented; no code change. |
