# Import / Export API — Design & Implementation Plan

## Context

This document captures the design decisions and implementation plan for a
`POST /api/resources/import` endpoint that ingests a JSON document containing 0..N resources
(plus optional reference data) into the ResourceMapper database.

A companion `GET /api/resources/export` endpoint is deferred; the schema and UID strategy
are designed with it in mind.

---

## Design Decisions

| # | Topic | Decision |
|---|-------|----------|
| 1 | Conflict resolution | Per-import policy in `policy.onConflict` |
| 2 | Validation failure | Full validation runs before any write; any validation error returns 400 with the complete error list and nothing is written |
| 3 | Write-phase failure | No cross-call transaction wrapper; writes execute sequentially after validation passes. Unexpected exception mid-write returns 500; partial writes are an accepted risk given low write volume. (Sprocs that do multi-statement writes use a sproc-local transaction — see `ResourceTag_SetForResource`.) |
| 4 | Export | Future feature; same JSON format as import; UIDs included in export output |
| 5 | UID strategy | `key` is the match field on import; `uid` ignored if present; `uid` included in export output |
| 6 | Reference data format | Object map keyed by name (`{ "TypeName": { ...props } }`) |
| 7 | Reference data conflict policy | Same `onConflict` policy as resources |
| 8 | Tags on resources | Object map; string for single value, array for multi-valued |
| 9 | Import response (success) | Summary: counts broken down by action per section (created/updated/skipped) |
| 10 | Import response (failure) | Flat list of errors naming section + key + field + message; nothing written. Errors are returned in the `ImportResponse` Data payload; a single summary validation message on the `ApiResponse` wrapper drives the 400 status |
| 11 | Custom tags | Every tag key on a resource **must** resolve to a tag definition (in the import payload or the DB). `ResourceType.AllowCustomTags` grants no exemption — `ResourceTag.TagDefinitionId` is a NOT NULL FK, so undefined tags cannot be stored |
| 12 | TVP support | `HT.Microsoft.SqlClient.Extensions` is extended with table-valued parameter support (`AddTvp`) plus string output-parameter support (`AddVarchar` with direction, `ReadString`) |
| 13 | `contentType` | Free-form string (max 50 chars). Not validated against a fixed list. `TagDefinition_Upsert` auto-registers unseen values in `TagContentType` (the table acts as a registry, not a validation gate). No schema change |
| 14 | ResourceType resolution | `Resource_Upsert` takes `@TypeName` and resolves `ResourceTypeId` internally — the service never needs generated IDs back from the type-upsert pass |
| 15 | Upsert sproc pattern | No MERGE. Pre-check IF/ELSE pattern with `@Result VARCHAR(10) OUTPUT` (`'created'`/`'updated'`/`'skipped'`); `Resource_Upsert` additionally returns `@ResourceId INT OUTPUT` in all cases, including skip |
| 16 | Skip semantics | Skip means skip **everything** for that resource: no row update, no tag replacement, no dependency replacement. Skipped resources are excluded from the tag pass and the dependency pass |
| 17 | "Updated" semantics | Matched + upsert counts as `updated` even if the incoming values are identical — no value diffing |
| 18 | Key comparison | Keys are case-insensitive throughout: C# lookups use `StringComparer.OrdinalIgnoreCase`; DB unique constraints already use the default case-insensitive collation. Keys differing only by case are duplicates |
| 19 | UID format | `[prefix] + Guid.NewGuid().ToString("N")` (32 hex chars). Prefixes: Resource `res-` (36 total), ResourceType `rt-` (35), TagDefinition `td-` (35). All fit `VARCHAR(40)` |
| 20 | `fail` policy at write time | `'fail'` never reaches the sprocs — conflict validation already rejected the request if anything existed. The service passes `'skip'` to sprocs when policy is `fail` (defensive: a row created concurrently mid-import gets skipped, not overwritten) |

---

## JSON Schema

