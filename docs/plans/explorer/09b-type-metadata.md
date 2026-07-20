# Slice #9b — Type metadata (ShortCode + IconKey on ResourceType)

## Context

First sub-slice of the [UI-polish pass 2](./09-ui-polish.md) (see its "Pass 2 — agreed design"). This
is the **back-end piece**: the explorer's new nodes show a per-type **short code** (`APP`, `APC`, `SB`,
`SBQ`, …) and a **type icon** — both are properties of the **resource type**, which the data doesn't
carry today. This slice adds them to `ResourceType` and threads them through the explorer read so 9c
(node rework) can render them. **Back-end only — no UI.**

**Key decisions (this slice):**

- **`ShortCode VARCHAR(10)` + `IconKey VARCHAR(40)`** (both nullable) added to `ResourceType`. `IconKey`
  is an opaque string (a key like `web`, `memory`, `queue`) that 9c maps to an actual glyph — the DB
  stores only the key.
- **Idempotent seed by `TypeName`** for all current types (Azure* + the demo App/Service/Database/Queue),
  as a `<None>` script (manual, like the other seeds) + applied to localdb.
- **`Resource_GetForExplorer` returns `ShortCode`/`IconKey`** per node (center + neighbors); threaded
  into `ExplorerNodeRow` → `ExplorerNodeModel`/`ExplorerNeighborModel` and the service mapping.
- Unit-tested (service mapping); sproc applied + exercised via `sqlcmd`.

---

## 1. Schema — add columns (`Database\HTResourceMapperDb\Tables\ResourceType.sql`)

Add the two columns (after `AllowCustomTags`, before `CreatedOn`):

```sql
    ,[ShortCode] VARCHAR(10) NULL
    ,[IconKey] VARCHAR(40) NULL
```

Apply to localdb via `sqlcmd` (additive `ALTER`, guarded):

```sql
IF COL_LENGTH('[HTResourceMapper].[ResourceType]','ShortCode') IS NULL
    ALTER TABLE [HTResourceMapper].[ResourceType] ADD [ShortCode] VARCHAR(10) NULL;
IF COL_LENGTH('[HTResourceMapper].[ResourceType]','IconKey') IS NULL
    ALTER TABLE [HTResourceMapper].[ResourceType] ADD [IconKey] VARCHAR(40) NULL;
```

- [ ] **Step 1:** Add the columns to `ResourceType.sql` and apply the guarded `ALTER` to localdb.

---

## 2. Seed the codes/icons (`Database\HTResourceMapperDb\Scripts\05_Seed_ResourceTypeCodes.sql`)

New idempotent seed keyed on `TypeName` (the exact current type names, including the existing
`ServcieFabric` typo — match verbatim). Add it to `HTResourceMapperDb.sqlproj` as a **`<None>`** item
(manual seed, matching the other `NN_Seed_*.sql`), and apply to localdb via `sqlcmd`.

```sql
-- Per-resource-type short codes + icon keys for the explorer node badges. Idempotent (UPDATE by
-- TypeName). IconKey is an opaque key the UI maps to a glyph.
UPDATE t
SET ShortCode = v.ShortCode, IconKey = v.IconKey, UpdatedOn = SYSUTCDATETIME()
FROM [HTResourceMapper].[ResourceType] t
INNER JOIN (VALUES
    ('Azure App Service',  'APP', 'web'),
    ('Application',        'APL', 'apps'),
    ('App',                'APP', 'web'),
    ('Azure App Config',   'APC', 'settings'),
    ('Azure App Insights', 'AI',  'insights'),
    ('Azure Redis',        'RDS', 'memory'),
    ('Azure ServiceBus',   'SB',  'bus'),
    ('Azure ServcieFabric','SF',  'hub'),
    ('Azure ResourceGroup','RG',  'folder'),
    ('Service',            'SVC', 'dns'),
    ('Database',           'DB',  'database'),
    ('Queue',              'SBQ', 'queue')
) AS v(TypeName, ShortCode, IconKey) ON v.TypeName = t.TypeName;
```

