# Home-Page Faceted Filtering — Implementation Plan

> Wireframes/screenshots in `docs/plans/filter-plan/` and `docs/plans/filter-plan/azure-images/`.
> **Revised after adversarial review** — see the changelog at the bottom for what changed and why.

## Context

The home-page resource grid (`Home.razor`) currently loads up to 100 resources with a single
hard-coded request and **no filtering or search wired in** — the contract even carries an unused
`Filters` list that the service silently drops. We want an Azure-portal-style faceted filter bar
above the grid: a free-text search plus up to four column filters, with value+count pick-lists,
so users can slice a **1,000–2,000-entry** catalog and share the exact view via URL.

Covers **UI/UX + server + database**. *Aware of* but does **not implement** saved/named filters and
last-search restore. **No application-menu changes.** URL state sync **is** in scope (foundation for
the future "copy link to this view").

## Locked decisions

- **5 dimensions:** 1 always-present Search box + up to **4** column filters (**AND** across filters).
- **Two filter kinds:**
  - **Enumerable** (`Type`, and **each tag key**): value+count checklist, operator
    **Equals / NotEquals**, multiple selected values **OR** within the filter.
  - **Text** (`Name`, `Description`): a **contains** box, no operator.
- **Tags are per-key (Azure model).** Each distinct tag key (`Environment`, `Owner`, …) is its own
  filterable column. Values within one tag-key filter **OR**; separate tag-key filters **AND** (via
  the normal across-filter AND). So `Environment=prod` + `Owner=me` = two chips = *prod AND owned by
  me*. Tag-key filters share the 4-filter cap with Type/Name/Description.
- **All/empty selection = no constraint**; partial selection constrains. An **applied-but-empty**
  enumerable filter is auto-removed (it doesn't squat a slot). Chip labels: `Type: Storage` (inline
  when exactly **1** value), `Type: 3 selected` (**>1**), `Type: not Deprecated` (single NotEquals) /
  `Type: not 2 selected` (>1), `Name contains "slug"`.
- **Facet counts respect the search box AND the other active filters** — never the facet's own
  selection. (Counts always match "what selecting this will return".)
- **Grid = Azure "top-N" model, not paging.** Server returns the top **N** of the filtered set
  (display-count selector **50 / 100 / 500**, default 100); header shows **"Showing 1–N of {Total}"**;
  **no pager** — the user refines filters to narrow. **Sorting is server-side** (re-query top-N).
- **URL query string** reflects search + filters + sort + display-count and restores on load.
  Distinct from saved filters / last-search, which stay out of scope.

## Architecture at a glance

Blazor **Interactive Server** (`App.razor` → `RenderMode.InteractiveServer`) — no WASM, no HTTP hop.
`Home.razor` injects `IResourceService` and calls it in-process, so grid read and facet read are
direct async DI calls.

Flow: `Home.razor` (owns `ResourceFilterState` + search + sort + displayCount)
→ `ResourceFilterBar` / editors (controlled, raise callbacks)
→ `IResourceService.GetResourceGridItems` / `GetResourceGridFacet`
→ `ResourceSqlRepository`
→ sprocs `Resource_GetItems` (extended) / `Resource_GetFilterValues` (new).

---

## 1. Data contract changes

File: `Modules/Common/ResourceMapper.Common.Shared/HomePage/Contracts/ResourceGridContract.cs`

**Replace** the unused single-value `ResourceGridFilterDefinition`. New shape (string-serialized
enums for readable URLs / future saved JSON):

```csharp
[JsonConverter(typeof(JsonStringEnumConverter))] public enum ResourceGridFilterKind { Enumerable, Text }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum ResourceGridFilterOperator { Equals, NotEquals, Contains }

public class ResourceGridFilterDefinition
{
    public string Column { get; set; } = "";          // "ResourceType" | "ResourceName" | "Description" | "Tag"
    public string? TagKey { get; set; }               // set iff Column == "Tag" (the tag key, e.g. "Environment")
    public ResourceGridFilterKind Kind { get; set; }
    public ResourceGridFilterOperator Operator { get; set; } = ResourceGridFilterOperator.Equals;
    public List<string>? Values { get; set; }         // enumerable OR-set (Type values, or a tag key's values)
    public bool IncludeBlank { get; set; }            // true when the "(blank)"/NULL bucket is selected
    public string? Text { get; set; }                 // text (contains) filters
}
```

