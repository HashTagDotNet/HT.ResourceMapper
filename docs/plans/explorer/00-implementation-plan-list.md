# Resource Explorer — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a graphical, SSMS-database-diagram-style explorer of the resource dependency graph — seed on a resource, expand/collapse one-hop neighbors, arrange, persist, share, and export.

**Architecture:** Bottom-up horizontal slices (data → sprocs → contracts → services → UI), mirroring the completed [editor build](../editor/00-implementation-plan-list.md). The canvas is a **Cytoscape.js** instance that holds all graph state in the browser; Blazor (Interactive Server) marshals only *events* (expand-request, save, etc.) across the circuit via an `IJSObjectReference` module and `DotNetObjectReference` callbacks. Diagrams persist server-side, owned by an **anonymous client id** (a cookie set by middleware). The canvas is read-only over the catalog — editing links out to the existing editor in a new tab.

**Design spec:** [resource-explorer-design-v1.md](./resource-explorer-design-v1.md) (read it first — every slice traces back to it).

**Tech Stack:** .NET 10 / C# 13, Blazor Web App (Interactive Server), MudBlazor 9.5.0, Cytoscape.js + MIT extensions (`cytoscape-svg`, `cytoscape-cxtmenu`, `cytoscape-popper`), SQL Server (old-style SSDT/DACPAC), xUnit + Moq + FluentAssertions, Playwright (`tools/e2e`).

---

## Global Constraints

Every task's requirements implicitly include these:

- **TFM:** `net10.0` via `$(HTTargetFramework)`; nullable reference types enabled everywhere.
- **Central Package Management:** all NuGet versions in `Directory.Packages.props`; **no `Version=`** in `.csproj`. **Do not upgrade** Moq (4.18.4) or FluentAssertions (7.1.0).
- **Tests:** xUnit, **unit only** (no integration/E2E in the test projects); repositories are **mocked**, services are the unit under test. Naming `MethodName_StateUnderTest_ExpectedBehavior` (no "Should"); `// ReSharper disable InconsistentNaming` above the namespace; class-level hierarchical `[Trait]`s incl. `[Trait("Category","Unit")]`; AAA with helpers in a bottom `#region`; every FluentAssertions call carries a `because` clause. UI behavior is covered by **Playwright** in `tools/e2e`, not a UI test project (there is none).
- **Database:** old-style SSDT project `Database/HTResourceMapperDb`. Every new `.sql` needs its own **`<Build Include="..."/>`** line in `HTResourceMapperDb.sqlproj` (no globbing). All objects in schema **`HTResourceMapper`**. Constraint naming: `PK_/FK_/UK_/AK_/CK_/DF_/IX_/UX_`. Post-deploy seed is idempotent `MERGE` in `Scripts/Script.PostDeployment1.sql`.
- **SQL is dual-tracked:** author/update every object as a `.sql` in the DB project (declarative source of truth) **and** apply the same object **directly** to `(localdb)\MSSQLLocalDB\ResourceMapper` via **`sqlcmd`** (use `sqlcmd`, **not** the SQL MCP, for this DB) so each slice is fully verifiable in-session without a manual DACPAC publish. Apply new procs as `CREATE OR ALTER` (re-runnable) while the project file keeps `CREATE`. A full VS 2026 / full-MSBuild publish (**with `/p:DropObjectsNotInSource=True`**, not `dotnet build`) reconciles the declarative project later.
- **UI:** Blazor **Interactive Server** (SignalR circuit). All live UI is in `UI/ResourceMapper.UI.Web` (the `UI.Client`/`UI.Server` dirs are dead). Components call services **in-process** — **no HTTP controllers** for app reads/writes.
- **Interop:** the canvas uses the **first** `wwwroot/js` module in the repo, loaded as an `IJSObjectReference`; register its `<script>`/module ref in `Components/App.razor`.
- **Boundary rule:** only **`ResourceUid`** (never internal `int` ids) crosses the C#→JS or URL boundary.
- **Identity/tables:** the anonymous `ClientId` is a **general-purpose primitive** (cookie via middleware); every new per-client table carries a `ClientId` column so the future settings store (slice 9) reuses it.

---

## Slices

