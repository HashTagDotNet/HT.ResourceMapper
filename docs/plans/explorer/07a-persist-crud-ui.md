# Slice #7a — Persist UI: Save / Save-As / Open-Recent / Delete

## Context

First half of slice 7 (see the [master list](./00-implementation-plan-list.md) — slice 7 is split
into **7a** owner-CRUD and **7b** share). See the [design spec](./resource-explorer-design-v1.md)
(§6, §7). Depends on slices 4 (canvas/actions), 5 (`IDiagramService`), 6 (`clientId`).

This slice makes diagrams **persist**: a toolbar with **Save / Save As / Open-Recent (with per-row
Delete)**, backed by `IDiagramService` (slice 5) using the `clientId` (slice 6). The canvas
serializes to/from `DiagramJson`. **Sharing (link + read-only + Save-a-copy) is slice 7b.**

**Key decisions (this slice):**

- **`DiagramJson` is self-contained** — it stores each node's display fields (name/key/type/domain/url)
  **and** position + expanded flag, plus edges, seed, and preset. Loading rebuilds the canvas from JSON
  with **no server round-trips** (accepts "no live refresh", design §8). Shape:
  `{ seedUid, preset, nodes:[{uid,name,key,type,domain,url,x,y,expanded}], edges:[{source,target}] }`.
- **The interim control bar becomes the toolbar** — Save/Save-As/Open move in beside the relocated
  Fit / Collapse-all / Display-preset / client caption. (Making it a *floating overlay* is a slice-9
  polish item; a top toolbar delivers the actions now.)
- **Dialogs follow the existing editor dialog pattern** — model `DiagramNameDialog` and
  `OpenDiagramDialog` on `UI/ResourceMapper.UI.Web/Components/Editor/ConfirmDeleteDialog.razor` /
  `UnsavedChangesDialog.razor` / `TagDefinitionDialog.razor` and how `ResourceEditor.razor` opens them
  via `IDialogService` — those are the version-correct (MudBlazor 9.5) conventions for the cascading
  dialog-instance type, `DialogResult`, and `DialogService.ShowAsync`. **Read one before writing these.**
- **Delete lives per-row in the Open dialog** (delete any of your diagrams), satisfying the "Delete"
  action without a separate toolbar button.
- **No unit tests** (UI); verified by driving (the visible slice). `IDiagramService` already has its
  own unit tests from slice 5.

---

## 1. JS: serialize / load (`wwwroot\js\explorer\explorer-canvas.js`)

Add these exports (near `fit`/`dispose`). `serializeJson`/`loadJson` are the string-in/string-out
functions the page uses; `loadJson` returns the expanded-node uids so C# can rebuild its `_expanded`
set.

```javascript
// ---- persistence (serialize / load) -------------------------------------

function serialize() {
    return {
        seedUid: seedId,
        preset: currentPreset,
        nodes: cy.nodes().map(n => {
            const p = n.position();
            const d = n.data();
            return {
                uid: n.id(), name: d.name, key: d.key, type: d.type,
                domain: d.domain, url: d.url, x: p.x, y: p.y,
                expanded: n.hasClass('expanded')
            };
        }),
        edges: cy.edges().map(e => ({ source: e.data('source'), target: e.data('target') }))
    };
}

export function serializeJson() {
    return JSON.stringify(serialize());
}

// Rebuild the canvas from a serialized diagram (positions preserved, no layout, no server calls).
// Returns the list of expanded node uids so C# can restore its expanded set.
export function loadJson(json) {
    const graph = JSON.parse(json);
    cy.elements().remove();
    seedId = graph.seedUid || null;
    currentPreset = graph.preset || 'nameType';

    const els = [];
    for (const n of graph.nodes || []) {
        els.push({ group: 'nodes',
            data: { id: n.uid, uid: n.uid, name: n.name, key: n.key, type: n.type, domain: n.domain, url: n.url },
            position: { x: n.x, y: n.y } });
    }
    for (const e of graph.edges || []) {
        els.push({ group: 'edges', data: { id: e.source + '__' + e.target, source: e.source, target: e.target } });
    }
    cy.add(els);

    if (seedId) cy.getElementById(seedId).addClass('seed');
    for (const n of graph.nodes || []) {
        if (n.expanded) cy.getElementById(n.uid).addClass('expanded');
    }
    applyLabels();
    fit();

    return (graph.nodes || []).filter(n => n.expanded).map(n => n.uid);
}

export function currentPresetValue() { return currentPreset; }
```

- [ ] **Step 1:** Add the four functions to `explorer-canvas.js`.

