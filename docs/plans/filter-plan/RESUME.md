# Filter UI — Resume State (as of 2026-07-09)

Branch: **home-page**. Plan: `docs/plans/filter-plan/filter-plan.md` (revised after adversarial review — read its changelog).

## Status: implementation COMPLETE, builds clean, unit tests green. In interactive UI-polish/verification.

### Done
- **Contract** `Modules/Common/ResourceMapper.Common.Shared/HomePage/Contracts/ResourceGridContract.cs` — reworked `ResourceGridFilterDefinition` (Column/TagKey/Kind/Operator/Values/IncludeBlank/Text) + enums + facet request/response types.
- **Client state + URL** `Modules/Common/.../HomePage/Filtering/ResourceFilterModels.cs` — `ActiveFilter`, `ResourceFilterState`, `ToGridRequest/ChipLabel/AllCheckboxState/IsConstraining/ToQueryString/FromQueryString`. Deterministic serialization.
- **DB** (canonical project `Database/HTResourceMapperDb/`): `User Defined Types/ResourceFilterList.sql` (TVP), extended `Stored Procedures/Resource_GetItems.sql` (8 params: +@TagLimit, +@Filters TVP; constant whitelisted WHERE blocks; @TagLimit cap in SQL via TagRank), new `Stored Procedures/Resource_GetFilterValues.sql` (facet, respects search + other filters, per-tag-key). Both added to `HTResourceMapperDb.sqlproj` `<Build Include>`. **dacpac builds** (full MSBuild — `dotnet` CLI can't; use `"C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe"`).
- **Server** `ResourceService.cs` (forwards Filters — no longer dropped; §5b validation guards; `GetResourceGridFacet`; `GetTagFilterKeys`), `IResourceService.cs`, `IResourceRepository.cs`, `ResourceSqlRepository.cs` (`BuildFilterTable`, TVP wiring, `GetFilterValuesAsync`, `GetAllTagKeysAsync`, tag-trim now in SQL), new model `Resources/Models/ResourceGridFacetItem.cs`.
- **UI** `UI/ResourceMapper.UI.Web/Components/Home/` — `ResourceFilterBar`, `FilterChip`, `EnumerableFilterEditor`, `TextFilterEditor`, `AddFilterMenu`. `Pages/Home.razor` = ServerData top-N grid + URL sync (NavigationManager, LocationChanged for Back/Forward). CSS in `wwwroot/app.css` (`rm-chip*`, `rm-filter*`, `rm-facet-count`). `_Imports.razor` updated.
- **Tests**: `_Tests/Common/Shared/ResourceMapper.Common.Shared.Tests` (65 pass), `_Tests/Common/Server/.../Resources/ResourceServiceTests.cs` (17 new, 37 total pass).

### UI polish applied (all built clean; need clean restart to view)
- Add filter menu uses MudMenu built-in activator (custom-activator didn't open).
- Search field `Clearable="true"` (verified renders as `mud-input-clear`).
- Display-count + "Showing X of Y" wrapped in `ml-auto` right group (flush right).
- "All" checkbox in enumerable editor is scoped to the value-search-**filtered** subset (Azure behavior).

## CRITICAL environment facts
- **App DB connection (user secrets id `1a3b74ec-d315-416c-9c29-a2781962918d`): `(localdb)\MSSQLLocalDB` database `ResourceMapper`** (both RO/RW). This is the DB the running app reads.
- The feature SQL (TVP + both sprocs) was applied **directly** to that localdb via sqlcmd and verified: `Resource_GetItems` with an empty `@Filters` returns **all 21** resources (43 tag rows, capped 5/resource). Default = all ✅.
- `localhost.publish.xml` was repointed to `Data Source=(localdb)\MSSQLLocalDB` / `ResourceMapper` (was `Data Source=.`). Publishing to `.` earlier is why changes "didn't take" — wrong server.
- **"Changes not visible after rerun" was stale build/cache, NOT a code bug** — verified fresh run shows them. Clean restart procedure: fully stop the app (Ctrl+C / Stop debugging — Hot Reload won't apply structural .razor edits), rebuild, hard-refresh browser (Ctrl+F5). Run project = `UI/ResourceMapper.UI.Web`.
- SQL MCP here is allow-listed only to unrelated `pingbyphone`; use `sqlcmd -S '(localdb)\MSSQLLocalDB' -d ResourceMapper` for DB checks.

## Outstanding / next steps
1. Clean restart + confirm the 4 UI polish items are visible; grid shows 21 by default.
2. Interactive verification against the now-correct localdb: facet **counts** shift when other filters applied; **sort** re-queries top-N server-side; **display-count** 50/100/500 re-queries; URL push/replace + Back/Forward; two tag-key filters AND ("prod AND owned-by-me"); NotEquals; `(blank)` bucket selectable.
3. Optional SQL §8 exercises via sqlcmd (escape chars, NotEquals-with-NULL survives, `@TotalRecords` == distinct count).
4. "Weird options" in Add-filter dropdown = distinct **tag keys** (by design). User may want to exclude system/internal tag keys from the picker — ask which.
5. **Nothing committed yet** on `home-page`.
