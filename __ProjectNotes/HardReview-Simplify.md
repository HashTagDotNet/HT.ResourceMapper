# Hard Review — Simplify (branch `home-page`)

Read-only review of MacroPoint.Azure.ResourceExplorer against the decided context: no model change, the
Azure resource id becomes a multi-valued tag, the graph UI is cut, and the app is done when a user can
find "PingByPhone" → see its Azure id(s) → see what is impacted (transitively), and the reverse from an
ARM id. Every claim cites file and symbol; §7 separates what was read from what was inferred.

## Summary

- **Top cut — graph UI (confirmed):** 4 sprocs + `Diagram` table + 1 read sproc, 2 services/2 repos/4
  interfaces/3 row models, 6 shared models, 1 page + 2 dialogs, 4 JS files (3 vendored), ~95 lines of
  CSS, 2 test classes, 3 `<script>` tags, 1 grid row action. Nothing else depends on it; the relationship
  reads it shares (`ResourceRelationship_GetForResource`) stay.
- **Next cuts:** Saved Views (table, 5 sprocs, page, menu, dialog, controller, context, tests), per-client
  "resume" settings (`ClientSetting`), the configured single-owner identity, the resource-type icon/short-code
  vocabulary (explorer-only), `UxLab.razor`, the `Sample` service, the unused `Collections` library, the
  5-tab editor's Review tab, the "Dependent On" authoring tab, and the home grid's Add-filter/Active-filters
  machinery (hide, do not delete).
- **Top defects:** (1) `Script.PostDeployment1.sql` rewrites the domain tag's `AllowedValues` on every publish,
  silently deleting any Tier/Subscription a user created via `DomainValue_Add`; (2) `ResourceService.SaveResourceAsync`
  never requires `request.Domain`, so a domain-less resource bypasses the code-enforced Domain+Type+Key
  uniqueness (`Resource_CheckUnique` can never match `NULL`); (3) save is three unrelated writes with no
  transaction (`Resource_Save`, `ResourceTag_SetForResource`, N× `ResourceRelationship_Add/Remove`);
  (4) the configured RO connection string is validated at start-up but never used — `DependencyRegistration`
  hands the RW string to both sides of `DbConnector`.
- **Use-case gaps:** no `AzureResourceId` tag definition exists in any seed; impact is one hop deep and lives
  only in the editor's Dependent On tab, whose rows are not links; the same-domain rule blocks the
  cross-tier edges the impact question needs; the search pattern silently truncates at 255 chars after
  LIKE-escaping.

---

## 0. Beta Release Target

**Beta is the smallest app that answers the migration question in both directions and lets one person
keep the catalog current by hand.**

### In scope (Beta)

1. **Find by name or Azure id.** One search box on the home page matches resource name, key, description and
   every tag value — including `AzureResourceId` values — so pasting an ARM id or a `usspi*` name lands on
   the logical resource.
2. **See the Azure id(s).** The resource view shows every `AzureResourceId` value up front, each marked as the
   old (`usspi*`) or new id, with copy.
3. **See impact.** The resource view lists Depends On and Used By (direct), with a per-row expand that walks
   the next hop until nothing is left, so the transitive closure of "who breaks" is readable without
   leaving the page or re-searching.
4. **Quick entry.** A resource can be created from Name, Type, Azure id, a couple of key tags and its
   dependencies on one short form; everything else is behind a disclosure.
5. **Keep what already works for the above:** the grid, server-side search, the editor's General/Tags/
   Dependencies, import/export (needed to bulk-load the new ids), Resource Types and Tags admin screens
   (needed to create the `AzureResourceId` definition), Domains admin.

### Out of scope (Post-Beta or Cut)

- The graph canvas, saved diagrams, share links, PNG/SVG/print export (Cut).
- Saved Views, per-client resume, the saved-views REST controller (Post-Beta).
- Any taxonomy/placement/wayfinding model change from `PreReleaseRefactorAnalysis.md` (deferred by decision).
- Typed relationships, cross-domain edge authoring in the import format, Azure tag import (Post-Beta).
- Icon keys / short codes / node previews on resource types (Cut with the graph).
- The Review tab, wizard footer, Dependent On authoring tab (Hide).
- Multi-user identity, auth, history/audit (Post-Beta).
- MDLP integration with cookie authentication (Post-Beta; required, scheduled after Beta).

### Acceptance criteria (observable outcomes; settings named, values not)

- Typing a resource name into the home search returns that resource within the configured display count,
  and opening it shows every Azure resource id tag value with its old/new status.
- Pasting a full ARM id (old or new) into the same search box returns the owning logical resource; so does
  pasting the trailing resource name segment alone.
- Pasting a resource-group or region fragment returns every logical resource whose Azure id contains it.
- On a resource, Used By lists every direct dependent; expanding any row lists that row's dependents, and
  so on until a row has none; a row already shown higher in the tree is marked rather than expanded again.
