# Slice #1 — Data Foundation (schema + seed)

## Context

First implementation slice of the refreshed resource model (design:
`docs/plans/editor/resource-logical-model-and-editor-ux.md`; slice list:
`docs/plans/editor/00-implementation-plan-list.md`). This slice lands the **database schema
only** — the desired end-state DDL plus reference/system seed — so later slices (sprocs,
contracts, services, import, UI) have a correct foundation to build on.

**Ground rules confirmed this session:**
- **Greenfield / data not retained** → no backfill, no refactorlog, no migration scripts.
  Author desired-state `.sql`; a **drop-and-recreate publish** is fine.
- **Keep the old-style SSDT project** (`Database\HTResourceMapperDb\HTResourceMapperDb.sqlproj`,
  the one in `HT.ResourceMapper.slnx`). It is **declarative** (edit `.sql` = desired state;
  publish diffs). Build via VS / full MSBuild → dacpac; publish via SqlPackage or VS to
  `(localdb)\MSSQLLocalDB\ResourceMapper` using `localhost.publish.xml`.
- **Scope = the DB project + repo housekeeping only.** C# POCOs, sprocs, and repo readers are
  intentionally **out of scope** (slices 2–3). The app will be temporarily out of sync with the
  DB between slices — expected for a bottom-up build; slice-1 verification is DB-level.

## Schema changes (edit desired-state `.sql` under `Database\HTResourceMapperDb\`)