> **Tag key/value:** a tag filter is identified by `Column="Tag"` + `TagKey`; its `Values` are that
> key's values (never a composed `Key:Value` string — avoids delimiter collisions). `IncludeBlank`
> handles NULL/blank values explicitly (see bug-fix in §2) rather than an unmatchable `NULL = NULL`.

**Add** facet contracts (same file):

```csharp
public class ResourceGridFacetRequest {
    public string Column { get; set; } = "";                 // "ResourceType" | "Tag"
    public string? TagKey { get; set; }                      // required when Column == "Tag"
    public string? SearchFor { get; set; }                   // NOW applied to counts (see §2c)
    public List<ResourceGridFilterDefinition>? Filters { get; set; } // all active filters; server ignores the same facet
}
public class ResourceGridFacetResponse { public string Column { get; set; } = ""; public string? TagKey { get; set; } public List<ResourceGridFacetValue> Values { get; set; } = new(); }
public class ResourceGridFacetValue { public string? Value { get; set; } public string Display { get; set; } = ""; public int Count { get; set; } public bool IsBlank { get; set; } }
```

`ResourceGridRequest` gains nothing new structurally — it already has `Filters`, `SearchFor`,
`OrderBy`, `OrderDirection`, `Skip`, `Take`. The grid uses `Skip=0`, `Take=displayCount`.

---

## 2. Database layer  (canonical project only: `Database/HTResourceMapperDb/`)

