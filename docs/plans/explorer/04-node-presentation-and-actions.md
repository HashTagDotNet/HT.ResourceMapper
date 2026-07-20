# Slice #4 — Node presentation & actions

## Context

Fourth slice of the [Resource Explorer build](./00-implementation-plan-list.md), building on the
slice-2/3 canvas. See the [design spec](./resource-explorer-design-v1.md) (§5.1, §5.3, §6, §8) and
the master list. Depends on slice 2 (canvas).

This slice makes nodes **informative and actionable**:

- **Display presets** — Name only / Name + Type / Detailed (Key + Name + Type + Domain), chosen from
  a selector; applied to every node, re-rendered live.
- **Inline expand affordance** — a `▸` / `▾` glyph prefix on each node label showing collapsed vs.
  expanded state (canvas-friendly; no HTML overlay).
- **Hover tooltip** — a lightweight, XSS-safe tooltip showing the node's Name / Key / Type / Domain /
  Link on mouse-over.
- **Right-click menu** (`cytoscape-cxtmenu`, MIT) — Expand/Collapse, Remove, **Open link** (primary
  URL, new tab), **Open in Mapper** (the editor at `/resources/{uid}`, new tab). This **replaces the
  interim Shift-click remove** from slice 3.

It modifies the two canvas files + `App.razor` + `app.css`. **No backend changes.**

**Key decisions (this slice):**

- **Labels are data-driven** (`label: data(label)`); a JS `applyLabels()` recomputes each node's
  `label` from the current preset + expand glyph, so preset/expand changes re-render reliably.
- **Tooltip is a plain positioned `<div>`** built with `textContent`/`createTextNode` (never
  `innerHTML`) — no `popper`/`tippy` vendoring, and safe against resource names/URLs containing markup.
- **Open-link / open-in-Mapper are pure JS `window.open(..., '_blank', 'noopener')`** — no C#
  round-trip; only Expand/Collapse and Remove call back to C# (they mutate graph + `_expanded`).
- **`cytoscape-cxtmenu` is already vendored** at `wwwroot/js/explorer/vendor/cytoscape-cxtmenu.js`
  (v3.5.0, MIT — fetched during planning; self-registers when loaded after cytoscape). **Commit it
  with this slice; do NOT re-download.**
- **The radial `cxtmenu` style is a candidate for the UI-polish slice (9)** if you'd prefer a
  different menu look — not changed here.
- **No unit tests** (UI/JS); verified by driving the app.

---

## 1. Load the cxtmenu extension (`UI\ResourceMapper.UI.Web\Components\App.razor`)

Add the extension `<script>` **after** `cytoscape.min.js` and **before** `blazor.web.js`:

```razor
<body>
    <Routes @rendermode="RenderMode.InteractiveServer" />
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
    <script src="js/explorer/vendor/cytoscape.min.js"></script>
    <script src="js/explorer/vendor/cytoscape-cxtmenu.js"></script>
    <script src="_framework/blazor.web.js"></script>
</body>
```

- [ ] **Step 1:** Add the one `<script>` line (keep the others and their order).

---

## 2. Replace the canvas module (`UI\ResourceMapper.UI.Web\wwwroot\js\explorer\explorer-canvas.js`)

Replace the **entire file** with the following (it folds slices 2–3 together with the new
labels/tooltip/menu — provided whole to avoid ambiguous partial edits):

