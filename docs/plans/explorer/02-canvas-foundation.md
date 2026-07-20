# Slice #2 — Canvas foundation

## Context

Second slice of the [Resource Explorer build](./00-implementation-plan-list.md), and the **first
visible/clickable** one. See the [design spec](./resource-explorer-design-v1.md) (§4.1, §4.2) and
the master list. Slice 1 (the `IExplorerService.GetNodeAsync` read) is **Done** and is the only
dependency.

This slice makes the graph **real on screen**: launch "Explore" from the home grid, land on a
Cytoscape canvas seeded on that resource, and **tap any node to expand/collapse its one-hop
neighbors**. It establishes the repo's **first** vendored JS library, first ES-module + `IJSObjectReference`
wrapper, and first `DotNetObjectReference` callback — so the wiring here is the template every later
UI slice builds on.

**Key decisions (this slice):**

- **Cytoscape holds all graph state in the browser.** C# only (a) pushes graph batches down and
  (b) receives *tap* events back via a `DotNetObjectReference`, then calls `IExplorerService`
  (slice 1) and pushes the resulting neighbors down. This keeps circuit chatter minimal (design §7,
  Blazor-Server reality).
- **JS interop is deferred past prerender** — Cytoscape is initialized in
  `OnAfterRenderAsync(firstRender)`, matching `Home.razor` (prerendering is on; `localStorage`/DOM
  aren't available during prerender).
- **Nodes are keyed by `ResourceUid`** (the boundary rule — no int ids cross to JS). Edges are keyed
  `"{source}__{target}"` and point **dependent → dependency**.
- **Dedup + cycle-safety live in JS:** `addGraph` skips any node/edge id already present, so
  expanding into an already-shown resource just draws the missing edge and never loops.
- **Cytoscape is already vendored** at `wwwroot/js/explorer/vendor/cytoscape.min.js`
  (v3.30.2, MIT — fetched during planning). **Commit it as part of this slice; do NOT re-download.**
- **Auto-layout uses the built-in `cose` layout** (no extension needed). This slice re-runs layout
  on each add; **slice 3 (arrangement)** changes that to preserve manual positions.
- **Simple collapse only:** collapse removes a node's degree-1, non-seed, non-expanded leaf
  neighbors. Full reachability-based remove is **slice 3**.
- **No unit tests** (UI/JS layer; the repo has no UI test project). Verified by driving the app in a
  browser (+ optional Playwright per `tools/e2e`).

---

## 1. Vendored library is in place

`wwwroot/js/explorer/vendor/cytoscape.min.js` (v3.30.2, MIT) already exists in the working tree.

- [ ] **Step 1:** Confirm the file is present and ~373 KB (`ls -l UI/ResourceMapper.UI.Web/wwwroot/js/explorer/vendor/cytoscape.min.js`). It will be committed with this slice. Do not download anything.

---

## 2. Load Cytoscape in the host page (`UI\ResourceMapper.UI.Web\Components\App.razor`)

Add the vendor `<script>` **before** `blazor.web.js` so the `cytoscape` global exists before the ES
module imports at runtime:

```razor
<body>
    <Routes @rendermode="RenderMode.InteractiveServer" />
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
    <script src="js/explorer/vendor/cytoscape.min.js"></script>
    <script src="_framework/blazor.web.js"></script>
</body>
```

- [ ] **Step 2:** Add the one `<script>` line to `App.razor` (leave the two existing scripts and their order intact).

---

## 3. The canvas ES module (`UI\ResourceMapper.UI.Web\wwwroot\js\explorer\explorer-canvas.js`)

A thin wrapper over the global `cytoscape`. One instance per page; all node/edge/expansion state
lives here. Exposes `init`, `addGraph`, `markExpanded`, `collapse`, `fit`, `dispose`.

```javascript
// Explorer canvas — thin wrapper over Cytoscape (global `cytoscape`, from vendor/cytoscape.min.js).
// One instance per page. Graph state (which nodes/edges exist, which are expanded) lives in the
// browser; C# pushes graph batches down (addGraph) and receives tap events (via dotNet ref).

let cy = null;
let dotNet = null;

export function init(hostEl, dotNetRef) {
    dotNet = dotNetRef;
    cy = cytoscape({
        container: hostEl,
        elements: [],
        style: [
            { selector: 'node', style: {
                'background-color': '#2563EB',
                'label': 'data(name)',
                'color': '#0f172a',
                'font-size': '11px',
                'text-valign': 'bottom',
                'text-halign': 'center',
                'text-margin-y': 4,
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

    // Tap a node -> ask C# to expand or collapse it.
    cy.on('tap', 'node', evt => {
        const uid = evt.target.id();
        dotNet.invokeMethodAsync('OnNodeTapped', uid);
    });
}

// Idempotently add nodes/edges. `nodes`: [{uid,key,name,type,domain,primaryUrl}].
// `edges`: [{source,target}] (uids, dependent -> dependency). `seedUid`: mark as the seed, or null.
export function addGraph(nodes, edges, seedUid) {
    const toAdd = [];
    for (const n of nodes) {
        if (cy.getElementById(n.uid).empty()) {
            toAdd.push({ group: 'nodes', data: {
                id: n.uid, uid: n.uid, name: n.name, key: n.key,
                type: n.type, domain: n.domain, url: n.primaryUrl
            }});
        }
    }
    for (const e of edges) {
        const id = e.source + '__' + e.target;
        if (cy.getElementById(id).empty()) {
            toAdd.push({ group: 'edges', data: { id: id, source: e.source, target: e.target } });
        }
    }
    if (toAdd.length) cy.add(toAdd);
    if (seedUid) cy.getElementById(seedUid).addClass('seed');
    relayout();
}

export function markExpanded(uid, expanded) {
    const n = cy.getElementById(uid);
    if (n.empty()) return;
    if (expanded) n.addClass('expanded'); else n.removeClass('expanded');
}

// Collapse: remove leaf neighbours that exist only because of `uid`
// (degree 1, not the seed, not themselves expanded).
export function collapse(uid) {
    const node = cy.getElementById(uid);
    if (node.empty()) return;
    const victims = node.neighborhood('node').filter(n =>
        n.degree(false) === 1 && !n.hasClass('seed') && !n.hasClass('expanded'));
    victims.remove();
    node.removeClass('expanded');
    relayout();
}

export function fit() { if (cy) cy.fit(undefined, 30); }

function relayout() {
    // Slice 3 replaces this with position-preserving layout.
    cy.layout({ name: 'cose', animate: false, padding: 30 }).run();
}

export function dispose() {
    if (cy) { cy.destroy(); cy = null; }
    dotNet = null;
}
```

- [ ] **Step 3:** Create `explorer-canvas.js` exactly as above.

---

## 4. Canvas host CSS (`UI\ResourceMapper.UI.Web\wwwroot\app.css`)

Cytoscape needs an explicitly-sized container. Append:

```css
/* Resource Explorer canvas */
.rm-explorer-host {
    display: flex;
    flex-direction: column;
    height: 100%;
}
.rm-explorer-canvas {
    width: 100%;
    height: calc(100vh - 180px);
    min-height: 420px;
    border: 1px solid var(--mud-palette-lines-default, #e2e8f0);
    border-radius: 8px;
    background: #f8fafc;
}
```

- [ ] **Step 4:** Append the CSS.

---

## 5. The explorer page (`UI\ResourceMapper.UI.Web\Components\Pages\ResourceExplorer.razor`)

Route `/explore/{ResourceUid}`. Initializes the module post-prerender, seeds the graph via
`IExplorerService.GetNodeAsync` (slice 1), and handles tap→expand/collapse.

```razor
@page "/explore/{ResourceUid}"
@using Microsoft.JSInterop
@using ResourceMapper.Common.Server.Explorer.Interfaces
@using ResourceMapper.Common.Shared.Explorer
@implements IAsyncDisposable
@inject IJSRuntime JS
@inject IExplorerService ExplorerService

<PageTitle>Explorer</PageTitle>

<div class="rm-explorer-host">
    @if (_error is not null)
    {
        <MudAlert Severity="Severity.Error" Class="ma-2">@_error</MudAlert>
    }
    <div class="rm-explorer-canvas" @ref="_canvasEl"></div>
</div>

@code {
    [Parameter] public string ResourceUid { get; set; } = string.Empty;

    private ElementReference _canvasEl;
    private IJSObjectReference? _module;
    private DotNetObjectReference<ResourceExplorer>? _selfRef;
    private readonly HashSet<string> _expanded = new();
    private string? _error;
    private bool _initialized;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _initialized) return;
        _initialized = true;

        // Absolute path so the module resolves the same from any /explore/{uid} route.
        _module = await JS.InvokeAsync<IJSObjectReference>(
            "import", "/js/explorer/explorer-canvas.js");
        _selfRef = DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("init", _canvasEl, _selfRef);

        await LoadNodeAsync(ResourceUid, seed: true);
        await _module.InvokeVoidAsync("fit");
    }

    private async Task LoadNodeAsync(string uid, bool seed)
    {
        var response = await ExplorerService.GetNodeAsync(uid);
        if (!response.IsSuccess() || response.ApiResponse.Data is null)
        {
            if (seed) { _error = $"Resource '{uid}' not found."; StateHasChanged(); }
            return;
        }
        var node = response.ApiResponse.Data;

        var nodes = new List<object>
        {
            NodePayload(node.ResourceUid, node.ResourceKey, node.ResourceName, node.ResourceType, node.Domain, node.PrimaryUrl)
        };
        var edges = new List<object>();
        foreach (var n in node.Neighbors)
        {
            nodes.Add(NodePayload(n.ResourceUid, n.ResourceKey, n.ResourceName, n.ResourceType, n.Domain, n.PrimaryUrl));
            // Edge points dependent -> dependency.
            var src = n.Direction == "DependsOn" ? node.ResourceUid : n.ResourceUid;
            var tgt = n.Direction == "DependsOn" ? n.ResourceUid : node.ResourceUid;
            edges.Add(new { source = src, target = tgt });
        }

        await _module!.InvokeVoidAsync("addGraph", nodes, edges, seed ? uid : null);
        _expanded.Add(uid);
        await _module.InvokeVoidAsync("markExpanded", uid, true);
    }

    [JSInvokable]
    public async Task OnNodeTapped(string uid)
    {
        if (_expanded.Contains(uid))
        {
            await _module!.InvokeVoidAsync("collapse", uid);
            _expanded.Remove(uid);
        }
        else
        {
            await LoadNodeAsync(uid, seed: false);
        }
    }

    private static object NodePayload(string uid, string key, string name, string type, string? domain, string? url)
        => new { uid, key, name, type, domain, primaryUrl = url };

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                await _module.InvokeVoidAsync("dispose");
                await _module.DisposeAsync();
            }
        }
        catch { /* circuit already torn down — nothing to clean up */ }
        _selfRef?.Dispose();
    }
}
```

- [ ] **Step 5:** Create `ResourceExplorer.razor` exactly as above.

---

## 6. Grid "Explore" launch (`UI\ResourceMapper.UI.Web\Components\Pages\Home.razor`)

Add an "Explore" `MudIconButton` beside the existing **Open details** edit button in the per-row
actions `TemplateColumn`, and an `Explore` handler beside `OpenDetails` (`NavigationManager` is
already injected as `Navigation`):

```razor
<MudIconButton Icon="@Icons.Material.Outlined.Schema" Size="Size.Small"
               aria-label="Explore dependencies"
               OnClick="@(() => Explore(context.Item.ResourceUid))" />
```
```csharp
private void Explore(string uid) =>
    Navigation.NavigateTo($"/explore/{uid}");
```

- [ ] **Step 6:** Add the icon button + `Explore` handler to `Home.razor`.

---

## UI verification hook (this IS the visible slice)

- [ ] **Step 7: Build + run.** `dotnet build` (repo root) must succeed, then
  `dotnet run --project UI/ResourceMapper.UI.Web`.
- [ ] **Step 8: Seed data if needed.** The canvas needs a resource **with relationships** to be
  interesting. If localdb still has no `ResourceRelationship` rows (slice 1 found none), insert a
  few via `sqlcmd` for manual verification — e.g. pick two existing `ResourceUid`s and
  `INSERT INTO [HTResourceMapper].[ResourceRelationship](FromResourceId,ToResourceId) ...`. Record
  what you inserted; you may leave a small demo set in place for the user to click through (note it
  in the report so they know it's test data).
- [ ] **Step 9: Drive it in a browser.** From the home grid, click the **Explore** (schema) icon on a
  resource that has dependencies. Confirm:
  1. The canvas renders with the **seed node** (amber) centered/fit.
  2. Its one-hop neighbors appear with **directed arrows** (arrow points at the dependency).
  3. **Tapping a neighbor expands** its neighbors (new nodes/edges appear; the tapped node gets the
     green "expanded" ring); **tapping an expanded node again collapses** its leaf neighbors.
  4. Expanding into an **already-shown** resource just draws the connecting edge (no duplicate node,
     no runaway) — verify with a small cycle if your data has one.
  5. **Pan** (drag background), **zoom** (wheel) work.
  6. An unknown uid (`/explore/does-not-exist`) shows the red not-found alert, no crash.
- [ ] **Step 10 (optional): Playwright smoke** per `tools/e2e/README.md` — a script that navigates to
  `/explore/{seededUid}`, waits for a `canvas` element inside `.rm-explorer-canvas`, and asserts a
  node count grew after a simulated tap. Not required to pass this slice, but note if added.

---

## Verification

1. `dotnet build` (repo root) succeeds (the `HTResourceMapperDb.sqlproj` `MSB4278` under `dotnet build` is expected).
2. `dotnet test` (full suite) — unchanged from slice 1 (this slice adds no unit tests; confirm nothing regressed).
3. Manual browser drive per Steps 8–9 above — the primary gate for this slice.

---

## Out of scope (later slices)

- **Auto-layout that preserves manual positions, drag-to-move persistence, collapse-all, and
  remove-node with reachability cleanup** → slice 3. (This slice re-runs `cose` on every add and does
  only simple leaf-collapse.)
- **Display presets, hover tooltip, primary-URL link click, right-click menu, open-in-editor** →
  slice 4. (This slice shows a fixed Name label and has no per-node menu; the `url`/`key`/`domain`
  data is already carried on each node's `data` for slice 4 to use.)
- **Save / Open / Share / persistence, client identity** → slices 5–7.
- **Export (PNG/SVG/clipboard/print)** → slice 8.
- **Demo seed script** for realistic explorer data → a small follow-up (noted in slice 1).

---

## Execution notes

_(Written after execution — record deviations, the module import-path behavior actually observed,
any Cytoscape sizing/timing issues, what demo relationship data was inserted, and commit hash(es).
Then mark slice #2 **Done** ✓ / slice #3 **Next** in the master list, and commit.)_