### 2a. New TVP — `User Defined Types/ResourceFilterList.sql`
```sql
CREATE TYPE [HTResourceMapper].[ResourceFilterList] AS TABLE (
    [FilterIndex]  TINYINT        NOT NULL,   -- groups multi-value rows of one filter
    [FilterColumn] NVARCHAR(50)   NOT NULL,   -- 'ResourceType' | 'ResourceName' | 'Description' | 'Tag'
    [TagKey]       NVARCHAR(50)   NULL,       -- set iff FilterColumn = 'Tag'
    [Operator]     NVARCHAR(20)   NOT NULL,   -- 'Equals' | 'NotEquals' | 'Contains'
    [FilterValue]  NVARCHAR(2100) NULL,       -- enumerable/tag value, or contains-text
    [IsBlank]      BIT            NOT NULL DEFAULT 0  -- row targets the NULL/blank bucket
);
```
One row per selected value; one row per text filter. **Empty/all selection ⇒ no rows** for that
column (enforced in C#, §3). Mirrors the existing `TagKeyValueList` TVP + `.AddTvp` pattern.

### 2b. Extend `Stored Procedures/Resource_GetItems.sql`
- Add param `@Filters [HTResourceMapper].[ResourceFilterList] READONLY`.
- Init `@whereClause = N'WHERE 1=1'`; change the search block from `WHERE (...)` to `AND (...)`.
  Search/tag-priority/order/`@OrderBy`-whitelist behaviour unchanged **except** `@OrderBy` gains
  `LastUpdatedOn`/relative-time ordering already present; no new identifier concatenation.
- **Push the tag cap into SQL** (bug-fix): add `WHERE TagRank <= @TagLimit` (or `TOP` per partition)
  before the outer select, and add `@TagLimit INT = 5` param — so a 500-row page never returns an
  unbounded denormalized rowset. Repo stops trimming in C#.
- Append **constant, whitelisted** predicate blocks (values bound only via `@Filters`; nothing
  user-supplied concatenated). Each block: `AND ( NOT EXISTS (rows for this filter) OR <predicate> )`
  so absent rows = no-op, AND-across from separate blocks, OR-within from `IN`/`EXISTS`:
  - **ResourceType Equals** → `rt_type.TypeName IN (SELECT FilterValue … WHERE IsBlank = 0)`
    `OR (EXISTS(… IsBlank=1) AND r.ResourceTypeId IS NULL)`.
  - **ResourceType NotEquals** → **`NOT EXISTS`** (NULL-safe; *not* `NOT IN`):
    `AND NOT EXISTS (SELECT 1 FROM @Filters f WHERE f.FilterColumn='ResourceType' AND f.Operator='NotEquals'
    AND ( f.FilterValue = rt_type.TypeName OR (f.IsBlank=1 AND r.ResourceTypeId IS NULL) ))`.
  - **Tag Equals (per-key AND, value OR)** — set-based over arbitrary keys:
    ```sql
    AND NOT EXISTS (                                   -- no required tag-key group is unsatisfied
      SELECT 1 FROM (SELECT DISTINCT TagKey FROM @Filters WHERE FilterColumn='Tag' AND Operator='Equals') g
      WHERE NOT EXISTS (
        SELECT 1 FROM [HTResourceMapper].[ResourceTag] rtf
        JOIN [HTResourceMapper].[TagDefinition] tdf ON rtf.TagDefinitionId = tdf.TagDefinitionId
        JOIN @Filters f ON f.FilterColumn='Tag' AND f.Operator='Equals' AND f.TagKey = g.TagKey
                        AND ( f.FilterValue = rtf.TagValue OR (f.IsBlank=1 AND rtf.TagValue IS NULL) )
        WHERE rtf.ResourceId = r.ResourceId AND tdf.TagDefinitionKey = g.TagKey ))
    ```
  - **Tag NotEquals** → `AND NOT EXISTS (SELECT 1 FROM ResourceTag rtf JOIN TagDefinition tdf … JOIN
    @Filters f ON f.FilterColumn='Tag' AND f.Operator='NotEquals' AND f.TagKey=tdf.TagDefinitionKey
    AND (f.FilterValue = rtf.TagValue OR (f.IsBlank=1 AND rtf.TagValue IS NULL)) WHERE rtf.ResourceId=r.ResourceId)`.
  - **Name / Description Contains** → `LIKE '%'+escaped+'%' ESCAPE ']'`, reusing the `@SafeSearchFor`
    escape expression.
- **Pass `@Filters` through `sp_executesql`** — TVPs require an explicit `READONLY` entry in the
  paramdef string, e.g. `N'@SafeSearchForParam NVARCHAR(255), @SkipParam INT, @TakeParam INT,
  @TagLimitParam INT, @FiltersParam [HTResourceMapper].[ResourceFilterList] READONLY,
  @TotalRecordsParam INT OUTPUT'` (count query) and the same minus the OUTPUT (page query).
  **Consolidate** the search-present/absent branches into a single paramdef each (pass a possibly-NULL
  `@SafeSearchFor`) so the **count and page paramdefs are byte-identical** — the two queries must
  apply the same `@whereClause`, or totals won't match rows. Spell both strings out in code review.

### 2c. New facet sproc — `Stored Procedures/Resource_GetFilterValues.sql`
```sql
CREATE PROCEDURE [HTResourceMapper].[Resource_GetFilterValues]
    @FacetColumn NVARCHAR(50),                               -- 'ResourceType' | 'Tag'
    @FacetTagKey NVARCHAR(50) = NULL,                        -- required when @FacetColumn = 'Tag'
    @SearchFor   NVARCHAR(255) = NULL,                       -- APPLIED to counts (reversed decision)
    @Filters     [HTResourceMapper].[ResourceFilterList] READONLY
```
- Build `@SafeSearchFor` exactly as `Resource_GetItems`; **apply it** so counts match the searched grid.
- Copy `@Filters` minus the facet's own dimension (`FilterColumn <> @FacetColumn`, and for tags
  `NOT (FilterColumn='Tag' AND TagKey=@FacetTagKey)`) into `@OtherFilters`; apply the **same** static
  predicate blocks from §2b against it.
