# Slice #3 — Arrangement

## Context

Third slice of the [Resource Explorer build](./00-implementation-plan-list.md), building directly on
slice 2's canvas. See the [design spec](./resource-explorer-design-v1.md) (§4.2, §4.3, §5.2) and the
master list. Slice 2 (**Done**) is the only dependency.

This slice makes the canvas **arrangeable and tidy-able**, which is what turns the slice-2 proof-of-
concept into something you'd actually use to build and read a diagram:

- **Position-preserving expansion** — dragging nodes into a layout that *stays put* when you expand
  more (slice 2 re-ran the whole layout on every add, scattering your arrangement).
- **Collapse all** — one action back to just the seed.
- **Remove node with reachability cleanup** — remove a node and drop anything no longer reachable
  from the seed; removing the seed clears the canvas.
- Plus the **interop-safety wrapper** deferred from the slice-2 review.

It modifies only the two slice-2 files (`explorer-canvas.js`, `ResourceExplorer.razor`) plus a small
CSS/host addition. No backend changes.

**Key decisions (this slice):**

- **Auto-layout runs only on the seed load; expansions place new nodes around their parent** without
  disturbing existing positions. Dragged positions therefore survive further expansion (design §5.2:
  "auto-layout once, then manual"). Persisting positions to a saved diagram is **slice 7**; this slice
  keeps them in the live Cytoscape instance only.
- **Interim triggers** (the floating toolbar is slice 7, the right-click menu is slice 4): a minimal
  **"Collapse all" button** above the canvas, and **Shift-click a node to remove it**. Slice 4 moves
  remove into the right-click menu; slice 7 moves collapse-all into the toolbar.
- **Reachability is undirected** — a node stays if it's connected to the seed by *any* path
  (arrow direction is about dependency, not visibility). Implemented with Cytoscape's `.component()`.
- **Safe interop:** all `IJSObjectReference` calls that can resolve after teardown go through a
  `SafeInvokeAsync` helper that swallows `JSDisconnectedException` (fixes the slice-2 review Minor).
- **No unit tests** (UI/JS); verified by driving the app (the visible slice).

---

## 1. JS module changes (`UI\ResourceMapper.UI.Web\wwwroot\js\explorer\explorer-canvas.js`)

### 1a. Track the seed id and route Shift-click to removal

Add a module-level `seedId` and change the tap handler in `init`.

Add near the top, beside `let cy = null;` / `let dotNet = null;`:

```javascript
let seedId = null;
```

Replace the tap handler inside `init` (the `cy.on('tap', 'node', ...)` block) with:

```javascript
    // Tap a node -> expand/collapse; Shift+tap -> remove (interim trigger; slice 4 adds a menu).
    cy.on('tap', 'node', evt => {
        const uid = evt.target.id();
        const remove = !!(evt.originalEvent && evt.originalEvent.shiftKey);
        dotNet.invokeMethodAsync(remove ? 'OnNodeRemoveRequested' : 'OnNodeTapped', uid);
    });
```

### 1b. Position-preserving `addGraph`

Replace the entire `addGraph` function with this version (new 4th param `expandFromUid`; layout runs
only on the seed load, otherwise new nodes are placed around their parent):

```javascript
// Idempotently add nodes/edges. nodes: [{uid,key,name,type,domain,primaryUrl}].
// edges: [{source,target}] (uids, dependent -> dependency). seedUid: mark as seed or null.
// expandFromUid: when set (an expansion), place new nodes around that parent WITHOUT relayout,
// preserving existing (possibly dragged) positions. When null (seed load), run the initial layout.
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
        // Seed load — lay the whole thing out once.
        cy.layout({ name: 'cose', animate: false, padding: 30 }).run();
    } else if (added.length) {
        placeAround(expandFromUid, added);
    }
}

// Place newly-added nodes in a ring around their parent, leaving existing nodes where they are.
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
```