---

## 2. Dialog components (`UI\ResourceMapper.UI.Web\Components\Explorer\`)

> **Read `Components/Editor/UnsavedChangesDialog.razor` (or `ConfirmDeleteDialog.razor`) first** and copy
> its MudDialog conventions exactly — the cascading dialog-instance parameter type, `MudDialog.Close(...)`/
> `Cancel()`, and `DialogResult.Ok(...)`. Use those same types here (do not guess the MudBlazor 9.5 API).

### 2a. `DiagramNameDialog.razor` — name prompt for Save-As

Structure (adapt the cascading-instance type to match the editor dialogs):

```razor
@namespace ResourceMapper.UI.Web.Components.Explorer

<MudDialog>
    <DialogContent>
        <MudTextField @bind-Value="Name" Label="Diagram name" Immediate="true" MaxLength="200" />
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary" OnClick="Submit"
                   Disabled="@string.IsNullOrWhiteSpace(Name)">Save</MudButton>
    </DialogActions>
</MudDialog>

@code {
    // Match the cascading instance TYPE used by the editor dialogs (e.g. IMudDialogInstance in MudBlazor 9.5).
    [CascadingParameter] public /* IMudDialogInstance */ object MudDialog { get; set; } = default!;
    [Parameter] public string Name { get; set; } = string.Empty;

    private void Submit() => ((dynamic)MudDialog).Close(DialogResult.Ok(Name.Trim()));
    private void Cancel() => ((dynamic)MudDialog).Cancel();
}
```
> Replace the `object`/`dynamic` placeholders with the concrete instance type from the editor dialogs —
> they are only here to avoid asserting a version-specific type name. The editor dialogs show the real one.

### 2b. `OpenDiagramDialog.razor` — list + open + per-row delete

```razor
@namespace ResourceMapper.UI.Web.Components.Explorer
@using ResourceMapper.Common.Server.Explorer.Interfaces
@using ResourceMapper.Common.Shared.Explorer.Diagrams
@inject IDiagramService DiagramService

<MudDialog>
    <DialogContent>
        @if (_loading)
        {
            <MudProgressCircular Indeterminate="true" />
        }
        else if (_items.Count == 0)
        {
            <MudText Typo="Typo.body2">No saved diagrams yet.</MudText>
        }
        else
        {
            <MudList T="string" Dense="true">
                @foreach (var d in _items)
                {
                    <MudListItem T="string">
                        <div class="d-flex align-center" style="gap:8px">
                            <MudText Class="flex-grow-1">@d.Name</MudText>
                            <MudText Typo="Typo.caption" Class="mud-text-secondary">@d.UpdatedOnUtc</MudText>
                            <MudButton Size="Size.Small" Color="Color.Primary" OnClick="@(() => Open(d))">Open</MudButton>
                            <MudIconButton Size="Size.Small" Icon="@Icons.Material.Outlined.Delete"
                                           aria-label="Delete diagram" OnClick="@(() => Delete(d))" />
                        </div>
                    </MudListItem>
                }
            </MudList>
        }
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Close</MudButton>
    </DialogActions>
</MudDialog>

@code {
    // Match the editor dialogs' cascading instance type.
    [CascadingParameter] public /* IMudDialogInstance */ object MudDialog { get; set; } = default!;
    [Parameter] public string ClientId { get; set; } = string.Empty;

    private readonly List<DiagramListItem> _items = new();
    private bool _loading = true;

    protected override async Task OnInitializedAsync() => await RefreshAsync();

    private async Task RefreshAsync()
    {
        _loading = true;
        _items.Clear();
        var resp = await DiagramService.ListForClientAsync(ClientId);
        if (resp.IsSuccess() && resp.ApiResponse.Data is not null)
            _items.AddRange(resp.ApiResponse.Data);
        _loading = false;
    }

    private void Open(DiagramListItem d) => ((dynamic)MudDialog).Close(DialogResult.Ok(d.ShareId));

    private async Task Delete(DiagramListItem d)
    {
        await DiagramService.DeleteAsync(ClientId, d.DiagramUid);
        await RefreshAsync();
    }

    private void Cancel() => ((dynamic)MudDialog).Cancel();
}
```
> As in 2a, replace the `object`/`dynamic` placeholders with the concrete dialog-instance type from the
> editor dialogs. Confirm `MudList`/`MudListItem` generic usage matches the MudBlazor 9.5 API the codebase
> already uses (grep the editor components).

- [ ] **Step 2:** Create both dialog components, matching the editor dialogs' concrete MudDialog types/usage.