```javascript
// Explorer canvas — thin wrapper over Cytoscape (global `cytoscape`) + the cxtmenu extension.
// One instance per page. Graph state lives in the browser; C# pushes graph batches (addGraph) and
// receives node events (tap / menu) via the DotNetObjectReference.

let cy = null;
let dotNet = null;
let seedId = null;
let currentPreset = 'nameType';   // 'name' | 'nameType' | 'detailed'
let tip = null;
let menu = null;

export function init(hostEl, dotNetRef) {
    dotNet = dotNetRef;
    cy = cytoscape({
        container: hostEl,
        elements: [],
        style: [
            { selector: 'node', style: {
                'background-color': '#2563EB',
                'label': 'data(label)',
                'color': '#0f172a',
                'font-size': '11px',
                'text-valign': 'bottom',
                'text-halign': 'center',
                'text-margin-y': 4,
                'text-wrap': 'wrap',
                'text-max-width': '140px',
                'width': 30, 'height': 30,
                'border-width': 2, 'border-color': '#1e3a8a'
            }},
            { selector: 'node.seed',     style: { 'background-color': '#f59e0b', 'border-color': '#b45309' }},
            { selector: 'node.expanded', style: { 'border-color': '#16a34a', 'border-width': 3 }},
            { selector: 'edge', style: {
                'width': 2,
                'line-color': '#94a3b8',
                'target-arrow-color': '#94a3b8',
                'target-arrow-shape': 'triangle',
                'curve-style': 'bezier'
            }}
        ],
        layout: { name: 'grid' },
        minZoom: 0.2, maxZoom: 3, wheelSensitivity: 0.2
    });

    // Tap toggles expand/collapse (remove/open actions live in the right-click menu).
    cy.on('tap', 'node', evt => {
        dotNet.invokeMethodAsync('OnNodeTapped', evt.target.id());
    });

    initTooltip(hostEl);
    initMenu();
}

// ---- labels / presets ---------------------------------------------------

function labelFor(ele) {
    const d = ele.data();
    const glyph = ele.hasClass('expanded') ? '▾ ' : '▸ ';
    let body;
    switch (currentPreset) {
        case 'name':
            body = d.name || d.uid; break;
        case 'detailed':
            body = [d.key, d.name, d.type, d.domain].filter(Boolean).join('\n'); break;
        case 'nameType':
        default:
            body = (d.name || d.uid) + (d.type ? '\n' + d.type : ''); break;
    }
    return glyph + body;
}

function applyLabels() {
    cy.nodes().forEach(n => n.data('label', labelFor(n)));
}

export function setPreset(preset) {
    currentPreset = preset;
    applyLabels();
}

// ---- tooltip (plain positioned div, XSS-safe via textContent) ------------

function initTooltip(container) {
    tip = document.createElement('div');
    tip.className = 'rm-explorer-tip';
    tip.style.display = 'none';
    container.appendChild(tip);
    cy.on('mouseover', 'node', evt => showTip(evt.target));
    cy.on('mouseout', 'node', hideTip);
    cy.on('pan zoom drag', hideTip);
}

function showTip(node) {
    if (!tip) return;
    const d = node.data();
    tip.textContent = '';
    const rows = [['Name', d.name], ['Key', d.key], ['Type', d.type], ['Domain', d.domain], ['Link', d.url]];
    for (const [k, v] of rows) {
        if (!v) continue;
        const row = document.createElement('div');
        const b = document.createElement('strong');
        b.textContent = k + ': ';
        row.appendChild(b);
        row.appendChild(document.createTextNode(v));
        tip.appendChild(row);
    }
    const p = node.renderedPosition();
    tip.style.left = (p.x + 16) + 'px';
    tip.style.top = (p.y + 16) + 'px';
    tip.style.display = 'block';
}

function hideTip() { if (tip) tip.style.display = 'none'; }

// ---- right-click menu ----------------------------------------------------

function initMenu() {
    menu = cy.cxtmenu({
        selector: 'node',
        menuRadius: 90,
        commands: node => {
            const uid = node.id();
            const url = node.data('url');
            return [
                { content: node.hasClass('expanded') ? 'Collapse' : 'Expand',
                  select: () => dotNet.invokeMethodAsync('OnNodeTapped', uid) },
                { content: 'Remove',
                  select: () => dotNet.invokeMethodAsync('OnNodeRemoveRequested', uid) },
                { content: 'Open link',
                  enabled: !!url,
                  select: () => { if (url) window.open(url, '_blank', 'noopener'); } },
                { content: 'Open in Mapper',
                  select: () => window.open('/resources/' + encodeURIComponent(uid), '_blank', 'noopener') }
            ];
        }
    });
}

// ---- graph mutation ------------------------------------------------------

export function addGraph(nodes, edges, seedUid, expandFromUid) {
    const added = [];
    for (const n of nodes) {
        if (cy.getElementById(n.uid).empty()) {
            added.push(n.uid);
            cy.add({ group: 'nodes', data: {
                id: n.uid, uid: n.uid, name: n.name, key: n.key,
                type: n.type, domain: n.domain, url: n.primaryUrl
            }});
        }
    }
    for (const e of edges) {
        const id = e.source + '__' + e.target;
        if (cy.getElementById(id).empty()) {
            cy.add({ group: 'edges', data: { id: id, source: e.source, target: e.target } });
        }
    }
    if (seedUid) { seedId = seedUid; cy.getElementById(seedUid).addClass('seed'); }

    if (!expandFromUid) {
        cy.layout({ name: 'cose', animate: false, padding: 30 }).run();
    } else if (added.length) {
        placeAround(expandFromUid, added);
    }
    applyLabels();
}

function placeAround(parentUid, newUids) {
    const parent = cy.getElementById(parentUid);
    if (parent.empty()) return;
    const p = parent.position();
    const radius = 150;
    const count = Math.max(newUids.length, 1);
    newUids.forEach((uid, i) => {
        const node = cy.getElementById(uid);
        if (node.empty()) return;
        const angle = (2 * Math.PI * i) / count - Math.PI / 2;
        node.position({ x: p.x + radius * Math.cos(angle), y: p.y + radius * Math.sin(angle) });
    });
}

export function markExpanded(uid, expanded) {
    const n = cy.getElementById(uid);
    if (n.empty()) return;
    if (expanded) n.addClass('expanded'); else n.removeClass('expanded');
    applyLabels();
}

export function collapse(uid) {
    const node = cy.getElementById(uid);
    if (node.empty()) return;
    const victims = node.neighborhood('node').filter(n =>
        n.degree(false) === 1 && !n.hasClass('seed') && !n.hasClass('expanded'));
    victims.remove();
    node.removeClass('expanded');
    applyLabels();
}

export function collapseAll() {
    if (!seedId) return [];
    cy.nodes().filter(n => n.id() !== seedId).remove();
    cy.getElementById(seedId).removeClass('expanded');
    applyLabels();
    fit();
    return cy.nodes().map(n => n.id());
}

export function removeNode(uid) {
    const node = cy.getElementById(uid);
    if (node.empty()) return cy.nodes().map(n => n.id());
    if (uid === seedId) { cy.elements().remove(); seedId = null; return []; }
    node.remove();
    const seed = cy.getElementById(seedId);
    if (seed.empty()) return cy.nodes().map(n => n.id());
    const keep = seed.component();
    cy.nodes().not(keep).remove();
    applyLabels();
    return cy.nodes().map(n => n.id());
}

export function fit() { if (cy) cy.fit(undefined, 30); }

export function dispose() {
    if (menu) { try { menu.destroy(); } catch (e) { /* extension teardown */ } menu = null; }
    if (tip && tip.parentNode) tip.parentNode.removeChild(tip);
    tip = null;
    if (cy) { cy.destroy(); cy = null; }
    dotNet = null;
    seedId = null;
}
```

