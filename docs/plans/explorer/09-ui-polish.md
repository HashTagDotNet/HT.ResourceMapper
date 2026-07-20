# Slice #9 — UI polish (collected adjustments)

> **Status: backlog / collecting.** This is a running list of the user's UI adjustments, captured as
> they arrive during earlier slices. It becomes a full TDD-style slice plan **when slice 9 starts**
> (after the explorer's UI surfaces exist). Applied holistically then — **not** ad-hoc mid-slice.
> Depends on slices 2, 3, 4, 7, 8.

## Collected adjustments

Each item notes the current state and any nuance to resolve at implementation time.

1. **Distinguish "depends-on" vs "dependent-on" edges — make one direction dashed.**
   - *Current:* every edge is a solid `#94a3b8` line, `source → target` = dependent → dependency; there is no per-direction visual distinction (edges carry only `{source, target}`, no direction/role tag).
   - *Nuance to resolve at polish time:* an edge's "depends-on vs dependent-on" role is **relative to the viewing node** (edge A→B is "depends-on" from A's side, "dependent-on" from B's side), so "one dashed" needs a fixed reference — likely relative to the **seed** (or currently-selected node): style out-edges (away from seed) solid and in-edges (toward seed) dashed, or similar. Confirm the intended reference before implementing; may require tagging edges with a role at add-time.

2. **Make arrows/edges thinner — they're overbearing (~50% smaller).**
   - *Current:* `edge` style `width: 2`, `target-arrow-shape: triangle` at default `arrow-scale` (1).
   - *Target:* roughly halve visual weight — e.g. `width: 1`, `arrow-scale: 0.5` (tune to taste).

3. **Node label size → 1.25rem.**
   - *Current:* node `font-size: '11px'`.
   - *Target:* `1.25rem` (≈ 20px at a 16px root). Cytoscape `font-size` typically wants px/em; if `rem` isn't honored, use the px equivalent (`20px`) or `em`.

4. **On-canvas navigation controls — zoom in/out + pan up/down/left/right.**
   - *Current:* pan is mouse-drag, zoom is wheel, and Fit is a toolbar button; there are **no on-canvas buttons** for zoom or directional pan.
   - *Target:* explicit controls (e.g. a `＋`/`－` zoom pair and a 4-way pan D-pad, likely an overlay in a canvas corner) driving `cy.zoom()`/`cy.pan()`/`cy.panBy()`. **Placement decided: slice 9 (this polish slice).**

## How this list is maintained

- New adjustments the user raises before slice 9 are appended here (numbered), with current-state +
  nuance notes, so nothing is lost and each has enough context to implement later.
- When slice 9 starts, this becomes the full plan: concrete before/after code per item (mostly in
  `explorer-canvas.js` style rules + `app.css`), a browser-drive verification per item, and the usual
  Context / Out-of-scope / Execution-notes sections.