---

## 3. Page: toolbar + persistence wiring (`Components\Pages\ResourceExplorer.razor`)

### 3a. Injections + usings

Add at the top (with existing usings):

```razor
@using ResourceMapper.Common.Server.Explorer.Interfaces
@using ResourceMapper.Common.Shared.Explorer.Diagrams
@using ResourceMapper.Common.Shared.Explorer.Diagrams.Contracts
@using ResourceMapper.UI.Web.Components.Explorer
@inject IDiagramService DiagramService
@inject IDialogService DialogService
@inject ISnackbar Snackbar
```

### 3b. State fields (beside `_clientId` / `_preset`)

```csharp
    private string _seedUid = string.Empty;
    private string? _diagramUid;
    private string? _shareId;
    private string _diagramName = string.Empty;
```

Set `_seedUid = ResourceUid;` at the start of `OnAfterRenderAsync`'s first-render block (before the
seed `LoadNodeAsync`), so a freshly-explored diagram knows its seed.

### 3c. Toolbar markup

Replace the current `<div class="rm-explorer-bar"> … </div>` block with:

```razor
    <div class="rm-explorer-bar">
        <MudButton Size="Size.Small" Variant="Variant.Filled" Color="Color.Primary"
                   StartIcon="@Icons.Material.Outlined.Save" OnClick="SaveAsync">Save</MudButton>
        <MudButton Size="Size.Small" Variant="Variant.Outlined" Class="ml-1"
                   StartIcon="@Icons.Material.Outlined.SaveAs" OnClick="SaveAsAsync">Save As</MudButton>
        <MudButton Size="Size.Small" Variant="Variant.Outlined" Class="ml-1"
                   StartIcon="@Icons.Material.Outlined.FolderOpen" OnClick="OpenAsync">Open</MudButton>
        <MudDivider Vertical="true" FlexItem="true" Class="mx-2" />
        <MudTooltip Text="Fit to screen">
            <MudIconButton Size="Size.Small" Icon="@Icons.Material.Outlined.CenterFocusStrong"
                           aria-label="Fit" OnClick="FitAsync" />
        </MudTooltip>
        <MudButton Size="Size.Small" Variant="Variant.Outlined" Class="ml-1"
                   StartIcon="@Icons.Material.Outlined.UnfoldLess" OnClick="CollapseAllAsync">Collapse all</MudButton>
        <MudSelect T="string" Value="_preset" ValueChanged="OnPresetChanged"
                   Dense="true" Margin="Margin.Dense" Variant="Variant.Outlined"
                   Class="ml-2" Style="max-width:170px" Label="Display">
            <MudSelectItem Value="@("name")">Name only</MudSelectItem>
            <MudSelectItem Value="@("nameType")">Name + Type</MudSelectItem>
            <MudSelectItem Value="@("detailed")">Detailed</MudSelectItem>
        </MudSelect>
        <MudSpacer />
        <MudText Typo="Typo.body2" Class="mr-3">
            @(string.IsNullOrWhiteSpace(_diagramName) ? "(unsaved)" : _diagramName)
        </MudText>
        @if (!string.IsNullOrEmpty(_clientId))
        {
            <MudText Typo="Typo.caption" Class="mud-text-secondary mr-1">
                Client: @(_clientId.Length >= 8 ? _clientId[..8] : _clientId)…
            </MudText>
            <MudTooltip Text="Copy your client id (carry it to another browser)">
                <MudIconButton Icon="@Icons.Material.Outlined.ContentCopy" Size="Size.Small"
                               aria-label="Copy client id" OnClick="CopyClientIdAsync" />
            </MudTooltip>
        }
    </div>
```

(This subsumes the slice-3/4/6 control-bar contents — Collapse-all, Display select, client caption/copy
are now here; the free-text hint is dropped in favor of the toolbar + tooltips.)

### 3d. Handlers (add beside the existing `OnPresetChanged` / `CopyClientIdAsync`)