- [ ] **Step 2:** Create `05_Seed_ResourceTypeCodes.sql`, add the `<None>` line to the `.sqlproj`, and apply to localdb.

---

## 3. Read sproc (`Database\HTResourceMapperDb\Stored Procedures\Resource_GetForExplorer.sql`)

Add `rt.ShortCode` and `rt.IconKey` to **each** of the three SELECTs (Self / DependsOn / DependentOn).
Replace the whole sproc body with this (only the two new projected columns per SELECT changed):

```sql
CREATE PROCEDURE [HTResourceMapper].[Resource_GetForExplorer]
    @ResourceUid VARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ResourceId INT =
        (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceUid = @ResourceUid);

    IF @ResourceId IS NULL
        RETURN;

    DECLARE @DomainTagDefId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE IsDomainTag = 1);

    SELECT
        'Self' AS Direction,
        r.ResourceUid, r.ResourceKey, r.ResourceName,
        rt.TypeName    AS ResourceType,
        rt.ShortCode   AS ShortCode,
        rt.IconKey     AS IconKey,
        dt.TagValue    AS Domain,
        purl.TagValue  AS PrimaryUrl
    FROM [HTResourceMapper].[Resource] r
    INNER JOIN [HTResourceMapper].[ResourceType] rt ON rt.ResourceTypeId = r.ResourceTypeId
    LEFT JOIN [HTResourceMapper].[ResourceTag] dt ON dt.ResourceId = r.ResourceId AND dt.TagDefinitionId = @DomainTagDefId
    LEFT JOIN [HTResourceMapper].[ResourceTag] purl ON purl.ResourceId = r.ResourceId AND purl.TagDefinitionId = r.PrimaryTagDefinitionId
    WHERE r.ResourceId = @ResourceId

    UNION ALL

    SELECT
        'DependsOn' AS Direction,
        other.ResourceUid, other.ResourceKey, other.ResourceName,
        rt.TypeName    AS ResourceType,
        rt.ShortCode   AS ShortCode,
        rt.IconKey     AS IconKey,
        dt.TagValue    AS Domain,
        purl.TagValue  AS PrimaryUrl
    FROM [HTResourceMapper].[ResourceRelationship] rel
    INNER JOIN [HTResourceMapper].[Resource] other ON other.ResourceId = rel.ToResourceId
    INNER JOIN [HTResourceMapper].[ResourceType] rt ON rt.ResourceTypeId = other.ResourceTypeId
    LEFT JOIN [HTResourceMapper].[ResourceTag] dt ON dt.ResourceId = other.ResourceId AND dt.TagDefinitionId = @DomainTagDefId
    LEFT JOIN [HTResourceMapper].[ResourceTag] purl ON purl.ResourceId = other.ResourceId AND purl.TagDefinitionId = other.PrimaryTagDefinitionId
    WHERE rel.FromResourceId = @ResourceId

    UNION ALL

    SELECT
        'DependentOn' AS Direction,
        other.ResourceUid, other.ResourceKey, other.ResourceName,
        rt.TypeName    AS ResourceType,
        rt.ShortCode   AS ShortCode,
        rt.IconKey     AS IconKey,
        dt.TagValue    AS Domain,
        purl.TagValue  AS PrimaryUrl
    FROM [HTResourceMapper].[ResourceRelationship] rel
    INNER JOIN [HTResourceMapper].[Resource] other ON other.ResourceId = rel.FromResourceId
    INNER JOIN [HTResourceMapper].[ResourceType] rt ON rt.ResourceTypeId = other.ResourceTypeId
    LEFT JOIN [HTResourceMapper].[ResourceTag] dt ON dt.ResourceId = other.ResourceId AND dt.TagDefinitionId = @DomainTagDefId
    LEFT JOIN [HTResourceMapper].[ResourceTag] purl ON purl.ResourceId = other.ResourceId AND purl.TagDefinitionId = other.PrimaryTagDefinitionId
    WHERE rel.ToResourceId = @ResourceId

    ORDER BY Direction, ResourceName;
END
```

