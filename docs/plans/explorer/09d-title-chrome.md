# Slice #9d — Title + report-date overlay (composited into exports)

## Context

Final sub-slice of [UI-polish pass 2](./09-ui-polish.md). Adds the **floating title + report-date
overlay** (top-left over the canvas) and — the key requirement — **composites it into PNG/SVG/print
exports** so the exported picture is self-documenting. `wwwroot`/Razor only. Depends on 9c.

> **Build note:** stop any running `ResourceMapper.UI.Web` (it locks build output) before `dotnet build`/`run`.

**Key decisions:**
- **On-screen overlay** is a small div the JS module creates in the canvas host (top-left), showing
  the **title** (diagram name, or seed name if unsaved) + a **subtitle** (report date + live resource
  count). Rendered by the module so the same strings drive the export.
- **Export compositing:** Cytoscape's `cy.png/cy.svg` capture the graph only, so the export functions
  (`copyPng`/`savePng`/`saveSvg`/`printDiagram`) draw the title/subtitle into a **header band** on the
  exported image (PNG: composite onto a canvas; SVG: prepend header + shift content). The **acceptance
  check** is that the exported PNG and SVG actually contain the title text.
- Title strings are pushed from C# via `setTitle(title, subtitle)`; JS appends the live node count.

---

## 1. `explorer-canvas.js`

### 1a. Overlay state + module var (near the top `let` declarations)

```javascript
let overlayEl = null;
let overlayTitle = '';
let overlaySub = '';
```

### 1b. In `init`, after `initTooltip(hostEl); initMenu();` — create the overlay

```javascript
    overlayEl = document.createElement('div');
    overlayEl.className = 'rm-explorer-title';
    hostEl.appendChild(overlayEl);
    renderOverlay();
```

### 1c. Title API + overlay render (add with the other helpers)

```javascript
export function setTitle(title, subtitle) {
    overlayTitle = title || '';
    overlaySub = subtitle || '';
    renderOverlay();
}

function subWithCount() {
    const n = cy ? cy.nodes().length : 0;
    const base = overlaySub || '';
    return base + (base ? ' · ' : '') + n + ' resource' + (n === 1 ? '' : 's');
}

function renderOverlay() {
    if (!overlayEl) return;
    overlayEl.textContent = '';
    if (overlayTitle) {
        const h = document.createElement('div');
        h.className = 'rm-title-h';
        h.textContent = overlayTitle;
        overlayEl.appendChild(h);
    }
    const s = document.createElement('div');
    s.className = 'rm-title-sub';
    s.textContent = subWithCount();
    overlayEl.appendChild(s);
    overlayEl.style.display = (overlayTitle || overlaySub) ? 'block' : 'none';
}
```

Call `renderOverlay();` at the end of `addGraph`, `loadJson`, `collapseAll`, and `removeNode` so the
live count stays current. In `dispose()`, add: `if (overlayEl && overlayEl.parentNode) overlayEl.parentNode.removeChild(overlayEl); overlayEl = null;`

### 1d. Export compositing helpers

```javascript
function loadImage(uri) {
    return new Promise((res, rej) => { const i = new Image(); i.onload = () => res(i); i.onerror = rej; i.src = uri; });
}

// Render the graph PNG with a title/subtitle header band drawn above it. Returns a Blob.
async function pngWithHeader(scale) {
    const uri = cy.png({ output: 'base64uri', full: true, bg: '#ffffff', scale: scale });
    const title = overlayTitle, sub = subWithCount();
    const img = await loadImage(uri);
    const padX = 16 * scale, padTop = 14 * scale, gap = 6 * scale, padBottom = 12 * scale;
    const titleF = 20 * scale, subF = 12 * scale;
    const headerH = (title || sub)
        ? padTop + (title ? titleF : 0) + (title && sub ? gap : 0) + (sub ? subF : 0) + padBottom
        : 0;
    const canvas = document.createElement('canvas');
    canvas.width = img.width;
    canvas.height = img.height + headerH;
    const ctx = canvas.getContext('2d');
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    ctx.drawImage(img, 0, headerH);
    ctx.textBaseline = 'top';
    ctx.textAlign = 'left';
    let y = padTop;
    if (title) { ctx.fillStyle = '#0f172a'; ctx.font = '700 ' + titleF + 'px system-ui, sans-serif'; ctx.fillText(title, padX, y); y += titleF + gap; }
    if (sub)   { ctx.fillStyle = '#64748b'; ctx.font = '400 ' + subF + 'px system-ui, sans-serif'; ctx.fillText(sub, padX, y); }
    return await new Promise(res => canvas.toBlob(res, 'image/png'));
}

// Prepend a title/subtitle header into an SVG string (bumps height/viewBox, shifts content down).
function svgWithHeader(svg) {
    const title = overlayTitle, sub = subWithCount();
    if (!title && !sub) return svg;
    const headerH = 52;
    const esc = s => String(s || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    const openMatch = svg.match(/^([\s\S]*?<svg[^>]*>)/);
    if (!openMatch) return svg;
    let open = openMatch[1];
    const inner = svg.slice(open.length, svg.lastIndexOf('</svg>'));
    const hM = open.match(/height="([\d.]+)"/);
    if (hM) open = open.replace(/height="[\d.]+"/, 'height="' + (parseFloat(hM[1]) + headerH) + '"');
    open = open.replace(/viewBox="([-\d.\s]+)"/, (m, vb) => {
        const p = vb.trim().split(/\s+/).map(Number);
        if (p.length === 4) p[3] = p[3] + headerH;
        return 'viewBox="' + p.join(' ') + '"';
    });
    const header =
        '<rect x="0" y="0" width="100%" height="' + headerH + '" fill="#ffffff"/>' +
        '<text x="16" y="26" font-family="system-ui,sans-serif" font-size="20" font-weight="700" fill="#0f172a">' + esc(title) + '</text>' +
        '<text x="16" y="44" font-family="system-ui,sans-serif" font-size="12" fill="#64748b">' + esc(sub) + '</text>';
    return open + header + '<g transform="translate(0,' + headerH + ')">' + inner + '</g></svg>';
}
```

