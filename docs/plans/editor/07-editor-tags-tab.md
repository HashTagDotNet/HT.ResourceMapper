# Slice #7 — Editor UI — Tags tab

## Context

Seventh implementation slice (design: `docs/plans/editor/resource-logical-model-and-editor-ux.md`
§7 + §12 "Tags tab (agreed)"; list: `docs/plans/editor/00-implementation-plan-list.md`). Slice #6
shipped the editor shell (General + Review tabs, Create/Edit/View, save+toast). This slice adds the
**Tags tab** — the richest UI in the whole build — and finally wires the **applied-tag write on
save** that slices #4/#5 deliberately deferred.

Scope (from the master list): pre-seeded entry-point rows from the type template; content-aware
value editors (Link/URL validation, controlled-vocab pickers, multi-valued chips); inline **full**
tag-definition create dialog (search-first, no system/domain flags); primary single-select;
empty-row drop on save. **Dependencies / Dependent On tabs → #8; delete + dirty-guard + nav-stack →
#9.**

This slice spans all layers: a **DB** read (entry-point templates — no sproc/data exists yet), a
**service** write (tags + primary + link validation, previously ignored), and a **large UI**
(TagsTab + a content-aware value editor + a MudDialog). Most of the contracts/models it needs
already exist from slice #3 (`TagRowEditor`, `TagDefinitionModel`, `ResourceTypeEntryPointModel`,
`SaveResourceRequest.Tags`/`PrimaryTagDefinitionId`, `CreateTagDefinitionAsync`,
`GetTagDictionaryAsync`, `OpenEditorResponse.TagDictionary`/`EntryPointTemplates`).

## Verified starting state