- Depends On behaves the same in the other direction.
- A new resource with Name, Type, one Azure id, one tag and one dependency can be saved from a single form
  without opening a second tab or dialog; Key defaults from Name; the domain/tier defaults to the configured
  default domain value and is only shown under More Options.
- Saving the same identity (Domain + Type + Key) twice is refused with a message, from the UI and from the
  import endpoint, including when the domain is omitted.
- Republishing the database project leaves every domain value a user has added in place.
- A failed save leaves no half-written resource (resource row without its tags or edges).

---

## 1. Graph Removal Inventory (Beta)

Everything below goes with the graph cut. "Shared" rows are things the graph code *uses* that must stay.
Verified by reading each file; `App.razor`, `Home.razor` and `DependencyRegistration.cs` were read whole.

### UI (`UI\MacroPoint.Azure.ResourceExplorer.UI.Web`)

_Widen the terminal if this renders as stacked Key: value records._

| Item | Symbol / detail | Action |
|---|---|---|
| `Pages\ResourceExplorer.razor` | routes `/explore/{ResourceUid}`, `/explore/shared/{ShareId}`; whole file | Delete |
| `Components\Explorer\OpenDiagramDialog.razor` | injects `IDiagramService` | Delete |
| `Components\Explorer\DiagramNameDialog.razor` | | Delete |
| `wwwroot\js\explorer\explorer-canvas.js` | ES module imported by `ResourceExplorer.OnAfterRenderAsync` | Delete |
| `wwwroot\js\explorer\vendor\cytoscape.min.js`, `cytoscape-cxtmenu.js`, `cytoscape-svg.js` | vendored | Delete folder |
| `Components\App.razor` | three `<script src="js/explorer/vendor/…">` tags | Remove tags |
| `wwwroot\app.css` | `.rm-explorer-host` … `.rm-explorer-title .rm-title-legend` (one contiguous block) | Remove block |
| `Pages\Home.razor` | row action "Explore dependencies" (`Explore(string uid)`, `MudTooltip Text="Explore dependencies"`) | Remove action + method |
| `Pages\Home.razor` | `Icons.Material.Outlined.Schema` usage only there | falls out |
| `Layout\MainLayout.razor` | comment "from the editor or the explorer" (PL-08) | comment only |
| `Components\ResourceTypes\ResourceTypeDialog.razor` | "Explorer node preview" section, `_shortCode`/`_iconKey` fields, `ResourceTypeIcons` picklist, helper that mirrors `explorer-canvas.js` | Remove section (see §2 Icon keys) |
| `Pages\ResourceTypes.razor` | "Node" and "Icon Key" columns, `NodeCode(…)`, `ResourceTypeIcons.ColorFor` | Remove columns |
| `Pages\ResourceTypes.razor.css`, `ResourceTypeDialog.razor.css` | `.rt-node`, `.rt-dialog-row`, `.rt-preview-body`, `.rt-preview-node` — both files contain only node-preview rules | Delete both files |
| `Editor\GeneralTab.razor` | Resource Type `Info` text "…icon and short code it draws with in the explorer" | Text only |
| `Editor\TagsTab.razor` | description "…opened from the grid and the explorer" | Text only |

### Modules