### 1e. Route the exports through the header helpers

Replace the current `copyPng`/`savePng`/`saveSvg`/`printDiagram` bodies:

```javascript
export async function copyPng() {
    try {
        const blob = await pngWithHeader(2);
        await navigator.clipboard.write([new ClipboardItem({ 'image/png': blob })]);
        return true;
    } catch (e) { return false; }
}

export async function savePng(filename) {
    try {
        const blob = await pngWithHeader(2);
        const url = URL.createObjectURL(blob);
        downloadUri(url, (filename || 'diagram') + '.png');
        setTimeout(() => URL.revokeObjectURL(url), 5000);
    } catch (e) { /* ignore */ }
}

export function saveSvg(filename) {
    const svg = svgWithHeader(cy.svg({ full: true, bg: '#ffffff' }));
    const url = URL.createObjectURL(new Blob([svg], { type: 'image/svg+xml;charset=utf-8' }));
    downloadUri(url, (filename || 'diagram') + '.svg');
    setTimeout(() => URL.revokeObjectURL(url), 5000);
}

export function printDiagram() {
    const svg = svgWithHeader(cy.svg({ full: true, bg: '#ffffff' }));
    const w = window.open('', '_blank');
    if (!w) return;
    w.document.write('<!doctype html><title>' + (overlayTitle || 'Diagram') + '</title>' + svg);
    w.document.close();
    w.focus();
    w.print();
}
```
(Keep `downloadUri`/`escapeHtml`/`isHttpUrl` as they are.)

- [ ] **Step 1:** Apply 1a–1e.

---

## 2. `ResourceExplorer.razor`

### 2a. Track the seed name + a title updater (add to `@code`)

```csharp
    private string _seedName = string.Empty;

    private async Task UpdateTitleAsync()
    {
        var title = !string.IsNullOrWhiteSpace(_diagramName) ? _diagramName
                  : (!string.IsNullOrWhiteSpace(_seedName) ? _seedName : "Resource Explorer");
        var subtitle = $"Dependency report · {DateTime.Now:yyyy-MM-dd}";
        await SafeInvokeAsync("setTitle", title, subtitle);
    }
```

### 2b. Set `_seedName` on the seed load

In `LoadNodeAsync`, in the `seed` branch (where it succeeds), set `_seedName = node.ResourceName;`
(alongside the existing seed handling).

### 2c. Call `UpdateTitleAsync()` after each state change

Add `await UpdateTitleAsync();` at the end of: the seed-load path in `OnAfterRenderAsync` (after the
seed `LoadNodeAsync` + `fit`), `OpenAsync` (after load succeeds), `LoadSharedAsync` (after load
succeeds), `PersistAsync` (success branch, after `_diagramName` is set), and `SaveCopyAsync` (success
branch). This keeps the overlay/exports in sync with the current name.

- [ ] **Step 2:** Apply 2a–2c.

---

## 3. CSS (`app.css`) — the on-screen overlay

```css
.rm-explorer-title {
    position: absolute;
    top: 10px;
    left: 14px;
    z-index: 6;
    pointer-events: none;           /* never blocks canvas interaction */
    max-width: 60%;
}
.rm-explorer-title .rm-title-h {
    font-size: 20px;
    font-weight: 800;
    color: #0f172a;
    line-height: 1.1;
    text-shadow: 0 1px 2px rgba(255,255,255,0.8);
}
.rm-explorer-title .rm-title-sub {
    font-size: 12px;
    color: #64748b;
    margin-top: 2px;
    text-shadow: 0 1px 2px rgba(255,255,255,0.8);
}
```

- [ ] **Step 3:** Add the CSS.

---

## UI verification hook (drive it — export is the key check)