- [ ] **Step 2:** Replace `explorer-canvas.js` with the whole file above.

---

## 3. Page: preset selector + drop Shift-click hint (`Components\Pages\ResourceExplorer.razor`)

### 3a. Control-bar markup

Replace the existing `<div class="rm-explorer-bar"> … </div>` block with:

```razor
    <div class="rm-explorer-bar">
        <MudButton Size="Size.Small" Variant="Variant.Outlined"
                   StartIcon="@Icons.Material.Outlined.UnfoldLess"
                   OnClick="CollapseAllAsync">Collapse all</MudButton>
        <MudSelect T="string" Value="_preset" ValueChanged="OnPresetChanged"
                   Dense="true" Margin="Margin.Dense" Variant="Variant.Outlined"
                   Class="ml-3" Style="max-width:180px" Label="Display">
            <MudSelectItem Value="@("name")">Name only</MudSelectItem>
            <MudSelectItem Value="@("nameType")">Name + Type</MudSelectItem>
            <MudSelectItem Value="@("detailed")">Detailed</MudSelectItem>
        </MudSelect>
        <MudText Typo="Typo.caption" Class="ml-3 mud-text-secondary">
            Tap to expand / collapse · right-click for actions · drag to arrange
        </MudText>
    </div>
```

### 3b. `@code` additions

