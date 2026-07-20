# Slice #9 — UI polish

> This slice applies the user's collected visual adjustments to the finished explorer UI. It is a
> living slice: additional adjustments may be added and applied in follow-up passes. This first pass
> covers the three items collected during slices 1–8.

## Context

Ninth slice of the [Resource Explorer build](./00-implementation-plan-list.md). See the
[design spec](./resource-explorer-design-v1.md). Depends on the built UI (slices 2–4, 7, 8). Pure
visual/UX polish — **no back-end, no new features**. Verified by driving the app.

**Key decisions (this pass):**

- **Dashed edges are seed-relative** (user decision): upstream edges (things that depend on the seed,
  via `seed.predecessors('edge')`) render **dashed**; downstream stay **solid**; recomputed on every
  graph change.
- **Thinner edges/arrows** (~half): edge `width` 2 → 1, add `arrow-scale: 0.5`.
- **Node labels 1.25rem.**
- **On-canvas zoom + pan controls** overlaid in a canvas corner (`cy.zoom`/`cy.panBy`).

---

## 1. Edge styling + node label size (`wwwroot\js\explorer\explorer-canvas.js`)

### 1a. Style tweaks in `init`

In the `style` array, change the `node` and `edge` entries and add an `edge.upstream` selector:

- In the `node` style, change the font size:
  ```javascript
                'font-size': '1.25rem',
  ```
  (was `'11px'`. If Cytoscape ignores `rem` at runtime, use `'20px'` — verify during the drive.)

- Replace the `edge` style block with the thinner version:
  ```javascript
            { selector: 'edge', style: {
                'width': 1,
                'line-color': '#94a3b8',
                'target-arrow-color': '#94a3b8',
                'target-arrow-shape': 'triangle',
                'arrow-scale': 0.5,
                'curve-style': 'bezier'
            }},
            { selector: 'edge.upstream', style: {
                'line-style': 'dashed'
            }},
  ```
  (Add the `edge.upstream` selector right after the `edge` selector; keep the existing `node.seed` /
  `node.expanded` selectors as-is.)

### 1b. Seed-relative dashed recompute

Add this helper and call it wherever the graph changes:

```javascript
// Dash "upstream" edges (things that depend on the seed). Recompute after any graph change.
function applyEdgeStyles() {
    cy.edges().removeClass('upstream');
    if (!seedId) return;
    const seed = cy.getElementById(seedId);
    if (seed.empty()) return;
    seed.predecessors('edge').addClass('upstream');   // edges leading INTO the seed = upstream
}
```

Call `applyEdgeStyles();` at the end of **`addGraph`**, **`loadJson`** (before its `return`),
**`collapse`**, **`collapseAll`**, and **`removeNode`** (right after each existing `applyLabels()` /
final mutation). It's cheap and idempotent.

- [ ] **Step 1:** Apply 1a + 1b.

---

## 2. Zoom / pan controls

### 2a. JS (`explorer-canvas.js`)

Add two exports (near `fit`):

```javascript
export function zoomBy(factor) {
    if (!cy) return;
    cy.zoom({ level: cy.zoom() * factor, renderedPosition: { x: cy.width() / 2, y: cy.height() / 2 } });
}

export function panByDir(dx, dy) {
    if (!cy) return;
    cy.panBy({ x: dx, y: dy });
}
```

### 2b. Overlay markup (`Components\Pages\ResourceExplorer.razor`)

Inside `.rm-explorer-host`, **after** the `<div class="rm-explorer-canvas" @ref="_canvasEl"></div>`,
add the nav overlay:

```razor
    <div class="rm-explorer-nav">
        <MudIconButton Icon="@Icons.Material.Outlined.Add" Size="Size.Small" Variant="Variant.Filled"
                       aria-label="Zoom in" OnClick="@(() => ZoomAsync(1.2))" />
        <MudIconButton Icon="@Icons.Material.Outlined.Remove" Size="Size.Small" Variant="Variant.Filled"
                       aria-label="Zoom out" OnClick="@(() => ZoomAsync(0.8))" />
        <div class="rm-explorer-dpad">
            <MudIconButton Icon="@Icons.Material.Outlined.KeyboardArrowUp" Size="Size.Small"
                           aria-label="Pan up" OnClick="@(() => PanAsync(0, 60))" />
            <div class="rm-explorer-dpad-mid">
                <MudIconButton Icon="@Icons.Material.Outlined.KeyboardArrowLeft" Size="Size.Small"
                               aria-label="Pan left" OnClick="@(() => PanAsync(60, 0))" />
                <MudIconButton Icon="@Icons.Material.Outlined.KeyboardArrowRight" Size="Size.Small"
                               aria-label="Pan right" OnClick="@(() => PanAsync(-60, 0))" />
            </div>
            <MudIconButton Icon="@Icons.Material.Outlined.KeyboardArrowDown" Size="Size.Small"
                           aria-label="Pan down" OnClick="@(() => PanAsync(0, -60))" />
        </div>
    </div>
```