Delete the old `relayout()` function (it's replaced; nothing calls it anymore).

### 1c. `collapse` — stop relaying out (preserve positions)

Replace `collapse` with this version (identical leaf logic, minus the `relayout()` call):

```javascript
// Collapse: remove leaf neighbours that exist only because of `uid`
// (degree 1, not the seed, not themselves expanded). Positions of survivors are preserved.
export function collapse(uid) {
    const node = cy.getElementById(uid);
    if (node.empty()) return;
    const victims = node.neighborhood('node').filter(n =>
        n.degree(false) === 1 && !n.hasClass('seed') && !n.hasClass('expanded'));
    victims.remove();
    node.removeClass('expanded');
}
```

### 1d. `collapseAll` and `removeNode`

Add these two exported functions (both return the surviving node-id array so C# can reconcile its
expanded-set):

```javascript
// Collapse everything back to just the seed.
export function collapseAll() {
    if (!seedId) return [];
    cy.nodes().filter(n => n.id() !== seedId).remove(); // edges are removed with their nodes
    cy.getElementById(seedId).removeClass('expanded');
    fit();
    return cy.nodes().map(n => n.id());
}

// Remove a node, then drop any node no longer reachable (undirected) from the seed.
// Removing the seed clears the canvas. Positions of survivors are preserved (no relayout).
export function removeNode(uid) {
    const node = cy.getElementById(uid);
    if (node.empty()) return cy.nodes().map(n => n.id());
    if (uid === seedId) { cy.elements().remove(); seedId = null; return []; }
    node.remove();
    const seed = cy.getElementById(seedId);
    if (seed.empty()) return cy.nodes().map(n => n.id());
    const keep = seed.component();          // undirected connected component containing the seed
    cy.nodes().not(keep).remove();
    return cy.nodes().map(n => n.id());
}
```

- [x] **Step 1:** Apply 1a–1d to `explorer-canvas.js` (add `seedId`; replace tap handler, `addGraph`, `collapse`; add `placeAround`, `collapseAll`, `removeNode`; delete `relayout`).

---

## 2. Page changes (`UI\ResourceMapper.UI.Web\Components\Pages\ResourceExplorer.razor`)

### 2a. Control bar + hint (markup)

Replace the `<div class="rm-explorer-host">…</div>` block with:

```razor
<div class="rm-explorer-host">
    <div class="rm-explorer-bar">
        <MudButton Size="Size.Small" Variant="Variant.Outlined"
                   StartIcon="@Icons.Material.Outlined.UnfoldLess"
                   OnClick="CollapseAllAsync">Collapse all</MudButton>
        <MudText Typo="Typo.caption" Class="ml-3 mud-text-secondary">
            Tap a node to expand / collapse · Shift-click to remove · drag to arrange
        </MudText>
    </div>
    @if (_error is not null)
    {
        <MudAlert Severity="Severity.Error" Class="ma-2">@_error</MudAlert>
    }
    <div class="rm-explorer-canvas" @ref="_canvasEl"></div>
</div>
```

### 2b. `@code` changes

Add a safe-invoke helper, thread `expandFromUid` through `LoadNodeAsync`, add the collapse-all and
remove handlers, and route all interop through the safe helper.

Replace the whole `@code { … }` block with:

```razor
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

        _module = await JS.InvokeAsync<IJSObjectReference>(
            "import", "/js/explorer/explorer-canvas.js");
        _selfRef = DotNetObjectReference.Create(this);
        await SafeInvokeAsync("init", _canvasEl, _selfRef);

        await LoadNodeAsync(ResourceUid, seed: true, expandFromUid: null);
        await SafeInvokeAsync("fit");
    }

    private async Task LoadNodeAsync(string uid, bool seed, string? expandFromUid)
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
            var src = n.Direction == "DependsOn" ? node.ResourceUid : n.ResourceUid;
            var tgt = n.Direction == "DependsOn" ? n.ResourceUid : node.ResourceUid;
            edges.Add(new { source = src, target = tgt });
        }

        await SafeInvokeAsync("addGraph", nodes, edges, seed ? uid : null, expandFromUid);
        _expanded.Add(uid);
        await SafeInvokeAsync("markExpanded", uid, true);
    }

    [JSInvokable]
    public async Task OnNodeTapped(string uid)
    {
        if (_expanded.Contains(uid))
        {
            await SafeInvokeAsync("collapse", uid);
            _expanded.Remove(uid);
        }
        else
        {
            await LoadNodeAsync(uid, seed: false, expandFromUid: uid);
        }
    }

    [JSInvokable]
    public async Task OnNodeRemoveRequested(string uid)
    {
        var surviving = await SafeInvokeResultAsync<string[]>("removeNode", uid) ?? System.Array.Empty<string>();
        _expanded.IntersectWith(surviving);
    }

    private async Task CollapseAllAsync()
    {
        await SafeInvokeAsync("collapseAll");
        _expanded.Clear();
    }

    private static object NodePayload(string uid, string key, string name, string type, string? domain, string? url)
        => new { uid, key, name, type, domain, primaryUrl = url };

    private async Task SafeInvokeAsync(string identifier, params object?[] args)
    {
        if (_module is null) return;
        try { await _module.InvokeVoidAsync(identifier, args); }
        catch (JSDisconnectedException) { /* circuit gone — nothing to do */ }
        catch (ObjectDisposedException) { /* module disposed mid-call */ }
    }

    private async Task<T?> SafeInvokeResultAsync<T>(string identifier, params object?[] args)
    {
        if (_module is null) return default;
        try { return await _module.InvokeAsync<T>(identifier, args); }
        catch (JSDisconnectedException) { return default; }
        catch (ObjectDisposedException) { return default; }
    }

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
        catch (JSDisconnectedException) { /* circuit already torn down */ }
        catch (ObjectDisposedException) { /* already disposed */ }
        _selfRef?.Dispose();
    }
}
```

Add the required `using` for the exception types at the top of the page (with the other `@using`
lines): `@using Microsoft.JSInterop` is already present and provides `JSDisconnectedException`;
`ObjectDisposedException` is in `System` (available by default). No new `@using` needed.

- [x] **Step 2:** Apply 2a–2b to `ResourceExplorer.razor`.

---

## 3. CSS (`UI\ResourceMapper.UI.Web\wwwroot\app.css`)

Add the control-bar rule and shrink the canvas height to make room for it. Replace the slice-2
`.rm-explorer-canvas` height and add `.rm-explorer-bar`:

```css
.rm-explorer-bar {
    display: flex;
    align-items: center;
    padding: 8px 4px;
}
.rm-explorer-canvas {
    width: 100%;
    height: calc(100vh - 230px);
    min-height: 400px;
    border: 1px solid var(--mud-palette-lines-default, #e2e8f0);
    border-radius: 8px;
    background: #f8fafc;
}
```

(Replace the existing `.rm-explorer-canvas` block from slice 2 with the one above — only the
`height` changed; keep the rest. Add `.rm-explorer-bar` new.)

- [x] **Step 3:** Update `app.css`.

---

## UI verification hook (visible slice)

- [x] **Step 4: Build + run.** `dotnet build` clean; `dotnet run --project UI/ResourceMapper.UI.Web`.
- [x] **Step 5: Drive it** (reuse the slice-2 demo data around `AAS001`, or reseed similarly). From
  the grid, Explore a resource with dependencies, then confirm:
  1. **Drag** several nodes into a deliberate arrangement.
  2. **Expand** another node — the newly-added neighbors appear **around the tapped node** and your
     previously-dragged nodes **stay put** (no full reshuffle).
  3. **Shift-click** a mid-graph node — it disappears, and any node that was only reachable *through*
     it also disappears, while nodes still connected to the seed remain.
  4. **Shift-click the seed** — the canvas clears.
  5. **Collapse all** — returns to just the seed node, re-fit.
  6. Re-expand after collapse-all works (no stale "already expanded" state).
- [x] **Step 6 (optional): Playwright** per `tools/e2e` — assert node count drops to 1 after
  Collapse-all, and that an expand-after-drag keeps a dragged node's position roughly stable.

---

## Verification

1. `dotnet build` clean (expected sqlproj `MSB4278` aside); `dotnet test` unchanged from slice 2 (no new unit tests; confirm no regression).
2. Manual browser drive per Steps 4–5 — the primary gate.

---

## Out of scope (later slices)

- **Right-click context menu** (the real home for remove / open-external / open-in-editor) → slice 4.
  This slice's Shift-click remove and "Collapse all" button are **interim**.
- **Display presets, tooltip, primary-URL link, open-in-editor** → slice 4.
- **Persisting positions / the diagram** (Save/Open/Share), floating toolbar → slices 5–7.
- **Export** → slice 8.

---

## Execution notes

Applied Steps 1–3 exactly as specified (1a–1d in `explorer-canvas.js`, 2a–2b in
`ResourceExplorer.razor`, the CSS block in `app.css`); no deviations from the brief's code. Verified
with `dotnet build` (clean, only the expected sqlproj `MSB4278`) and `dotnet test` (398 total, 2
pre-existing/unrelated `HT.Api.Service.Contracts.Tests` failures, everything else — including all
396 other tests — green, no regression vs slice 2).

**Demo data:** slice 2's `AAS001`-centered `ResourceRelationship` rows were still present (5 rows),
but they only reached one hop from the seed in every direction, so expanding a *non-seed* node never
introduced a genuinely new node — insufficient to prove position-preserving placement. Added one
extra demo row via `sqlcmd` (`APP007` (User Authentication) → `APP008` (Notification Service)) to
`(localdb)\MSSQLLocalDB\ResourceMapper` so expanding `APP007` after the seed load pulls in a real
second-hop node. Left in place as reusable demo data (not cleaned up), matching slice 2's precedent.

**Browser verification:** driven headlessly via Playwright (`playwright-core` + system Chrome), not
just eyeballed. Real user gestures throughout (mouse drag, click, shift+click, the actual "Collapse
all" button) — no direct JS function calls bypassing the UI. To get hard node-count/position
evidence without touching any shipped file, the driver wraps the global `cytoscape` factory via
`page.addInitScript` *before* navigation (intercepting `globalThis.cytoscape = factory()` from the
vendor UMD bundle) so the app's own `cy = cytoscape({...})` call in `init()` also stashes the
instance on `window.__cy`. This is a test-harness-only interception living in the driver script, not
a change to `explorer-canvas.js`.

Verified, with hard assertions plus screenshots:
- Seed load: 5 nodes (`AAS001`,`AI001`,`CONFIG001`,`REDIS001`,`APP007`).
- Drag `CONFIG001` and `AI001` to new spots (rendered position moved > 50px each).
- Expand `APP007` → node count 6 (`APP008` added); `CONFIG001`/`AI001` positions **unchanged**
  (< 3px, i.e. floating-point-only drift) confirming **no relayout on expansion**; `APP007` itself
  also stayed put; `APP008` landed **exactly 150 model units** from `APP007` — `placeAround`'s ring
  radius, confirmed in *model* space (screen-space distance scales with zoom, which is why the first
  attempt at this check — using rendered/screen distance — was wrong and had to be corrected to
  model-space `position()`).
- Shift-click `APP007` (a mid-graph node, connected to both the seed *and* `APP008`) → node count 4:
  `APP007` and `APP008` both removed (`APP008` was only reachable through `APP007`), while
  `AAS001`/`AI001`/`CONFIG001`/`REDIS001` (each with their own direct edge to the seed) survive —
  confirms `.component()`-based undirected reachability cleanup, not a naive "remove neighbors" rule.
- Shift-click the seed (`AAS001`) → node count 0 (canvas fully cleared).
- Reload, re-expand `APP007` (count 6), click the **Collapse all** button → node count 1 (`AAS001`
  only), re-fit.
- Tap the seed again post-collapse-all → node count back to 5 — confirms `_expanded` is correctly
  cleared client-side (no stale "already expanded" state blocking re-expansion).

One pre-existing, unrelated console `404` (`GET /favicon.ico`) observed in both the seed-load and
reload navigations — present before this slice's changes, not investigated further.

Commit: see git log (`feat(ui): slice #3 — arrangement`).

Slice #3 marked **Done** ✓ / slice #4 **Next** in the master list.