```json
{
  "version": "1.0",
  "policy": { "onConflict": "upsert" },
  "resourceTypes": {
    "AppConfiguration": { "allowCustomTags": true },
    "ServiceBus": { "allowCustomTags": false }
  },
  "tagDefinitions": {
    "environment": {
      "contentType": "string",
      "allowCustomValue": false,
      "isMultiValued": false,
      "allowedValues": ["dev", "int", "preprod", "prod"]
    },
    "owner": {
      "contentType": "string",
      "allowCustomValue": true,
      "isMultiValued": true
    }
  },
  "resources": [
    {
      "key": "visibility-app-config-prod",
      "name": "Visibility App Configuration",
      "type": "AppConfiguration",
      "description": "Azure App Configuration store for Visibility (production)",
      "tags": {
        "environment": "prod",
        "owner": ["team-a", "team-b"],
        "url": "https://portal.azure.com/..."
      },
      "dependencies": ["other-resource-key"]
    }
  ]
}
```

**Schema rules:**
- `resourceTypes` and `tagDefinitions` are optional sections
- `type` must reference a key in the `resourceTypes` section OR an existing `TypeName` in the DB
- `dependencies` must reference `key` values within this import OR existing `ResourceKey` values in the DB; a resource may not depend on itself
- Every tag key used on a resource must reference a key in the `tagDefinitions` section OR an existing `TagDefinitionKey` in the DB (no `AllowCustomTags` exemption — see decision 11)
- All key comparisons are case-insensitive (decision 18)
- All three sections follow the same `onConflict` policy
- Tag values: string = single value, array = multi-value. An array value is only legal when the tag definition has `isMultiValued: true`
- When a tag definition has `allowCustomValue: false`, every supplied value must appear in its `allowedValues`
- `contentType` is a free-form string, max 50 chars (decision 13)
- `allowedValues` is serialized to the DB `AllowedValues NVARCHAR(2000)` column as a JSON array string; the serialized form must fit 2000 chars
- `onConflict` values: `"upsert"` | `"skip"` | `"fail"`

---

## Response Shape

### Success (HTTP 200)
```json
{
  "success": true,
  "summary": {
    "resourceTypes": { "created": 1, "updated": 0, "skipped": 0 },
    "tagDefinitions": { "created": 2, "updated": 0, "skipped": 0 },
    "resources": { "created": 8, "updated": 1, "skipped": 1 }
  }
}
```

### Failure (HTTP 400)
The flat error list lives in the `ImportResponse` (the `ApiResponse.Data` payload). The service
also adds **one** summary validation message via `builder.Validation.AddValidation("import", "...")`
— that wrapper message is what drives the 400 status through `MapServiceResponseToActionResult`.
`ImportResponse.Errors` is the source of truth for error detail.

```json
{
  "success": false,
  "errors": [
    {
      "section": "resources",
      "key": "visibility-app-config",
      "field": "type",
      "message": "ResourceType 'AppConfiguration' not found"
    }
  ]
}
```

---

## API Endpoints

| Method | Path | Status |
|--------|------|--------|
| `POST` | `/api/resources/import` | To implement |
| `GET` | `/api/resources/export` | Future |

---

## Deliverables & File Map