| Project / folder | Symbols | Action |
|---|---|---|
| `Common.Server\Explorer\` | `ExplorerService`, `ExplorerSqlRepository`, `DiagramService`, `DiagramSqlRepository` | Delete folder |
| `Common.Server\Explorer\Interfaces\` | `IExplorerService`, `IExplorerRepository`, `IDiagramService`, `IDiagramRepository` | Delete |
| `Common.Server\Explorer\Models\` | `DiagramRow`, `DiagramListRow`, `DiagramUpsertResult` | Delete |
| `Common.Server\Resources\Models\ExplorerNodeRow.cs` | read model for `Resource_GetForExplorer` | Delete |
| `Common.Server\Utils\DependencyRegistration.cs` | four `TryAddScoped` lines for `IExplorerRepository/Service`, `IDiagramRepository/Service` | Remove |
| `Common.Shared\Explorer\` | `ExplorerNodeModel`, `ExplorerNeighborModel`, `ExplorerEdgeModel` | Delete |
| `Common.Shared\Explorer\Diagrams\` | `DiagramModel`, `DiagramListItem`, `SaveDiagramRequest`, `SaveDiagramResponse` | Delete |
| `Common.Shared\ResourceTypes\ResourceTypeIcons.cs` | `KnownKeys`, `ColorFor` — "mirrors ICON_PATHS / TYPE_COLORS in explorer-canvas.js" | Delete (see §2) |

### Database (`Database\MacroPoint.Azure.ResourceExplorer.Db`)

| Item | Detail | Action |
|---|---|---|
| `Tables\Diagram.sql` | `[ResourceExplorer].[Diagram]`, `IX_Diagram_OwnerId` | Drop (publish deletes the table; SSDT will block if `BlockOnPossibleDataLoss` is on) |
| `Stored Procedures\Diagram_Upsert.sql`, `Diagram_GetByShareId.sql`, `Diagram_ListForOwner.sql`, `Diagram_Delete.sql` | | Delete |
| `Stored Procedures\Resource_GetForExplorer.sql` | only caller is `ExplorerSqlRepository.GetForExplorerAsync` | Delete |
| `MacroPoint.Azure.ResourceExplorer.Db.sqlproj` | six `<Build Include>` lines for the above | Remove |
| `Scripts\Script.PostDeployment1.sql` | no graph content | none |
| `Scripts\Live_Purge_Catalog.sql` | `DELETE FROM [ResourceExplorer].[Diagram]` and the `[Diagrams]` count | Remove two lines |
| `Scripts\Demo_Insert_ExplorerGraph.sql`, `Demo_Purge_ExplorerGraph.sql` | seed a *relationship graph* for the canvas; the data (resources + edges + PortalUrl) is still valid for the list view | Keep as demo data, or rename; not graph-UI code |
| `Scripts\05_Seed_ResourceTypeCodes.sql` | "short codes + icon keys for the explorer node badges"; already a no-op against the live vocabulary (type names differ, see `PreReleaseRefactorAnalysis.md` §3.2) | Delete |
| `ResourceType.ShortCode`, `ResourceType.IconKey` columns | **No model change was decided**, so leave the columns; stop writing/showing them (see §2) | Keep columns |

### Tests and tooling

| Item | Detail | Action |
|---|---|---|
| `_Tests\…\Common.Server.Tests\Explorer\ExplorerServiceTests.cs` | mocks `IExplorerRepository` | Delete |
| `_Tests\…\Common.Server.Tests\Explorer\DiagramServiceTests.cs` | mocks `IDiagramRepository` | Delete |
| `_Tests\…\Resources\ResourceServiceResourceTypeTests.cs` | asserts on `ShortCode`/`IconKey` "because … the explorer prints/colours …" (`SaveResourceTypeAsync_*` cases) | Rewrite or drop those assertions with §2's icon cut |
| `tools\e2e\` | no spec, helper or seed row touches `/explore` or `Diagram` (grep of `tools/e2e/**` found only the product name) | none |
| `bruno\` | import collection only | none |
| `docs\plans\explorer\*` | design history | leave; mark superseded in `docs/` index if one exists |

### Shared with code that stays (do **not** delete)

- `ResourceRelationship_GetForResource.sql` + `ResourceSqlRepository.GetRelationshipsForResourceAsync` — the
  editor's Dependencies/Dependent On tabs, `ResourceService.ReconcileRelationshipsAsync` and
  `ResourceService.ProjectResourceDetailAsync` all read it. It is also the natural seed for the new
  Depends On / Used By list.
- `Resource.PrimaryTagDefinitionId` and the `PortalUrl` tag: `Resource_GetForExplorer` joins them for
  `PrimaryUrl`, but the grid's links menu (`Home.razor` actions column) and `ResourceService.ProjectResourceDetailAsync`
  (`PrimaryLinkUrl`) also use them.
- `ICurrentIdentity` / `ConfiguredIdentity`: the explorer passes `OwnerId` to `IDiagramService`, but
  Saved Views and Client Settings also take it. Goes only if §2's Saved Views cut goes.
- Demo/live seed scripts reference "the explorer" in comments only; their data is relationship data.

---

## 2. Further Cut Candidates

Recommendation key: **Cut** = delete now, **Hide** = keep code, remove from the default path (menu/tab/
disclosure), **Keep** = needed for the proving use case or quick entry. Tagged Beta/Post-Beta for when the
action happens.

_Widen the terminal if this renders as stacked Key: value records._

| Feature | Where (file : symbol) | Cost to keep | Recommendation |
|---|---|---|---|
| Saved Views | `SavedView.sql`, 5 `SavedView_*` sprocs, `SavedViewOrderList` UDT, `SavedViewService`, `SavedViewSqlRepository`, `SavedViewValidation`, `GridViewContext`, `SavedViewsMenu.razor`, `SaveViewDialog.razor`, `Pages\SavedViews.razor`, `SavedViewsController`, `SavedViewServiceTests`, `saved-views.spec.js`, half of `Home.razor` (`TryRestoreDefaultViewAsync`, `ApplySavedViewAsync`, `_hasDefaultView`) | High — `Home.razor` carries three competing start-up paths (URL, starred view, resume setting) and a layout↔page event seam just for this | **Hide (Beta) → Cut or finish (Post-Beta)**. Nobody in the proving use case saves a view; the migration user types a name. Remove the menu entry and the restore branch from `OnAfterRenderAsync`; leave the rest in place. |
| Per-client resume | `ClientSetting.sql`, `ClientSetting_Get/Upsert`, `ClientSettingsService/SqlRepository`, `Home.razor : PersistViewAsync`, `GridViewSettingKey` | Medium — a write on every grid navigation (`UpdateUrl`), gated by two flags | **Cut (Beta)**. With one configured owner it is "remember the last filter for everyone"; a bare URL should show the whole catalog. |
| Configured identity | `ConfiguredIdentity`, `ICurrentIdentity`, `ConfiguredIdentityTests` | Low on its own | **Cut with Saved Views**; only they and the diagrams consume `OwnerId`. |
| Icon keys / short codes | `ResourceTypeIcons`, `ResourceTypeDialog.razor` preview section, `ResourceTypes.razor` Node/Icon Key columns, `05_Seed_ResourceTypeCodes.sql`, `ResourceType_Save` parameters | Medium — a vocabulary whose only renderer is the cut canvas; `Live_Seed_Vocabulary.sql` already says "IconKey is deliberately left NULL" | **Cut (Beta)** from the UI and seeds; leave the two columns (no model change). |
| `UxLab.razor` | `/uxlab`, header says "THROWAWAY DESIGN LAB — not production" | Low, but it ships | **Cut (Beta)**. |
| Sample service | `Sample\SampleService`, `SampleSqlRepository`, `ISampleService`, `IQueryRepository`, `SampleGetDateTimeContract`, `Resource_GetDaysAgo.sql`, `PlaceholderStoredProcedure.sql` | Low | **Cut (Beta)**. Not registered in `DependencyRegistration`; only a commented-out sproc call. |
| Collections library | `Libraries\Collections\*` (`PropertyBag`, `Property`, `CollectionExtensions`) | Low | **Cut (Beta)**. No project references it (`Common.Server.csproj`, `UI.Web.csproj` checked); only the `.slnx` lists it. |
| Logging extensions | `Libraries\Logging\*` | Low | **Keep** — referenced by `Common.Server.csproj`; no finding. |
| Review tab | `Editor\ReviewTab.razor`, `ResourceEditor.TabNames` | Medium — duplicates every field, owns `PrimaryLinkTooltip` shared with `TagsTab` | **Hide (Beta)**. In View mode the General+Tags panes already *are* the review. Move the constant. |
| Wizard footer (Previous/Next : name) | `ResourceEditor.razor : GoPreviousTab/GoNextTab/NextTabLabel` | Low | **Cut (Beta)**. Tabs are clickable; two extra buttons in the footer push Save right. |
| "Dependent On" authoring tab | `ResourceEditor.razor` second `DependencyTab` (`Direction="DependentOn"`), `ReconcileRelationshipsAsync` far-side writes | Medium — editing another resource's out-edge from this page is the design note RD7 itself flags | **Hide (Beta)**: show Used By read-only on the resource view; author edges from the dependent's side only. Keep the server path for import. |
| Nested create + nav stack | `EditorNavStack`, `EditorFrame`, `ReturnedChild`, `ResourceEditor : InitiateNestedCreate/RestoreFromFrame/CommitChildAndReturn`, `EditorNavStackTests`, `nested-create.spec.js` | High — ~200 lines of frame/history bookkeeping and the `?n=` marker | **Hide (Beta)**: keep "Create new…" from the picker (it is quick entry's friend) but it is a modal quick-add in the mockup, not a navigation. Delete the stack if the modal lands. |
| Add-filter / Active-filters chips | `Home\AddFilterMenu`, `ActiveFiltersMenu`, `FilterChip`, `EnumerableFilterEditor`, `TextFilterEditor`, `ResourceFilterState`, `Resource_GetFilterValues`, `ResourceFilterList` UDT | High to delete (URL round-trip, facets, tests) | **Hide (Beta)** behind an "Advanced" toggle; default path is the one search box. Keep the sproc. |
| Display count select | `ResourceFilterBar.razor : OnDisplayCountChanged`, `AllowedTakes` | Low | **Hide** under Advanced. |
| "Copy details link" row action | `Home.razor : CopyDetailsLink` | Low | **Keep** — cheap and useful during cutover handoffs. |
| Links menu in the grid | `Home.razor` actions column `MudMenu Icon=Link` | Low | **Keep**; collapse to the primary link icon only. |
| Domain value admin | `Pages\Domains.razor`, 5 `DomainValue_*` sprocs, rename/reassign dialogs | Medium | **Keep (Beta)**, but see defect H1 — it is undone by every publish. |
| Tags admin | `Pages\Tags.razor`, `TagDefinition_*` | Medium | **Keep (Beta)**: creating `AzureResourceId` needs it once; thereafter rarely. Move to the overflow menu. |
| Resource Types admin | `Pages\ResourceTypes.razor`, `ResourceTypeDialog` entry-point template editor | Medium | **Keep (Beta)**; the template is what pre-seeds the quick-add form. |
| Import / Export | `Pages\Import.razor`, `ImportService`, `ExportService`, `ResourceImportController`, 3 `Export_*` sprocs, `bruno\` | Medium | **Keep (Beta)**: bulk-loading the new ARM ids is realistically an import. |
| Reassign-and-delete cascades | `ConfirmCascadeDialog`, `DomainValue_ReassignAndDelete`, `ResourceType_ReassignAndDelete`, `TagDefinition_ForceDelete` | Medium | **Hide (Post-Beta)** — admin edge cases; keep sprocs. |
| REST controllers | `SavedViewsController`, `ResourceImportController` | Low | Cut the first with Saved Views; keep the second. |

---

## 3. Common-Path Friction (Beta)

Walk of the proving use case through the Razor as it is. Click counts assume the app is open on `/`.

### Find PingByPhone → Azure id → impact

| # | Step today | File : symbol | Friction | Proposal |
|---|---|---|---|---|
| 1 | Type "PingByPhone" in "Filter for any field…" | `ResourceFilterBar.razor` `MudTextField` | Fine; debounced 300 ms. The bar also shows Add filter, Active filters, "Showing N of M", Display count — five controls for one job | Keep only the search box in the default view; move the rest under **Advanced** |
| 2 | Grid row: Name, Type, Description, Tags, 4 actions | `Home.razor` columns | The Tags cell is a junk drawer (already noted in `PreReleaseRefactorAnalysis.md`); an ARM id as a tag value would render as a 150-char `key: value` string per row | Add an **Azure Id** column rendering the `AzureResourceId` values as short chips (name segment, old/new badge); drop the Tags cell from the default view |
| 3 | Click the pencil "Open details" | `Home.razor : OpenDetails` → `/resources/{uid}` | Lands on the **General** tab in View mode: Identity, Key, Name, Description. Azure id is not here | Make the row name a link; land on a read-only **Resource view** whose first block is the Azure ids |
| 4 | Click **Tags** tab | `ResourceEditor.razor` `MudTabPanel Text="Tags"` | The `AzureResourceId` values are rendered as `MudChip`s with copy, inside the editing component (`TagValueEditor`, `IsMultiValued` branch), mixed with every other tag | Azure ids get their own section at the top; other tags collapsed under "More Tags" |
| 5 | Click **Dependent On** tab (4th) | `DependencyTab.razor` with `Direction="DependentOn"` | Rows are plain `<div>`s (`rm-dep-name`), **not links**; one hop only (`ResourceRelationship_GetForResource` is non-recursive) | Replace with **Used By** list on the resource view; rows link to the resource and carry an expand chevron that loads that row's dependents in place |
| 6 | To go deeper: Home › search the dependent › pencil › Dependent On | 4 more clicks per hop, plus re-typing | Transitive impact for a 3-level tree is ~12 clicks and 3 searches | The expand-in-place tree above collapses this to one click per hop, no navigation |
| 7 | Footer "Previous : …" / "Next : …" buttons | `ResourceEditor.razor` `rm-shell-foot` | Noise in View mode | Remove in View mode (or entirely, §2) |

### Reverse: start from an ARM id / region

| # | Step today | Friction | Proposal |
|---|---|---|---|
| 1 | Paste the ARM id into the search box | `Resource_GetItems` matches `rt2.TagValue LIKE @SafeSearchForParam` — **works** once an `AzureResourceId` tag exists. The grid gives no visual confirmation of *which* tag matched | Highlight the matched Azure id chip (the sproc already computes per-tag `Priority`; surface it) |
| 2 | Paste a region/RG fragment (`eastus2`, `usspi-rg-…`) | Same path; works as substring. `@SafeSearchFor NVARCHAR(255)` is assigned after wrapping and escaping, so a long id with underscores can exceed 255 and be silently truncated (see H5) | Widen the variable or search a dedicated `AzureResourceId` predicate |

### Quick entry (create)

| # | Step today | File : symbol | Friction | Proposal |
|---|---|---|---|---|
| 1 | Hamburger → Create Resource | `MainLayout.razor` menu | Two clicks to start; the primary action is behind a menu | **+ Add Resource** button in the header / grid toolbar |
| 2 | General tab: **Tier** select first (required, with helper "Pick the boundary first — it limits which resources this one can depend on") | `GeneralTab.razor` Domain `FormRow` | A migration user does not think in tiers; the pick gates the dependency picker (`ResourcePicker` shows "Choose a Tier on the General tab first") | Default to the configured default domain value; show the select under **More Options** |
| 3 | Resource Type select | `GeneralTab.razor` | Fine. Changing it drops pre-seeded tag rows (`ResourceEditor.OnResourceTypeChanged`) | Keep; put it first |
| 4 | Key (auto-slugged from Name) + Identity preview row + uniqueness check on blur | `GeneralTab.razor : OnNameChanged/Slugify`, `CheckUniquenessAsync` | Three rows of identity plumbing before the user has typed a name | Hide Key and Identity under More Options; keep the uniqueness check running on Name/Type |
| 5 | Name, Description | | Fine | Name first on the form |
| 6 | Tags tab: pre-seeded rows per type template + required tags, "Add existing tag…" autocomplete, "Create new tag…", primary-link radio | `TagsTab.razor`, `ResourceEditor.OnResourceTypeChanged` | Azure id is one row among many; the primary radio column is a concept the user has to learn | Azure Id is a first-class field on the quick form; the type template's remaining tags appear under **Key Tags** (first two) and **More Tags** |
| 7 | Dependencies tab: picker + "Create new…" (navigates to a nested editor via `EditorNavStack`) | `DependencyTab.razor`, `ResourcePicker.razor` | Separate tab; nested create leaves the form | Picker inline on the quick form; "Create new…" opens the quick form **as a dialog** |
| 8 | Save in footer; then redirected to `/resources/{uid}` in View mode | `ResourceEditor.SaveAsync` | Fine | Save returns to the resource view |

Minimum today: 2 (menu) + 1 (Tier) + 1 (Type) + 1 (Name) + 1 (Tags tab) + 1..n (values) + 1 (Dependencies
tab) + 1..n (picks) + 1 (Save) = **9 clicks + typing across 3 tabs**. Proposed: Add → Name → Type → Azure id
→ Depends on → Save = **6 on one form**.

---

## 4. Hardening Defects (surviving code only)

Only correctness, swallowed errors, invariants and transactions. Severity is for Beta.

### H1 — Publishing wipes user-created domain values (Beta, **High**)

`Script.PostDeployment1.sql` MERGEs the `Domain` tag definition and, `WHEN MATCHED`, sets
`AllowedValues = '["prod","non-prod","shared"]'` unconditionally. `DomainValue_Add` (used by
`GeneralTab.OpenCreateDomainValueDialog` → `ResourceService.CreateDomainValueAsync` and by `Domains.razor`)
appends to that same column. Every `dotnet publish`/SqlPackage run therefore deletes every Tier/Subscription a
user created. Resources carrying the removed value survive and show as `IsUnlisted = 1` in
`DomainValue_GetAllWithUsage`, but the editor's Tier select (`GeneralTab` `DomainAllowedValues`) no longer offers
it, so new dependencies on them cannot be created same-domain. `Live_Seed_Vocabulary.sql` has the same MERGE
(manual script, lower risk). **Fix shape:** seed `AllowedValues` only `WHEN NOT MATCHED`, or seed it once and
let `DomainValue_Add` own it.

### H2 — Domain is not required server-side; uniqueness is bypassable (Beta, **High**)

`ResourceService.SaveResourceAsync` validates `ResourceTypeId`, `ResourceUid`, `ResourceName`, `ResourceKey` and
nothing else; the comment in `ValidateTagsForSave` ("its requiredness is enforced via request.Domain") describes
a check that does not exist. Only `EditorValidation.ValidateGeneral` (client) requires it. With `Domain = null`:

- `Resource_CheckUnique` takes the domain branch (a domain tag is defined) and tests `rt.TagValue = @Domain`,
  which is never true for `NULL` → always "unique";
- `Resource_Save` inserts the row and skips the domain-tag insert (`@Domain IS NOT NULL` guard);
- the resource now has no domain tag; `Resource_GetByResourceUid` returns `Domain = NULL`; the editor shows a
  blank, disabled Tier select (`Model.IsPersisted` disables it) with no way to repair it; and any number of
  (Type + Key) duplicates can be created the same way.

The import path is protected (`ImportService.ValidateReferences` checks `domain.Required`); the editor API path
and any future quick-add dialog are not. **Fix shape:** require `request.Domain` in `SaveResourceAsync` when a
domain tag exists and is `RequirementLevel = 'Error'`; make `Resource_CheckUnique` treat `NULL` as a value
(`ISNULL`-style match) so the fallback branch cannot be reached by accident.

### H3 — Save is three writes with no transaction (Beta, **Medium**)

`ResourceService.SaveResourceAsync` calls `Resource_Save`, then `ResourceTag_SetForResource`, then N×
`ResourceRelationship_Add`/`_Remove` via `ReconcileRelationshipsAsync`. Each sproc opens its own connection
through `IDbConnector`; `DbConnector`/`DbExecute` expose no ambient transaction (the code comments RD4/RD8 say
"non-atomic" and accept it). A failure after `Resource_Save` in Create mode leaves a resource with no tags and
no edges; the UI shows "Save failed" and stays on the form, and because `Resource_Save` is keyed by
`ResourceUid` a retry upgrades to an update, so recovery is possible but invisible. For Beta the cheap fix is a
single sproc (`Resource_SaveWithTagsAndEdges`) taking the tag TVP and two uid TVPs inside one `BEGIN TRAN`;
`ResourceTag_SetForResource` and `Resource_Delete` already show the pattern.

### H4 — RO connection string validated but never used (Beta, **Medium**)

`Program.cs` fails fast unless both `…ConnectionStrings:Database:RO` and `:RW` exist; `GlobalConfig.ConnectionStringsConfig`
reads both; `DependencyRegistration.RegisterCommonDependencies` then constructs
`new DbConnector(cfg.ConnectionStrings.DatabaseReadWrite, cfg.ConnectionStrings.DatabaseReadWrite)`. Every
`_db.RO.SprocCommand(...)` in the repositories runs on the RW string. Harmless on localdb; a deployment that
points RO at a replica will silently not use it. Pass `DatabaseReadOnly` as the first argument or drop the RO
key and the start-up check.

### H5 — Search pattern silently truncates after escaping (Beta, **Low**)

`Resource_GetItems` and `Resource_SearchForPicker` build `@SafeSearchFor NVARCHAR(255)` as
`'%' + REPLACE(…four escapes…) + '%'`. The service caps the raw term at 255 (`ResourceService.MaxSearchLength`),
but each `_`, `[`, `]`, `%` expands to three characters and the wrapper adds two, so a long value is cut mid-pattern
with no error and no match. ARM ids with underscores in resource-group or resource names are the realistic case
once `AzureResourceId` is searched. Declare the variable `NVARCHAR(MAX)` (both sprocs) or shorten the cap.

### H6 — `Home.OnLocationChanged` is `async void` (Beta, **Low**)

`Home.razor : OnLocationChanged` is `async void` and awaits `_grid.ReloadServerData()`. An exception there is
unobserved and tears down the Blazor Server circuit instead of surfacing `_loadError` the way `LoadGridData`'s
catch does. Wrap the body in `try/catch` and route to `_loadError`, as `LoadGridData` already does.

### H7 — Tag-key match in free-text search (Beta, **Low**, becomes visible with the new tag)

`Resource_GetItems` search predicate also matches `td2.TagDefinitionKey LIKE @SafeSearchForParam`. Searching
"resource" or "azure" will return every resource that merely *has* an `AzureResourceId` tag. Drop the key
predicate from the free-text search (filters still key on `TagDefinitionKey`).

### Not defects (checked and fine)

- `Resource_Delete` and `ResourceTag_SetForResource` are transactional.
- `ResourceRelationship_Add` is idempotent; the `UK_ResourceRelationship_From_To` and no-self-loop constraints hold.
- Swallowed exceptions in `MainLayout.OnInitializedAsync`, `Home.OnInitializedAsync` (tag keys), `PersistViewAsync`
  and `ResourceEditor.CheckUniquenessAsync` all degrade to a documented fallback and are acceptable.
- `TagDefinition_Upsert` refuses system tags; `AddAllowedValueAsync` and `UpdateTagDefinitionAsync` pre-check
  `IsSystemTag`/`IsDomainTag` so the domain definition cannot be reshaped from the UI.

---

## 5. Gaps for the Use Case (needs, not designs)

Tagged Beta unless noted.

1. **An `AzureResourceId` tag definition does not exist.** `Live_Seed_Vocabulary.sql` seeds `Subscription`,
   `ResourceGroup`, `Environment`, `System`, link tags and selectors; `Demo_Insert_TagVocabulary.sql` and the e2e
   seed define none either. Need: a seeded, system-managed, multi-valued Text definition with a stable key, in
   `Script.PostDeployment1.sql` so it exists on every database, and added to every Azure type's entry-point
   template (`ResourceTypeTag`) so quick entry pre-seeds it.
2. **Old vs new status has nowhere to live.** The tag value is a string; "old `usspi*`" vs "new" must be derivable
   (prefix/subscription match against a configured list of legacy subscriptions or name prefixes) or carried
   as a sibling tag. Need: a rule the resource view can apply per value, with the legacy markers configurable.
3. **Transitive impact.** `ResourceRelationship_GetForResource` is one hop; nothing recursive exists in sprocs or
   services. Need: a read that returns the dependents (and dependencies) closure from a resource with depth
   and path, cycle-safe (the demo seed deliberately contains a cycle: checkout → pricing → inventory → checkout),
   plus a UI that expands hop by hop rather than dumping the closure.
4. **Cross-tier edges cannot be authored.** `ResourceService.ValidateAndResolveRelationshipsAsync` and
   `ImportService.ValidateAndResolveDependencies` reject any edge whose ends differ in domain;
   `Live_Seed_PostImport.sql` exists to hand-write exactly the edges the impact question needs
   (`vnext` → `usspi-aps-pd-…`). Need: a decision whether the same-domain rule survives Beta; if impact must
   cross tiers, the rule must relax (code only — no model change) or the quick form must fail loudly with a
   reason.
5. **Impact lives behind authoring.** The only dependents list is the editor's Dependent On tab and
   `ConfirmDeleteDialog.Dependents`. Need: a read-only resource view route (or View mode of the editor
   restructured) whose dependents rows are links.
6. **Searching by Azure id gives no confirmation of the match.** The grid has no Azure id column and the
   matched tag is not highlighted. Need: the id visible in the result row, and the matching value distinguishable.
7. **Search by region.** Region is not a segment of an ARM id (it lives on the resource, not in the id) and no
   seeded tag carries it. Need: either a `Region` tag in the same template or acceptance that region search is
   really resource-group/name-prefix search (`usspi-…-eus2-…`). Say which in the Beta definition.
8. **Default domain for quick entry.** Nothing configures a default Tier; `BuildCreateEditorModel` leaves it
   blank and the editor forces a pick. Need: a configured default domain value so the quick form can hide it.
9. **Import carries no primary link and no `AzureResourceId` yet.** `Resource_Upsert`/`ImportService` never set
   `PrimaryTagDefinitionId` (`Live_Seed_PostImport.sql` (c) patches it). Need: import to populate the new tag like
   any other multi-valued tag (already supported if the definition is `isMultiValued: true`) and to set the
   primary from the template.
10. **Post-Beta:** typed relationships, Azure tag import, drift detection against ARM, per-environment links —
    all from `PreReleaseRefactorAnalysis.md` §4, deliberately out of scope.

---

## 6. HTML Mockups

Static, self-contained, MudBlazor-like, in `__ProjectNotes\mockups\`. Each starts with a "What Changed vs Today"
note naming the Razor page it replaces.

- [`mockups/home.html`](mockups/home.html) — simplified home: one search box matching names and Azure ids,
  Azure Id column, Advanced disclosure, + Add Resource. Replaces `Home.razor` + `ResourceFilterBar.razor`.
- [`mockups/resource.html`](mockups/resource.html) — resource view: Azure ids with Old/New badges, Depends On /
  Used By with transitive expand, other tags collapsed. Replaces `ResourceEditor.razor` View mode (General, Tags,
  Dependencies, Dependent On, Review tabs).
- [`mockups/quick-add.html`](mockups/quick-add.html) — quick entry form with More Options disclosure to the full
  editor fields. Replaces `ResourceEditor.razor` Create mode (`GeneralTab`, `TagsTab`, `DependencyTab`).

---

## 7. Verified vs Inferred

**Read in full:** `Program.cs`, `MainLayout.razor`, `Home.razor`, `ResourceFilterBar.razor`, `AddFilterMenu.razor`
(top), `PageHeader.razor`, `ResourceEditor.razor`, `GeneralTab.razor`, `TagsTab.razor`, `TagValueEditor.razor`,
`DependencyTab.razor`, `ResourcePicker.razor`, `ReviewTab.razor`, `EditorValidation.cs`, `ResourceExplorer.razor`,
`Import.razor`, `UxLab.razor` (header), `ResourceTypes.razor`/`Tags.razor`/`Domains.razor`/`SavedViewsMenu.razor`
(headers), `SavedViewsController.cs` (header), `App.razor`, `DependencyRegistration.cs`, `GlobalConfig.cs`,
`ConfiguredIdentity.cs`, `ResourceService.cs` (whole), `ResourceSqlRepository.cs`, `ImportService.cs`,
`ExportService.cs` (header), `ExplorerService.cs`, `ExplorerSqlRepository.cs`, `IDiagramService.cs`,
`IDiagramRepository.cs`, `ClientSettingsService.cs` (header), `TagRowEditor.cs`, `ResourceEditorModel.cs`,
`ResourceFilterModels.cs`, `ResourceTypeIcons.cs`, the `.slnx`, `Common.Server.csproj`, the `.sqlproj`,
`Script.PostDeployment1.sql`, `Live_Seed_Vocabulary.sql` (to the tag MERGE), `Live_Seed_PostImport.sql` (header),
`Demo_Insert_ExplorerGraph.sql` (header), and these sprocs/tables: `Resource_GetItems`, `Resource_Save`,
`Resource_Upsert`, `Resource_CheckUnique`, `Resource_GetByResourceUid`, `Resource_SearchForPicker`,
`Resource_GetForExplorer`, `Resource_Delete`, `ResourceTag_SetForResource`, `ResourceRelationship_Add`,
`ResourceRelationship_GetForResource`, `DomainValue_Add`, `DomainValue_GetAllWithUsage`, `Resource.sql`,
`ResourceTag.sql`, `ResourceRelationship.sql`, `TagDefinition.sql`, `Diagram.sql`, `tools\e2e\sql\seed.sql`,
`tools\e2e\sql\cleanup.sql`. Repo-wide greps backed the inventory (`Diagram`, `IExplorer…`, `/explore`,
`cytoscape`, `rm-explorer`, `IconKey|ShortCode`, `ISampleService|Collections|Logging.Extensions`).

**Inferred, not verified:**
- That `DbConnector`/`DbExecute` offer no ambient transaction (H3) — concluded from the repository call sites,
  which open a command per sproc; the `SqlClient.Extensions` library itself was not read.
- The click counts in §3 are from reading the markup, not from driving the app (the brief forbade running it).
- The e2e specs (`delete`, `dependencies`, `dirty-guard`, `nested-create`, `saved-views`) were not opened; the
  "no explorer coverage" claim rests on grep.
- `Live_Seed_Vocabulary.sql` below the tag MERGE (entry-point templates) and `Demo_Insert_TagVocabulary.sql` were
  not read; the "no `AzureResourceId` definition" claim rests on the tag MERGE list and a repo-wide grep for
  `AzureResourceId|ArmId|ResourceIdTag`, which matched only this document.
- ARM ids are assumed not to carry region (true of the standard
  `/subscriptions/…/resourceGroups/…/providers/…` shape; location is a property, not an id segment).