- **No `ResourceTypeTag` read sproc and no entry-point seed data exist** (all commented out in the
  seed scripts). `OpenEditorResponse.EntryPointTemplates` is hard-returned **empty** (slice #4
  deferred it to #7).
- **`SaveResourceAsync` ignores `request.Tags`** — it persists General + Domain + `PrimaryTag
  DefinitionId` only. `SetResourceTagsAsync` exists **only on `IImportRepository`**, not
  `IResourceRepository` (which `ResourceService` uses).
- **`ResourceTag_SetForResource` is already domain-safe** (slice #5 — deletes only non-domain rows,
  skips a domain-keyed TVP row).
- **`Resource_Save` already takes `@PrimaryTagDefinitionId`** and returns the resource id.
- **`CreateTagDefinitionAsync`** (inline create, forces domain/system off, search-first via
  `skip`+return-existing) and **`GetTagDictionaryAsync`** already exist (slice #4).

## DB — new sproc

- **`ResourceTypeTag_GetAll.sql`** (NEW; + `<Build>` item in `.sqlproj`): returns every
  `(ResourceTypeId, TagDefinitionId, IsDefaultPrimary, RequirementLevel)` row. **All** rows (not
  per-type) so the client can re-seed on type change with **no round trip** (the slice-#3 design
  note). Build dacpac via full MSBuild → publish via SqlPackage `/p:DropObjectsNotInSource=True`
  — **and republish after the edit** (the recurring slice-#4/#6 lesson).

## Service + repository

- **`IResourceRepository` / `ResourceSqlRepository`:**
  - `GetAllEntryPointTemplatesAsync` → `List<ResourceTypeTag>` (reuse the existing model as a read
    shape) via `ResourceTypeTag_GetAll`.
  - `SetResourceTagsAsync(int resourceId, IReadOnlyList<(string TagKey, string TagValue)> tags, ct)`
    — add to `IResourceRepository`, calling the same `ResourceTag_SetForResource` sproc the import
    repo already uses (domain-safe). (Duplicates the import-repo method; acceptable — keeps the
    editor write path on `IResourceRepository`.)
- **`ResourceService.GetResourceEditorModelAsync`:** populate `EntryPointTemplates` from
  `GetAllEntryPointTemplatesAsync` (project to `ResourceTypeEntryPointModel`). **Bug fix (rubber-
  duck):** `BuildEditEditorModel` currently builds `model.Tags` from **all** applied tags — which
  includes the **domain tag** (it's a `ResourceTag` row). Slice #6 never rendered the Tags tab so
  it was invisible; slice #7 would show the domain as an editable row. **Filter out the domain-tag
  def** (look up each tag read's def in the dictionary; drop `IsDomainTag`) when building
  `model.Tags`.
- **`ResourceService.SaveResourceAsync`:** wire the tag write (this is the deferred piece):
  1. Capture the `resourceId` from `Resource_Save` (currently discarded as `var (result, _)`).
  2. Validate tags server-side (defense; the client also validates): for each `request.Tags`
     entry whose def is **Link**, the value must be well-formed `http`/`https` (design §7 —
     security: reject `javascript:` etc.); required (`RequirementLevel=Error`) defs must have a
     value. Collect as validation errors → return before writing.
  3. `SetResourceTagsAsync(resourceId, request.Tags → (key,value) pairs)` — domain excluded
     (client already excludes it; sproc also skips it — double-safe). Delete-all-then-reinsert
     semantics = the request carries the **full** non-domain tag set, not a delta.
  4. `PrimaryTagDefinitionId` is already passed to `Resource_Save`; the client only sends it when
     the primary row is a Link tag **with a non-empty value that survives save** (see RD below).
  - **Non-atomic note:** `Resource_Save` then `SetResourceTagsAsync` are two sprocs (no shared
    transaction — matches the import path). Documented risk; a single orchestrating sproc is future
    hardening, not this slice.

## UI — components

- **`Components/Editor/TagsTab.razor`** — the tab body. Renders `Model.Tags` (non-domain) as
  compact rows: `DisplayName` label (`*` if required, hint if suggested) · value editor · primary
  radio (Link, single-valued only) · remove. Below the list: an **"Add tag"** control — a
  searchable `MudAutocomplete` over `TagDictionary` (excluding already-added ids, the domain tag,
  and system tags) **plus a "Create new tag…"** action opening the dialog. `ReadOnly` param
  (View mode → no add/remove/edit).
- **`Components/Editor/TagValueEditor.razor`** — the content-aware editor for one row's value(s),
  driven by the row's `Definition`:
  - **Link** → `MudTextField` with live `http`/`https` validation (inline error; feeds the save
    gate).
  - **Controlled vocab** (`AllowCustomValue=false` + `AllowedValues`) → `MudSelect` (single) /
    multi-select (multi-valued) from `AllowedValues`.
  - **Free text / suggest** (`AllowCustomValue=true`) → `MudTextField` (or `MudAutocomplete` when
    `AllowedValues` present, as suggestions).
  - **Multi-valued** → N discrete values as chips (`MudChipSet`) with add/remove, each honoring the
    content-type rule. Binds to `TagRowEditor.Values` (`List<SingleValueEditor>`).
  - **System (non-domain) tags** → rendered read-only (`IsEditable=false`).
- **`Components/Editor/TagDefinitionDialog.razor`** — a `MudDialog` (via `IDialogService`) full
  definition editor: Key, DisplayName, ContentType (`Text`|`Link` select), AllowCustomValue,
  IsMultiValued, AllowedValues (chip/CSV input), RequirementLevel (Error|Suggested|Optional),
  DisplayOrder. **No IsDomainTag/IsSystemTag** (setup-only, per design). **Search-existing-first:**
  as the Key is typed, surface close dictionary matches to avoid duplicates. On confirm →
  `CreateTagDefinitionAsync` → returns the (created or existing) `TagDefinitionModel`; caller adds
  it to the in-memory dictionary and appends a tag row.
- **`ResourceEditor.razor` wiring:**
  - Add a **Tags** `MudTabPanel` between General and Review.
  - **Pre-seed on type change (Create mode):** when the General Type changes, rebuild the
    pre-seeded rows — drop existing `IsPreSeeded` rows, keep user-added rows, and add rows from
    `EntryPointTemplates` filtered by the new `ResourceTypeId` (look each `TagDefinitionId` up in
    `TagDictionary`; `IsDefaultPrimary` → pre-mark `IsPrimary`). Needs a new callback from
    `GeneralTab.OnTypeChanged` up to the parent. (Edit/View: rows come from the loaded model, no
    seeding.)
  - **Include tags in the save request:** flatten `Model.Tags` → `List<SaveTagValue>` — for each
    non-removed, non-domain row, one entry per **non-empty** value (`{TagDefinitionId,
    TagDefinitionKey, Value}`). Empty rows/values dropped. Set `PrimaryTagDefinitionId` = the
    primary row's def **only if** it's a Link tag with a surviving non-empty value, else null.
- **`EditorValidation`** — add `ValidateTags(model)`: required (Error) tag with no value → error;
  Link value not `http`/`https` → error. Combine with `ValidateGeneral` in the save gate.
- **`ReviewTab.razor`** — extend the roll-up to list applied tags + surface tag validation
  messages (required-empty, bad URL).
- **`app.css`** — a small `rm-tags-editor` block (compact rows, chip layout) in the `rm-` style.

## Rubber-duck — bugs / logic traps caught up front

- **RD1 — domain tag leaks into the Tags tab.** `BuildEditEditorModel` builds `model.Tags` from
  *all* applied tags incl. the domain `ResourceTag`. **Fix:** exclude `IsDomainTag` when building
  editor rows (service side). Without this the user would see/edit "Subscription" as a normal tag.
- **RD2 — dangling primary.** If the primary Link row is emptied or dropped on save,
  `PrimaryTagDefinitionId` would reference a def with no applied value (FK still holds; read yields
  a null primary link). **Fix:** client sends `PrimaryTagDefinitionId` only when the primary row
  is Link + non-empty + surviving; clear primary when its row is removed/emptied.
- **RD3 — empty-row persistence.** Only non-empty values are sent; empty pre-seeded *and*
  user-added rows are dropped. An empty **Suggested/Optional** row must not block save; only empty
  **required (Error)** rows error (design). `ValidateTags` encodes exactly that.
- **RD4 — non-atomic save.** `Resource_Save` + `SetResourceTagsAsync` aren't one transaction — a
  failure between them leaves the resource saved without (updated) tags. Documented; matches import;
  hardening deferred.
- **RD5 — `SetResourceTagsAsync` was import-only.** Adding it to `IResourceRepository` (not
  reaching into `IImportRepository` from `ResourceService`) keeps layering clean.
- **RD6 — re-seed clobbering edits.** Changing Type in Create re-seeds template rows; a value the
  user typed into a pre-seeded row before switching type is lost. Accepted (type change ⇒ template
  change); user-added rows are preserved. Flagged as OQ2.
- **RD7 — capture the resource id.** `SaveResourceAsync` discards `Resource_Save`'s id; it's needed
  for `SetResourceTagsAsync`. Capture it.
- **RD8 — no entry-point data ⇒ nothing to pre-seed.** The mechanism is built, but pre-seed shows
  nothing until `ResourceTypeTag` rows exist (OQ1).

## Open questions / decisions (documented defaults)

- **OQ1 — entry-point seed data.** No `ResourceTypeTag` rows exist, so pre-seed is a no-op until
  data is present. *Default:* build the read + pre-seed mechanism; seed a small template set
  **temporarily via sqlcmd** for the drive-the-app verification (cleaned up after), and do **not**
  add permanent seed — entry-point templates are deployment-specific setup data, and a setup UI to
  manage `ResourceTypeTag` is out of scope. Flag if you'd rather ship a demo seed.
- **OQ2 — type-change re-seed.** *Default:* re-seed pre-seeded rows on Type change, preserve
  user-added rows (RD6).
- **OQ3 — vocab enforcement location.** *Default:* controlled vocab enforced **client-side** via
  the select (can't pick an invalid value); server validates **Link format + required** only (the
  security- and integrity-critical checks). Full server-side vocab re-validation deferred.

## Out of scope (later slices)

- Dependencies / Dependent On tabs → **#8**. Delete + dirty-guard + nested-editor nav-stack →
  **#9**. A setup screen to manage `ResourceTypeTag` entry-point templates → not planned.

## Tests

- **`ResourceServiceTests`:** `SaveResourceAsync` writes tags (`SetResourceTagsAsync` called with
  the flattened non-domain pairs; domain excluded); required-empty tag → validation error, no
  write; Link value not http/https → validation error; primary passed through only when valid.
  `GetResourceEditorModelAsync` populates `EntryPointTemplates` from the repo and **excludes the
  domain tag** from `EditorModel.Tags` on edit-load.
- **Drive the app** (no bUnit — same as #6): seed a type + a couple entry-point templates + a
  couple tag defs (incl. a Link and a controlled-vocab multi-valued like Environment) via sqlcmd;
  Create a resource → Tags tab shows pre-seeded rows with default-primary marked → fill a Link
  (valid + invalid URL to see the error) → add a multi-valued vocab tag (chips) → "Create new
  tag…" inline (search-first) → set primary → Save → reopen in View/Edit and confirm tags + primary
  round-tripped; confirm the domain tag does **not** appear as a row. Clean up seed data after.

## Verification

1. DB builds + publishes (full MSBuild dacpac → SqlPackage `/p:DropObjectsNotInSource=True`;
   **republish after the sproc edit**).
2. `dotnet build` clean (only the known non-SDK sqlproj failure); `dotnet test` green (existing 66
   + new tag tests).
3. Drive the app end-to-end in a browser (the flow above), screenshotting each step; check the
   browser console for errors.

Then: flip slice #7 → Done and slice #8 → Planning in `00-implementation-plan-list.md`, and commit.
Per our pattern, planning is on the stronger model; execution switches to the cheaper one.