> **⚠ Legacy SSDT project:** `HTResourceMapperDb.sqlproj` is legacy format — every new `.sql`
> file must be added to the `.sqlproj` with an explicit `<Build Include>` entry.
> The **top-level** `Tables\` and `Stored Procedures\` folders are the source of truth;
> the nested `HTResourceMapperDb/HTResourceMapper/` folder tree is a stale duplicate not
> referenced by the project — do not add files there.

### Library changes (prerequisite)

| File | Change |
|------|--------|
| `Libraries/SqlClient/HT.Microsoft.SqlClient.Extensions/SqlParametersExtensions.SqlCommandExt.cs` | Add `AddTvp(name, typeName, DataTable)` (SqlDbType.Structured); add `AddVarchar` overload with `ParameterDirection`; add `ReadString(this SqlCommand, parameterName)` |

### New files to create

| File | Purpose |
|------|---------|
| `Modules/Common/ResourceMapper.Common.Shared/Import/Contracts/ImportContract.cs` | Request + response DTOs |
| `Modules/Common/ResourceMapper.Common.Server/Resources/Interfaces/IImportService.cs` | Service interface |
| `Modules/Common/ResourceMapper.Common.Server/Resources/Interfaces/IImportRepository.cs` | Repository interface |
| `Modules/Common/ResourceMapper.Common.Server/Resources/ImportService.cs` | Service implementation |
| `Modules/Common/ResourceMapper.Common.Server/Resources/ImportSqlRepository.cs` | SQL repository |
| `UI/ResourceMapper.UI.Server/Controllers/Api/ResourceImportController.cs` | Controller |
| `Database/HTResourceMapperDb/User Defined Types/ResourceKeyList.sql` | TVP type: `(ResourceKey NVARCHAR(250))` |
| `Database/HTResourceMapperDb/User Defined Types/TagKeyValueList.sql` | TVP type: `(TagDefinitionKey NVARCHAR(50), TagValue NVARCHAR(2000))` |
| `Database/HTResourceMapperDb/Stored Procedures/TagDefinition_GetAll.sql` | Pre-validation lookup |
| `Database/HTResourceMapperDb/Stored Procedures/Resource_GetKeysByKeys.sql` | Bulk key existence check (TVP) |
| `Database/HTResourceMapperDb/Stored Procedures/ResourceType_Upsert.sql` | |
| `Database/HTResourceMapperDb/Stored Procedures/TagDefinition_Upsert.sql` | Auto-registers contentType |
| `Database/HTResourceMapperDb/Stored Procedures/Resource_Upsert.sql` | Resolves TypeName internally |
| `Database/HTResourceMapperDb/Stored Procedures/ResourceTag_SetForResource.sql` | Replaces all tags for a resource |
| `Database/HTResourceMapperDb/Stored Procedures/ResourceDependency_SetForResource.sql` | Replaces all deps for a resource |
| `bruno/bruno.json` | Bruno collection manifest (auto-discovered by VS Code extension) |
| `bruno/import-upsert.bru` | Happy path request |
| `bruno/import-skip.bru` | Skip existing resources |
| `bruno/import-fail.bru` | Conflict → error |
| `bruno/import-validation.bru` | Schema/reference error |
| `bruno/import-empty.bru` | Empty resources array |
| `_Tests/Modules/Common/ResourceMapper.Common.Server.Tests/ImportServiceTests.cs` | Unit tests |

### Files to modify

| File | Change |
|------|--------|
| `Modules/Common/ResourceMapper.Common.Server/Utils/DependencyRegistration.cs` | Register `IImportService` + `IImportRepository` |
| `Database/HTResourceMapperDb/HTResourceMapperDb.sqlproj` | `<Build Include>` entries for every new `.sql` file |

### Already exists (no action)

| File | Note |
|------|------|
| `Database/HTResourceMapperDb/Stored Procedures/ResourceType_GetAll.sql` | Used as-is for the pre-validation lookup |

---

## Step 0 — Library: TVP + string output parameter support

In `SqlParametersExtensions.SqlCommandExt.cs`:

```csharp
public static SqlCommand AddTvp(this SqlCommand command, string parameterName,
    string typeName, DataTable rows)
{
    var p = command.Parameters.Add(parameterName, SqlDbType.Structured);
    p.TypeName = typeName;   // e.g. "[HTResourceMapper].[ResourceKeyList]"
    p.Value = rows;
    return command;
}

public static SqlCommand AddVarchar(this SqlCommand command, string parameterName,
    string? parameterValue, int size, ParameterDirection direction)
{ /* SqlDbType.VarChar with explicit size + direction */ }

public static string? ReadString(this SqlCommand command, string parameterName)
{ /* read output param value, DBNull → null */ }
```

---

## Step 1 — Shared Contracts

**`Modules/Common/ResourceMapper.Common.Shared/Import/Contracts/ImportContract.cs`**

```csharp
public class ImportRequest
{
    public string Version { get; set; } = "1.0";
    public ImportPolicy Policy { get; set; } = new();
    public Dictionary<string, ImportResourceTypeDefinition>? ResourceTypes { get; set; }
    public Dictionary<string, ImportTagDefinitionModel>? TagDefinitions { get; set; }
    public List<ImportResourceItem> Resources { get; set; } = [];
}

