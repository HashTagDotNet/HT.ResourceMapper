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
- **Identity/tables:** the anonymous `clientId` is a **GUID in `localStorage`, read/written via JS interop** (NOT a cookie — `HttpContext` is `null` in the interactive circuit, see design §7.1) and passed as a **parameter** into the identity-agnostic services. Every new per-client table carries a `ClientId` column so the future settings store (slice 9) reuses the same handle.

---

## Slices

| # | Slice | Scope (summary) | Depends on | Status |
|---|---|---|---|---|
| 1 | **[Explorer neighbor read](./01-explorer-neighbor-read.md)** | New read sproc `Resource_GetForExplorer @ResourceUid` (center node + one-hop neighbors, each with `Domain` + **`PrimaryUrl`**) as a single UNION-ALL result set; `ExplorerNodeRow` repo model; `IExplorerRepository`/`ExplorerSqlRepository`; `IExplorerService.GetNodeAsync`; `ExplorerNodeModel`/`ExplorerNeighborModel` shared DTOs; DI; unit tests. **Back-end only** (verified by unit tests + `sqlcmd`); no UI. | — | **Done** ✓ |
| 2 | **[Canvas foundation](./02-canvas-foundation.md)** | First `wwwroot/js/explorer/` Cytoscape module (vendored, no CDN) + `IJSObjectReference` wrapper; **new `ResourceExplorer.razor` page (`/explore/{ResourceUid}`) + grid "Explore" row action**; seed load + **one-hop expand/collapse** via `DotNetObjectReference` → `IExplorerService` (slice 1); **deduped, cycle-safe** node adds; directed-arrow edges; pan/zoom/fit. JS-interop deferred past prerender. First clickable slice. | 1 | **Done** ✓ |
| 3 | **[Arrangement](./03-arrangement.md)** | Position-preserving expansion (auto-layout on seed load only; new nodes placed around their parent); **drag-to-move** held in client state; **collapse-all** (button); **remove-node with reachability cleanup** (Shift-click; drop nodes no longer reachable from the seed; removing the seed clears the canvas); interop-safety wrapper. | 2 | **Done** ✓ |
| 4 | **[Node presentation & actions](./04-node-presentation-and-actions.md)** | Display **presets** (Name only / Name+Type / Detailed); hover **tooltip** (`popper`); inline expand control; **primary-URL link** (opens new tab); **right-click menu** (`cxtmenu`): remove, open external, **open in Resource Mapper (new tab)**. | 2 | **Done** ✓ |
| 5 | **[Diagram persistence](./05-diagram-persistence.md)** *(back-end)* | `Diagram` table (`ClientId`, unique `ShareId`, `DiagramUid`, `Name`, `SeedResourceUid`, `DisplayPreset`, `DiagramJson` NVARCHAR(MAX), audit); sprocs `Diagram_Upsert`/`_GetByShareId`/`_ListForClient`/`_Delete`; `IDiagramRepository`/repo; `IDiagramService` (Save/SaveAs/List/GetByShare/SaveCopy/Delete) taking **`clientId` as a parameter**; shared contracts; DI; unit tests. No UI. | — | **Done** ✓ |
| 6 | **[Client identity (localStorage)](./06-client-identity.md)** | `clientId` GUID **get-or-create in `localStorage` via JS interop** (deferred past prerender; matches `Home.razor`), exposed as a small reusable helper the explorer page uses; optional "copy recovery key". **Not** a cookie/middleware primitive (see design §7.1). | 2 | **Done** ✓ |
| 7a | **[Persist: Save/Open/Delete](./07a-persist-crud-ui.md)** | Toolbar (Save / Save-As / Open-Recent + per-row Delete, absorbing Fit / Collapse-all / Display / client caption); serialize canvas ⇄ **self-contained `DiagramJson`**; owner CRUD via `IDiagramService` + `clientId`. | 4,5,6 | **Done** ✓ |
| 7b | **[Share](./07b-share.md)** | **Share link** `/explore/shared/{ShareId}` → **read-only** load when `clientId` ≠ owner (suppress owner `DiagramUid`); **"Save a copy to mine"**; recovery-key restore; guard `loadJson` against malformed persisted JSON (slice-7a review note). | 7a | **Done** ✓ |
| 8 | **Export** | **Copy PNG to clipboard**, **Save PNG** (`cy.png`), **Save SVG** (`cytoscape-svg`), and browser **Print → PDF**. Plus **copy share link as a rich hyperlink** (clipboard `text/html` `<a href>` with the **diagram name** as anchor text, so pasting into Teams/Outlook shows "My Diagram", not the raw URL). | 2 | **Next** |
| 9 | **[UI polish](./09-ui-polish.md)** | Collect **the user's UI adjustments** (gathered when this slice starts — I ask for the list then) and apply them across the explorer UI: canvas chrome, spacing, colors, node styling, toolbar/menu layout, labels, and any visual refinements not tied to a feature slice. Placed late so the whole UI can be tuned holistically. | 2,3,4,7,8 | Planned |
| 10 | **Client settings migration** *(last)* | `ClientSetting` table (`ClientId`, `SettingKey`, `SettingJson`) + sprocs + `IClientSettingsService`, reusing slice-6 `clientId`; migrate the **home grid filters/view** off ad-hoc `localStorage` (`Home.razor`) to the server. | 5,6 | Planned |

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