| # | Slice | Scope (summary) | Depends on | Status |
|---|---|---|---|---|
| 1 | **[Explorer neighbor read](./01-explorer-neighbor-read.md)** | New read sproc `Resource_GetForExplorer @ResourceUid` (center node + one-hop neighbors, each with `Domain` + **`PrimaryUrl`**) as a single UNION-ALL result set; `ExplorerNodeRow` repo model; `IExplorerRepository`/`ExplorerSqlRepository`; `IExplorerService.GetNodeAsync`; `ExplorerNodeModel`/`ExplorerNeighborModel` shared DTOs; DI; unit tests. **Back-end only** (verified by unit tests + `sqlcmd`); no UI. | — | **Planning** |
| 2 | **Anonymous client identity** | `rm_client_id` cookie ensured by middleware (durable GUID); `IClientIdentityAccessor` (reads/creates via `IHttpContextAccessor`); registration in `Program.cs`; unit tests for accessor logic. Reusable primitive. No UI. | — | Planned |
| 3 | **Diagram persistence** | `Diagram` table (`ClientId`, unique `ShareId`, `DiagramUid`, `Name`, `SeedResourceUid`, `DisplayPreset`, `DiagramJson` NVARCHAR(MAX), audit); sprocs `Diagram_Upsert`/`_GetByShareId`/`_ListForClient`/`_Delete`; `IDiagramRepository`/repo; `IDiagramService` (Save/SaveAs/List/GetByShare/SaveCopy/Delete) scoped to `ClientId`; shared contracts; DI; unit tests. No UI. | 2 | Planned |
| 4 | **Canvas foundation** | First `wwwroot/js` Cytoscape module + `IJSObjectReference` wrapper; **new `ResourceExplorer.razor` page (`/explore/{ResourceUid}`) + grid "Explore" row action**; seed load + **one-hop expand/collapse** via `DotNetObjectReference` → `IExplorerService` (slice 1); **deduped, cycle-safe** node adds; directed-arrow edges; pan/zoom/fit. First clickable slice. | 1 | Planned |
| 5 | **Arrangement** | Auto-layout on first appearance, then **drag-to-move** held in client state; **collapse-all**; **remove-node with reachability cleanup** (drop nodes no longer reachable from the seed; removing the seed clears the canvas). | 4 | Planned |
| 6 | **Node presentation & actions** | Display **presets** (Name only / Name+Type / Detailed); hover **tooltip** (`popper`); inline expand control; **primary-URL link** (opens new tab); **right-click menu** (`cxtmenu`): remove, open external, **open in Resource Mapper (new tab)**. | 4 | Planned |
| 7 | **Persist / Open / Share UI** | Floating **toolbar**; serialize canvas ⇄ `DiagramJson`; **Save / Save-As / Open-Recent / Delete** (slice 3); **share link** `/explore/shared/{ShareId}` → read-only load; **"Save a copy to mine"** (clone under caller's `ClientId`). | 3,4,5,6 | Planned |
| 8 | **Export** | **Copy PNG to clipboard**, **Save PNG** (`cy.png`), **Save SVG** (`cytoscape-svg`), and browser **Print → PDF**. | 4 | Planned |
| 9 | **Client settings migration** *(last)* | `ClientSetting` table (`ClientId`, `SettingKey`, `SettingJson`) + sprocs + `IClientSettingsService`, reusing slice-2 identity; migrate the **home grid filters/view** off `localStorage` (`Home.razor`) to the server. | 2 | Planned |

---

## Deferred (not in this build)

Carried from the design spec's non-goals — parked for future stories:

- **Typed edges** (ProducesTo / ConsumesFrom) and the producer/consumer split.
- **Transitive expansion / "expand all" / computed blast-radius** (needs a recursive CTE).
- **On-canvas authoring** (drawing edges, catalog CRUD) — editing stays in the editor.
- **Multi-root / unconnected canvases** (dropping unrelated resources on one canvas).
- **Real user identity / auth**, cross-device account merge beyond the recovery-key.
- **Undo/redo, minimap, on-canvas find, secondary (non-primary) URL tags.**
- **Live refresh** of a node after it's edited in the editor tab (re-expand/refresh to pick up changes).

---

## Notes

- **Slices with a UI surface ship a visible verification hook** so progress can be eyeballed in the running app; **pure back-end slices (sprocs / repos / services / identity / persistence) need no UI stub** — they're verified by unit tests + `sqlcmd`. The reusable UI vehicle is the `/explore/{ResourceUid}` page reached from a **grid "Explore" row action**, both introduced with the real canvas in **slice 4** and grown by later UI slices. **Slices 1–3 are back-end only** (no UI); slices 4–9 each make something new clickable — the slice doc's **"UI verification hook"** section spells out exactly what.
- **Verify each slice end-to-end** before starting the next (build, tests, and — for UI slices — drive the actual app in a browser per `tools/e2e/README.md`).
- The DB project is `Database/HTResourceMapperDb`; app DB is `(localdb)\MSSQLLocalDB\ResourceMapper`. It builds in **VS/full MSBuild**, not `dotnet build`; publish **with `/p:DropObjectsNotInSource=True`**.
- Sprocs/repos are verified by manual `sqlcmd` exercises + a throwaway in-process smoke test (deleted before commit), not committed DB integration tests — same as the editor slices.
- Per-slice plan docs are added here as `NN-<slug>.md` and linked from the table **when each slice starts**.
- **Engine:** Cytoscape.js + MIT extensions, decided at plan time (see design spec §10). Vendored under `wwwroot/js/explorer/` (no CDN — this app has no external script references today).
- End each slice by moving its plan doc into place, flipping status in this table, and committing.