- [ ] **Step 4: Stop the app**, `dotnet build` clean, `dotnet run`.
- [ ] **Step 5: Drive** `/explore/DEMOEXP-checkout`:
  1. **Title overlay** shows top-left: the seed/diagram name + "Dependency report · <today> · N resources"; the count updates as you expand/collapse; **Save-As "My Diagram"** → the title switches to "My Diagram".
  2. The overlay **doesn't block** node clicks/drag (pointer-events none).
  3. **Export → Save SVG**: open the `.svg` — it **contains the title + date header** above the graph (the graph is shifted down, nothing clipped).
  4. **Export → Save PNG** and **Copy image**: the PNG **has the title/date band** at the top.
  5. **Print / PDF**: the print preview shows the header + graph.
- [ ] **Step 6:** This is the acceptance gate — **confirm the exported PNG *and* SVG actually contain the title text** (open/inspect them, don't just trust the on-screen overlay). Capture the exported files / screenshots.

---

## Verification

1. Stop app → `dotnet build` clean; `dotnet test` unchanged (no unit tests — UI/JS).
2. Browser drive per Steps 4–6, especially the **export-contains-title** check.

---

## Out of scope
- Metadata wording / per-type color tuning — kept-as-drafted (future).

## Execution notes

Applied 1a–1e, 2a–2c, and the CSS essentially verbatim; see the "collapse() gap" note below for the
one deliberate deviation.

**`svgWithHeader` vs. the real `cy.svg()` output:** the brief's regexes assume the opening `<svg>`
tag carries both `height` and `viewBox`. The actual output from this app's `cy.svg({ full: true, bg:
'#ffffff' })` is `<svg version="1.1" xmlns="..." xmlns:xlink="..." width="1068" height="655">` —
**no `viewBox` at all**. The `height` bump still works as written (confirmed: exported height =
original content height + 52px header). The `viewBox` replace is a no-op here (`String.replace`
returns the string unchanged when the pattern doesn't match), so **no code change was required** —
the function already degrades gracefully for a viewBox-less SVG; width/height alone are the only
scale reference and the `<g transform="translate(0,52)">` wrapper lines up correctly (confirmed
visually, nothing clipped).

**Bug found + fixed while driving the app:** §1c's list of `renderOverlay()` call sites
(`addGraph`, `loadJson`, `collapseAll`, `removeNode`) omits the single-node `collapse()` function
(the one invoked by tapping an already-expanded node, or via the cxtmenu's "Collapse"). This is a
real gap against this slice's own acceptance check ("the count updates as you expand/collapse" —
Step 5.1): after collapsing a node, the overlay kept showing the pre-collapse count until some
unrelated action (e.g. Save As) happened to call `renderOverlay()` again. Reproduced this live
(collapsing the seed's neighbours down to 1 node while the overlay kept showing "7 resources"
until Save As revealed the true "1 resource"), then added `renderOverlay();` to the end of
`collapse()` to match the other call sites. Re-verified end-to-end after the fix by expanding then
collapsing "Mobile BFF" (re-locating it on-canvas by hover-scan before *and* after each click,
since `fit()` re-centers/rescales the whole viewport on every graph change): 7 → 8 resources on
expand (Catalog Service added), 8 → 7 on collapse (Catalog Service removed) — both changes reflected
in the overlay immediately.

**Export-contains-title evidence:**
- *SVG* (`Save SVG`, downloaded via Playwright): raw file starts
  `<svg version="1.1" ... width="1068" height="655"><rect x="0" y="0" width="100%" height="52"
  fill="#ffffff"/><text ...>My Diagram</text><text ...>Dependency report · 2026-07-20 · 7
  resources</text><g transform="translate(0,52)">...graph...</g></svg>` — title + subtitle text
  present, height bumped by exactly the 52px header, graph content shifted into a `<g
  transform="translate(0,52)">` wrapper, nothing clipped.
- *PNG* (`Save PNG`): downloaded PNG (189834 bytes, 2136×1334px — 2x scale) renders with a white
  header band reading "My Diagram" / "Dependency report · 2026-07-20 · 7 resources" above the
  full, unclipped graph.
- *Copy image*: clipboard read back a 164906-byte `image/png` blob immediately after clicking —
  consistent with the composited header+graph image, not the bare graph.
- *Print/PDF*: popup tab title is the diagram name ("My Diagram"); popup body contains the same
  `svgWithHeader()` output, rendering the header above the graph identically to the SVG/PNG
  exports.

**Driven in Playwright** against `/explore/DEMOEXP-checkout` (system Chrome via `playwright-core`,
headless): overlay on load, expand/collapse count updates (see above), Save-As rename switching
the title live, `pointer-events: none` confirmed via computed style (never blocks canvas
interaction), and all four export paths (Save SVG / Save PNG / Copy image / Print) — see the
export-contains-title evidence above for each.

**Build/test:** `dotnet build` clean aside from the expected `HTResourceMapperDb.sqlproj` MSB4278
(pre-existing, ignored per CLAUDE.md). `dotnet test`: 2 pre-existing, unrelated failures in
`HT.Api.Service.Contracts.Tests` (NotFound → HTTP status mapping); all other tests (414) passed —
no regressions (this slice is `wwwroot`/Razor only, no server-side test coverage to affect).

**Commit:** `e783d0e` on `home-page`. This completes pass 2.
