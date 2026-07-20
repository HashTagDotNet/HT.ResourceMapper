# Slice #7b — Share (link, read-only, Save-a-copy)

## Context

Second half of slice 7 (see the [master list](./00-implementation-plan-list.md)). See the
[design spec](./resource-explorer-design-v1.md) (§7.3, §8). Depends on slice 7a (persist UI) and
slice 5/6.

This slice adds **sharing**: a **Share** button that copies a `/explore/shared/{ShareId}` link;
opening that link **loads the diagram read-only** when you're not its owner, with a **"Save a copy
to mine"** button that clones it into your own (editable) diagram. It also **resolves the slice-5
carried note** (don't leak the owner's private `DiagramUid` to a read-only recipient) and adds the
**`loadJson` malformed-JSON guard** from the 7a review.

**Key decisions (this slice):**

- **Ownership is computed server-side, in `GetByShareIdAsync`** — the method gains a `callerClientId`
  argument, sets `DiagramModel.IsOwner`, and **blanks `DiagramUid` for non-owners** (they never need
  the owner handle; they clone via `ShareId`). The page reads `IsOwner` to choose editable vs.
  read-only. This replaces the slice-7a `GetByShareIdAsync(shareId)` call (now passes `_clientId`).
- **One page, two routes** — `ResourceExplorer.razor` adds `@page "/explore/shared/{ShareId}"` beside
  the existing `/explore/{ResourceUid}` (different segment counts → no routing conflict).
- **Read-only mode** disables Save and Delete and shows a banner + **"Save a copy to mine"** (→
  `SaveCopyAsync`), which turns the read-only view into your own editable diagram.
- **Recovery-key *restore*** (paste an id) is still deferred — `ClientIdentity.SetAsync` exists for it.
- No new UI unit tests; `IDiagramService` gets one updated + one new test for the ownership flag.

---

## 1. Server: ownership flag on share read

### 1a. Shared DTO — add `IsOwner` (`Modules\Common\ResourceMapper.Common.Shared\Explorer\Diagrams\DiagramModel.cs`)

Add the property:

```csharp
        public bool IsOwner { get; set; }
```

### 1b. Service signature + logic (`Modules\Common\ResourceMapper.Common.Server\Explorer\`)

In `Interfaces\IDiagramService.cs`, change `GetByShareIdAsync` to take the caller's client id:

```csharp
        Task<ApiServiceResponse<DiagramModel>> GetByShareIdAsync(string shareId, string? callerClientId, CancellationToken cancellationToken = default);
```

In `DiagramService.cs`, update the method: after fetching the row, set `IsOwner` and suppress the
`DiagramUid` for non-owners:

```csharp
        public async Task<ApiServiceResponse<DiagramModel>> GetByShareIdAsync(string shareId, string? callerClientId, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<DiagramModel>();
            try
            {
                if (string.IsNullOrWhiteSpace(shareId))
                {
                    builder.Validation.AddValidation("shareId", "Share id is required");
                    return builder.BuildResponse();
                }

                var row = await _repo.GetByShareIdAsync(shareId, cancellationToken);
                if (row is null)
                {
                    builder.Errors.AddError(CallStatusCode.NotFound, "Diagram Not Found",
                        $"No diagram for share id '{shareId}'.", "shareId");
                    return builder.BuildResponse();
                }

                var model = ToModel(row);
                model.IsOwner = !string.IsNullOrEmpty(callerClientId) && row.ClientId == callerClientId;
                if (!model.IsOwner)
                {
                    model.DiagramUid = string.Empty;   // don't leak the owner handle to a read-only recipient
                }

                builder.Data.Set(model);
                return builder.BuildResponse();
            }
            catch (System.Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }
```

- [ ] **Step 1:** Apply 1a + 1b.

### 1c. Update/extend the unit tests (`_Tests\...\Explorer\DiagramServiceTests.cs`)

The existing `GetByShareIdAsync_Missing_ReturnsNotFound` now needs the extra arg — update its call to
`_sut.GetByShareIdAsync("nope", "client1", CancellationToken.None)`. Add two tests to the
`#region GetByShareIdAsync / SaveCopyAsync`:

```csharp
        [Fact]
        public async Task GetByShareIdAsync_CallerIsOwner_SetsIsOwnerTrueAndKeepsDiagramUid()
        {
            _repo.Setup(r => r.GetByShareIdAsync("s1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DiagramRow { DiagramUid = "d1", ShareId = "s1", ClientId = "owner", Name = "N", SeedResourceUid = "seed", DisplayPreset = "nameType", DiagramJson = "{}" });

            var response = await _sut.GetByShareIdAsync("s1", "owner", CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the share resolves");
            response.ApiResponse.Data!.IsOwner.Should().BeTrue("because the caller owns it");
            response.ApiResponse.Data!.DiagramUid.Should().Be("d1", "because the owner may edit and needs the handle");
        }

        [Fact]
        public async Task GetByShareIdAsync_CallerNotOwner_SetsIsOwnerFalseAndBlanksDiagramUid()
        {
            _repo.Setup(r => r.GetByShareIdAsync("s1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DiagramRow { DiagramUid = "d1", ShareId = "s1", ClientId = "owner", Name = "N", SeedResourceUid = "seed", DisplayPreset = "nameType", DiagramJson = "{}" });

            var response = await _sut.GetByShareIdAsync("s1", "someone-else", CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the share resolves for anyone");
            response.ApiResponse.Data!.IsOwner.Should().BeFalse("because the caller is not the owner");
            response.ApiResponse.Data!.DiagramUid.Should().BeEmpty("because the owner handle is suppressed for a read-only recipient");
        }
```

- [ ] **Step 2:** Update the one test + add the two above. `dotnet test` the `DiagramServiceTests`.

---

## 2. JS: guard `loadJson` against malformed JSON (`wwwroot\js\explorer\explorer-canvas.js`)

Wrap the parse so a corrupt/empty stored payload returns `null` instead of throwing through the
circuit (7a review note). Change the start of `loadJson`:

```javascript
export function loadJson(json) {
    let graph;
    try { graph = JSON.parse(json); }
    catch (e) { return null; }
    if (!graph) return null;

    cy.elements().remove();
    seedId = graph.seedUid || null;
    // ... (rest of the existing function unchanged) ...
```

- [ ] **Step 3:** Add the guard (the rest of `loadJson` stays as-is).

---

## 3. Page: share, shared route, read-only, Save-a-copy (`Components\Pages\ResourceExplorer.razor`)

### 3a. Routes + injection

Add the second route and the `NavigationManager` injection (with the existing directives), and make
`ResourceUid` nullable:

```razor
@page "/explore/{ResourceUid}"
@page "/explore/shared/{ShareId}"
@inject NavigationManager Navigation
```
```csharp
    [Parameter] public string? ResourceUid { get; set; }
    [Parameter] public string? ShareId { get; set; }
    private bool _isReadOnly;
```

### 3b. Load-by-share on init

In `OnAfterRenderAsync`'s first-render block, replace the current seed load
(`_seedUid = ResourceUid; await LoadNodeAsync(ResourceUid, seed: true, ...);`) with a branch:

```csharp
        if (!string.IsNullOrEmpty(ShareId))
        {
            await LoadSharedAsync(ShareId);
        }
        else
        {
            _seedUid = ResourceUid ?? string.Empty;
            await LoadNodeAsync(ResourceUid ?? string.Empty, seed: true, expandFromUid: null);
        }
```

(Keep the `await SafeInvokeAsync("setPreset", _preset);` and the client-id acquisition before this,
as in slice 6/7a — `_clientId` must be set before `LoadSharedAsync` so ownership resolves.)

### 3c. Load-shared + Save-a-copy handlers (add beside `OpenAsync`)