public class ImportPolicy
{
    public string OnConflict { get; set; } = "upsert"; // "upsert" | "skip" | "fail"
}

public class ImportResourceTypeDefinition
{
    public bool AllowCustomTags { get; set; } = true;
}

public class ImportTagDefinitionModel
{
    public string ContentType { get; set; } = "string";   // free-form, max 50 chars
    public bool AllowCustomValue { get; set; } = true;
    public bool IsMultiValued { get; set; } = false;
    public List<string>? AllowedValues { get; set; }
}

public class ImportResourceItem
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Type { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, JsonElement>? Tags { get; set; }  // string or array — normalized in service
    public List<string>? Dependencies { get; set; }
}

public class ImportResponse
{
    public bool Success { get; set; }
    public ImportSummary? Summary { get; set; }
    public List<ImportError>? Errors { get; set; }
}

public class ImportSummary
{
    public ImportSectionSummary ResourceTypes { get; set; } = new();
    public ImportSectionSummary TagDefinitions { get; set; } = new();
    public ImportSectionSummary Resources { get; set; } = new();
}

public class ImportSectionSummary
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
}

public class ImportError
{
    public string Section { get; set; } = "";
    public string? Key { get; set; }
    public string? Field { get; set; }
    public string Message { get; set; } = "";
}
```

**Tag deserialization:** `Dictionary<string, JsonElement>` preserves raw JSON.
Service normalizes: `JsonValueKind.String` → `["value"]`, `JsonValueKind.Array` → `["v1","v2"]`.

**Case-insensitivity:** JSON deserialization produces case-sensitive dictionaries, so
`"Env"` and `"env"` can coexist in the payload. The service first detects case-only
duplicate keys (validation error), then re-wraps each dictionary with
`StringComparer.OrdinalIgnoreCase` for all subsequent lookups.

---

## Step 2 — Repository Interface & Stored Procedures

**`Modules/Common/ResourceMapper.Common.Server/Resources/Interfaces/IImportRepository.cs`**

```csharp
public interface IImportRepository
{
    // Pre-validation lookups (read-only)
    Task<List<ResourceType>> GetAllResourceTypesAsync(CancellationToken ct);
    Task<List<TagDefinition>> GetAllTagDefinitionsAsync(CancellationToken ct);
    Task<HashSet<string>> GetExistingResourceKeysByKeysAsync(IEnumerable<string> keys, CancellationToken ct);

    // Write operations — called only after full validation passes.
    // onConflict here is 'upsert' | 'skip' only ('fail' is mapped to 'skip' by the service — decision 20)
    Task<string> UpsertResourceTypeAsync(string typeName, bool allowCustomTags,
        string onConflict, CancellationToken ct);                       // returns 'created'|'updated'|'skipped'

    Task<string> UpsertTagDefinitionAsync(string key, ImportTagDefinitionModel def,
        string onConflict, CancellationToken ct);                       // returns 'created'|'updated'|'skipped'

    Task<(string Result, int ResourceId)> UpsertResourceAsync(ImportResourceItem resource,
        string resourceUid, string onConflict, CancellationToken ct);   // ResourceId returned even on skip

    Task SetResourceTagsAsync(int resourceId,
        IReadOnlyList<(string TagKey, string TagValue)> tags, CancellationToken ct);