Add the `_preset` field (beside the other private fields) and the change handler, and sync the
preset once after `init`. Add the field:

```csharp
    private string _preset = "nameType";
```

In `OnAfterRenderAsync`, after `await SafeInvokeAsync("init", _canvasEl, _selfRef);` add:

```csharp
        await SafeInvokeAsync("setPreset", _preset);
```

Add the handler (beside `CollapseAllAsync`):

```csharp
    private async Task OnPresetChanged(string preset)
    {
        _preset = preset;
        await SafeInvokeAsync("setPreset", preset);
    }
```

- [ ] **Step 3:** Apply 3a + 3b. (No other `@code` changes — `OnNodeTapped`/`OnNodeRemoveRequested`/`SafeInvokeAsync` from slice 3 are reused by the menu.)

---

## 4. CSS (`UI\ResourceMapper.UI.Web\wwwroot\app.css`)

The canvas container must be `position: relative` so the tooltip positions against it; add the
tooltip style. Replace the slice-3 `.rm-explorer-canvas` block with the one below (adds
`position: relative`; height nudged for the taller bar) and add `.rm-explorer-tip`:

```css
.rm-explorer-canvas {
    position: relative;
    width: 100%;
    height: calc(100vh - 240px);
    min-height: 400px;
    border: 1px solid var(--mud-palette-lines-default, #e2e8f0);
    border-radius: 8px;
    background: #f8fafc;
}
.rm-explorer-tip {
    position: absolute;
    z-index: 10;
    pointer-events: none;
    background: rgba(15, 23, 42, 0.92);
    color: #f8fafc;
    font-size: 11px;
    line-height: 1.35;
    padding: 6px 8px;
    border-radius: 6px;
    max-width: 260px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.25);
}
```

- [ ] **Step 4:** Update `app.css`.

---

## UI verification hook (visible slice)

- [ ] **Step 5: Build + run.** `dotnet build` clean; `dotnet run --project UI/ResourceMapper.UI.Web`.
- [ ] **Step 6: Drive it** (reuse the `AAS001` / "Production Web App" demo data;
  uid `DEMO5d0ee27dbbf148589cdcf2248f579b91`). Explore it, then confirm:
  1. Each node label is prefixed with `▸` (collapsed) / `▾` (expanded).
  2. **Display** selector: *Name only* shows just the name; *Name + Type* adds the type line;
     *Detailed* shows Key / Name / Type / Domain — and switching re-renders every node live.
  3. **Hover** a node → tooltip with Name/Key/Type/Domain/Link (only populated fields); it hides on
     mouse-out and while panning/zooming/dragging.
  4. **Right-click** a node → radial menu: **Expand/Collapse** works; **Remove** removes it (with the
     same reachability cleanup as slice 3); **Open link** opens the node's primary URL in a new tab
     (greyed out when the node has no URL); **Open in Mapper** opens `/resources/{uid}` in a new tab.
  5. **Shift-click no longer removes** (that was interim) — tap still expands/collapses.
- [ ] **Step 7 (optional): Playwright** — assert the label text changes when the Display preset
  changes, and that right-clicking a node shows the cxtmenu overlay.

---

## Verification

1. `dotnet build` clean (sqlproj `MSB4278` aside); `dotnet test` unchanged (no new unit tests; confirm no regression).
2. Manual browser drive per Steps 5–6 — the primary gate.

---

## Out of scope (later slices)

- **Persisting the chosen preset / positions / the diagram** (Save/Open/Share), floating toolbar → slices 5–7. This slice's Display selector lives on the interim control bar; slice 7 moves it into the toolbar and persists the choice.
- **Client identity / diagram store** → slices 5–6.
- **Export (PNG/SVG/clipboard/print)** → slice 8.
- **Restyling the right-click menu / node visuals** per your adjustments → slice 9 (UI polish).

---

## Execution notes

_(Written after execution — record deviations, cxtmenu registration/behavior actually observed,
tooltip positioning quirks, and commit hash(es). Then mark slice #4 **Done** ✓ / slice #5 **Next**
in the master list, and commit.)_