```csharp
    private async Task LoadSharedAsync(string shareId)
    {
        var resp = await DiagramService.GetByShareIdAsync(shareId, _clientId);
        if (!resp.IsSuccess() || resp.ApiResponse.Data is null)
        {
            _error = "Shared diagram not found.";
            StateHasChanged();
            return;
        }
        var model = resp.ApiResponse.Data;

        _shareId = model.ShareId;
        _diagramName = model.Name;
        _seedUid = model.SeedResourceUid;
        _preset = string.IsNullOrWhiteSpace(model.DisplayPreset) ? "nameType" : model.DisplayPreset;
        _isReadOnly = !model.IsOwner;
        _diagramUid = model.IsOwner ? model.DiagramUid : null;   // suppressed for non-owners

        var expanded = await SafeInvokeResultAsync<string[]>("loadJson", model.DiagramJson);
        if (expanded is null)
        {
            _error = "This shared diagram could not be loaded (corrupt data).";
            StateHasChanged();
            return;
        }
        _expanded.Clear();
        foreach (var u in expanded) _expanded.Add(u);
        StateHasChanged();
    }

    private async Task SaveCopyAsync()
    {
        if (string.IsNullOrEmpty(_shareId)) return;
        var resp = await DiagramService.SaveCopyAsync(_clientId, _shareId, null);
        if (resp.IsSuccess() && resp.ApiResponse.Data is not null)
        {
            _diagramUid = resp.ApiResponse.Data.DiagramUid;
            _shareId = resp.ApiResponse.Data.ShareId;
            _diagramName = $"{_diagramName} (copy)";   // matches the server-side default name
            _isReadOnly = false;
            Snackbar.Add("Saved a copy to your diagrams.", Severity.Success);
            StateHasChanged();
        }
        else
        {
            Snackbar.Add("Could not save a copy.", Severity.Error);
        }
    }

    private async Task ShareAsync()
    {
        if (string.IsNullOrEmpty(_shareId))
        {
            await SaveAsAsync();                       // must be saved to have a share id
            if (string.IsNullOrEmpty(_shareId)) return; // cancelled
        }
        var url = $"{Navigation.BaseUri.TrimEnd('/')}/explore/shared/{_shareId}";
        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", url);
            Snackbar.Add("Share link copied to clipboard.", Severity.Success);
        }
        catch
        {
            Snackbar.Add(url, Severity.Info);          // clipboard blocked — show it to copy manually
        }
    }
```

Also update the 7a `OpenAsync` handler's `GetByShareIdAsync` call to pass the client id and set
read-only from ownership:

```csharp
        var resp = await DiagramService.GetByShareIdAsync(shareId!, _clientId);
```
and after assigning `model`, add:
```csharp
        _isReadOnly = !model.IsOwner;
        _diagramUid = model.IsOwner ? model.DiagramUid : null;
```
(replacing the 7a line that unconditionally set `_diagramUid = model.DiagramUid`).

### 3d. Toolbar: Share button, read-only gating, banner

- Add a **Share** button (after Open) and gate **Save** on read-only. In the toolbar block, change the
  Save button to `Disabled="_isReadOnly"` and add:

```razor
        <MudButton Size="Size.Small" Variant="Variant.Outlined" Class="ml-1"
                   StartIcon="@Icons.Material.Outlined.Share" OnClick="ShareAsync">Share</MudButton>
        @if (_isReadOnly)
        {
            <MudButton Size="Size.Small" Variant="Variant.Filled" Color="Color.Secondary" Class="ml-1"
                       StartIcon="@Icons.Material.Outlined.ContentCopy" OnClick="SaveCopyAsync">Save a copy to mine</MudButton>
        }
```
```razor
    <MudButton ... StartIcon="@Icons.Material.Outlined.Save" OnClick="SaveAsync" Disabled="_isReadOnly">Save</MudButton>
```

- Add a read-only banner just inside `.rm-explorer-host`, above the canvas (beside the existing
  `_error` alert):

```razor
    @if (_isReadOnly)
    {
        <MudAlert Severity="Severity.Info" Dense="true" Class="ma-1">
            Read-only shared diagram — use “Save a copy to mine” to edit.
        </MudAlert>
    }
```

- [ ] **Step 4:** Apply 3a–3d.

---

## UI verification hook (visible slice)

- [ ] **Step 5: Build + run.** `dotnet build` clean; `dotnet run --project UI/ResourceMapper.UI.Web`.
- [ ] **Step 6: Drive it** at `/explore/DEMOEXP-checkout`:
  1. Save-As a diagram, then click **Share** → a `.../explore/shared/{ShareId}` link is copied
     (snackbar confirms).
  2. Open that shared URL **in the same browser** (you're the owner) → it loads **editable** (no
     read-only banner; Save enabled).
  3. Open the shared URL in a **different browser / cleared localStorage** (a different `clientId`) →
     it loads **read-only** (banner shown, Save disabled), and **"Save a copy to mine"** appears.
  4. Click **Save a copy to mine** → the banner clears, it becomes editable, and it now shows in that
     client's **Open** list as "… (copy)".
  5. Confirm the recipient never sees the owner's `DiagramUid` (dev-tools: the shared load's model has
     an empty `DiagramUid`).
  6. A deliberately corrupted share (edit a `Diagram.DiagramJson` row to invalid JSON via `sqlcmd`,
     then open its share link) shows the "corrupt data" error instead of crashing.