    Task SetResourceDependenciesAsync(int resourceId,
        IReadOnlyList<string> dependencyKeys, CancellationToken ct);
}
```

> **No cross-call transaction.** Validation is fully completed before any write is attempted.
> Write volume is low enough that partial-write on an unexpected exception is an accepted risk.
> Summary counts are accumulated in C# from the per-call results; the repository exposes
> per-item operations, the service owns the loop.
>
> Repository call pattern (matches existing `ResourceSqlRepository`):
> ```csharp
> using var cmd = _db.Execute.SprocCommand("[HTResourceMapper].Resource_Upsert")
>     .AddNVarchar("@ResourceKey", resource.Key)
>     ...
>     .AddVarchar("@Result", null, 10, ParameterDirection.Output)
>     .AddInteger("@ResourceId", 0, ParameterDirection.Output);
> await _db.Execute.ExecuteNonQueryAsync(cmd, ct);
> var result = cmd.ReadString("@Result");
> var resourceId = cmd.ReadInt("@ResourceId");
> ```

### User-Defined Table Types

**`ResourceKeyList.sql`**
```sql
CREATE TYPE [HTResourceMapper].[ResourceKeyList] AS TABLE (
    [ResourceKey] NVARCHAR(250) NOT NULL
);
```

**`TagKeyValueList.sql`**
```sql
CREATE TYPE [HTResourceMapper].[TagKeyValueList] AS TABLE (
    [TagDefinitionKey] NVARCHAR(50) NOT NULL,
    [TagValue] NVARCHAR(2000) NULL
);
```

### Stored Procedures

All upsert sprocs use the **pre-check IF/ELSE pattern** (decision 15) — no MERGE.
`@OnConflict` is `'upsert'` or `'skip'`; anything else raises an error.

**`TagDefinition_GetAll.sql`** — SELECT all TagDefinition rows joined to TagContentType
(TagDefinitionKey, TagDefinitionUid, TagCode AS ContentType, AllowCustomValue, IsMultiValued, AllowedValues).

**`Resource_GetKeysByKeys.sql`** — `@Keys [HTResourceMapper].[ResourceKeyList] READONLY`;
returns the subset of keys that exist in `Resource`. Used for bulk conflict pre-check.

**`ResourceType_Upsert.sql`**
```sql
-- @TypeName NVARCHAR(250), @ResourceTypeUid VARCHAR(40) (used only on create),
-- @AllowCustomTags BIT, @OnConflict VARCHAR(10), @Result VARCHAR(10) OUTPUT
DECLARE @Id INT;
SELECT @Id = ResourceTypeId FROM [HTResourceMapper].ResourceType WHERE TypeName = @TypeName;

IF @Id IS NULL
BEGIN
    INSERT INTO [HTResourceMapper].ResourceType (ResourceTypeUid, TypeName, AllowCustomTags)
    VALUES (@ResourceTypeUid, @TypeName, @AllowCustomTags);
    SET @Result = 'created';
END
ELSE IF @OnConflict = 'upsert'
BEGIN
    UPDATE [HTResourceMapper].ResourceType
    SET AllowCustomTags = @AllowCustomTags, UpdatedOn = SYSUTCDATETIME()
    WHERE ResourceTypeId = @Id;
    SET @Result = 'updated';
END
ELSE
    SET @Result = 'skipped';
```

**`TagDefinition_Upsert.sql`** — same IF/ELSE pattern on `TagDefinitionKey`, plus
contentType auto-registration (decision 13). `TagContentTypeId` has no IDENTITY, so
new registry rows use `MAX+1`:
```sql
-- @TagDefinitionKey NVARCHAR(50), @TagDefinitionUid VARCHAR(40), @ContentType VARCHAR(50),
-- @AllowCustomValue BIT, @IsMultiValued BIT, @AllowedValues NVARCHAR(2000) (JSON array or NULL),
-- @OnConflict VARCHAR(10), @Result VARCHAR(10) OUTPUT
DECLARE @ContentTypeId INT;
SELECT @ContentTypeId = TagContentTypeId
FROM [HTResourceMapper].TagContentType WHERE TagCode = @ContentType;

IF @ContentTypeId IS NULL
BEGIN
    SELECT @ContentTypeId = ISNULL(MAX(TagContentTypeId), -1) + 1
    FROM [HTResourceMapper].TagContentType;
    INSERT INTO [HTResourceMapper].TagContentType (TagContentTypeId, TagCode)
    VALUES (@ContentTypeId, @ContentType);
END
-- ...then IF/ELSE upsert on TagDefinitionKey as above
```

**`Resource_Upsert.sql`** — resolves `@TypeName` internally (decision 14); returns
`@ResourceId` in **all** cases including skip (decision 15/16):
```sql
-- @ResourceKey NVARCHAR(250), @ResourceUid VARCHAR(40) (used only on create),
-- @TypeName NVARCHAR(250) NULL, @ResourceName NVARCHAR(250), @Description NVARCHAR(2000) NULL,
-- @OnConflict VARCHAR(10), @ResourceId INT OUTPUT, @Result VARCHAR(10) OUTPUT
DECLARE @ResourceTypeId INT =
    (SELECT ResourceTypeId FROM [HTResourceMapper].ResourceType WHERE TypeName = @TypeName);