- **Reorder (post-slice-1, accuracy correction):** a working canvas needs neither identity nor persistence, so the plan goes **canvas-first**. Identity was re-scoped from a server cookie to a **`localStorage` JS-interop helper** because Blazor-Server `HttpContext` is `null` in the interactive circuit (design §7.1). Persistence (slice 5) is pure back-end and takes `clientId` as a parameter, so it no longer gates the canvas.
- **Slices with a UI surface ship a visible verification hook** so progress can be eyeballed in the running app; **pure back-end slices (1 read, 5 persistence) need no UI stub** — they're verified by unit tests + `sqlcmd`. The reusable UI vehicle is the `/explore/{ResourceUid}` page + **grid "Explore" row action**, introduced with the canvas in **slice 2** and grown by later slices — each such slice doc's **"UI verification hook"** section spells out exactly what becomes clickable.
- **Verify each slice end-to-end** before starting the next (build, tests, and — for UI slices — drive the actual app in a browser per `tools/e2e/README.md`).
- The DB project is `Database/HTResourceMapperDb`; app DB is `(localdb)\MSSQLLocalDB\ResourceMapper`. It builds in **VS/full MSBuild**, not `dotnet build`; publish **with `/p:DropObjectsNotInSource=True`**.
- Sprocs/repos are verified by manual `sqlcmd` exercises + a throwaway in-process smoke test (deleted before commit), not committed DB integration tests — same as the editor slices.
- Per-slice plan docs are added here as `NN-<slug>.md` and linked from the table **when each slice starts**.
- **UI adjustments** the user raises before slice 9 are parked into **slice 9 (UI polish)** rather than applied ad-hoc mid-slice; I ask for the full list when that slice starts.
- **Carried to slice 7** (from slice-5 review): (a) `IDiagramService.DeleteAsync` returns `ApiServiceResponse<object>` (not `<bool>` — the envelope is constrained `class, new()`); consume it accordingly. (b) The public `Diagram_GetByShareId`/`DiagramModel` currently returns the owner's private `DiagramUid`; slice 7's share flow must decide whether a **read-only recipient** should receive it (recipients use `SaveCopy` via `ShareId`, so likely blank/omit `DiagramUid` for non-owners). `Diagram_Upsert` now safely **denies** a cross-client `DiagramUid` (maps to NotFound).
- **Engine:** Cytoscape.js + MIT extensions, decided at plan time (see design spec §10). Vendored under `wwwroot/js/explorer/` — **no CDN scripts** (the app loads only local `_content`/`_framework` scripts today; the one external ref is a Google Fonts stylesheet).
- End each slice by moving its plan doc into place, flipping status in this table, and committing.
