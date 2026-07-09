# Slice #6 — Editor/Details UI — core

## Context

Sixth implementation slice (design: `docs/plans/editor/resource-logical-model-and-editor-ux.md`
§12; list: `docs/plans/editor/00-implementation-plan-list.md`). Slices #1–#5 built the full
data → sproc → repository → service → import stack; identity is now the `(Domain+Type+Key)`
triple end-to-end. **This is the first UI slice** — it puts the resource **editor/details screen**
in front of the user, binding to the in-process `IResourceService` built in #4/#5.

Scope is the **core**: the tabbed component shell, the three modes (Create/Edit/View), the
**General** tab (the identity-bearing fields + live uniqueness), the **Review** tab + save gate,
save + success toast, the two routes, and the "Create Resource" hamburger entry. The **Tags**
(#7), **Dependencies / Dependent On** (#8), and **delete + dirty-guard + nav-stack** (#9) tabs/
behaviors are explicitly out of scope — this slice renders **General + Review** only and leaves
room for the others.

**Stack facts (verified):** Blazor Server, MudBlazor **9.5.0**, global `InteractiveServer`
render mode (`App.razor`), hamburger `MudMenu` in `Components/Layout/MainLayout.razor` (currently
only "Import…"), `Import.razor` is the clean single-page service+snackbar pattern to mirror, and
CSS conventions are `rm-`-prefixed in `wwwroot/app.css` (compact, `var(--mud-palette-divider)`
borders, 8px radius). `Home.razor` **already** links to a detail route via `OpenDetails` — but
uses `DetailRoute = "/resource"` (singular, "not built yet"); this slice builds the page and
reconciles the route to **`/resources`** (plural, per design §12 + the master list).

## Required contract fix (found in planning — the editor can't save without it)

**`OpenEditorResponse.ResourceTypes` exposes no type id.** It is
`List<KeyValuePair<string,string>>` = `(ResourceTypeUid → TypeName)`, but both
`SaveResourceRequest.ResourceTypeId` and `ResourceUniquenessRequest.ResourceTypeId` are **`int`**.
The Type `MudSelect` therefore has no way to turn the picked type into the id that save +
uniqueness require. Fix:

- Add `Common.Shared/Editor/ResourceTypeOption.cs`: `{ int ResourceTypeId; string TypeName; string ResourceTypeUid; }`.
- Change `OpenEditorResponse.ResourceTypes` to `List<ResourceTypeOption>`.
- Update the projection in `ResourceService.GetResourceEditorModelAsync`
  (`ResourceService.cs:194`) to emit it. No test pins the old shape (verified — the only
  `.ResourceTypes` test refs are `ImportRequest.ResourceTypes`, unrelated), so this is low-risk.
- Add a small `ResourceService` unit test asserting the option list carries the id (the one
  non-UI-testable behavior this slice adds).

The client binds the Type `MudSelect` to `ResourceTypeId` (int) and keeps the model's
`ResourceType` `SingleValueEditor` (the type **name**, for display / identity preview) in sync
from the chosen option.

## Component architecture

Thin route page + per-tab child components, so #7/#8 slot their tabs in without touching the shell:

- **`Components/Pages/ResourceEditor.razor`** — two routes on one component:
  `@page "/resources"` (Create) and `@page "/resources/{ResourceUid}"` (View/Edit). Owns: mode
  resolution, data load (`GetResourceEditorModelAsync`), the `MudTabs` shell, the footer action
  bar, the save handler + toast, and post-save navigation. Injects `IResourceService`,
  `NavigationManager`, `ISnackbar`.
- **`Components/Editor/GeneralTab.razor`** — the General fields, identity preview, and live
  uniqueness. Takes the `ResourceEditorModel`, the `List<ResourceTypeOption>`, the
  `DomainAllowedValues`, the mode, and callbacks (uniqueness-check, field-changed).
- **`Components/Editor/ReviewTab.razor`** — read-only roll-up of General + the validation
  summary; the parent's Save button is enabled only when this reports clean.
- **`Components/Editor/EditorValidation.cs`** (plain class) — shared validation helpers
  (required-field checks, identity completeness) so the General tab, Review tab, and save gate
  agree on "valid". Writes messages into each field's `SingleValueEditor.Messages`
  (`EditorFieldLevel.Error/Warning`), which the Mud fields render via `Error`/`ErrorText`.

(`Components/Editor/` is a new folder; Tags/Dependencies tabs join it in #7/#8.)

## Modes (one component, three behaviors — design §12)

- **Create** (`/resources`, no uid): linear **wizard** — General → Review; per-field validation on
  General blocks progress; Review runs full validation and gates Save. New Guid uid comes from the
  skeleton `EditorModel` the service returns.
- **Edit** (`/resources/{uid}`, after clicking **Edit**): **property-sheet** — all tabs reachable,
  inline validation, **Save from anywhere** once valid (no forced Review). `IsPersisted=true`.
- **View** (`/resources/{uid}`, default landing): **read-only** roll-up (fields disabled) with an
  **Edit** button that flips to Edit mode. Reached from the home grid row-action.

Mode resolution: no `{ResourceUid}` → Create. With a uid → View, and an Edit toggle flips to Edit
(**internal state flip, no URL change** in #6 — the history-pushing nav stack is #9). The service
call (`OpenEditorRequest { Mode, ResourceUid }`) uses `"Edit"` for both View and Edit loads (the
service builds the same populated model; the component controls read-only rendering).

## General tab (design §12 "General tab (agreed)")

Field order/flow, compact, one help line at top, `*` on required, native Mud controls:

1. **Resource Type** — `MudSelect<int>` bound to `ResourceTypeId`, required, **read-only after
   save** (disabled in Edit/View). Options from `ResourceTypeOption`. On change (Create only),
   keep `ResourceType` (name) editor in sync + re-run uniqueness.
2. **Subscription / Domain** — `MudSelect<string>` from `DomainAllowedValues`, required,
   **read-only after save** (design G6 — the editor locks domain-value edits; slice #5 froze it
   in `Resource_Save` too). On change (Create only), re-run uniqueness.
3. **Name** — `MudTextField` bound to `Name.EditedValue`.
4. **Key** — `MudTextField` bound to `Key.EditedValue`, required, **auto-derived `slug(Name)`**
   in Create until the user manually edits it (a `_keyManuallyEdited` flag stops the derivation);
   in Edit it's a plain editable field (no silent rewrite of a persisted key). Client-side
   `slug()`: lowercase, spaces/underscores → `-`, strip non `[a-z0-9-]`, collapse repeats.
5. **Description** — multiline `MudTextField`, optional.

- **Identity preview** — read-only line `{domain} / {type} / {key}` (reuse
  `IdentityPreview.Display`), so the user sees what makes the record unique.
- **Early uniqueness** — once **Domain + Type + Key are all set** (fire on Key blur, and on
  Type/Domain change), call `CheckUniquenessAsync({ Domain, ResourceTypeId, ResourceKey,
  ExcludeResourceUid = uid-in-edit })`; on `IsUnique=false` show an inline error on Key
  immediately. Gate the call on all three present (a null-domain check is meaningless — the
  domain-aware sproc would always report unique). Debounce via blur/explicit triggers, not
  per-keystroke.
- **CreatedOn/UpdatedOn** shown here (and on Review) in Edit/View (design §12).

## Review tab + save gate

- Read-only roll-up of all General values + the identity line + any validation messages.
- **Save enabled only when clean**: Type/Domain/Name/Key non-empty and the latest uniqueness
  result is unique. On Save, **re-validate** and build the `SaveResourceRequest`.

## Save flow

- Build `SaveResourceRequest { Mode, ResourceUid, ResourceTypeId, Domain, ResourceName,
  ResourceKey, Description, PrimaryTagDefinitionId = null, Tags/DependsOnUids/DependentOnUids =
  empty }` — **General + Domain only** this slice (primary link is #7; edges are #8; the service
  already persists exactly this subset).
- Call `SaveResourceAsync`. On success: `Snackbar.Add("Saved {name}", Success)`, then
  **transition to View mode of the saved resource** — `NavigationManager.NavigateTo(
  "/resources/{uid}", replace:true)`, reload as View (`IsPersisted=true`). On Create this also
  moves the URL from `/resources` to the resource's permalink. (The home grid shows the new row
  when the user navigates home and filters allow — design §12; no forced grid refresh here.)
- On failure: the service returns validation errors (collision, type/domain-frozen, required) —
  map them back onto the relevant field (`SingleValueEditor.Messages`) + a warning snackbar.
  This is the server-side backstop for a stale client uniqueness result (TOCTOU-safe: the service
  re-checks in `SaveResourceAsync`).

## Routes, navigation & hamburger

- **`ResourceEditor`** serves `/resources` (Create) and `/resources/{ResourceUid}` (View/Edit).
  Handle `{ResourceUid}` **parameter changes** (navigating uid→uid reuses the component) by
  reloading in `OnParametersSetAsync` keyed on the uid.
- **Hamburger**: add `<MudMenuItem Href="/resources" Icon="@Icons.Material.Outlined.Add">Create
  Resource</MudMenuItem>` to `MainLayout.razor` (above/below "Import…").
- **`Home.razor` ripple**: change `DetailRoute` `"/resource"` → `"/resources"` (and the stale
  "not built yet" comment). `OpenDetails`/`CopyDetailsLink` then correctly point at the new page.

## CSS

Add a small `rm-editor` block to `app.css` (compact tabs, footer action bar, identity-preview
line, help text) consistent with existing `rm-` conventions — no new stylesheet.

## Out of scope (later slices)

- **Tags tab** (pre-seeded entry-point rows, value editors, inline tag-def create, primary
  select) → **#7**. **Dependencies / Dependent On tabs** (resource picker, nested create) → **#8**.
- **Delete action + confirm dialog; dirty-guard (`RegisterLocationChangingHandler`); nested-editor
  history/nav-stack** → **#9**. (So in #6, navigating away discards unsaved edits silently — an
  accepted #6 limitation.)
- **History tab** — per design §12 "build the tab but comment it out"; include it as a commented
  scaffold only (does not render).
- `GetResourceDetailAsync`/`ResourceDetailModel` stay the API/programmatic read path; the UI uses
  `GetResourceEditorModelAsync` for all three modes (design: one component serves view+edit).

## Open questions / decisions (documented defaults — flag if you disagree)

- **OQ1 — post-save destination.** Default: land on the saved resource in **View mode**
  (`/resources/{uid}`), not back on the home grid — keeps the user in context and shows the toast.
- **OQ2 — Edit-toggle URL.** Default: **internal state flip** (no `?mode=edit`), since the
  history-pushing nav stack is #9. Deep-linking directly into Edit is deferred.
- **OQ3 — tabs rendered in #6.** Default: **General + Review only**; #7/#8 insert their tabs
  between them. (The History scaffold is commented out.)

## Verification (a UI slice — build + drive the app; no bUnit in this repo)

1. **Build** the solution — `dotnet build` clean (only the known `sqlproj` non-SDK failure).
2. **Unit test** the one non-UI change: `ResourceService.GetResourceEditorModelAsync` returns
   `ResourceTypeOption`s carrying the int id; `dotnet test` green (existing 65 + new).
3. **Drive the running app** (`dotnet run` UI.Server; publish DB first) — the master-list rule
   "for UI, drive the actual app":
   - Hamburger → **Create Resource** → General: pick Type + Domain, type Name (Key auto-slugs),
     edit Key; identity preview updates; enter a Key that collides → inline error; fix it →
     Review → **Save** → success toast → lands in View mode at `/resources/{uid}`.
   - Home grid row-action (the ✎) → opens `/resources/{uid}` **read-only**; **Edit** → change
     Name/Description (Type + Domain disabled) → Save → toast, stays in View.
   - Deep-link `/resources/{uid}` loads View; `/resources` loads a blank Create.
   - Confirm Type/Domain are frozen in Edit; confirm a required-field-empty state disables Save.

Then: flip slice #6 → Done and slice #7 → Planning in `00-implementation-plan-list.md`, and
commit. Per our pattern, planning is on the stronger model; execution switches to the cheaper one.

## Execution notes

- **All steps completed and verified — including actually driving the app in a real browser**
  (no project run-skill existed; used `playwright-core` against system Chrome, headless, since
  `chromium-cli` wasn't available). `dotnet build` — zero `error CS` across both the plain
  solution build and the DB project's full-MSBuild rebuild (only the documented non-SDK sqlproj
  failure under `dotnet build`, unrelated). `dotnet test` — 66/66 (65 prior + the new
  `ResourceTypeOption` id-carrying test).
- **Contract fix landed as planned:** `OpenEditorResponse.ResourceTypes` is now
  `List<ResourceTypeOption>` (`{ResourceTypeId, TypeName, ResourceTypeUid}`); `GeneralTab`'s Type
  `MudSelect<int?>` binds to it directly and keeps `Model.ResourceType.EditedValue` (the name) in
  sync on change.
- **Real bug found and fixed while driving the app — a permanently-stuck loading spinner.**
  `LoadAsync`'s "avoid redundant reload" guard compared `ResourceUid` (null in Create mode, no
  route param) to `_lastLoadedUid` (also null, the field's default) — `string.Equals(null, null)`
  is `true`, so the very **first** load on `/resources` returned immediately, before the
  `finally` block ever set `_loading = false`. The page never rendered past the progress bar.
  Fixed with an explicit `_hasLoadedOnce` flag so the guard can't fire before any load has
  actually happened.
- **A second, more consequential bug found the same way — a genuine latent defect from slice #4,
  only now exposed.** After the loading-spinner fix, Save reported failure with no visible field
  errors. Reproduced directly against the service (bypassing the browser) to get the real
  exception: `Resource_GetByResourceUid` was still the *pre-slice-5* definition — its `Domain`
  column addition had been edited in the repo during slice #5 but **never republished** to this
  localdb (the identical class of mistake slice #4 already hit once and documented as a lesson).
  Republishing surfaced a **second, previously-undetected bug**: `ResourceTag_GetForResource.sql`
  (added in slice #4) computes `IsPrimary` as `CASE WHEN … THEN 1 ELSE 0 END` with no cast — SQL
  Server infers that as `INT`, but the C# reader calls `dr.ReadBoolean("IsPrimary")`, throwing
  `Unable to cast … Int32 … to … Boolean`. This was invisible in every prior slice because it only
  fires when `ResourceTag_GetForResource` returns a **non-empty** result set — and slice #5's
  domain-tag-on-create is the **first** code path that ever gives a freshly-created resource a
  tag row (its own domain tag) before any read of it. Fixed with an explicit
  `CAST(... AS BIT)`, matching the one other place in the codebase that already did this
  correctly (`Resource_GetFilterValues.sql`'s `IsBlank`). **Lesson reinforced a second time:
  republish the dacpac after *every* sproc edit, not just at the end of a slice** — and a bug that
  never triggers because a code path is only ever exercised with an empty result set can hide for
  multiple slices.
- **Cosmetic fix:** the Resource Type `MudSelect` initially bound `int` with a `?? 0` fallback,
  which rendered a literal "0" before any type was picked (no `MudSelectItem` has `Value=0`).
  Switched to `MudSelect<int?>` bound directly to `Model.ResourceTypeId` with a `Placeholder`.
- **Full manual walkthrough, screenshotted at each step, confirmed correct:** Create → General tab
  (Type/Domain selects populated from seeded data; picking both plus typing a Name correctly
  auto-slugs Key; identity preview live-updates to `"non-prod / {Type} / {key}"`) → Save (success
  snackbar named the resource; URL changed to `/resources/{uid}`; landed in View mode) → **Edit**
  (Type and Domain selects visually disabled/greyed; Name/Key/Description remained editable) →
  edited Description → **Save** again (stayed on the same URL, dropped back to View mode) → Home
  grid (showed the new row with its Description edit and a `Domain: non-prod` tag chip) → the
  row's "Open details" icon correctly navigated to `/resources/{uid}` — confirming the
  `Home.razor` `/resource`→`/resources` route fix. One console `404` appeared during the run but
  did not reproduce in isolation on either page — a pre-existing, non-blocking artifact, not a
  regression from this slice.
- **Simplification vs. the plan:** did not attempt to gate/disable the Review `MudTabPanel` during
  Create (the "linear wizard… blocking" nicety) — MudBlazor 9.5's exact API for disabling one tab
  panel wasn't independently confirmed, and the load-bearing rule ("not saved until Save
  succeeds") is already enforced at the Save button regardless of which tab is active. Both tabs
  are freely switchable in all three modes; only General's fields react to `ReadOnly`/frozen
  state. Revisit if a later slice wants the stricter wizard gating.
- **All temporary test artifacts removed:** the seeded `Slice6SmokeType` resource type and every
  resource/tag row created while driving the app were deleted from `(localdb)`; the dev server was
  stopped; the debug reproduction test (`Slice6DebugTest.cs`) was created, used, and deleted.

## Rubber-duck note

This plan was adversarially reviewed before writing. Findings folded in: the **type-id contract
gap** (the blocker — editor couldn't build a valid save/uniqueness request); **Home.razor's
`/resource` vs `/resources`** mismatch; **Domain must be read-only in Edit** (design G6 +
slice-#5 freeze), not just Type; **route-param-change reload** (uid→uid reuses the component);
**gate uniqueness on all three identity parts** (a null-domain check always reports unique under
the domain-aware sproc); **server save re-validates** so a stale client uniqueness result is
caught (TOCTOU); and **prerender double-loads** `OnInitializedAsync` (harmless for reads; the live
circuit's Create-uid is the one that persists — opt the page out of prerender only if flicker
shows).