All paths below are under `Database\HTResourceMapperDb\`. Enum-ish columns use a short string +
`CHECK` (readable, no lookup table). `RequirementLevel ∈ (Error, Suggested, Optional)`.

**`Tables\Resource.sql`**
- `ResourceTypeId` `INT NULL` → **`INT NOT NULL`** (keep `FK_Resource_ResourceTypeId`).
- `ResourceName` `NVARCHAR(250) NULL` → **`NOT NULL`**. (`Description` stays `NULL`.)
- **Drop** `CONSTRAINT [UK_Resource_ResourceKey] UNIQUE` (identity is now code-enforced
  `Domain+Type+Key`; domain is a tag, so the full triple can't be a DB constraint). Keep the
  column `NOT NULL`; add non-unique `INDEX IX_Resource_ResourceKey (ResourceKey)` for lookups.
- **Add** `PrimaryTagDefinitionId INT NULL CONSTRAINT [FK_Resource_PrimaryTagDefinitionId]
  FOREIGN KEY REFERENCES [HTResourceMapper].[TagDefinition](TagDefinitionId)` (NO ACTION on
  delete → can't delete a definition that is some resource's primary).

**`Tables\TagDefinition.sql`**
- `IsSystemTag` `INT` → **`BIT`** (keep `DEFAULT (0)`).
- **Add** `DisplayName NVARCHAR(100) NULL` (UI/queries `COALESCE(DisplayName, TagDefinitionKey)`).
- **Add** `RequirementLevel NVARCHAR(20) NOT NULL DEFAULT ('Optional')` + `CHECK (RequirementLevel
  IN ('Error','Suggested','Optional'))`.
- **Add** `IsDomainTag BIT NOT NULL DEFAULT (0)`; enforce ≤1 via filtered unique index
  `UX_TagDefinition_SingleDomainTag ON (IsDomainTag) WHERE IsDomainTag = 1`.
- **Add** `DisplayOrder INT NOT NULL DEFAULT (1000)` (curated tags get low values; unset sorts last).
- Fix PK-name typo `PK_TagDefinitons_...` → `PK_TagDefinitions_TagDefinitionId` (greenfield).

**`Tables\TagContentType.sql`** (constrain to the enum + prepare for seed)
- `TagCode VARCHAR(50) NULL` → **`NOT NULL`**; add `CHECK (TagCode IN ('Text','Link'))`.
- Keep non-IDENTITY PK (seed assigns fixed ids: Text=1, Link=2). *(The auto-register MAX+1 logic
  in `TagDefinition_Upsert` will violate this CHECK for unknown types — that sproc is fixed in
  slice 2; no imports run in slice 1.)*

**`Tables\ResourceTypeTag.sql`** (redesign — replace free string + value-type FK)
- Remove `Tag NVARCHAR(40)` and `TagValueTypeId` (+ its FK to `TagValueType`).
- Keep `ResourceTypeTagId` PK, `ResourceTypeId` FK.
- **Add** `TagDefinitionId INT NOT NULL FK → TagDefinition(TagDefinitionId)`; `IsDefaultPrimary BIT
  NOT NULL DEFAULT (0)`; `RequirementLevel NVARCHAR(20) NULL CHECK (…)` (per-type override).
- **Add** `UNIQUE (ResourceTypeId, TagDefinitionId)`; filtered unique index
  `UX_ResourceTypeTag_DefaultPrimary ON (ResourceTypeId) WHERE IsDefaultPrimary = 1`.

**`Tables\TagValueType.sql`** — **delete the file** (drop the table). Its FK from
`ResourceTypeTag` is already gone via the redesign above.

**`Tables\ResourceDependency.sql` → rename to `Tables\ResourceRelationship.sql`** (new content)
- `RelationshipId INT IDENTITY PK`; `FromResourceId INT NOT NULL FK → Resource`; `ToResourceId INT
  NOT NULL FK → Resource`; `CreatedOn` (default `SYSUTCDATETIME()`), `UpdatedOn NULL`.
- `UNIQUE (FromResourceId, ToResourceId)`; `CHECK (FromResourceId <> ToResourceId)`.
- **FKs NO ACTION** (two FKs to the same table can't both cascade — "multiple cascade paths").
  Cascade-delete of edges on resource delete is handled in the **delete sproc (slice 2)**.

**Optional typo cleanup (greenfield):** `FK_ResourceTag_TagDefintionId` → `…TagDefinitionId` in
`Tables\ResourceTag.sql`.

## Project file, seed & housekeeping

**`HTResourceMapperDb.sqlproj`** (`<Build>`/`<None>` items are explicit — old-style, no globbing):
- Remove `<Build Include="Tables\TagValueType.sql" />`.
- Rename `<Build Include="Tables\ResourceDependency.sql" />` → `Tables\ResourceRelationship.sql`.
- Add a post-deploy item: `<PostDeploy Include="Scripts\Script.PostDeployment1.sql" />` (this
  project has none today — introduces the pattern).

**Seed — new `Scripts\Script.PostDeployment1.sql`** (runs every publish; idempotent `MERGE`):
- `TagContentType`: MERGE rows `(1,'Text')`, `(2,'Link')`.
- `TagDefinition` **Domain** boundary tag: MERGE on `TagDefinitionKey='Domain'` →
  `DisplayName='Subscription'`, `TagContentTypeId=1` (Text), `AllowCustomValue=0`,
  `IsMultiValued=0`, `AllowedValues='["prod","non-prod"]'`, `IsDomainTag=1`, `IsSystemTag=1`,
  `RequirementLevel='Error'`, `DisplayOrder=10`, fixed `TagDefinitionUid`.

**Housekeeping (requested):**
- Delete stale, non-built duplicate trees: `Database\HTResourceMapperDb\HTResourceMapper\`,
  `Database\ResourceMapperDatabase\`, `Database\HTResourceMapper-nothing\`, and the orphaned
  SDK-preview `Database\HT.Services.sqlproj` (+ its private files).
- Update `Scripts\Demo_Purge_ResourceItemsForGrid.sql`: `ResourceDependency` → `ResourceRelationship`;
  remove the `TagValueType` delete (table dropped). (Demo scripts are manual `<None>`.)
- Fix `CLAUDE.md`: correct the DB-project description — it is **old-style SSDT**
  (`TargetFrameworkVersion v4.7.2`, `Microsoft.Data.Tools.Schema.SqlTasks.targets`), built in
  VS / full MSBuild → dacpac and published via SqlPackage/VS — **not** `Microsoft.Build.Sql`
  SDK 2.2.0 / `dotnet build`. Remove the inaccurate SDK/`net472`/`dotnet build` claims.

## Out of scope (later slices) — note the temporary inconsistency

Not touched here (would break the *build* only if they referenced dropped objects — they don't;
they break at *runtime* until updated): EF POCOs (`Resource.cs` `ResourceTypeId int?`→`int` + add
`PrimaryTagDefinitionId`; `TagDefinition.cs` `IsSystemTag int`→`bool` + 4 new props; rename
`ResourceDependency.cs`→`ResourceRelationship.cs`; delete `TagValueType.cs`; rework
`ResourceTypeTag.cs`) → **slices 2–3**; sprocs (`Resource_Upsert`/`Resource_Create` nullable-type
lookup vs NOT NULL; `TagDefinition_GetAll`/`_Upsert` new columns; `TagDefinition_Upsert`
content-type auto-register vs the CHECK; `Resource_GetItems` `ResourceTypeId IS NULL` branches) →
**slice 2**; repo reader `ImportSqlRepository.cs:34` `ReadInt("IsSystemTag")`→`ReadBoolean` →
**slice 2/3**.

## Verification (DB-level)

1. **Build dacpac** with full MSBuild (VS 2026 installed):
   `MSBuild Database\HTResourceMapperDb\HTResourceMapperDb.sqlproj /t:Build` → confirm
   `bin\Debug\HTResourceMapperDb.dacpac` builds with **no errors** (validates all `.sql` +
   post-deploy compiles; catches dropped-object references).
2. **Publish (recreate)** to `(localdb)\MSSQLLocalDB\ResourceMapper` via SqlPackage
   `/Action:Publish /SourceFile:…dacpac /Profile:…\localhost.publish.xml` (or VS Publish).
   Target DB name **must be `ResourceMapper`** (known publish-target trap).
3. **Confirm with `sqlcmd`** (per project convention, use sqlcmd — not the SQL MCP):
   - `ResourceRelationship` exists; `ResourceDependency` and `TagValueType` do **not**.
   - `Resource`: `ResourceTypeId` is `NOT NULL`, `PrimaryTagDefinitionId` FK present, no UNIQUE
     on `ResourceKey`.
   - `TagDefinition`: `IsSystemTag` is `bit`; `DisplayName`/`RequirementLevel`/`IsDomainTag`/
     `DisplayOrder` present; filtered unique index enforces one domain tag.
   - `ResourceTypeTag`: `TagDefinitionId` FK + `IsDefaultPrimary`; no `Tag`/`TagValueTypeId`.
   - `TagContentType`: rows `Text`,`Link` and the CHECK; `TagDefinition` has the seeded `Domain`
     row (`IsDomainTag=1`, `AllowedValues='["prod","non-prod"]'`).

Then: move this plan to `docs/plans/editor/01-data-foundation.md`, flip slice #1 → Done and
slice #2 → Planning in `00-implementation-plan-list.md`, and commit.