### 2c. Handlers (beside `FitAsync`)

```csharp
    private async Task ZoomAsync(double factor) => await SafeInvokeAsync("zoomBy", factor);
    private async Task PanAsync(double dx, double dy) => await SafeInvokeAsync("panByDir", dx, dy);
```

> During the drive, confirm each pan arrow moves the view intuitively; if any is reversed, flip the
> sign of that button's `PanAsync` argument.

- [ ] **Step 2:** Apply 2a–2c.

---

## 3. CSS (`wwwroot\app.css`)

Make the host a positioning context and add the overlay styles:

```css
.rm-explorer-host {
    position: relative;
    display: flex;
    flex-direction: column;
    height: 100%;
}
.rm-explorer-nav {
    position: absolute;
    right: 16px;
    bottom: 16px;
    z-index: 5;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 4px;
    background: rgba(255, 255, 255, 0.85);
    border: 1px solid var(--mud-palette-lines-default, #e2e8f0);
    border-radius: 8px;
    padding: 4px;
}
.rm-explorer-dpad {
    display: flex;
    flex-direction: column;
    align-items: center;
}
.rm-explorer-dpad-mid {
    display: flex;
}
```

(If `.rm-explorer-host` already sets `display:flex; flex-direction:column; height:100%` from an
earlier slice, just add `position: relative;` to it rather than duplicating.)

- [ ] **Step 3:** Apply the CSS.

---

## UI verification hook (visible slice)

- [ ] **Step 4: Build + run**, drive at `/explore/DEMOEXP-checkout` (expand upstream + downstream):
  1. **Edges into the seed** (its dependents / upstream chain) render **dashed**; edges the seed
     **depends on** (downstream) stay **solid**; arrows/edges are noticeably **thinner**. Expanding/
     removing nodes keeps the dashing correct (recomputed).
  2. **Node labels** are visibly larger (~1.25rem / 20px).
  3. The **zoom `＋`/`－`** buttons zoom about the canvas center; the **4-way pad** pans; each arrow
     moves the view the intuitive direction (fix signs if not).
- [ ] **Step 5 (optional): Playwright** — assert `.rm-explorer-nav` renders, a zoom-in click raises
  `cy.zoom()`, and at least one edge has the `upstream` class when upstream nodes are present.

---

## Verification

1. `dotnet build` clean (sqlproj `MSB4278` aside); `dotnet test` unchanged (no new unit tests).
2. Manual browser drive per Step 4 — the gate.

---

## Out of scope

- **Further adjustments** the user adds later → appended here and applied in another pass.
- **Client settings migration** → slice 10.

---

## Execution notes

Executed 2026-07-19/20. All three changes applied verbatim per the plan, with one deviation:

- **Label unit:** `1.25rem` was tried first and confirmed broken — Cytoscape's style parser doesn't
  recognize `rem`, drops the unit, and treats the bare number as raw pixels (`node.pstyle('font-size')`
  resolved to `pfValue: 1.25`, i.e. ~1.25px — smaller than the original 11px, not bigger). Switched to
  `'20px'`, confirmed resolving correctly (`pfValue: 20`) and visibly much larger in the browser.
- **Pan signs:** all four verified correct as originally specified — no flips needed.
  `PanAsync(0, 60)`=up, `PanAsync(0, -60)`=down, `PanAsync(60, 0)`=left, `PanAsync(-60, 0)`=right.
  Confirmed via `cy.pan()` before/after values plus before/after screenshots.
- **Dashed edges on the demo cycle:** correct on initial load (upstream dashed / downstream solid).
  After fully expanding the demo's built-in cycle (`checkout → pricing → inventory → checkout`), the
  direct `checkout→pricing` edge — originally solid/downstream — flips to dashed/upstream. This is an
  inherent property of `seed.predecessors('edge')` (a full reverse-reachability traversal): once the
  cycle closes, the seed becomes its own indirect predecessor through the loop, so the traversal
  legitimately includes the direct downstream edge as part of a reverse path back to the seed. Not a
  wiring bug — `applyEdgeStyles()` recomputes correctly on every graph change, verified via debug
  instrumentation of `cy.edges()` at each expand step. Flagging for team awareness since it's a visible
  quirk on this specific demo graph; no change made since it matches the plan's decided algorithm.
- Zoom-in appeared to do nothing on a first click from a fresh demo load — the small seed graph's
  initial `fit()` already sits at the pre-existing `maxZoom: 3` cap (unrelated, set in an earlier
  slice), so there's nowhere to zoom further in until zoomed out first. Not a regression.

`dotnet build` clean (sqlproj MSB4278 aside); `dotnet test` 410 total / 408 passed / 2 failed (the 2
pre-existing unrelated `HT.Api.Service.Contracts.Tests` failures). Browser-driven via Playwright
against `/explore/DEMOEXP-checkout` (demo data already seeded in localdb).

Slice #9 (pass 1) done. Slice #10 (client settings migration) next.
