# Slice #9 — Navigation, dirty guard & delete

## Context

Ninth (and final planned) editor slice (design: `docs/plans/editor/resource-logical-model-and-editor-ux.md`
§12 "Navigation stack, back button & unsaved changes (agreed)" + "Delete" + "Dependencies /
Dependent On tabs (agreed) → Create new…"; list: `docs/plans/editor/00-implementation-plan-list.md`).
Slices #6–#8 shipped the editor shell, all five visible tabs (General / Tags / Dependencies /
Dependent On / Review), and wired every persisted write (General, tags, relationship edges). What
remains is the **navigation & lifecycle** layer the earlier slices deliberately deferred:

1. **Dirty guard** — intercept navigation away from an editor with unsaved changes and prompt
   **Save / Discard / Cancel** (Blazor `RegisterLocationChangingHandler`; Mud dialog, never
   browser-native).
2. **Nested "Create new…" target** — the dependency picker's "target doesn't exist yet" path:
   push a nested editor (history-pushing route), domain-locked to the parent, that persists the
   child immediately and pops back to the parent with the child linked (edge persists on the
   parent's Save). This is the piece **moved from #8 (OQ1/RD13)** because it depends on the
   nav-stack machinery that lands here.
3. **Delete** — an action button + Mud confirm dialog that lists dependents ("The following
   resources depend on this…"), then cascade-deletes the resource and its edges/tags.

**This slice is UI-only.** No DB changes (no sproc, no republish), no contract changes, no new
server methods:

- `IResourceService.DeleteResourceAsync(uid)` **already exists** (slice #4) and already cascades
  edges + tags server-side (`ResourceSqlRepository.DeleteResourceAsync` → `Resource_Delete`).
- The delete-confirm dialog's dependents list is **already loaded** into `model.DependentOn` by
  `BuildEditEditorModel` (slice #8) — no extra read.
- `DependencyRowEditor` and `SaveResourceResponse.ResourceUid` already carry everything the
  nested-create link needs; the child's Name/Type/Domain come from the nested editor's own
  in-memory model at save time.
- The only new **wiring** is a circuit-scoped nav-stack service + its DI registration; everything
  else is Razor + the existing `ResourceEditor.razor` code-behind.

The low blast-radius (no DB / no contracts) is the good news; the risk is concentrated entirely in
the **nav-stack + dirty-guard interaction**, which is genuinely fiddly. That is where the
rubber-duck section below spends its effort, and it drives the sizing/split question (OQ8).

## Verified starting state

- **Render mode is `InteractiveServer`** (`App.razor`: `<Routes @rendermode="RenderMode.
  InteractiveServer" />`; `Program.cs` `AddInteractiveServerComponents()` /
  `AddInteractiveServerRenderMode()`), so `RegisterLocationChangingHandler` and awaited Mud
  dialogs are available. `Home.razor` already implements `IDisposable` and subscribes to
  `Navigation.LocationChanged`, so the disposable-handler pattern is established here.
- **`ResourceEditorModel.IsDirty` already exists** (checks all `SingleValueEditor.IsChanged` +
  tag row changes + `DependsOn`/`DependentOn` `IsNew`/`IsRemoved`) — the dirty guard needs no new
  dirty-tracking, only to consult it.
- **`ResourceEditor.razor` today** has: `LoadAsync` with a `(_hasLoadedOnce && ResourceUid ==
  _lastLoadedUid)` reload-dedupe guard; `Cancel()` (Create → `NavigateTo("/")`, Edit → reload +
  back to View); `SaveAsync` (Edit → reload same route; Create → `NavigateTo("/resources/{uid}",
  replace:true)`); `_mode` ∈ `Create` | `Edit` | `View`. **No nav guard, no delete, no nested
  create.** Routes: `@page "/resources"` + `@page "/resources/{ResourceUid}"`.
- **`DependencyTab.razor`** owns the picker + rows; the picker (`ResourcePicker.razor`) is gated on
  `Domain` being set and its `NoItemsTemplate` currently just says "the target must already exist"
  — that empty-state is where "Create new…" plugs in. `DependencyTab` already bubbles changes to
  the parent via `OnFieldChanged`; a second callback (`OnCreateNewRequested`) is the hook for #9.
- **`GeneralTab.razor`** renders the Domain `MudSelect` with `Disabled="@(ReadOnly ||
  Model.IsPersisted)"`. Domain-lock for nested create = add a `DomainLocked` param to that
  condition + pre-set `Model.Domain.EditedValue`.
- **The hamburger "Create Resource"** is an `<MudMenuItem Href="/resources">` (an internal
  anchor nav) — it **will** trip the `LocationChangingHandler`, so the dirty guard covers it for
  free. "Import…" (`/import`) likewise.
- **E2E cleanup** (`tools/e2e/sql/cleanup.sql`) deletes **all** `Resource` rows of type
  `E2eDepType` (edges → tags → resources), so any resource a spec creates via the UI of that type
  — subject, dependents, **or nested-created children** — is wiped by `globalSetup`/`globalTeardown`
  without touching shared system rows (RD15/RD16 from #8). No new fixture rows are required.

## Architecture note — one component instance, a data stack (read this first)

The design's mental model is "nested editors are separate editor **components** stacked in browser
history." The faithful Blazor realization is subtly different and **much** simpler, and the whole
plan below depends on understanding it:

> There is exactly **one** `ResourceEditor` component instance. Navigating between `/resources`,
> `/resources/{uid}`, and the nested `/resources?n=k` routes all resolve to that same `@page`
> component, so Blazor **reuses the instance** and re-runs `OnParametersSetAsync` rather than
> tearing it down. The "stack of editors" is therefore a **stack of data** (`EditorFrame`s held in
> a circuit-scoped service), not a stack of live components. At any instant the single component
> *is* the current editor level; its ancestors live only as frames.

This is why (a) refresh loses the stack (the circuit-scoped service is rebuilt empty — matches the
design's "refresh mid-stack loses the in-progress stack"); (b) we must **stash the parent's model
into a frame before navigating**, because the reused instance's `_response` field gets overwritten
by the nested load; and (c) there is only ever **one** registered location-changing handler,
consulting the one current `_model`.

## New — circuit-scoped nav-stack service

`UI/ResourceMapper.UI.Web/Components/Editor/EditorNavStack.cs` (new), registered
**`AddScoped<EditorNavStack>()`** in `Program.cs` (scoped == per-circuit in Interactive Server ==
survives route changes within a connection, cleared on refresh/reconnect — exactly the desired
lifetime).

```csharp
public sealed class EditorNavStack
{
    private readonly Stack<EditorFrame> _frames = new();
    public bool HasFrames => _frames.Count > 0;
    public int Depth => _frames.Count;
    public void Push(EditorFrame f) => _frames.Push(f);
    public EditorFrame Peek() => _frames.Peek();
    public EditorFrame Pop() => _frames.Pop();
    public void Clear() => _frames.Clear();
}

public sealed class EditorFrame
{
    public required OpenEditorResponse Response { get; init; } // the parent's LIVE edit state
    public required string ParentRoute { get; init; }          // base-relative path+query the parent was at
    public required string Direction { get; init; }            // "DependsOn" | "DependentOn" the create came from
    public required string ParentMode { get; init; }           // "Create" | "Edit"
    public ReturnedChild? Returned { get; set; }               // set by the child on save; consumed on pop
}

public sealed class ReturnedChild
{
    public required string ResourceUid { get; init; }
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public string? Domain { get; init; }
}
```

`Response` holds the whole `OpenEditorResponse` (model **and** its reference data — `ResourceTypes`,
`TagDictionary`, `EntryPointTemplates`, `DomainAllowedValues`), so restoring a parent needs **zero**
DB reload and preserves every unsaved edit verbatim.

## `ResourceEditor.razor` — the orchestration (the heart of the slice)

Inject `EditorNavStack Stack` and (already present) `NavigationManager Navigation`,
`IDialogService DialogService`, `ISnackbar Snackbar`. Implement `IDisposable`.

### Route / parameter changes

- Add `[SupplyParameterFromQuery(Name = "n")] public int? NestedLevel { get; set; }` — the nested
  marker. It (a) makes each nested level a **distinct URL** so browser history and
  `OnParametersSetAsync` treat descents as real navigations, and (b) lets `LoadAsync`'s dedupe key
  distinguish two nested creates that both have `ResourceUid == null`.
- **Widen the reload-dedupe key** from `ResourceUid` to `(ResourceUid, NestedLevel)`. Today two
  nested creates (both `uid==null`, `n=1` vs `n=2`) would falsely dedupe and skip the second load.

### Entry: `OnParametersSetAsync`

```
if (IsReturningToFrame())  => RestoreFromFrame();       // ascending: don't reload, restore parent
else {
    if (NestedLevel is null) Stack.Clear();             // fresh TOP-LEVEL entry abandons any stale stack (RD12)
    await LoadAsync();                                  // fresh load; nested iff (NestedLevel is not null && Stack.HasFrames)
}
```

`IsReturningToFrame()` = `Stack.HasFrames && CurrentRelativePath() == Stack.Peek().ParentRoute`.
This is the descend-vs-ascend discriminator:
- **Descending** parent → nested: after the push, `Peek().ParentRoute` is the *parent's* URL but the
  current URL is the *nested* URL (`resources?n=k`) → not returning → fresh nested load. ✔
- **Ascending** nested → parent (via child Save, Cancel, **or** browser Back — dirty or clean):
  current URL == `Peek().ParentRoute` → returning → restore (preserves the parent's in-memory
  edits; never reloads from DB). ✔
- **Top-level** (stack empty, or entered without `?n`): not returning → `Stack.Clear()` +
  fresh load. ✔

Use `Navigation.ToBaseRelativePath(Navigation.Uri)` for both the stored `ParentRoute` and the
current-path comparison so they're normalized identically (path **and** query). The `Stack.Clear()`
on a `?n`-less entry is what makes the hamburger "Create Resource" a genuine fresh start even if an
earlier nested flow was abandoned without popping (the one narrow residual is called out in RD12).

### `LoadAsync` (nested augmentation)

After the existing fresh-load populates `_response`, if **this load is nested**
(`NestedLevel is not null && Stack.HasFrames`), apply the **domain lock**:
```
var parentDomain = Stack.Peek().Response.EditorModel.Domain.EditedValue;
_model.Domain.EditedValue = parentDomain;
_domainLocked = true;                 // passed to GeneralTab
OnGeneralFieldChanged();              // sync IdentityPreview.Domain
```
`_mode` stays `"Create"`. (Top-level loads leave `_domainLocked=false`.)

### `RestoreFromFrame`

```
var frame = Stack.Peek();
_response      = frame.Response;      // parent's live model + reference data, edits intact
_mode          = frame.ParentMode;
_domainLocked  = false;               // parent set its own domain
_loading       = false; _loadError = null;
_hasLoadedOnce = true;                // seed dedupe so a stray OnParametersSet won't reload over us
_lastLoadedUid = ResourceUid; _lastNestedLevel = NestedLevel;

if (frame.Returned is not null)
{
    LinkReturnedChild(frame.Direction, frame.Returned);
    frame.Returned = null;
}
Stack.Pop();
StateHasChanged();
```

`LinkReturnedChild(direction, child)` appends a `DependencyRowEditor` to `_model.DependsOn` or
`_model.DependentOn` (`IsNew=true`, `OtherResourceUid/Name/Type/Domain` from `child`) **unless** a
non-removed row for that uid is already present (dedupe, mirrors the picker's RD10/RD9). This marks
the parent dirty (a new edge) so the guard/Save see it — the edge itself only persists on the
parent's next Save, exactly as the design's persistence-timing rule requires.

### Initiate nested create (called from `DependencyTab.OnCreateNewRequested`)

```
void InitiateNestedCreate(string direction)
{
    Stack.Push(new EditorFrame {
        Response    = _response!,               // stash BEFORE navigating (instance is reused!)
        ParentRoute = CurrentRelativePath(),
        Direction   = direction,
        ParentMode  = _mode
    });
    _suppressNavGuard = true;                    // intentional descent — don't prompt
    Navigation.NavigateTo($"resources?n={Stack.Depth}");
}
```

### Save factored into `SaveInternalAsync` + one shared child-return path

Factor the persist half of today's `SaveAsync` into **`Task<string?> SaveInternalAsync()`** — runs
validation, calls `SaveResourceAsync`, applies server errors on failure, and **returns the saved
`ResourceUid` on success or `null` on failure**. It does **not** navigate. Three callers own the
follow-on:

- **Save button (`SaveAsync`)** — `var uid = await SaveInternalAsync(); if (uid is null) return;`
  then: if `Stack.HasFrames` → `CommitChildAndReturn(uid)`; else existing top-level behavior
  (Create → `NavigateTo("/resources/{uid}", replace:true)`; Edit → `_mode="View"` + reload same
  route, no navigation).
- **Dirty guard (save choice)** — see below; also routes through `CommitChildAndReturn(uid)` when
  the leave lands on the parent frame, so a nested child committed via the back-prompt links exactly
  as the button would (RD19).

`CommitChildAndReturn(string uid)` is the single pop-back-with-link path:
```
Stack.Peek().Returned = new ReturnedChild {
    ResourceUid = uid,
    Name        = _model.Name.EditedValue,
    TypeName    = _model.ResourceType.EditedValue,   // the type NAME, for display
    Domain      = _model.Domain.EditedValue
};
_suppressNavGuard = true;                            // just saved; don't dirty-prompt the ascent
Navigation.NavigateTo(Stack.Peek().ParentRoute);     // ascend; RestoreFromFrame links the child
```
(Top-level Edit save is unchanged — same route, reload, no navigation, no guard interaction.)

### `Cancel` (nested-aware)

```
if (Stack.HasFrames) { _suppressNavGuard = true; Navigation.NavigateTo(Stack.Peek().ParentRoute); } // discard child, pop
else if (_model.IsPersisted) { _mode = "View"; _ = LoadAsync(forceReload:true); }                    // existing
else { _suppressNavGuard = true; Navigation.NavigateTo("/"); }                                        // existing (Create → home)
```
Cancel is an explicit discard, so it suppresses the guard (no double-prompt). Ascending via Cancel
leaves `Returned == null`, so `RestoreFromFrame` restores the parent and links nothing.

### Dirty guard — the location-changing handler

Register in `OnAfterRenderAsync(firstRender)` (interactive only), store the returned `IDisposable`,
dispose in `Dispose()`.

**Chosen pattern — "always prevent, then re-navigate on confirm"** (robust; avoids awaiting a
dialog mid-commit — RD1):
```
async ValueTask OnLocationChanging(LocationChangingContext ctx)
{
    if (_suppressNavGuard) { _suppressNavGuard = false; return; }   // consume intentional nav
    if (_mode == "View" || _model is null || !_model.IsDirty) return; // nothing to guard
    if (_navPromptOpen) { ctx.PreventNavigation(); return; }        // re-entrancy guard

    ctx.PreventNavigation();                                        // hold the URL first
    var target = ctx.TargetLocation;
    _navPromptOpen = true;
    var choice = await ShowUnsavedDialog();                         // Save | Discard | Cancel
    _navPromptOpen = false;

    if (choice == "cancel") return;                                 // stay put (already prevented)

    if (choice == "save")
    {
        var uid = await SaveInternalAsync();
        if (uid is null) return;                                    // save failed → stay
        // If this is a nested child leaving toward its parent, commit+link exactly as the
        // Save button would (RD19) rather than a bare navigate.
        if (Stack.HasFrames && SameRoute(target, Stack.Peek().ParentRoute)) { CommitChildAndReturn(uid); return; }
    }
    _suppressNavGuard = true;                                       // discard, or save-with-no-frame → re-nav
    Navigation.NavigateTo(target);
}
```
- `_suppressNavGuard` is a one-shot consumed at the top of the handler; every intentional
  navigation (nested descent, child pop, Cancel, post-Create permalink, confirmed-leave) sets it and
  the handler clears it. Also cleared defensively at the end of a fresh `LoadAsync` (belt-and-braces
  against a suppress that never met a handler — RD3).
- `SaveInternalAsync()` = the existing `SaveAsync` persist body factored to **return `string?`**
  (the saved uid, or `null` on validation/server failure) and to **not** navigate; each caller (Save
  button, guard) owns its own follow-on navigation. The Save button becomes a thin wrapper over it.
- **Browser Back** is covered: `RegisterLocationChangingHandler` intercepts back/forward for in-app
  history in Interactive Server; `PreventNavigation` re-pushes state to keep the URL, and the
  confirmed re-nav to `target` (the previous URL) proceeds. This is the tab→nested→parent→home pop
  chain the design describes, now guarded per level.

### Delete

Header gains a **Delete** button whenever `_model.IsPersisted` (shown in both View and Edit;
alongside Edit in View mode). On click:
```
var dependents = _model.DependentOn.Where(d => !d.IsRemoved)
                       .Select(d => d.OtherResourceName).ToList();
var confirmed = await ShowConfirmDeleteDialog(_model.Name.EditedValue, dependents);
if (!confirmed) return;
var resp = await ResourceService.DeleteResourceAsync(_model.ResourceUid, CancellationToken.None);
if (resp.IsSuccess()) {
    Snackbar.Add($"Deleted {name}", Severity.Success);
    _suppressNavGuard = true;          // deleting is a leave; don't dirty-prompt
    Navigation.NavigateTo("/");
} else { Snackbar.Add("Could not delete…", Severity.Error); }
```
Cascade (edges both directions + tags) is entirely server-side and already implemented; the dialog
merely **warns** with the dependents list (allow-with-warning, per design — never blocks).

## New / changed components

- **`Components/Editor/EditorNavStack.cs`** (new) — the service + `EditorFrame`/`ReturnedChild`
  records above. Registered `AddScoped` in `Program.cs`.
- **`Components/Editor/UnsavedChangesDialog.razor`** (new) — a plain Mud dialog with body "You have
  unsaved changes to <name>." and three buttons returning `"save"` / `"discard"` / `"cancel"`
  (`MudDialog.Close(DialogResult.Ok("save"))` etc.). Not a history entry (RD2).
- **`Components/Editor/ConfirmDeleteDialog.razor`** (new) — plain Mud dialog; params: resource name
  + `List<string> Dependents`. Body: "Delete <name>?" and, if `Dependents` non-empty, "The
  following resources depend on it: …" list. Returns `bool`. Not a history entry.
- **`Components/Editor/DependencyTab.razor`** (modify) — add `[Parameter] EventCallback<string>
  OnCreateNewRequested`. Add a **"Create new…"** `MudButton` (icon `Add`) next to the picker,
  visible when `!ReadOnly` and `Domain` is set (same gate as the picker); on click →
  `OnCreateNewRequested.InvokeAsync(Direction)`. Also surface it from the picker's
  `NoItemsTemplate` ("No match — create a new resource" → same callback) so the not-found path the
  design names is covered.
- **`Components/Editor/GeneralTab.razor`** (modify) — add `[Parameter] bool DomainLocked`; change the
  Domain select to `Disabled="@(ReadOnly || Model.IsPersisted || DomainLocked)"` and add a hint
  ("Locked to the parent resource's <DomainLabel>") when locked.
- **`Components/Pages/ResourceEditor.razor`** (modify) — all the orchestration above: inject
  `EditorNavStack`; `NestedLevel` query param + widened `(ResourceUid, NestedLevel)` dedupe;
  `IsReturningToFrame`/`RestoreFromFrame`/`LinkReturnedChild`/`InitiateNestedCreate`/
  `CommitChildAndReturn`/`SameRoute`; `SaveInternalAsync` (returns `string?` uid) with `SaveAsync`
  and `Cancel` made nested-aware; the `OnLocationChanging` handler + `_suppressNavGuard`/
  `_navPromptOpen` + `IDisposable`; the Delete button + handler; pass `DomainLocked` to `GeneralTab`
  and `OnCreateNewRequested="InitiateNestedCreate"` to both `DependencyTab`s.
- **`app.css`** (modify) — minor: header Delete-button spacing, "Create new…" button row in the
  deps editor (reuse the existing `rm-deps-*` / `rm-editor-*` style vocabulary).

No changes below the UI layer.

## Rubber-duck — bugs / logic traps caught up front

- **RD1 — don't `await` a dialog while a navigation is mid-commit.** Awaiting `ShowUnsavedDialog()`
  *before* `PreventNavigation()` risks the navigation committing (URL changes, component starts
  tearing down) while the dialog is still open — especially for browser Back. **Always
  `PreventNavigation()` first**, then show the dialog, then re-`NavigateTo(target)` on confirm. The
  re-nav re-enters the handler, so `_suppressNavGuard` must gate it. This is the single most
  important correctness point and the #1 thing to verify in a real browser.
- **RD2 — Mud dialogs must NOT be history entries.** `UnsavedChangesDialog`, `ConfirmDeleteDialog`,
  and #7's `TagDefinitionDialog` are `DialogService.ShowAsync` modals closed by their own buttons —
  they never call `NavigateTo`, so browser Back does not pop them (design: "dialogs are plain Mud
  modals, not history"). Only the nested **editor** is a route.
- **RD3 — `_suppressNavGuard` must never leak into the next guard.** It's a one-shot: set
  immediately before an intentional `NavigateTo`, consumed at the top of the handler. Every
  intentional in-app nav is SPA and trips the handler, so it's consumed. Defensively also reset it
  to `false` at the end of a completed fresh `LoadAsync`, so a suppress that somehow never met a
  handler (e.g. a swallowed nav) can't silently disable the guard for a later real navigation.
- **RD4 — stash the parent model BEFORE navigating (reused instance).** Because the one component
  instance is reused, descending overwrites `_response` with the nested load. `InitiateNestedCreate`
  pushes the frame (capturing `_response`) *before* `NavigateTo`. Verified against the
  architecture note.
- **RD5 — widen the reload-dedupe key to include `NestedLevel`.** Two nested creates both have
  `ResourceUid == null`; without `NestedLevel` in the key, the second descent's `LoadAsync` would
  early-return and show the first child's model. Key = `(ResourceUid, NestedLevel)`.
- **RD6 — descend-vs-ascend hinges on `CurrentPath == Peek().ParentRoute`.** Store and compare via
  the *same* `ToBaseRelativePath` normalization (path **and** query), or a descent whose parent was
  `/resources` (Create) vs a return to `/resources` won't be told apart. The nested marker `?n=k`
  guarantees the nested URL never equals the parent URL, so descent is never misread as return.
- **RD7 — domain lock reads the IMMEDIATE parent (`Peek()`), applied on the nested fresh load.**
  Multi-level chains stay same-domain because each descent locks to its immediate parent, which was
  itself already locked to *its* parent. Set the value + `_domainLocked` right after the nested
  `LoadAsync`, and call `OnGeneralFieldChanged` so the identity preview shows the domain.
- **RD8 — child persists immediately; edge persists on the PARENT's Save (design rule).** The child
  is a normal Create `SaveResourceAsync` (fully persisted). The parent gets only an in-memory
  `DependencyRowEditor (IsNew)`; the edge is written by the existing slice-#8 reconciliation on the
  parent's Save. Therefore **discarding the parent leaves the child standalone/unlinked** — exactly
  the design's persistence-timing contract. No special cleanup.
- **RD9 — linking the child must dedupe (RD9/RD10 from #8).** `LinkReturnedChild` skips if a
  non-removed row for that uid already exists, and (defensively) un-removes a matching removed row
  rather than appending — same rules the picker already enforces, so a create-new that duplicates an
  existing target can't double-add.
- **RD10 — the returned child could be a self-loop or cross-domain? No.** It's brand-new (fresh
  uid, can't equal the parent) and domain-locked to the parent, so it always passes the slice-#8
  self-loop + same-domain checks on the parent's Save. The server backstop still runs regardless.
- **RD11 — deep-link / refresh mid-stack has an empty stack ⇒ behaves as top-level.** A refresh
  rebuilds the circuit-scoped `EditorNavStack` empty, so a nested URL `/resources?n=2` loads as a
  plain top-level Create (no parent to return to; Cancel → home). Matches the design's "refresh
  mid-stack loses the in-progress stack; a deep-linked nested editor falls back to home." No crash,
  no orphaned-frame lookup.
- **RD12 — stale-frame hygiene, and the one accepted residual.** Two mitigations keep abandoned
  frames from corrupting normal flows: (a) **nested detection is gated on `NestedLevel is not null`**
  (not merely `Stack.HasFrames`), so a bare `/resources` never behaves as a nested child of a stale
  frame; (b) any **fresh top-level entry** (`?n`-less, not a return) calls `Stack.Clear()`, so the
  hamburger "Create Resource" is a real fresh start even after an abandoned nested flow. The one
  **residual** (documented, accepted): if the parent was itself a *top-level Create* (its
  `ParentRoute` == `resources`) and the user abandons a nested create *without* popping and then
  re-enters `/resources`, `IsReturningToFrame` matches and their earlier in-progress create is
  restored instead of a blank form. That only resurrects the user's *own* unsaved work via a
  convoluted path; it's within the design's out-of-scope "adversarial/unusual navigation" boundary,
  so we accept it rather than add frame-expiry bookkeeping.
- **RD13 — the Save button vs. the guard.** Both callers share `SaveInternalAsync` (returns the
  saved uid or `null`). The button, on a **Create** top-level success, `NavigateTo`s the permalink
  **while the model is still dirty**, which would trip the guard — so that navigation sets
  `_suppressNavGuard` first. Edit-mode save reloads the same route (no navigation) and never touches
  the guard.
- **RD14 — one handler, one current model.** Since there's a single component instance, there's a
  single registered handler consulting the current `_model`. After `RestoreFromFrame` swaps
  `_response` back to the parent, the handler automatically guards the parent's dirtiness. No
  per-level handler bookkeeping. Register once (first render), dispose once.
- **RD15 — Delete's dependents list is a snapshot (accepted).** `model.DependentOn` is as-of load;
  a dependent added elsewhere afterward won't show in the warning, but the server cascade deletes
  the edge regardless. UI concurrency is out of scope (design). The dialog **warns**, never blocks
  (allow-with-warning).
- **RD16 — deleting is a "leave" and must suppress the guard.** After a successful delete we
  `NavigateTo("/")`; the model may still read dirty, so set `_suppressNavGuard` before navigating,
  else the user gets a "save changes?" prompt for a resource that no longer exists.
- **RD17 — `NestedLevel` must be a real parameter change to fire `OnParametersSetAsync`.**
  `[SupplyParameterFromQuery]` makes `?n=k` a bound parameter, so changing it re-invokes
  `SetParametersAsync`/`OnParametersSetAsync` even though the `@page` component type is unchanged.
  (A non-bound query string would not reliably re-trigger the lifecycle.)
- **RD18 — the hamburger/Import links are internal navs ⇒ already guarded.** They use `Href` (anchor)
  which Blazor routes internally, tripping `OnLocationChanging`. No extra wiring; but it means the
  guard's dirty check must be correct or the user is stuck — hence the View-mode / `!IsDirty`
  short-circuits at the top of the handler.
- **RD19 — a nested child committed via the back-prompt must still link.** If the user fills a nested
  child then triggers **browser Back** (instead of the Save button) and chooses **Save** in the
  dirty dialog, the child must link into the parent exactly as the button would. The guard therefore
  routes a save-choice through `CommitChildAndReturn(uid)` when the leave target *is* the parent
  frame's route (`SameRoute` check), rather than a bare `NavigateTo(target)`. Without this, a
  back-prompt save would persist the child standalone but silently fail to link it — a subtle
  inconsistency with the Save button. `SameRoute` normalizes via `ToBaseRelativePath` (same as the
  return discriminator).

## Open questions / decisions (documented defaults)

- **OQ1 — nested create UI entry point.** *Default:* a visible **"Create new…"** button next to the
  picker (enabled once Domain is set) **plus** the same action in the picker's `NoItemsTemplate`
  (the "target not found" path the design literally names). Rationale: discoverable without forcing
  the user to type a non-matching search first. Flag if you want it *only* in the not-found
  template.
- **OQ2 — where the Delete button lives.** *Default:* header, shown whenever the resource is
  persisted (View **and** Edit). Alternative: View-only. Header keeps it away from the
  Save/Cancel footer so it can't be fat-fingered during editing.
- **OQ3 — does Cancel prompt when dirty?** *Default:* **no** — Cancel is an explicit discard, so it
  suppresses the guard (prompting would double-confirm). The dirty guard is for *implicit* leaves
  (hamburger, links, browser Back). Flag if you'd rather Cancel also confirm.
- **OQ4 — dialog while navigating (RD1) is the key runtime risk.** *Default:* ship the
  "prevent-first, re-navigate-on-confirm" pattern and **verify it in a real browser** for all three
  triggers (hamburger link, browser Back, in-app link). Fallback if it misbehaves: the higher-level
  `<NavigationLock OnBeforeInternalNavigation=… ConfirmExternalNavigation=true />` component, which
  wraps the same primitive; the design says `RegisterLocationChangingHandler`, so that's the
  default, but `NavigationLock` is an accepted swap if the raw handler fights us.
- **OQ5 — nav-stack service lifetime.** *Default:* `AddScoped` (per-circuit). Not singleton (would
  leak one user's stack across circuits) and not transient (would lose the stack immediately).
  Confirmed against the "refresh loses the stack" design requirement.
- **OQ6 — external navigation (closing the tab / typing a new URL).** *Default:* out of scope for the
  Mud prompt (can't show a Blazor dialog as the tab closes); `NavigationLock.ConfirmExternalNavigation`
  could add the browser-native "Leave site?" prompt, but that contradicts "never browser-native
  dialogs," so we **don't** — the guard covers in-app navigation only. Documented, matches design
  intent.
- **OQ7 — deep multi-level nesting depth cap.** *Default:* no artificial cap; the stack handles
  arbitrary depth. Realistically ≤2. No UI breadcrumb for the stack in this slice (the browser back
  button + titles suffice); a breadcrumb is future polish.
- **OQ8 — sizing / split.** This slice bundles **three** semi-independent workstreams. The dirty
  guard and delete are low-risk and self-contained; the nested-create nav-stack is the intricate
  one and **depends on** the dirty-guard machinery. *Default:* keep all three in #9 but **build in
  order — (a) dirty guard, (b) delete, (c) nested create** — verifying each before the next, so if
  the nav-stack proves troublesome the slice still lands guard+delete. Flag if you'd rather formally
  split into **#9a (guard + delete)** / **#9b (nested create)** as separate commits.

## Out of scope (later / by design)

- **UI concurrency / lost-update** (stale dependents on delete, stale relationship sets) — design
  says UI concurrency is out of scope (reverify later).
- **Non-atomic save** (resource + tags + N edges are still separate calls) — unchanged from #8;
  hardening via a single orchestrating sproc is future work.
- **External-navigation / tab-close prompt** — OQ6.
- **A visual nav-stack breadcrumb** and **History tab** — deferred (History remains commented-out
  scaffold per §12).
- **Typed relationship catalog**, **audit actor**, **graphical explorer** — design-deferred.

## Tests

### Unit (no new server logic ⇒ thin)

This slice adds no server methods, so there's little for `ResourceServiceTests`. Cover what *is*
unit-testable without a browser:

- **`EditorNavStack`** — push/peek/pop/depth/`HasFrames`/`Clear` semantics; `EditorFrame.Returned`
  round-trips a `ReturnedChild`. (New small test class, `[Trait("Category","Unit")]`, xUnit +
  FluentAssertions with `because` clauses, `// ReSharper disable InconsistentNaming`.)
- (No new `ResourceService` behavior — `DeleteResourceAsync` already has slice-#4 coverage; the
  nested child save is a plain Create already covered. Don't invent redundant service tests.)

The bulk of #9's behavior is navigation/lifecycle, which is **only** meaningfully verifiable in the
browser — hence the E2E emphasis below.

### Committed E2E — new specs in `tools/e2e/tests/` (run via `npm run test:e2e`)

The #8 harness (webServer auto-start, `globalSetup` cleanup-then-seed, `E2eDepType` fixture, MudBlazor
helpers) is reused as-is; no new seed rows (nested children are `E2eDepType`, so cleanup wipes them).

1. **`delete.spec.js`** — create a subject (non-prod, `E2eDepType`), add an existing candidate as a
   **Dependent On**, Save; reopen; click **Delete**; assert the confirm dialog **lists the
   dependent's name**; confirm; assert redirect to `/`; assert the subject's uid now 404s / "not
   found"; open the (former) dependent's editor and assert its **Dependencies** no longer lists the
   subject (edge cascade). Also a no-dependents variant: dialog shows no dependents list, delete
   succeeds.
2. **`dirty-guard.spec.js`** — open a persisted subject, click **Edit**, change the Name; then
   (a) click the hamburger → **Create Resource** → assert the **Save/Discard/Cancel** dialog appears;
   **Cancel** → assert still on the subject with the edit intact; (b) repeat → **Discard** → assert
   navigation proceeded and (reopen) the change was **not** persisted; (c) repeat → **Save** → assert
   navigation proceeded **and** the change **was** persisted. Include a browser-**Back** variant of
   (a) to cover RD1.
3. **`nested-create.spec.js`** — create a subject (non-prod); Dependencies tab → **Create new…**;
   assert the nested editor opened (URL has `?n=1`) with the Domain select **disabled and preset to
   `non-prod`** (domain lock, RD7); fill Type=`E2eDepType` + an `E2E Nested Child` name; **Save**;
   assert popped back to the parent (URL back to the subject's create/permalink route) with **`E2E
   Nested Child` now listed under Dependencies** (RD8 link); Save the parent; reload; assert the
   child persists **as a standalone resource** and the **edge round-trips**. A second assertion:
   start a nested create, fill nothing, then **browser-Back** (clean child ⇒ no prompt) → assert
   return to the parent with **no** new dependency row (`Returned == null` path) and that **no new
   `E2eDepType` resource was created** (child persists only on the child's Save — RD8). A third:
   start a nested create, fill it, **browser-Back → Save** in the prompt → assert the child links
   into the parent anyway (RD19).

All specs filter the known benign 404 console pattern (as #8 does) and assert zero other console
errors. `workers:1` / `fullyParallel:false` unchanged (shared localdb).

## Verification

1. `dotnet build HT.ResourceMapper.slnx` clean (only the known non-SDK `sqlproj` MSB4278 under
   `dotnet build`).
2. `dotnet test` green — existing suite unchanged + the new `EditorNavStack` unit tests; the 2
   pre-existing unrelated `HT.Api.Service.Contracts.Tests` failures remain out of scope (as noted in
   #7/#8).
3. **Drive the real browser** (the #1 requirement for this slice — it's almost entirely
   navigation/lifecycle behavior): `cd tools/e2e && npm run test:e2e` green for the three new specs
   **plus** the existing `dependencies.spec.js` (no regressions), and green again on a **second**
   run (idempotent seed/teardown). Beyond the specs, manually confirm the RD1 dialog-while-navigating
   path for browser Back specifically, and a **2-level** nested create (child → grandchild → pop →
   pop) since the specs cover only depth-1.
4. Confirm no E2E rows leak (cleanup wipes all `E2eDepType` resources, incl. nested children).

Then: flip slice #8 is already Done; flip **#9 → Done** in `00-implementation-plan-list.md` (this
completes the editor build), add an **Execution notes** section here, and commit. Per our pattern,
planning is on the stronger model; execution switches to the cheaper one.

## Execution notes

- **All steps completed and verified.** `dotnet build` — zero `error CS` (only the documented
  non-SDK sqlproj MSB4278 under `dotnet build`; unrelated). `dotnet test` — 157/157 relevant tests
  green (73 Shared + 84 Server, spanning the whole solution — 65 pre-existing Shared + 8 new
  `EditorNavStack` tests; Server suite unchanged since this slice is UI-only). The 2 pre-existing
  `HT.Api.Service.Contracts.Tests` failures noted in #7/#8 remain, confirmed unrelated — untouched
  by this slice.
- **File-placement deviation from the plan (deliberate, low-risk):** `EditorNavStack`/`EditorFrame`/
  `ReturnedChild` were placed in `Modules/Common/ResourceMapper.Common.Shared/Editor/` rather than
  `UI/ResourceMapper.UI.Web/Components/Editor/` as originally planned. Reason: the class has zero
  Blazor/Razor dependencies (only references `OpenEditorResponse`, already in
  `Common.Shared.Editor.Contracts`), and there is no dedicated UI test project in this solution —
  placing it in Common.Shared let its unit tests live in the existing, proven
  `ResourceMapper.Common.Shared.Tests` project instead of standing up a new test project just for
  one small class. Behavior is unchanged; `Program.cs` still registers `AddScoped<EditorNavStack>()`
  and `ResourceEditor.razor` still `@inject`s it identically to the plan.
- **A real bug found and fixed while driving the app (not caught by any unit test or a clean
  build):** the plan's own RD13 called for suppressing the dirty-navigation guard before the
  Create-mode "move to the resource's permalink" navigation in `SaveAsync`, but the initial
  implementation missed it. Every **successful Create save** therefore silently triggered the
  guard's `PreventNavigation()` — the user would see no visible error (the "Saved" snackbar still
  fired from inside `SaveInternalAsync`, which runs before the guard is ever consulted), but the
  URL would never advance to the new resource's permalink, an invisible "unsaved changes?" dialog
  would open, and the page would appear to silently hang on "Create Resource." Root-caused by a
  temporary `Console.WriteLine` diagnostic (removed before commit) tracing `SaveAsync`'s branch
  selection, which showed the correct branch executing but no visible effect — the missing
  `_suppressNavGuard = true` before that one `NavigateTo` call was the fix. Confirmed fixed by
  driving the real Create → Save flow repeatedly afterward.
- **A second, more subtle bug caught before it ever shipped:** the domain-lock write for a nested
  create (`_model.Domain.EditedValue = parentDomain`) set only `EditedValue`, not `OriginalValue`.
  Since `SingleValueEditor.IsChanged` compares the two, this would have made every **untouched**
  nested-create page register as dirty the instant it loaded (both `OriginalValue` and
  `EditedValue` default to `null` for a fresh Create model; setting only one made them differ).
  A user who opened "Create new…" and immediately hit browser Back — having typed nothing — would
  have hit an unwarranted "unsaved changes?" prompt for a purely system-driven pre-fill. Caught by
  manually re-deriving `IsChanged`'s definition against the fix *before* running it, not by a test
  failure; fixed by setting both `OriginalValue` and `EditedValue` to the parent's domain.
- **Framework-mechanics risk (RD1's core assumption) verified against current Microsoft Learn docs,
  not assumed from memory**, since getting this wrong would have silently broken the dirty guard:
  confirmed (a) this app's global-interactivity render mode (`<Routes @rendermode="RenderMode.
  InteractiveServer">`, root-level, no static-SSR routing) means `RegisterLocationChangingHandler`
  reliably fires for both in-app link clicks *and* the browser back/forward buttons — the
  "handlers only fire for programmatic nav" caveat in the docs applies specifically to a
  static-SSR-plus-enhanced-nav app, which this is not; (b) `LocationChangingContext.TargetLocation`'s
  exact string shape (absolute vs. root-relative vs. base-relative, with/without leading slash) is
  not documented precisely, so the implementation normalizes defensively (`NormalizeRoute`: run
  `ToBaseRelativePath` if the string parses as an absolute URI, else `TrimStart('/')`) rather than
  assuming one specific format — verified against Microsoft's own canonical example, which compares
  `TargetLocation` directly to a literal root-relative string.
- **Extensive real-browser verification** (beyond the committed specs) drove every scenario in the
  plan's Tests section manually first, to separate genuine defects from test-script artifacts
  before trusting any assertion: Create→Save→permalink navigation; Delete with and without
  dependents, including Cancel-does-not-delete and the cascade removing the edge from the
  dependent's own Dependencies tab and the deleted uid 404ing; the dirty guard's Cancel/Discard/Save
  choices via an in-app hamburger-menu link click; the same three choices via genuine browser
  back/forward (required rebuilding real SPA-pushed history via in-app navigation rather than
  `page.goto`, which performs a full cross-document reload that tears down the interactive circuit
  and any in-app history the router relies on — a test-methodology lesson documented at the top of
  `dirty-guard.spec.js`); the nested-create domain lock, child-save-links-into-parent, and
  parent-Save-persists-the-edge-after-reload round trip; an untouched nested create producing no
  dirty-guard prompt on Back; and RD19 specifically (a nested child saved via the back-prompt
  dialog, not the Save button, still links into the parent) — confirmed functionally correct, with
  one incidental, purely cosmetic finding: `MudTabs` resets to the first tab (General) after
  `RestoreFromFrame` swaps the model back in on a stack pop, so a spec must re-select "Dependencies"
  after any pop-back before asserting on its rows rather than assume the tab stayed active. Not a
  data-correctness defect — out of scope to "fix" (no design requirement to preserve tab position
  across a nested pop) — but documented in `nested-create.spec.js` so a future reader doesn't
  mistake it for one.
- **A second, harder-won test-methodology lesson: Playwright's own timing margins, not the app,
  caused real flakiness across full-suite runs.** Three consecutive full-suite passes plus one
  more after a cold server restart were required before trusting the suite as stable, because
  early runs intermittently failed in different specs each time with symptoms that looked
  app-side (a Save validation failure, a field silently empty) but were purely test-script races:
  (a) selecting Resource Type and/or Subscription each triggers its own async change-handler
  round-trip (`OnResourceTypeChanged` re-seeds Tags-tab rows; `OnDomainChanged` similarly
  re-renders) — if the test moved on to fill the Name field before those handlers' own
  server-driven re-render had landed, the late re-render could silently overwrite the
  just-typed Name back to the (still-empty) model value, confirmed via a failure screenshot
  showing Type/Domain correctly selected but Name visibly blank; (b) MudTextField's `ValueChanged`
  fires on blur but a fixed short wait afterward proved too tight under the heavier load of a full
  multi-spec run, so an immediate next action (clicking Save, or calling `goBack()`) could race
  the round-trip and act on a stale/empty Name. Fixed by adding two new shared, *observable*
  readiness gates to `mud-helpers.js` rather than guessing at longer fixed timeouts:
  `selectTypeAndDomain` (selects both, then waits for the read-only Identity preview to reflect
  both values before returning) and `fillNameAndWaitForSlug` (fills Name, then waits for the Key
  field's auto-slug to appear — a directly observable proof the C# model received the update).
  Both are now used consistently by all three new spec files' subject-creation helpers. Verified
  by four consecutive clean full-suite runs after the fix (three back-to-back, one more after a
  cold `dotnet run` restart) — zero failures, versus intermittent failures in every run beforehand.
- **New tests:** `EditorNavStackTests` (8 unit tests — push/peek/pop/depth/`HasFrames`/`Clear`
  semantics, `EditorFrame.Returned` round-tripping a `ReturnedChild`, and frame field carriage) in
  `ResourceMapper.Common.Shared.Tests/Editor/`. Three new committed Playwright specs:
  `delete.spec.js` (2 tests: with-dependents listing + Cancel + cascade-confirmed delete;
  no-dependents variant), `dirty-guard.spec.js` (2 tests: Cancel/Discard/Save via an in-app link
  click; the same via browser back/forward per RD1), `nested-create.spec.js` (3 tests: full
  domain-locked create-link-persist-round-trip flow; untouched-child-clean-Back; RD19's
  save-via-back-prompt still links). All run alongside the existing `dependencies.spec.js` with no
  regressions.
- **UI implementation matches the plan's design as written**, with the one deliberate file-
  placement deviation noted above: `EditorNavStack` service + DI registration; the
  descend/ascend discriminator (`IsReturningToFrame` / fresh-entry `Stack.Clear()`); the
  `NestedLevel` query param widening the reload-dedupe key; domain lock with the Original+Edited
  fix; `SaveInternalAsync` factored out and shared by both the Save button and the dirty guard's
  save choice; `CommitChildAndReturn` as the single pop-back-with-link path (used identically by
  both the button and the guard, per RD19); the "always prevent, then re-navigate on confirm"
  dirty-guard pattern; `UnsavedChangesDialog` / `ConfirmDeleteDialog` as plain Mud modals (never
  history entries); the Delete button in the header (visible in both View and Edit, per OQ2's
  default); the "Create new…" affordance both as a standalone `DependencyTab` button and in the
  picker's `NoItemsTemplate` (per OQ1's default, both surfaces wired to the same callback).
- **All temporary artifacts removed:** the diagnostic `Console.WriteLine` in `SaveAsync` (used to
  root-cause the missing-suppression bug) was deleted before the final build; two ad-hoc probe
  screenshots (`probe-before-save.png`, `probe-after-save.png`) generated while manually driving
  the app were deleted before commit; the dev server was stopped (`taskkill //F //IM dotnet.exe`)
  after the final verification runs; `tools/e2e/test-results/` (Playwright's own run-tracking
  artifact) remains gitignored, unchanged from #8.
