# Slice #2 — Sprocs & Repository (data-access layer)

## Context

Second implementation slice (design: `docs/plans/editor/resource-logical-model-and-editor-ux.md`;
list: `docs/plans/editor/00-implementation-plan-list.md`). Slice #1 changed the **schema**; this
slice makes the **data-access layer** (stored procedures + C# POCOs + repositories) correct and
consistent under that schema, keeps JSON import working, and adds the **CRUD sproc + repository
surface** (relationships, delete, get-by-uid) that the editor slices (#6–9) will wire to. No
service/controller/UI changes here (slice #4+).

**Decisions (this session):**
- **Domain deferred to slice #5.** Slice #2 identity is `(ResourceType + ResourceKey)` only; no
  `@Domain` param, no domain-tag joins, `Resource_GetAllKeys` and the import uniqueness check are
  **left as-is** and finished in slice #5 when Domain lands end-to-end.
- **Drop `Resource_Create`** (dead + broken under NOT-NULL type). Creation goes through
  `Resource_Upsert`.
- **Grid sprocs: correctness only** — remove the now-dead "blank ResourceType" branches; defer
  `DisplayName`/`DisplayOrder`, Domain facet, and primary-link projection to the UI slices.
- **Build relationship + delete CRUD now** (sprocs + repo methods), unconsumed until #6–9.

## SQL — change existing sprocs (`Database\HTResourceMapperDb\Stored Procedures\`)

- **`Resource_Upsert.sql`** — resolve `@ResourceTypeId` from `@TypeName`; **guard: if it resolves
  NULL, `RAISERROR` / return an error `@Result` instead of inserting NULL** (ResourceTypeId is now
  NOT NULL). Change the find-existing predicate from `WHERE ResourceKey=@ResourceKey` to
  `WHERE ResourceKey=@ResourceKey AND ResourceTypeId=@ResourceTypeId` (partial identity; Domain
  added in #5). Leave `PrimaryTagDefinitionId` unset (nullable; editor sets it in #6). Signature
  unchanged → no C#/import ripple.
- **`TagDefinition_GetAll.sql`** — add the new columns to the SELECT: `DisplayName`,
  `RequirementLevel`, `IsDomainTag`, `DisplayOrder`; order by `DisplayOrder, TagDefinitionKey`.
- **`TagDefinition_Upsert.sql`** — **remove the TagContentType auto-register block** (MAX+1 INSERT
  now violates the Text/Link CHECK); replace with a lookup that `RAISERROR`s if `@ContentType`
  isn't a seeded code. Add params `@DisplayName`, `@RequirementLevel`, `@IsDomainTag`,
  `@IsSystemTag`, `@DisplayOrder` and include them in INSERT + UPDATE. If `@IsDomainTag=1`, clear
  any other row's flag first (respect the single-domain filtered unique index).
- **`Resource_GetItems.sql`** — remove the dead `r.ResourceTypeId IS NULL` filter branches (blank
  ResourceType bucket no longer exists); `LEFT JOIN ResourceType` → `INNER JOIN` (FK now
  mandatory). No projection/label changes this slice.
- **`Resource_GetFilterValues.sql`** — remove the dead blank-ResourceType branches
  (`IsBlank` on `ResourceTypeId` is always 0); `LEFT JOIN` → `INNER JOIN` in the ResourceType
  facet. No Domain facet this slice.
- **`Resource_GetByResourceUid.sql`** — replace `SELECT *` with an explicit column list including
  the new `PrimaryTagDefinitionId` (feeds the editor/detail read in #6).
- **`Resource_GetAllKeys.sql`, `Resource_GetDaysAgo.sql`, `ResourceType_GetAll.sql`,
  `ResourceType_Upsert.sql`, `ResourceTag_SetForResource.sql`** — **unchanged** this slice (domain
  safety on `SetForResource` lands in #5).
- **Delete `Resource_Create.sql`** and remove its `<Build>` item from the `.sqlproj`.

## SQL — new sprocs (+ `<Build>` items in the `.sqlproj`)

- **`Resource_Delete.sql`** `@ResourceId INT` — in a TRAN: `DELETE ResourceRelationship WHERE
  FromResourceId=@ResourceId OR ToResourceId=@ResourceId;` then `DELETE ResourceTag WHERE
  ResourceId=@ResourceId;` then `DELETE Resource WHERE ResourceId=@ResourceId;` (cascade handled
  here — the FKs are NO ACTION by design). Allow-with-warning: caller confirms first (UI #9).
- **`ResourceRelationship_GetForResource.sql`** `@ResourceId INT` — one result set: `RelationshipId`,
  `Direction` (`'DependsOn'` for out-edges `FromResourceId=@ResourceId`; `'DependentOn'` for
  in-edges `ToResourceId=@ResourceId`), and the *other* resource's `ResourceUid`, `ResourceKey`,
  `ResourceName`, `TypeName`. (Feeds Dependencies/Dependent On tabs + the delete dialog's
  dependents list.)
- **`ResourceRelationship_Add.sql`** `@FromResourceId INT, @ToResourceId INT` — idempotent
  `IF NOT EXISTS (...) INSERT`; the `CHECK (From<>To)` guards self-loops.
- **`ResourceRelationship_Remove.sql`** `@FromResourceId INT, @ToResourceId INT` — `DELETE` the edge.

## C# model layer (`Modules\Common\ResourceMapper.Common.Server\Resources\Models\`)

- **Rename `ResourceDependency.cs` → `ResourceRelationship.cs`**: `[Table("ResourceRelationship",…)]`,
  props `RelationshipId`, `FromResourceId`, `ToResourceId`, `CreatedOn`, `UpdatedOn`; navs
  `FromResource`/`ToResource`.
- **`Resource.cs`**: `ResourceTypeId` `int?` → `int`; add `int? PrimaryTagDefinitionId`; retype the
  `Dependencies`/`Dependents` collections to `ResourceRelationship`.
- **Delete `TagValueType.cs`.**
- **`ResourceTypeTag.cs`**: drop `Tag` + `TagValueTypeId` + the `TagValueType` nav; add
  `int TagDefinitionId`, `bool IsDefaultPrimary`, `string? RequirementLevel`, `TagDefinition` nav.
- **`TagDefinition.cs`**: `IsSystemTag` `int` → `bool`; add `string? DisplayName`,
  `string RequirementLevel` (default `"Optional"`), `bool IsDomainTag`, `int DisplayOrder`.
- New repo result models (this folder): `ResourceRelationshipItem` (Direction + other-resource
  fields) and a lean `ResourceDetail` (or reuse `Resource`) for get-by-uid.

## Repository layer (`...\Resources\`)

- **`ImportSqlRepository.GetAllTagDefinitionsAsync`**: change `IsSystemTag = dr.ReadInt(...)` →
  `dr.ReadBoolean("IsSystemTag")`; map the four new columns (`ReadString("DisplayName")`,
  `ReadString("RequirementLevel")`, `ReadBoolean("IsDomainTag")`, `ReadInt("DisplayOrder")`).
- **`IResourceRepository` + `ResourceSqlRepository`** — add (backing the new/updated sprocs, using
  the existing `SprocCommand`/`.Add*`/`.AddTvp`/`dr.Read*`/`ExecuteRowAsync`/`ExecuteQueryAsync`/
  `ExecuteNonQueryAsync` helpers):
  - `GetResourceByUidAsync(string uid, …)` → `ResourceDetail?` (via `Resource_GetByResourceUid`, `ExecuteRowAsync`).
  - `GetRelationshipsForResourceAsync(int resourceId, …)` → `List<ResourceRelationshipItem>`.
  - `AddRelationshipAsync(int fromId, int toId, …)` / `RemoveRelationshipAsync(int fromId, int toId, …)`.
  - `DeleteResourceAsync(int resourceId, …)`.
  - These are unconsumed until slice #4/#6–9 (compile + covered by sproc-level verification now).

## Tests (`_Tests\Common\Server\ResourceMapper.Common.Server.Tests\`)

- Update any test that constructs `TagDefinition` with an `int` `IsSystemTag` (now `bool`) or
  references the removed `ResourceTypeTag`/`TagValueType` shapes. `UpsertResourceAsync` /
  `GetExistingResourceKeysAsync` signatures are **unchanged**, so `ImportServiceTests` /
  `ResourceServiceTests` Moq setups should not need re-pinning.
- No repository-test infra exists; slice-#2 verification is at the sproc + compile + import level
  (below), not new repo unit tests.

## Out of scope (later slices)

Domain param/joins, `Resource_GetAllKeys` tuples, import (`type` required + defaults), and
`ResourceTag_SetForResource` domain-safety → **#5**. `DisplayName`/`DisplayOrder`, Domain facet,
primary-link projection, primary-link seeding from `ResourceTypeTag` → **#6+**. Service/controller
methods that call the new repo methods → **#4**.

## Verification

1. **DB builds + publishes:** `MSBuild HTResourceMapperDb.sqlproj /t:Rebuild` (VS18 MSBuild) → dacpac
   with no errors (validates all sproc changes compile against the schema); `sqlpackage
   /Action:Publish` to `(localdb)\MSSQLLocalDB\ResourceMapper`.
2. **Exercise the sprocs via `sqlcmd`** with a tiny sample: seed a ResourceType + a couple
   TagDefinitions; `Resource_Upsert` (verify created→updated, and the type-guard error path when
   `@TypeName` is bogus); `TagDefinition_Upsert` (verify new columns set; verify a non-Text/Link
   `@ContentType` errors); `TagDefinition_GetAll` (new columns returned); insert two resources +
   `ResourceRelationship_Add`/`_Remove`/`_GetForResource` (both directions, self-loop rejected,
   idempotent add); `Resource_Delete` (edges + tags gone, no orphans); `Resource_GetByResourceUid`
   (explicit columns incl. PrimaryTagDefinitionId).
3. **C# compiles + tests pass:** `dotnet build` the affected C# projects (Common.Server + Tests —
   not the sqlproj) and `dotnet test` the Common.Server.Tests project; fix fallout from the POCO
   type changes.
4. **Import smoke:** run an import of a small JSON with a valid `type` → resource + tags written
   (confirms the type-guarded upsert path still works end-to-end).

## Execution notes

- **All steps completed and verified** (dacpac build, publish, 14-point sqlcmd exercise, C# build
  + 37 existing tests unchanged/passing, import smoke test via a temporary throwaway xUnit test
  deleted before commit).
- **`sqlpackage /Action:Publish` needs `/p:DropObjectsNotInSource=True`** — the publish profile
  (`localhost.publish.xml`) doesn't set it, and SqlPackage defaults to `false`, so a plain publish
  does **not** drop objects removed from source (e.g. the deleted `Resource_Create.sql` stayed in
  the DB until republished with this flag). Use this flag on every publish for this declarative
  project so the DB always matches the desired-state `.sql` files.
- `Resource_Upsert`, `Resource_Create` (deleted), and `TagDefinition_Upsert` now return
  `@Result = 'error'` on an unresolvable type / disallowed content type (in addition to
  `created`/`updated`/`skipped`); callers checking only those three strings should treat anything
  else as a failure.

Then: move this plan to `docs/plans/editor/02-sprocs-and-repository.md`, mark slice #2 Done / #3
Planning in the master list, and commit.