SELECT @ResourceId = ResourceId
FROM [HTResourceMapper].[Resource] WHERE ResourceKey = @ResourceKey;

IF @ResourceId IS NULL
BEGIN
    INSERT INTO [HTResourceMapper].[Resource]
        (ResourceUid, ResourceKey, ResourceTypeId, ResourceName, [Description])
    VALUES (@ResourceUid, @ResourceKey, @ResourceTypeId, @ResourceName, @Description);
    SET @ResourceId = SCOPE_IDENTITY();
    SET @Result = 'created';
END
ELSE IF @OnConflict = 'upsert'
BEGIN
    UPDATE [HTResourceMapper].[Resource]
    SET ResourceName = @ResourceName, [Description] = @Description,
        ResourceTypeId = @ResourceTypeId, UpdatedOn = SYSUTCDATETIME()
    WHERE ResourceId = @ResourceId;
    SET @Result = 'updated';
END
ELSE
    SET @Result = 'skipped';   -- @ResourceId already holds the existing row's id
```

**`ResourceTag_SetForResource.sql`** — sproc-local transaction so a failed insert
can't leave the resource with zero tags:
```sql
-- @ResourceId INT, @Tags [HTResourceMapper].[TagKeyValueList] READONLY
BEGIN TRAN;
DELETE FROM [HTResourceMapper].[ResourceTag] WHERE ResourceId = @ResourceId;
INSERT INTO [HTResourceMapper].[ResourceTag] (ResourceId, TagDefinitionId, TagValue)
SELECT @ResourceId, td.TagDefinitionId, t.TagValue
FROM @Tags t
JOIN [HTResourceMapper].[TagDefinition] td ON td.TagDefinitionKey = t.TagDefinitionKey;
COMMIT;
```
(The JOIN cannot drop rows: validation guarantees every tag key has a definition — decision 11.)

**`ResourceDependency_SetForResource.sql`** — same local-transaction pattern:
```sql
-- @ResourceId INT, @DependencyKeys [HTResourceMapper].[ResourceKeyList] READONLY
BEGIN TRAN;
DELETE FROM [HTResourceMapper].[ResourceDependency] WHERE ResourceId = @ResourceId;
INSERT INTO [HTResourceMapper].[ResourceDependency] (ResourceId, DependencyResourceId)
SELECT @ResourceId, r.ResourceId
FROM @DependencyKeys dk
JOIN [HTResourceMapper].[Resource] r ON r.ResourceKey = dk.ResourceKey;
COMMIT;
```

---

## Step 3 — Service Logic

**`Modules/Common/ResourceMapper.Common.Server/Resources/Interfaces/IImportService.cs`**

```csharp
public interface IImportService
{
    Task<ApiServiceResponse<ImportResponse>> ImportAsync(
        ImportRequest request, CancellationToken cancellationToken);
}
```

### `ImportService.ImportAsync` flow

All key sets/dictionaries below use `StringComparer.OrdinalIgnoreCase` (decision 18).

```
1. Schema validation (no DB calls)
   • version present and == "1.0"
   • policy.onConflict ∈ {"upsert","skip","fail"}
   • each resource: key non-empty, name non-empty
   • no duplicate keys within the payload — including keys differing only by case,
     across resources, resourceTypes, and tagDefinitions sections independently
   • contentType length ≤ 50
   • allowedValues JSON-array serialization ≤ 2000 chars
   • no resource depends on itself
   → any error: return 400 with full error list, nothing committed

2. Load DB reference data (read-only)
   • GetAllResourceTypesAsync()               → existingTypeNames (+ AllowCustomTags)
   • GetAllTagDefinitionsAsync()              → existingTagDefs (key → def)
   • GetExistingResourceKeysByKeysAsync(...)  → existingResourceKeys