Apply to localdb as `CREATE OR ALTER` (the file keeps `CREATE`).

- [ ] **Step 3:** Update the sproc file + apply `CREATE OR ALTER` to localdb.

---

## 4. Thread the fields through C#

### 4a. Repo row (`Modules\Common\ResourceMapper.Common.Server\Resources\Models\ExplorerNodeRow.cs`)

Add:
```csharp
        public string? ShortCode { get; set; }
        public string? IconKey { get; set; }
```

### 4b. Repository reader (`Modules\Common\ResourceMapper.Common.Server\Explorer\ExplorerSqlRepository.cs`)

In the `GetForExplorerAsync` mapper, add:
```csharp
                ShortCode = dr.ReadString("ShortCode"),
                IconKey = dr.ReadString("IconKey"),
```

### 4c. Shared DTOs (`Modules\Common\ResourceMapper.Common.Shared\Explorer\`)

Add to **both** `ExplorerNodeModel.cs` and `ExplorerNeighborModel.cs`:
```csharp
        public string? ShortCode { get; set; }
        public string? IconKey { get; set; }
```

### 4d. Service mapping (`Modules\Common\ResourceMapper.Common.Server\Explorer\ExplorerService.cs`)

In `GetNodeAsync`, set the two fields on the center `ExplorerNodeModel` (from `self`) and on each
`ExplorerNeighborModel` (from `r`):
```csharp
                    ShortCode = self.ShortCode,
                    IconKey = self.IconKey,
```
(center) and
```csharp
                            ShortCode = r.ShortCode,
                            IconKey = r.IconKey,
```
(each neighbor in the `.Select(...)`).

- [ ] **Step 4:** Apply 4a–4d.

---

## 5. Tests (`_Tests\...\Explorer\ExplorerServiceTests.cs`)

Update the `Row(...)` helper to accept + set `shortCode`/`iconKey`, and extend the mapping test to
assert they flow through. Minimal change:

- Extend the helper signature: `Row(string direction, string uid, string key, string name, string type, string? domain, string? primaryUrl, string? shortCode = null, string? iconKey = null)` and set `ShortCode = shortCode, IconKey = iconKey` on the returned row.
- In `GetNodeAsync_SelfAndNeighbors_MapsCenterFieldsAndSplitsNeighbors`, pass a short code/icon on the `Self` row (e.g. `"APP","web"`) and assert:
```csharp
            data.ShortCode.Should().Be("APP", "because the center node's type short code is projected");
            data.IconKey.Should().Be("web", "because the center node's type icon key is projected");
```

- [ ] **Step 5:** Update the helper + add the two assertions; run `dotnet test` for `ExplorerServiceTests`.

---

## Verification

1. Apply schema `ALTER` + seed + sproc to localdb via `sqlcmd`.
2. **Exercise:** `EXEC [HTResourceMapper].[Resource_GetForExplorer] @ResourceUid='DEMOEXP-checkout'` → each row now has non-null `ShortCode`/`IconKey` (e.g. the seed's type shows its code). Verify the seed populated all types: `SELECT TypeName, ShortCode, IconKey FROM [HTResourceMapper].[ResourceType]` → no NULLs for the listed types.
3. `dotnet build` clean; `dotnet test` — `ExplorerServiceTests` pass, no regression.

---

## Out of scope (→ 9c / 9d)

- **Rendering** the code/icon inside nodes, hover-only name/type labels, layered layout, Re-tidy,
  auto-fit → **slice 9c**.
- **Title/date overlay + export compositing, edge "depends on" tooltip** → **slice 9d**.
- Mapping `IconKey` → an actual glyph/SVG is 9c's concern; this slice only carries the key.

---

## Execution notes

_(After execution — record the sqlcmd exercise, any TypeName spelling reconciliations, test results, and commit hash(es). Then proceed to 9c.)_