- [ ] **Step 7 (optional): Playwright** — two contexts (owner vs. fresh) asserting editable vs.
  read-only, and that Save-a-copy produces an owned row.

---

## Verification

1. `dotnet build` clean; `dotnet test` — the updated + 2 new `DiagramServiceTests` pass, no regression.
2. Manual browser drive per Steps 5–6 — the gate (use two browsers / an incognito window for the
   non-owner case).

---

## Out of scope (later slices)

- **Recovery-key restore UI** (paste an id to reclaim a library) → later; `ClientIdentity.SetAsync` exists.
- **Export** → slice 8. **Visual polish** (edges, labels, zoom/pan controls, floating toolbar) → slice 9.

---

## Execution notes

Implemented as written; no deviations from the brief.

- **Both `GetByShareIdAsync` call sites** updated to pass `_clientId`: the new `LoadSharedAsync`
  (`/explore/shared/{ShareId}`) and the existing 7a `OpenAsync` handler (Open dialog). Both now also
  set `_isReadOnly = !model.IsOwner` and `_diagramUid = model.IsOwner ? model.DiagramUid : null`.
- **Two-browser ownership check** — driven with Playwright (`playwright-core`, system Chrome) using
  two separate `BrowserContext`s (each gets its own `localStorage`, hence its own `rm_client_id` —
  no manual storage clearing needed). Flow: Context A (owner) opens the seed, Save-As's a diagram,
  clicks Share (clipboard granted via `context.grantPermissions`), reads the copied URL back off
  `navigator.clipboard`. Reopening that URL in a **new page in the same context** loads editable (no
  banner, Save enabled). Opening the same URL in **Context B** (different `rm_client_id`, confirmed
  distinct via `localStorage.getItem('rm_client_id')` in both contexts) loads read-only: banner
  shown, Save disabled, "Save a copy to mine" visible. Clicking it clears the banner, re-enables
  Save, and the diagram shows in Context B's Open dialog as "… (copy)". Verified via `sqlcmd` that
  the copy landed as a distinct `Diagram` row owned by Context B's `ClientId` with a brand-new
  `DiagramUid`/`ShareId` (not reusing the owner's blanked handle) — screenshots captured for the
  owner-reopen and non-owner-shared states.
  - Note: since this is Blazor **Server** (interactive server render mode), `GetByShareIdAsync` is an
    in-process service call from the component, not a client-visible HTTP/JSON response — there's no
    browser Network-tab payload to inspect for the blanked `DiagramUid`. That guarantee is instead
    verified directly at the unit level (`GetByShareIdAsync_CallerNotOwner_SetsIsOwnerFalseAndBlanksDiagramUid`)
    and indirectly in the browser run: Save is disabled for the whole read-only session (so the
    blanked/null `_diagramUid` is never exercised for a write), and `SaveCopyAsync` takes the
    `shareId`, not `_diagramUid`, so the clone path never depends on it either.
- **Corrupt-JSON guard** — set an existing `Diagram.DiagramJson` row to `'{not valid json!!!'` via
  `sqlcmd`, then opened its share link fresh. Result: "This shared diagram could not be loaded
  (corrupt data)." alert shown, canvas stays empty, no unhandled-exception page, no thrown JS error
  reaching the console beyond an unrelated 404 (favicon-class, present in all runs including
  unmodified pages). Confirms both the JS `loadJson` try/catch (returns `null` on `JSON.parse`
  failure) and the C# `LoadSharedAsync` null-check path work end-to-end.
- Test diagram rows created during the browser drive were deleted afterward via `sqlcmd`; the DB
  was restored to its pre-existing 2 `Diagram` rows.
- Slice #7b is done. Master list update deferred — out of scope for this task (only this slice
  doc's execution notes were touched; see the task's explicit instruction not to read/edit the
  master list).
