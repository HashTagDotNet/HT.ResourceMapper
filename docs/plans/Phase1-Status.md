# Phase 1 — Status & Resume (last updated 2026-06-19)

Self-contained handoff for restarting work. Authoritative plan: `docs/plans/UX-Plan-V2.md`.
Run instructions: `UI/ResourceMapper.UI.Web/README.md`.

## TL;DR
**Phase 1 is functionally complete and live-verified.** Import pipeline (validate + write) works against a
real DB; the new Blazor Web App SSR UI shows the imported wiki resources with working Azure links; the old
WASM UI is removed. Commits on branch `home-page`: `eda23be`, `67c9d6f`, `a579042`, `62106ed` (+ this doc).

## How to run (this machine is already configured)
1. LocalDB `ResourceMapper` is published with the schema/sprocs; user-secret connection strings are set for
   `UI/ResourceMapper.UI.Web` (UserSecretsId `1a3b74ec-d315-416c-9c29-a2781962918d`).
2. `dotnet run --project UI/ResourceMapper.UI.Web` (or F5 in VS) → home grid shows 21 resources.
3. Fresh machine / re-publish steps: see `UI/ResourceMapper.UI.Web/README.md`.

## What's done (verified)
- **Import pipeline** (`Modules/Common/ResourceMapper.Common.Server/Resources/`):
  `ImportService` does schema/reference/conflict validation + write phase (UID gen, two-pass, skip-means-skip);
  `IImportRepository`/`ImportSqlRepository`; sprocs `TagDefinition_GetAll`, `Resource_GetAllKeys`, `*_Upsert`,
  `ResourceTag_SetForResource`; TVP `TagKeyValueList`; SqlClient helpers `AddTvp`/`AddVarchar(direction)`/
  `ReadString(cmd)`. **20/20 unit tests** (`_Tests/Common/Server/.../Resources/ImportServiceTests.cs`).
  Endpoint `POST /api/resources/import` + the in-app Import page both work.
- **UI** (`UI/ResourceMapper.UI.Web`, Blazor Web App, static SSR + `InteractiveServer`): MudBlazor light theme;
  full-width header + hamburger menu (Import…); resource grid bound to `GetResourceGridItems` with Link-tag
  copy-on-hover and Text tags; Import page (file upload → summary). Single-load via `PersistentComponentState`.
- **Old UI removed**: `ResourceMapper.UI.Client/Server`, stale `HT.ResourceMapper.UI/*`, and the orphaned
  `ResourceMapper.Common.Client` module — all deleted and out of the solution.
- **Data**: the wiki "Visibility Resources" table extracted → `docs/plans/artifacts/wiki-import.json`
  (21 resources, 7 types), imported live into LocalDB and rendering. Decisions in
  `docs/plans/artifacts/wiki-extraction-notes.md`.

## Locked decisions (so a fresh session need not re-derive)
- **Stack**: Blazor Web App on .NET 10, global static SSR + `InteractiveServer` (server-rendered first paint,
  no WASM). Azure App Service + Always On for "wiki-fast" in prod. Reuse the existing backend; replace UI only.
- **Tags model**: `ResourceGridTagModel` = key + value + contentType (`Text`|`Link`). Links are `Link`-type
  tags (no separate display-text field). `Environment` and `Link` are distinct tags.
- **Extraction model**:
  - *Visibility Resources table* → each Azure-link cell is a resource (`name`=Azure id, `description`=row label,
    `type` per row), with `Environment` (Text) + `Link` (Link) tags; multi-link cells → one resource per link.
  - *Per-environment matrix tables* (Database App Names, Service Fabric, App Insights Query Filters) → the ROW
    is the resource (e.g. "Alert Aggregator"); each table adds env-scoped tags (`DbCode`, `SF`,
    `AppInsightsQuery`). **Not yet extracted.**
  - Out of scope: logging/Serilog config, KQL config text, Tools.
- **Import contract**: `Modules/Common/ResourceMapper.Common.Shared/Import/Contracts/ImportContract.cs`
  (camelCase JSON; every tag key needs a tagDefinition; every type a resourceType; keys case-insensitive;
  dependencies omitted this phase). Full design: `docs/plans/ImportExportApiDesign.md`.

## Current working-tree state (as of session close 2026-06-19)
HEAD is `4b5bf35` on branch `home-page` (4 commits ahead of `origin/home-page` — **not pushed**). Two
**uncommitted** modifications are in the working tree:
- `Database/HTResourceMapperDb/HTResourceMapperDb.sqlproj` — the legacy-SSDT-format rewrite (see below). Left
  **uncommitted on purpose**; decide whether to keep it before staging. Do not revert without asking.
- `.claude/settings.local.json` — permission-allow entries added while running `mp-code-review` on an unrelated
  repo (PR 272445, macropoint.com). **Unrelated to this project**; safe to keep or discard.

Leftover (untracked-ish) cruft: `UI/ResourceMapper.UI.Client/obj` and `UI/ResourceMapper.UI.Server/obj` remain
after the source projects were deleted — empty build output, safe to delete.

## Known issues / watch-outs
- **DB project is now legacy SSDT format** (`Database/HTResourceMapperDb/HTResourceMapperDb.sqlproj`, changed by
  a concurrent agent). It **does not build under `dotnet build`** (needs VS/MSBuild + SSDT). Build the .NET
  projects individually (e.g. `dotnet build UI/ResourceMapper.UI.Web/...`) rather than the whole solution, and
  build/publish the DB in VS or via SqlPackage on the dacpac. Do not revert without asking.
- A **second agent** has been working in this repo concurrently — expect working-tree changes you didn't make
  (e.g. the .sqlproj). Stage deliberately.
- App **requires** the connection-string user secrets or it throws `KeyNotFoundException` at startup.
- Grid currently loads top 100, default sort Name A→Z; no paging UI yet.

## Next steps (suggested order)
1. **Extend extraction** to the per-environment matrix tables (row-is-the-resource model above) for richer data.
2. **Section tables** (`## App Configuration`, etc.) — overlap the 21 already imported; dedupe by key (low value).
3. **Phase 1b**: search/filter UI (the grid request contract + `Resource_GetItems` already support `SearchFor`).
4. **Phase 2**: resource detail page at `/resources/{uid}` (the grid name can then link to it), add/edit.
5. **Human browser pass**: run the checklist in `UI/ResourceMapper.UI.Web/README.md` (menu, links, copy, import).