3. Reference validation
   • resource.type → must be in import.resourceTypes OR existingTypeNames
   • resource.dependencies[i] → must be in import resource keys OR existingResourceKeys
   • every resource tag key → must be in import.tagDefinitions OR existingTagDefs
     (no AllowCustomTags exemption — decision 11)
   • tag value conformance, using the import definition when present, else the DB definition:
       – array value for a definition with IsMultiValued = false → error
       – any value not in AllowedValues when AllowCustomValue = false → error
   → any error: return 400, nothing committed

4. Conflict validation (only when onConflict == "fail")
   • Any resourceType key already in existingTypeNames → conflict error
   • Any tagDefinition key already in existingTagDefs  → conflict error
   • Any resource key already in existingResourceKeys  → conflict error
   → any error: return 400, nothing committed

5. Execute writes sequentially (no cross-call transaction)
   • effectiveOnConflict = policy == "fail" ? "skip" : policy   (decision 20)
   • Upsert resource types (if section present), tallying results
   • Upsert tag definitions (if section present), tallying results
   • Resources — two passes:
       Pass 1: per resource → Resource_Upsert; if result != 'skipped',
               ResourceTag_SetForResource (skip-means-skip — decision 16)
       Pass 2: per non-skipped resource with dependencies →
               ResourceDependency_SetForResource (all referenced rows now exist)
   • return 200 with summary (tallied in C#)
   • on unexpected exception → return 500 (partial writes are an accepted risk)
```

**Key implementation details:**
- Use `ServiceResponseBuilder<ImportResponse>` (matches existing pattern in `ResourceService`)
- On validation failure: `builder.Data.Set(new ImportResponse { Success = false, Errors = ... })`
  **plus** `builder.Validation.AddValidation("import", "Import failed validation; see data.errors")`
  — the wrapper message drives the 400, `ImportResponse.Errors` carries the detail (decision 10)
- Normalize `JsonElement` tag values to `List<string>` via a private helper
- UID generation (decision 19): resources `$"res-{Guid.NewGuid():N}"`,
  resource types `$"rt-{Guid.NewGuid():N}"`, tag definitions `$"td-{Guid.NewGuid():N}"`
- `AllowedValues` persisted as a JSON array string (e.g. `["dev","int"]`)
- Collect all validation errors before returning (don't stop at first error)

---

## Step 4 — Controller

**`UI/ResourceMapper.UI.Server/Controllers/Api/ResourceImportController.cs`**

```csharp
[Route("api/resources")]
[Produces("application/json")]
[Tags("Resource Import")]
public class ResourceImportController : ApiControllerBase
{
    private readonly IImportService _svc;

    public ResourceImportController(IImportService service) => _svc = service;

    [HttpPost("import")]
    [ProducesResponseType(typeof(ApiResponse<ImportResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<ImportResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<ImportResponse>), 500)]
    public async Task<ActionResult<ApiResponse<ImportResponse>>> Import(
        [FromBody] ImportRequest request,
        CancellationToken cancellationToken = default)
    {
        var svcResponse = await _svc.ImportAsync(request, cancellationToken);
        return MapServiceResponseToActionResult(svcResponse);
    }
}
```

---

## Step 5 — Dependency Registration

Add to `DependencyRegistration.RegisterCommonDependencies()`:

```csharp
services.TryAddScoped<IImportRepository, ImportSqlRepository>();
services.TryAddScoped<IImportService, ImportService>();
```

---

## Step 6 — Bruno Collection

Location: `bruno/` at solution root (VS Code Bruno extension discovers via `bruno.json`).

```
bruno/
  bruno.json              ← collection manifest; baseUrl = http://localhost:5000
  import-upsert.bru       ← happy path, onConflict: upsert
  import-skip.bru         ← existing resources, onConflict: skip
  import-fail.bru         ← triggers conflict error, onConflict: fail
  import-validation.bru   ← resource references unknown type → 400
  import-empty.bru        ← resources: [], valid no-op → 200 all zeros
```

---

## Step 7 — Tests

**`_Tests/Modules/Common/ResourceMapper.Common.Server.Tests/ImportServiceTests.cs`**

| Test method | Scenario | Expected |
|-------------|----------|---------|
| `ImportAsync_NullVersion_ReturnsValidationError` | missing version field | 400 |
| `ImportAsync_InvalidOnConflict_ReturnsValidationError` | onConflict = `"merge"` | 400 |
| `ImportAsync_DuplicateKeysInPayload_ReturnsValidationError` | two resources same key | 400 |
| `ImportAsync_DuplicateKeysDifferingByCase_ReturnsValidationError` | `"Key-A"` + `"key-a"` | 400 |
| `ImportAsync_SelfDependency_ReturnsValidationError` | resource depends on itself | 400 |
| `ImportAsync_UnknownResourceType_ReturnsReferenceError` | type not in section or DB | 400 |
| `ImportAsync_UnknownDependency_ReturnsReferenceError` | dep key not in import or DB | 400 |
| `ImportAsync_UnknownTagKey_ReturnsReferenceError` | tag key has no definition anywhere | 400 |
| `ImportAsync_ArrayValueForSingleValuedTag_ReturnsValidationError` | array on IsMultiValued=false | 400 |
| `ImportAsync_ValueNotInAllowedValues_ReturnsValidationError` | AllowCustomValue=false, value off-list | 400 |
| `ImportAsync_ConflictWithFailPolicy_ReturnsConflictError` | existing key + fail | 400 |
| `ImportAsync_UpsertPolicy_CallsRepositoryAndReturnsSuccess` | happy path upsert | 200 with summary |
| `ImportAsync_SkipPolicy_SkipsExistingResources` | 1 existing + 1 new | 200, created:1 skipped:1 |
| `ImportAsync_SkippedResource_TagsAndDependenciesNotTouched` | repo returns 'skipped' | Set* methods never called for it |
| `ImportAsync_KeysDifferingInCaseFromDb_MatchExisting` | DB has `"My-Key"`, import has `"my-key"` | treated as match, not create |
| `ImportAsync_EmptyResources_ReturnsSuccessWithZeros` | `resources: []` | 200, all zeros |
| `ImportAsync_RepositoryThrows_ReturnsInternalError` | repo throws during write phase | 500 (partial writes may have occurred) |

Mock `IImportRepository` with Moq. Test service in isolation.

---

## Implementation Order

### Phase 1 — Walking skeleton (no DB)

Prove the route, model binding, DI wiring, and Bruno tooling end-to-end before any real logic.

1. `ImportContract.cs` — shared models (controller needs them to bind)
2. `IImportService` + stub `ImportService` that ignores the request and returns a **fixed,
   hardcoded success `ImportResponse`** (canned summary counts)
3. `ResourceImportController` + `DependencyRegistration` update (service only; no repository yet)
4. `bruno/bruno.json` + `bruno/import-upsert.bru`
5. **Verify:** `dotnet run` → run `import-upsert.bru` → 200 with the canned summary

### Phase 2 — Validation (still no DB writes)

6. Schema validation (flow step 1) in `ImportService`; error response shape per decision 10
7. `ImportServiceTests` — validation test rows
8. **Verify:** `import-validation.bru` → 400 with error list

### Phase 3 — Database layer

9. Library: TVP + string output param support (`SqlParametersExtensions.SqlCommandExt.cs`)
10. User-defined table types + stored procedures (+ `.sqlproj` entries)
11. `IImportRepository` + `ImportSqlRepository`; register in DI
12. Reference/conflict validation (flow steps 2–4) + write phase (flow step 5) replace the stub
13. Remaining `ImportServiceTests` rows; remaining Bruno files

---

## Verification Steps (full implementation)

1. Deploy DB project from Visual Studio (SSDT) — new types + sprocs
2. `dotnet run --project UI/ResourceMapper.UI.Server`
3. Bruno: run `import-upsert.bru` → expect 200, `created` counts
4. Bruno: run `import-upsert.bru` again → expect 200, `updated` counts
5. Bruno: run `import-skip.bru` → expect 200, `skipped` on second resource
6. Bruno: run `import-fail.bru` → expect 400, re-check grid to confirm nothing changed
7. Bruno: run `import-validation.bru` → expect 400 reference error
8. Bruno: run `import-empty.bru` → expect 200, all zeros
9. `dotnet test --filter "FullyQualifiedName~ImportService"`