```csharp
    private async Task FitAsync() => await SafeInvokeAsync("fit");

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_diagramName)) { await SaveAsAsync(); return; }
        await PersistAsync(_diagramUid, _diagramName);
    }

    private async Task SaveAsAsync()
    {
        var parameters = new DialogParameters { ["Name"] = _diagramName };
        var dialog = await DialogService.ShowAsync<DiagramNameDialog>("Save diagram as", parameters);
        var result = await dialog.Result;
        if (result is null || result.Canceled) return;
        var name = result.Data as string;
        if (string.IsNullOrWhiteSpace(name)) return;
        await PersistAsync(null, name!);   // null uid => new diagram
    }

    private async Task PersistAsync(string? diagramUid, string name)
    {
        var json = await SafeInvokeResultAsync<string>("serializeJson") ?? "{}";
        var request = new SaveDiagramRequest
        {
            DiagramUid = diagramUid,
            Name = name,
            SeedResourceUid = _seedUid,
            DisplayPreset = _preset,
            DiagramJson = json
        };
        var resp = await DiagramService.SaveAsync(_clientId, request);
        if (resp.IsSuccess() && resp.ApiResponse.Data is not null)
        {
            _diagramUid = resp.ApiResponse.Data.DiagramUid;
            _shareId = resp.ApiResponse.Data.ShareId;
            _diagramName = name;
            Snackbar.Add($"Saved “{name}”.", Severity.Success);
            StateHasChanged();
        }
        else
        {
            Snackbar.Add("Save failed.", Severity.Error);
        }
    }

    private async Task OpenAsync()
    {
        var parameters = new DialogParameters { ["ClientId"] = _clientId };
        var dialog = await DialogService.ShowAsync<OpenDiagramDialog>("Open diagram", parameters);
        var result = await dialog.Result;
        if (result is null || result.Canceled) return;
        var shareId = result.Data as string;
        if (string.IsNullOrWhiteSpace(shareId)) return;

        var resp = await DiagramService.GetByShareIdAsync(shareId!);
        if (!resp.IsSuccess() || resp.ApiResponse.Data is null)
        {
            Snackbar.Add("Could not open diagram.", Severity.Error);
            return;
        }
        var model = resp.ApiResponse.Data;

        _diagramUid = model.DiagramUid;
        _shareId = model.ShareId;
        _diagramName = model.Name;
        _seedUid = model.SeedResourceUid;
        _preset = string.IsNullOrWhiteSpace(model.DisplayPreset) ? "nameType" : model.DisplayPreset;

        var expanded = await SafeInvokeResultAsync<string[]>("loadJson", model.DiagramJson);
        _expanded.Clear();
        if (expanded is not null) foreach (var u in expanded) _expanded.Add(u);

        Snackbar.Add($"Opened “{model.Name}”.", Severity.Success);
        StateHasChanged();
    }
```

- [ ] **Step 3:** Apply 3a–3d.

---

## UI verification hook (visible slice)

- [ ] **Step 4: Build + run.** `dotnet build` clean; `dotnet run --project UI/ResourceMapper.UI.Web`.
- [ ] **Step 5: Drive it** at `/explore/DEMOEXP-checkout` (the demo graph):
  1. Expand a few nodes, drag them into an arrangement, pick a Display preset.
  2. **Save As** → enter a name → toolbar shows the name, a "Saved" snackbar appears.
  3. Reload the page (fresh `/explore/DEMOEXP-checkout`), then **Open** → the saved diagram is listed →
     Open it → the canvas returns to your **exact arrangement, expansion, and preset** (no re-layout).
  4. Rearrange, **Save** (no prompt — updates in place); reopen → the update persisted.
  5. **Save As** a second copy with a different name → both appear in Open.
  6. In **Open**, **Delete** one → it disappears from the list.
- [ ] **Step 6 (optional): Playwright** — Save-As then reopen and assert node count + a node's position
  match; assert an Open-dialog row count drops after Delete.

---

## Verification

1. `dotnet build` clean (sqlproj `MSB4278` aside); `dotnet test` unchanged (no new unit tests).
2. Manual browser drive per Steps 4–5 — the gate. Diagrams round-trip through `(localdb)` (the
   `Diagram` table from slice 5); you can confirm rows via `sqlcmd` if desired.

---

## Out of scope (→ slice 7b)

- **Share link** (`/explore/shared/{ShareId}`), **read-only** load when `clientId` ≠ owner, and
  **"Save a copy to mine"** → **slice 7b**. (7b also decides whether the read-only view should suppress
  the owner's `DiagramUid` — the slice-5 carried note.)
- **Recovery-key restore UI** (paste an id) → later; `ClientIdentity.SetAsync` exists for it.
- **Floating-overlay toolbar positioning**, menu restyle → slice 9 (polish).

---

## Execution notes

_(Written after execution — record the concrete MudBlazor 9.5 dialog-instance type used, any
`MudList`/`DialogService` API reconciliations against the editor dialogs, serialize/load round-trip
behavior observed, and commit hash(es). Then mark slice #7a done / 7b next in the master list, and commit.)_