- Branch on `@FacetColumn`:
  - **ResourceType:** `GROUP BY r.ResourceTypeId`, `COUNT(DISTINCT r.ResourceId)`; NULL type →
    `IsBlank=1`, `Value=NULL`.
  - **Tag:** filtered to `tdf.TagDefinitionKey = @FacetTagKey`, `GROUP BY rtf.TagValue`,
    `COUNT(DISTINCT r.ResourceId)`; NULL `TagValue` → `IsBlank=1`, `Value=NULL` (so blank tags are
    both **shown** and **selectable** — the earlier design couldn't match them).
  - `ORDER BY IsBlank ASC, ItemCount DESC, Value ASC` (blank bucket last, not floated to top by count).
- Per-key facets are naturally short and cheap — no TOP-N cap needed at 1–2K.

---

## 3. Server service/repo layer

- **`IResourceService.cs`** — add `GetResourceGridFacet(ResourceGridFacetRequest, CancellationToken)`;
  keep `GetResourceGridItems` signature.
- **`ResourceService.cs`**
  - `GetResourceGridItems`: **stop dropping `Filters`.** Validate/sanitize before the repo call
    (see §5b guards): ≤4 filters, whitelist `Column`, tag filters require `TagKey`, operator valid
    for kind, length guards, **drop empty enumerable `Values` (with `IncludeBlank=false`) and blank
    `Text`** (the "All = no constraint" + auto-remove rule). Clamp `Take` to `{50,100,500}`,
    whitelist `OrderBy`. Pass sanitized filters to a new repo overload.
  - `GetResourceGridFacet`: whitelist `Column ∈ {ResourceType, Tag}`; require `TagKey` for tags;
    strip the same facet dimension; map rows → `ResourceGridFacetValue` with
    `Display = IsBlank ? "(blank)" : Value`.
- **`IResourceRepository.cs`** — grid overload taking the filter list + `orderBy`/`orderDirection`
  (existing overloads delegate with empty filters, per the current `tagLimit` pattern); add
  `GetFilterValuesAsync(column, tagKey, searchFor, filters, ct)`.
- **`ResourceSqlRepository.cs`** — `BuildFilterTable(filters)` → `DataTable`
  (`FilterIndex,FilterColumn,TagKey,Operator,FilterValue,IsBlank`), one row per value / tag value /
  text (mirror `ImportSqlRepository.SetResourceTagsAsync`, incl. `DBNull` where needed);
  `.AddTvp("@Filters","[HTResourceMapper].[ResourceFilterList]", …)` on both calls; **drop the C#
  tag-trim loop** (now `@TagLimit` in SQL). Implement `GetFilterValuesAsync`.
- **New model** `Resources/Models/ResourceGridFacetItem.cs` `{ string? Value; bool IsBlank; int ItemCount; }`.
- Empty TVP: **verified** `.AddTvp` with a zero-row `DataTable` sends an empty table (not NULL) —
  `SetResourceTagsAsync` already does this; predicates no-op on empty. No guard needed.

---

## 4. Client UI layer

Filter bar renders **empty on prerender**; no facet fetch in `OnInitializedAsync` (facets are
user-event driven).

### 4a. Client state — Shared, `HomePage/Filtering/ResourceFilterModels.cs`
Plain POCOs (no MudBlazor types) so they serialize for URL/saved-filter layers:
```csharp
class ActiveFilter { string Column; string? TagKey; ResourceGridFilterKind Kind;
                     ResourceGridFilterOperator Operator; List<string> SelectedValues; bool IncludeBlank; string? Text; }
class ResourceFilterState { string? SearchFor; List<ActiveFilter> Filters; string OrderBy="ResourceName";
                            string OrderDirection="Asc"; int DisplayCount=100; const int MaxFilters=4; }
```
Pure, unit-tested methods (reused by URL + future copy-link): `ToGridRequest()`, `ChipLabel(filter)`,
All-checkbox tri-state resolver, `IsConstraining(filter)`, and the URL parse/format (§5). **All
serialization is deterministic** — filters ordered by column then TagKey, value lists sorted — so the
prerender snapshot key (§5.2) is stable across passes.

### 4b. Components — new folder `UI/ResourceMapper.UI.Web/Components/Home/` (register in `_Imports.razor`)
| Component | Role | MudBlazor |
|---|---|---|
| `ResourceFilterBar` | search box + chip set + add button + result-count/display-count header | `MudTextField`, layout, `MudSelect` (display count) |
| `FilterChip` | one chip: computed label + X, click to edit | `MudChip`/styled button + `MudIconButton` |
| `EnumerableFilterEditor` | operator select, value-search, **All** tri-state + virtualized checkbox list (incl. `(blank)`), Apply/Cancel | `MudPopover`+`MudOverlay`, `MudRadioGroup`, `MudTextField`, `MudCheckBox T="bool?"`, `MudVirtualize`, `MudProgressCircular` |
| `TextFilterEditor` | single text box + Apply/Cancel | `MudPopover`+`MudOverlay`, `MudTextField` |
| `AddFilterMenu` | searchable pick-list: `Name, Type, Description` + **one entry per tag key** (from `TagDefinition_GetAll`) | `MudMenu` + `MudTextField` search + `MudList` |

> Editors use `MudPopover`+`MudOverlay` (not `MudMenu` — form controls close on click in 9.x).
> Verify popover API against MudBlazor 9.5.0.

### 4c. Grid — Azure top-N model
- `MudDataGrid<ResourceGridItemModel>` bound to the server result. Use **`ServerData`** purely as the
  reload hook: it fires on initial load and on **sort** change, handing us the sort column/direction;
  we map to `ResourceGridRequest { Skip=0, Take=DisplayCount, OrderBy, OrderDirection, SearchFor, Filters }`
  and return the server page. **PageSize = DisplayCount, pager hidden** — single window, no multi-page
  navigation, so none of the fiddly `ServerData` pagination UX applies. (This is the minimal use of
  `ServerData` that buys correct **server-side sort** without a pager.)
- Header shows **"Showing 1–{items.Count} of {Total}"** and the **`50/100/500` display-count select**;
  changing it re-queries. When `Total > items.Count`, show a subtle "refine to see more" hint.

### 4d. Flow
- `Home.razor` owns `ResourceFilterState` + `_items` + `_total`; children are controlled, raise
  `EventCallback`s; a `FetchFacets` delegate keeps service access in `Home`.
- **Search:** `MudTextField Immediate DebounceInterval="300"` → update `SearchFor` → reload (`replace` URL).
- **Add filter:** `AddFilterMenu` (Name/Type/Description + tag keys) → append `ActiveFilter`, open its
  editor; disable Add (tooltip "Maximum of 4 filters") at 4.
- **Open enumerable editor → fetch facet:** build `ResourceGridFacetRequest` (Column, TagKey,
  **SearchFor**, all filters except this dimension); spinner; render `value (count)` + `(blank)` if
  present; pre-check from state; **value-search filters the loaded list client-side** (per-key lists
  are short); fetch **only on open**.
- **Apply/Cancel:** editor edits a working copy; Apply commits + reload (**push** URL); an
  applied-but-empty filter is **auto-removed** (no squatting chip); Cancel discards (and removes a
  never-applied new filter).
- **Remove chip / change sort / change display-count:** reload (push for structural, replace for sort).
- **Reload:** cancel any in-flight reload via a per-reload `CancellationTokenSource`; set grid `Loading`.
- **Chip label** (pure fn): text → `Name contains "x"`; enumerable unconstrained → hidden/`Type: any`;
  **exactly 1 value → inline** (`Type: Storage`); **>1 → `Type: N selected`**; NotEquals → `Type: not …`
  (single) / `Type: not N selected` (>1).

### 4e. Accessibility / edge states
`aria-label`s on search/chips/remove/add/display-count; focus first control on popover open; Esc=Cancel,
Enter=Apply; empty facet → "No values"; virtualized list in a fixed-height scroll box; empty grid after
a text filter → "No matches — adjust filters".

### 4f. Home.razor changes
`@inject NavigationManager Navigation`; add `_filterState`/`_total`; render `<ResourceFilterBar/>` above
the grid (space cleared by the removed heading); grid uses `ServerData` (§4c); parse URL on init (§5.2)
keeping the state-keyed prerender snapshot.

---

## 5. URL query-string state sync

### 5.1 Scheme — repeated `f` params, tilde-delimited
```
?q=<search>&s=<col>:<dir>&n=<50|100|500>&f=<col>~<op>~<payload>[&f=…]   (≤4 f params)
```
- `q` search; `s` sort (`col:asc|desc`); `n` display-count; `f` one per constraining filter.
- `f` shape: `column~operator~payload`. `column` = `Type|Name|Description|tag:<key>`; kind derived
  from column. `operator` = `eq|ne|ct`. `payload`: enumerable = comma-joined OR-set (with a literal
  `~blank` token for the blank bucket); text = the string.
- **Encoding:** `Uri.EscapeDataString` each *atom* (search, each value, tag key) **before** joining
  with literal delimiters; decode per-atom after splitting. Deterministic order (filters by column
  then key; values sorted).
- Example: `?q=api&n=100&s=ResourceName:asc&f=Type~eq~Compute,Storage&f=tag%3AEnvironment~eq~prod&f=tag%3AOwner~ne~ana%40x.com`

### 5.2 Read path + prerender reconciliation
`ResourceFilterState.FromQueryString(uri)` (pure). In `Home.OnInitializedAsync`, parse `Navigation.Uri`
→ `_filterState` **before** the snapshot, and key the `PersistentComponentState` snapshot on
**`"grid:" + _filterState.ToQueryString()`** (deterministic, §4a). Prerender queries the *filtered/
sorted/top-N* request and persists under that key; interactive pass rebuilds the byte-identical key and
reuses it (no double query). Empty state → key `grid:` = today's behaviour; a filtered URL can never
reuse the default snapshot.

### 5.3 Write path (no reload)
`ResourceFilterState.ToQueryString()` (pure). In `ReloadGridAsync(bool pushHistory=false)` call
`Navigation.NavigateTo(target, forceLoad:false, replace:!pushHistory)`. Search/sort/display-count →
`replace`; add/remove/Apply → push. **Self-echo guard:** subscribe to `LocationChanged` for Back/Forward,
but suppress reload when the incoming normalized query equals the last query *we* wrote (store it) —
prevents parse→navigate→parse loops.

### 5.4 Robustness (inside `FromQueryString`)
Skip unknown/mismatched `col`/`op`; unknown tag key dropped; keep first 4 valid `f`; dedupe by
column+key; drop empty payloads; clamp `n` to `{50,100,500}`; validate `s` column against the
`@OrderBy` whitelist; never throw — garbage URL → empty/default state.

## 5b. Security & server-side input validation  (mandatory)

**Threat model:** every filter value, tag key, search string, sort column, and display-count arrives
untrusted (filter bar *or* hand-crafted URL). Two lines of defense:

**A. Structural — no injection surface.** Filter **values, tag keys, and text** ride **only** as TVP
rows / bound `sp_executesql` params — never concatenated. Predicate blocks (§2b/§2c) are **constant**
text comparing to literals. The only concatenated identifier remains `@OrderBy`, already strict
whitelist-validated (`Resource_GetItems.sql:96`) — and the new sort path routes through that same
whitelist. `Contains` reuses `ESCAPE ']'` so wildcard metacharacters match literally.

**B. Server-side guards in `ResourceService` (before repo call), via `ServiceResponseBuilder.Validation`:**
- **Caps:** `Filters.Count ≤ 4`; per-filter selected values ≤ 500; total TVP rows ≤ a cap;
  `Take ∈ {50,100,500}`.
- **Whitelists:** `Column ∈ {ResourceType, ResourceName, Description, Tag}` (facet `{ResourceType, Tag}`);
  tag filters require a `TagKey` that exists in `TagDefinition`; `Operator` valid for `Kind`
  (enumerable→Equals/NotEquals, text→Contains); `OrderBy` ∈ the sproc whitelist; `OrderDirection ∈ {Asc,Desc}`.
- **Length guards (trim first, match DB widths):** search ≤ 255; text value ≤ 255; each value ≤ 2000
  (`TagValue`/`FilterValue`); tag key ≤ 50 (`TagDefinitionKey`). Over-length → reject.
- **Null/blank:** drop enumerable filters with no values and `IncludeBlank=false`, and text filters
  with blank `Text`. Facet requests strip the same-dimension filter server-side.
- Never throw raw — emit validation errors. Client mirrors caps/lengths for UX only; server treats all
  input as untrusted. URL parser (§5.4) fails safe.

---

## 6. Files to change / add

**Change:** `…/HomePage/Contracts/ResourceGridContract.cs`; `Resources/Interfaces/IResourceService.cs`;
`Resources/ResourceService.cs`; `Resources/Interfaces/IResourceRepository.cs`;
`Resources/ResourceSqlRepository.cs`; `Database/HTResourceMapperDb/Stored Procedures/Resource_GetItems.sql`;
**`Database/HTResourceMapperDb/HTResourceMapperDb.sqlproj`** (add `<Build Include>` entries — see below);
`UI/…/Components/Pages/Home.razor`; `UI/…/Components/_Imports.razor`.

**Add:** `Database/HTResourceMapperDb/User Defined Types/ResourceFilterList.sql`;
`Database/HTResourceMapperDb/Stored Procedures/Resource_GetFilterValues.sql`;
`…/HomePage/Filtering/ResourceFilterModels.cs`; `Resources/Models/ResourceGridFacetItem.cs`;
`UI/…/Components/Home/{ResourceFilterBar,FilterChip,EnumerableFilterEditor,TextFilterEditor,AddFilterMenu}.razor`.

> **`.sqlproj` is an explicit-include manifest (not globbed).** The two new SQL files **must** be
> added to `<Build Include>` or they won't compile into the dacpac and `Resource_GetItems` will fail
> to resolve the TVP type:
> ```xml
> <Build Include="User Defined Types\ResourceFilterList.sql" />
> <Build Include="Stored Procedures\Resource_GetFilterValues.sql" />
> ```

## 7. Suggested sequencing
1. **Contract** (§1). 2. **DB** (§2) incl. the **`.sqlproj` includes**; build the `.dacpac` to prove
resolution. 3. **Server** (§3) + validation; unit tests. 4. **Client state + URL** (§4a,§5) pure models
+ tests. 5. **Client UI** (§4b–4f) incl. `ServerData` grid. 6. **Verify** (§8).

## 8. Testing & verification

**Unit (xUnit + Moq 4.18.4 + FluentAssertions 7.1.0; `MethodName_State_Expected`, `[Trait("Category","Unit")]`, "because"):**
- `ResourceServiceTests`: filters **forwarded** (regression on today's drop); empty `Values`/blank `Text`
  stripped; `>4` filters / unknown `Column` / tag missing `TagKey` / bad `OrderBy` / over-length →
  validation error, repo not called; `Take` clamped to `{50,100,500}`; `GetResourceGridFacet` strips
  same dimension, maps blank → `(blank)`/`IsBlank`.
- Shared-model: `ToGridRequest`/`ChipLabel`/tri-state/`IsConstraining`; **deterministic key** —
  `ToQueryString()` identical across two independent constructions of the same state; URL round-trip
  incl. values with `~ , : @ % space` and the `~blank` token; robustness (unknown col/key dropped,
  >4 clamped, `n` clamped, bad sort dropped, garbage→empty).

**SQL via SQL MCP** (`mp-execute-procedure`/`mp-run-query`/`mp-explain-query`), seed
`Database/HTResourceMapperDb/Scripts/Demo_Insert_ResourceItemsForGrid.sql` (add rows with **NULL
ResourceTypeId and NULL TagValue**):
- `Resource_GetItems` with a hand-built `ResourceFilterList`: single/multi Type Equals (OR-within);
  Type NotEquals incl. **untyped survives**; **two tag-key filters AND** (`Environment=prod` +
  `Owner=me`); tag value OR-within; Tag NotEquals; **blank bucket** selectable for both Type and a tag
  key; Name contains with `% _ [` (escape); Search+filters combined; assert `@TotalRecords` == distinct
  paged count for the **same** `@whereClause`; assert `@TagLimit` caps tags per row **in SQL**.
- `Resource_GetFilterValues`: per-key tag facet returns only that key's values; counts **change with
  search text and other filters**, independent of the facet's own selection; `(blank)` present and
  selectable.
- Collation: assert the CI-collation assumption with a mixed-case value test (or add `COLLATE`).
- `mp-explain-query` the tag facet + grid on the largest seed.

**End-to-end (`/run`):** deep-link a filtered+sorted URL → correct top-N, no double query, "Showing 1–N
of Total" correct; add Type + two tag-key filters → *prod AND owned-by-me* returns the AND set; search
narrows and counts track it; switch display-count 100→500 re-queries; click a column header → server
re-sorts the top-N (not just the window); reach 4 filters → Add disabled; Back/Forward restores states
without a reload loop.

## 9. Risks (residual, after fixes)
- **`ServerData` minimal-use** — confirm hiding the pager while using `ServerData` behaves in MudBlazor
  9.5.0; fallback is manual reload on sort with `Items` mode + custom sortable headers.
- **Collation** — assumes case-insensitive DB collation (self-consistent since facet values round-trip
  the same columns); stated + tested.
- **Tag facet on a near-unique key** (e.g. `Owner`) can still be a few hundred values — acceptable per-key
  with client value-search; add server `ValueSearch` + TOP-N only if a key proves pathological.
- **`NavigateTo` self-echo** — guarded (§5.3) and tested; the one genuinely fiddly Blazor bit.

---

## Changelog — revisions from adversarial review (usability + implementation critics)
- **Tags are now per-key filters** (Azure model), replacing the single flat `Key:Value` OR list.
  Fixes: unnavigable multi-thousand-row list, and makes *key-AND-key* ("prod owned by me") expressible
  via two chips using the existing across-filter AND. Values still OR within a key.
- **Grid: Azure top-N model** (display-count `50/100/500` + "Showing 1–N of Total" + **server-side
  sort**, no pager) — replaces the silent 100-row client-mode cap that hid matches 101+ and mis-sorted
  the loaded window.
- **Facet counts now respect the search box** (reversed decision) — counts no longer contradict the grid.
- **`.sqlproj` `<Build Include>`** entries added (explicit-include manifest; new files wouldn't compile otherwise).
- **NULL/blank handled explicitly** (`IsBlank`) for Type and tag values — previously an unmatchable
  `NULL = NULL`; blank buckets are now shown *and* selectable.
- **Type NotEquals uses `NOT EXISTS`** (was `NOT IN`, a NULL landmine).
- **`sp_executesql` TVP passing** spelled out (`… READONLY` in paramdef; count/page paramdefs identical).
- **Tag cap pushed into SQL** (`@TagLimit`/`TagRank`) — bounds rows when display-count is 500.
- **Deterministic `ToQueryString()`** (sorted) so the prerender snapshot key is stable (no accidental double query).
- **Applied-but-empty filters auto-removed**; chips show values inline (≤3); NotEquals surfaced in the label.
- Confirmed sound (kept): injection posture, empty-TVP delivery, `NOT EXISTS` no-op logic, `EXISTS`
  (not `JOIN`) keeping count/page in lockstep.
