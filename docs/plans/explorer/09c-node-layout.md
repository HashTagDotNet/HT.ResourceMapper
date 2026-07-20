# Slice #9c — Node rendering + layered layout

## Context

Second sub-slice of [UI-polish pass 2](./09-ui-polish.md); the big canvas rework. Depends on **9b**
(the read now carries `ShortCode`/`IconKey` per node). Reworks `explorer-canvas.js` +
`ResourceExplorer.razor`. **Verified by driving the app** — canvas pixel details (icon/caption
positions, node size) are tuned in the browser, not on paper.

> **Build note:** a running `ResourceMapper.UI.Web` instance locks build output. **Stop any running
> app** (e.g. `taskkill`/close it) before `dotnet build`/`dotnet run`.

**Agreed design (see pass-2 section):**
- **Node:** type-color **circle** with a white **type icon** + white **short code** inside — always
  visible. Everything is **canvas-rendered so it appears in PNG/SVG exports** (HTML-overlay nodes are
  ruled out because they'd vanish from exports).
- **Name/type caption:** the **name (14px bold) + type (12px)** render below the node **only for the
  seed / hovered / selected** node — as a **baked SVG image** (two font sizes, export-safe).
- **Layout:** tidy **layered** (`breadthfirst`, directed from the seed) on load; on expand, place new
  nodes near their parent (preserve drags); a **Re-tidy** toolbar button re-runs the layered layout;
  **auto-fit** after expand and re-tidy.
- Keep pass-1 **thin arrows** + **seed-relative dashed upstream** edges.

**Rendering technique (canvas, export-safe):**
- Node `background-color` = the type color (the circle).
- Node **`label`** = the short **code** (white, centered) — one font size is fine for a code.
- Node **`background-image`** = an **array** of up to two SVG data-URIs: `[iconUri, captionUri]`.
  `iconUri` = the white type icon (always). `captionUri` = the name/type caption (only when the node is
  "labeled"; otherwise a transparent 1×1). Cytoscape supports **multiple background images** with
  per-image position/size + `background-clip: none` + `bounds-expansion`, so the caption can sit
  *below* the circle and still export.
- A node's **`labeled`** class is toggled for the seed (always), the hovered node, and the selected
  node; changing it swaps `captionUri`.

---

## 1. `explorer-canvas.js`

### 1a. Icon map + data-URI helpers (add near the top helpers)

```javascript
// White type-icon glyphs keyed by ResourceType.IconKey (from slice 9b). Minimal, schematic.
const ICON_PATHS = {
    web:      '<rect x="3" y="4" width="18" height="13" rx="2"/><rect x="8" y="19" width="8" height="2"/>',
    apps:     '<rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>',
    settings: '<circle cx="12" cy="12" r="6" fill="none" stroke="#fff" stroke-width="2.4"/><circle cx="12" cy="12" r="2"/>',
    insights: '<rect x="3" y="13" width="4" height="8"/><rect x="10" y="8" width="4" height="13"/><rect x="17" y="3" width="4" height="18"/>',
    memory:   '<ellipse cx="12" cy="6" rx="8" ry="3"/><path d="M4,6 v12 a8,3 0 0 0 16,0 v-12" fill="none" stroke="#fff" stroke-width="2.2"/>',
    database: '<ellipse cx="12" cy="6" rx="8" ry="3"/><path d="M4,6 v12 a8,3 0 0 0 16,0 v-12" fill="none" stroke="#fff" stroke-width="2.2"/>',
    bus:      '<rect x="3" y="6" width="18" height="4" rx="1"/><rect x="3" y="14" width="18" height="4" rx="1"/>',
    queue:    '<rect x="3" y="5" width="18" height="3"/><rect x="3" y="10.5" width="18" height="3"/><rect x="3" y="16" width="18" height="3"/>',
    hub:      '<circle cx="12" cy="12" r="3"/><circle cx="4" cy="4" r="2.4"/><circle cx="20" cy="4" r="2.4"/><circle cx="4" cy="20" r="2.4"/><circle cx="20" cy="20" r="2.4"/>',
    folder:   '<path d="M3,6 h6 l2,2 h10 v11 h-18 z"/>',
    dns:      '<circle cx="12" cy="12" r="8" fill="none" stroke="#fff" stroke-width="2.2"/><path d="M4,12 h16 M12,4 a12,8 0 0 0 0,16 a12,8 0 0 0 0,-16" fill="none" stroke="#fff" stroke-width="1.6"/>'
};

function svgDataUri(svg) {
    return 'data:image/svg+xml;utf8,' + encodeURIComponent(svg);
}

const TRANSPARENT_PX = svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="1" height="1"></svg>');

// The white type icon (or a neutral dot if the key is unknown / missing).
function iconUri(iconKey) {
    const body = ICON_PATHS[iconKey] || '<circle cx="12" cy="12" r="4"/>';
    return svgDataUri('<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="#fff">' + body + '</svg>');
}

// The name (14 bold) + type (12) caption drawn as an SVG image (export-safe, two font sizes).
function captionUri(name, type) {
    const w = 220, h = 40;
    const esc = s => String(s || '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
    return svgDataUri(
        '<svg xmlns="http://www.w3.org/2000/svg" width="' + w + '" height="' + h + '">' +
        '<text x="' + (w/2) + '" y="16" text-anchor="middle" font-family="system-ui,sans-serif" font-size="14" font-weight="700" fill="#0f172a">' + esc(name) + '</text>' +
        '<text x="' + (w/2) + '" y="33" text-anchor="middle" font-family="system-ui,sans-serif" font-size="12" fill="#64748b">' + esc(type) + '</text>' +
        '</svg>');
}
```

### 1b. Node style (in `init`) — replace the `node` style block

Node data now carries `color`, `code`, `iconUri`, `caption` (set in `addGraph`/`loadJson`). Style:

```javascript
            { selector: 'node', style: {
                'shape': 'ellipse',
                'background-color': 'data(color)',
                'width': 46, 'height': 46,
                'border-width': 2, 'border-color': '#1e293b',
                'label': 'data(code)',
                'color': '#fff',
                'font-size': '11px',
                'font-weight': 700,
                'text-valign': 'center',
                'text-halign': 'center',
                // two background images: [type icon (always), name/type caption (labeled only)]
                'background-image': 'data(bgImages)',
                'background-image-crossorigin': 'anonymous',
                'background-width':  ['20px', '220px'],
                'background-height': ['20px', '40px'],
                'background-position-x': ['50%', '50%'],
                'background-position-y': ['30%', '128%'],
                'background-clip': ['none', 'none'],
                'background-image-containment': ['inside', 'over'],
                'bounds-expansion': 30
            }},
            { selector: 'node.seed',     style: { 'border-color': '#f59e0b', 'border-width': 4 }},
            { selector: 'node.expanded', style: { 'border-style': 'double' }},
```

> The exact `background-position-y`/`background-width`/`bounds-expansion`/icon color-contrast values are
> **tuned in the browser** — start here and adjust so the icon sits centered-upper in the circle and the
> caption sits neatly below. (Removed the old `▸/▾` glyph + wrapped name label from pass 1.)

Node **`data`** carries: `color` (type color), `code` (short code), and `bgImages` = `[iconUri, captionOrTransparent]`. Add a color map + a per-node builder:

```javascript
const TYPE_COLORS = {
    web:'#2563EB', apps:'#2563EB', insights:'#7c3aed', memory:'#dc2626', database:'#4338ca',
    bus:'#ea580c', queue:'#ea580c', settings:'#0d9488', hub:'#0891b2', folder:'#64748b', dns:'#0d9488'
};
function nodeColor(iconKey) { return TYPE_COLORS[iconKey] || '#2563EB'; }

// Recompute a node's bg images from its data + labeled state.
function nodeBgImages(n) {
    const d = n.data();
    const caption = n.hasClass('labeled') ? captionUri(d.name, d.type) : TRANSPARENT_PX;
    return [d._icon, caption];
}
function refreshNode(n) { n.data('bgImages', nodeBgImages(n)); }
function refreshAll() { cy.nodes().forEach(refreshNode); }
```

### 1c. `addGraph` / `loadJson` — set the new node data

When adding a node (in `addGraph` and `loadJson`), include the new fields on `data`:

```javascript
            cy.add({ group: 'nodes', data: {
                id: n.uid, uid: n.uid, name: n.name, key: n.key, type: n.type,
                domain: n.domain, url: n.primaryUrl,
                code: n.shortCode || (n.type ? n.type.substr(0,3).toUpperCase() : '?'),
                color: nodeColor(n.iconKey),
                _icon: iconUri(n.iconKey),
                bgImages: [iconUri(n.iconKey), TRANSPARENT_PX]
            }});
```
(For `loadJson`, the serialized node must now also persist `shortCode`/`iconKey` — add them to
`serialize()`'s per-node object and read them back here.)

**Remove** the old `applyLabels()`/`labelFor()` (the `▸/▾`+name label). Replace all its call sites
(addGraph/markExpanded/collapse/collapseAll/removeNode/setPreset) with the new **`applyLabeled()`**:

```javascript
// Seed + hovered + selected nodes get the caption; everyone else just the code+icon.
function applyLabeled() {
    const hoverId = cy.scratch('_hover');
    cy.nodes().forEach(n => {
        const on = n.hasClass('seed') || n.selected() || n.id() === hoverId;
        if (on) n.addClass('labeled'); else n.removeClass('labeled');
        refreshNode(n);
    });
}
```
Call `applyLabeled()` at the end of `addGraph`, `loadJson`, `markExpanded`, `collapse`, `collapseAll`,
`removeNode`, and on the events below. (Keep `applyEdgeStyles()` calls from pass 1.)

### 1d. Hover / select → caption; and preset selector now hides (code is fixed)

In `init`, after creating `cy`, wire hover + selection:

```javascript
    cy.on('mouseover', 'node', e => { cy.scratch('_hover', e.target.id()); applyLabeled(); });
    cy.on('mouseout',  'node', () => { cy.scratch('_hover', null); applyLabeled(); });
    cy.on('select unselect', 'node', () => applyLabeled());
```
The old tooltip (`initTooltip`) can stay for the URL/domain details, or be removed since the caption
now covers name/type — **keep the tooltip** (it still adds key/domain/link). The **Display preset**
control becomes redundant (the code is always the in-node text); `setPreset` may be left as a no-op or
removed with its toolbar control in the Razor page (see §2).

### 1e. Layout — layered tidy + Re-tidy + auto-fit

Replace the seed-load `cose` layout with a directional **`breadthfirst`** rooted at the seed, and add a
`reTidy()` export + auto-fit:

```javascript
function runTidy() {
    cy.layout({
        name: 'breadthfirst', directed: true, roots: seedId ? '#' + cssId(seedId) : undefined,
        spacingFactor: 1.3, padding: 30, animate: false
    }).run();
    fit();
}
export function reTidy() { runTidy(); }
function cssId(id) { return id.replace(/[^a-zA-Z0-9_-]/g, m => '\\' + m); }
```
- In `addGraph`: when `!expandFromUid` (seed load) call `runTidy()` instead of the old `cose` block;
  when expanding, keep `placeAround(...)` then `fit()` (auto-fit so new nodes show).
- Keep `placeAround`, `collapseAll`→`fit()`, etc.

- [ ] **Step 1:** Apply 1a–1e to `explorer-canvas.js`. Update `serialize()` to persist `shortCode`/`iconKey`; drop `applyLabels`/`labelFor`.

---

## 2. `ResourceExplorer.razor`

- **Pass `shortCode`/`iconKey` to the canvas.** `ExplorerNodeModel`/`ExplorerNeighborModel` now have
  `ShortCode`/`IconKey` (9b). Extend `NodePayload(...)` to include them:
  ```csharp
  private static object NodePayload(string uid, string key, string name, string type, string? domain, string? url, string? shortCode, string? iconKey)
      => new { uid, key, name, type, domain, primaryUrl = url, shortCode, iconKey };
  ```
  and update the two call sites in `LoadNodeAsync` to pass `node.ShortCode/node.IconKey` and
  `n.ShortCode/n.IconKey`.
- **Add a Re-tidy toolbar button** (beside Fit): `<MudButton … OnClick="ReTidyAsync">Re-tidy</MudButton>`
  with `private async Task ReTidyAsync() => await SafeInvokeAsync("reTidy");`.
- **Remove the Display-preset `MudSelect`** from the toolbar (the code is now always in-node; the
  preset is moot). Drop `_preset`/`OnPresetChanged`, and the `setPreset` interop call in
  `OnAfterRenderAsync`. (If you'd rather keep presets for future use, leave them but hide the control.)

- [ ] **Step 2:** Apply the payload change, Re-tidy button, and preset-control removal.

---

## UI verification hook (drive it)

- [ ] **Step 3: Stop any running app**, `dotnet build` (clean), `dotnet run`.
- [ ] **Step 4: Drive** `/explore/DEMOEXP-checkout`:
  1. Nodes are **type-color circles with a white icon + code** inside; **no** overlapping name labels.
  2. Only the **seed** shows the **name (14) + type (12) caption**; **hovering** any node shows its
     caption; clicking (**selecting**) pins the caption; moving away hides it.
  3. On load the graph is **tidy/layered** (dependency flow, few crossings); **expanding** adds nodes
     near their parent and the view **auto-fits**; **Re-tidy** re-lays-out cleanly.
  4. Upstream edges **dashed**, arrows **thin** (pass-1 behaviour retained).
  5. **Export → Save PNG/SVG**: the icons, codes, and the seed caption are **present in the image**
     (canvas-rendered). This is the key check that the "baked" approach worked.
- [ ] **Step 5:** Tune icon/caption positions + node size in the browser until it matches the mockup;
  record the final values in the execution notes.

---

## Verification

1. Stop app → `dotnet build` clean; `dotnet test` unchanged (no new unit tests — UI/JS).
2. Browser drive per Steps 3–5 — the gate, incl. the **export-contains-icons/codes/caption** check.

---

## Out of scope (→ 9d)

- **Title + report-date overlay** and **compositing it into exports**; **edge "depends on" hover pill** → **slice 9d**.
- Per-type color/icon fine-tuning beyond a first pass → future polish.

---

## Execution notes

_(After execution — record the final tuned canvas values (bg positions, node size, bounds-expansion),
whether `breadthfirst` gave acceptable layering (or if a layout extension was needed), the export check
result, and commit hash(es). Then proceed to 9d.)_
