# Slice #4 — Services & API

## Context

Fourth implementation slice of the refreshed resource model (design:
`docs/plans/editor/resource-logical-model-and-editor-ux.md`; list:
`docs/plans/editor/00-implementation-plan-list.md`). Slices #1 (schema), #2 (sprocs +
repository), and #3 (contracts & DTOs) are done. This slice makes the **service layer**
real: it turns the slice-#3 transport shapes into working operations the editor/details UI
(#6–9) binds to **in-process** — **get, create/update, delete, uniqueness-check, tag-definition
inline create, and relationship CRUD**. This is a **server-hosted Blazor** app: components inject
`IResourceService` and call it directly (Program.cs:18 — "the UI calls services in-process"),
so slice #4 adds **no HTTP controllers** (see the decision below).

It also closes the read gap flagged in slice #3 (a resource's applied tags had no read path)
and adds the small set of new/extended **sprocs** those operations require. This is the layer
slice #6 (General tab + create/edit/view) consumes first; the Tags (#7) and Dependencies (#8)
tabs build on the tag/relationship surface stubbed and wired here.

**Key decisions (this session):**
- **Editor writes are keyed by `ResourceUid` (the immutable anchor), not by `(type+key)`.** The
  import path (`Resource_Upsert`) matches by `(type+key)` for idempotency — but the editor lets
  the user *rename* `Key`, so an upsert-by-key would wrongly insert a duplicate on rename. Slice
  #4 adds a **`Resource_Save`** sproc keyed by `@ResourceUid` (insert-if-absent-else-update; the
  editor supplies a client-generated Guid for Create). Import keeps `Resource_Upsert`; the two
  write paths stay separate.
- **Domain stays deferred to #5.** Uniqueness is checked on `(type + key)` only; the editor
  accepts a Domain value but slice #4 does **not** persist it as a tag (that, plus
  `Resource_CheckUnique`/`ResourceTag_SetForResource` domain-safety, lands in #5).
- **Applied-tag *writing* and relationship *save-reconciliation* are deferred to #7/#8.** Slice
  #4 persists **General-tab fields + primary link** on save, reads tags/relationships for
  detail/edit-load, and exposes **discrete relationship add/remove** (per the master list's
  "relationship CRUD"). The `SaveResourceRequest` shape is defined forward-looking (carries tag
  + edge lists) but slice #4 persists only General + primary; tag/edge persistence-on-save is
  wired in #7/#8. This keeps save single-sproc (no multi-sproc transaction needed yet).
- **No HTTP controllers this slice.** Because the UI is server-hosted Blazor calling services
  in-process, editor CRUD has no external caller — a controller would be dead surface. The lone
  existing `ResourceImportController` stays (bulk import is plausibly driven externally via
  Bruno/CI); the editor operations do not get one. If an external driver ever appears, thin
  `ApiControllerBase` controllers wrapping these same services can be added then (they'd just call
  `MapServiceResponseToActionResult(...)`).

## SQL — new / changed sprocs (`Database\HTResourceMapperDb\Stored Procedures\`)

Add `<Build>` items to `HTResourceMapperDb.sqlproj` for each new file (old-style project — items
are explicit, no globbing). Declarative SSDT: build the dacpac via VS/full MSBuild, publish via
SqlPackage **with `/p:DropObjectsNotInSource=True`** (the slice-#2 note — the profile omits it).

- **`Resource_Save.sql`** (NEW) — the editor's uid-keyed create/update:
  `@ResourceUid VARCHAR(40), @ResourceTypeId INT, @ResourceKey NVARCHAR(250),
  @ResourceName NVARCHAR(250), @Description NVARCHAR(2000)=NULL,
  @PrimaryTagDefinitionId INT=NULL, @ResourceId INT OUTPUT, @Result VARCHAR(10) OUTPUT`
  (`'created'|'updated'|'error'`). Find by `@ResourceUid`. **Absent** → INSERT (all fields incl.
  primary). **Present** → UPDATE name/key/description/primary + `UpdatedOn`; **`Type` is frozen**
  — if the incoming `@ResourceTypeId` differs from the stored one, set `@Result='error'` and
  return (defense-in-depth; the service also enforces). Does not touch tags/edges.
- **`ResourceTag_GetForResource.sql`** (NEW — closes the slice-#3 gap) — `@ResourceId INT`;
  returns one row per applied `ResourceTag` joined to `TagDefinition` (+ `TagContentType`):
  `TagDefinitionId, TagDefinitionKey, DisplayName, ContentType (TagCode), TagValue,
  IsSystemTag, IsMultiValued`, and `IsPrimary` (`= 1` when the resource's `PrimaryTagDefinitionId`
  matches). Feeds detail-get and edit-load; `IsSystemTag` lets #5/#7 preserve system tags.
- **`Resource_CheckUnique.sql`** (NEW) — `@ResourceTypeId INT, @ResourceKey NVARCHAR(250),
  @ExcludeResourceUid VARCHAR(40)=NULL, @IsUnique BIT OUTPUT`. `@IsUnique=0` iff another row
  shares `(ResourceTypeId, ResourceKey)` excluding the row whose `ResourceUid=@ExcludeResourceUid`.
  Domain joins this predicate in #5.
- **`TagDefinition_Upsert.sql`** (EXTEND) — add `@TagDefinitionId INT OUTPUT` set from
  `SCOPE_IDENTITY()` on insert / the found id on update/skip, so inline-create can return the new
  definition's id without a re-fetch. Import (`ImportSqlRepository.UpsertTagDefinitionAsync`)
  simply doesn't read the new output — **no import ripple**.

## C# — server model (`...\Resources\Models\`)

- **`ResourceTagRead.cs`** (NEW) — read shape for `ResourceTag_GetForResource`:
  `TagDefinitionId, TagDefinitionKey, DisplayName, ContentType, TagValue, IsSystemTag,
  IsMultiValued, IsPrimary`. (Kept distinct from the EF `ResourceTag` entity; projected to
  `ResourceTagModel` in the service.)

## Repository (`IResourceRepository` + `ResourceSqlRepository`)

Add, using the established `SprocCommand`/`.Add*`/`ExecuteRowAsync`/`ExecuteQueryAsync`/
`ExecuteNonQueryAsync`/`dr.Read*` helpers (existing `GetResourceByUidAsync`,
`GetRelationshipsForResourceAsync`, `Add/RemoveRelationshipAsync`, `DeleteResourceAsync`,
`GetAllResourceTypesAsync` stay as-is):

- `SaveResourceAsync(uid, typeId, key, name, description, primaryTagDefinitionId, ct)` →
  `(string Result, int ResourceId)` via `Resource_Save`.
- `GetTagsForResourceAsync(resourceId, ct)` → `List<ResourceTagRead>` via `ResourceTag_GetForResource`.
- `CheckResourceUniqueAsync(typeId, key, excludeUid, ct)` → `bool` via `Resource_CheckUnique`.
- `GetAllTagDefinitionsAsync(ct)` → `List<TagDefinition>` (dictionary; mirror the mapping already
  in `ImportSqlRepository.GetAllTagDefinitionsAsync` over `TagDefinition_GetAll`).
- `CreateTagDefinitionAsync(key, uid, contentType, allowCustomValue, isMultiValued,
  allowedValuesJson, displayName, requirementLevel, displayOrder, ct)` →
  `(string Result, int TagDefinitionId)` via `TagDefinition_Upsert` (**forces
  `@IsDomainTag=0, @IsSystemTag=0`**, `@OnConflict='skip'`).

## Service (`IResourceService` + `ResourceService`)

All new methods return `ApiServiceResponse<T>` built with `ServiceResponseBuilder<T>` (mirror the
existing `GetResourceGridItems` validation/try-catch/`builder.Data.Set`/`BuildResponse` pattern;
`builder.Validation.AddValidation` for field errors, `builder.Errors.AddError(CallStatusCode.…)`
for not-found/internal). Add a small `Slug`/uid helper as needed (uid = `Guid.NewGuid()`, as
`GetResourceEditorModelAsync` already does).

- **`GetResourceDetailAsync(uid, ct)` → `ResourceDetailModel`** — `GetResourceByUidAsync` (404 via
  `AddError(NotFound,…)` if null) + `GetTagsForResourceAsync` + `GetRelationshipsForResourceAsync`
  + resolve `ResourceTypeName` (from `GetAllResourceTypesAsync`, cached per call). Project tags →
  `ResourceTagModel`, relationships → `ResourceRelationshipModel`; compute `PrimaryLinkUrl`
  (the primary tag's value) and `IdentityDisplay` (`type / key`; domain added #5; `Domain` stays
  null until #5).
- **`GetResourceEditorModelAsync(request, ct)` → `OpenEditorResponse`** (EXPAND the slice-#3
  skeleton):
  - **Create**: skeleton editor (new Guid uid, empty `SingleValueEditor`s, `Mode="Create"`,
    `IsPersisted=false`).
  - **Edit/View**: load via the detail path → populate `ResourceEditorModel` — scalars with
    `OriginalValue==EditedValue` (Type/Domain/Name/Key/Description), `IsPersisted=true`,
    `Mode` from request, `PrimaryTagDefinitionId`, `CreatedOn/UpdatedOn`, `IdentityPreview`, tag
    rows (`TagRowEditor` composing each tag's `TagDefinitionModel` from the dictionary),
    dependency rows (`DependencyRowEditor` from relationships split by `Direction`).
  - Always populate `ResourceTypes`, `TagDictionary` (all defs → `TagDefinitionModel`,
    `AllowedValues` JSON parsed here), and `DomainAllowedValues` (the `IsDomainTag` def's parsed
    `AllowedValues`). **`EntryPointTemplates` returns empty** in #4 (no `ResourceTypeTag` read/seed
    yet — populated in #7).
- **`SaveResourceAsync(SaveResourceRequest, ct)` → `SaveResourceResponse`** — validate: Type
  required; Name/Key required; Key format (slug-safe); on Edit, Type unchanged; **uniqueness**
  (`CheckResourceUniqueAsync`, excluding self by uid) → inline validation error on collision.
  Then `SaveResourceAsync` (General + primary). **Persists General + primary only**; tag/edge
  lists on the request are accepted but not persisted in #4 (see Out of scope). Returns
  `{ ResourceUid, Message="Saved {name}", Saved = projected ResourceDetailModel }`.
- **`CheckUniquenessAsync(ResourceUniquenessRequest, ct)` → `ResourceUniquenessResponse`** —
  `CheckResourceUniqueAsync(type,key,excludeUid)`; `IdentityDisplay="type / key"` (domain #5).
- **`DeleteResourceAsync(uid, ct)`** — resolve uid → id (404 if absent) → `DeleteResourceAsync`
  (cascades edges + tags via the slice-#2 sproc). The dependents list for the confirm dialog is
  read by the UI (#9) via the detail/relationships path before calling delete.
- **`CreateTagDefinitionAsync(CreateTagDefinitionRequest, ct)` → `TagDefinitionModel`** — validate
  key + `ContentType ∈ {Text,Link}` + `AllowedValues` well-formed; generate uid; call
  `CreateTagDefinitionAsync` (domain/system forced off). If the key already exists
  (`Result='skipped'`, search-first duplicate), return the existing definition. Return the created
  (or existing) `TagDefinitionModel`.
- **`GetTagDictionaryAsync(ct)` → `List<TagDefinitionModel>`** — `GetAllTagDefinitionsAsync`
  projected (for the "Add tag" picker / refresh after inline create).
- **`AddRelationshipAsync(fromUid, toUid, ct)` / `RemoveRelationshipAsync(fromUid, toUid, ct)`** —
  resolve both uids → ids (validation error if either missing), reject self-loops, then the
  existing repo `Add/RemoveRelationshipAsync`.

## Contracts (`Common.Shared/Editor/Contracts/`) — new for #4

- **`SaveResourceContract.cs`** — `SaveResourceRequest` (`Mode`, `ResourceUid`, `ResourceTypeId`,
  `Domain?`, `ResourceName`, `ResourceKey`, `Description?`, `PrimaryTagDefinitionId?`, and
  forward-looking `List<SaveTagValue> Tags` {`TagDefinitionId`, `TagDefinitionKey`, `Value`} +
  `List<string> DependsOnUids` + `List<string> DependentOnUids`) and `SaveResourceResponse`
  (`ResourceUid`, `Message`, `ResourceDetailModel? Saved`).
- **`TagDefinitionCreateContract.cs`** — `CreateTagDefinitionRequest` (`TagDefinitionKey`,
  `DisplayName?`, `ContentType`, `AllowCustomValue`, `IsMultiValued`, `AllowedValues?`,
  `RequirementLevel`, `DisplayOrder`) — the inline-create input (no domain/system flags).

(`OpenEditorContract`, `ResourceIdentityContract`, `ResourcePickerContract` from slice #3 are
reused; the picker's backing sproc + service stay deferred to #8. Relationship add/remove take
two uids directly — no request contract needed for an in-process call.)

## Dependency registration

No change — `IResourceService`/`IResourceRepository` are already registered in
`DependencyRegistration.RegisterCommonDependencies`; the new methods live on existing types.

## Tests (`_Tests\Common\Server\ResourceMapper.Common.Server.Tests\`)

Add `ResourceService` unit tests (xUnit + Moq 4.18.4 + FluentAssertions 7.1.0, `[Trait("Category",
"Unit")]`, `Method_State_Expected` naming, `because` clauses) with a mocked `IResourceRepository`:
- `GetResourceDetailAsync` — not-found → `NotFound`; happy path projects tags/relationships +
  computes `PrimaryLinkUrl`/`IdentityDisplay`.
- `SaveResourceAsync` — missing type/name/key → validation; collision (mock
  `CheckResourceUniqueAsync=false`) → validation; happy path calls `SaveResourceAsync` and returns
  the toast message; Edit with changed type → error.
- `CheckUniquenessAsync` — unique vs collision.
- `CreateTagDefinitionAsync` — bad content type → validation; existing key → returns existing;
  new → returns created; asserts domain/system forced off.
- `AddRelationshipAsync` — missing endpoint → validation; self-loop → validation.

## Out of scope (later slices)

- **Domain** persistence + `Resource_CheckUnique`/`ResourceTag_SetForResource` domain-safety +
  `Resource_GetAllKeys` tuples → **#5**.
- **Applied-tag write on save** (`ResourceTag_SetForResource` orchestration, primary derived from
  applied Link tags, system-tag preservation, multi-sproc save transaction) + **`ResourceTypeTag`
  entry-point read/seed** (`EntryPointTemplates`) → **#7**.
- **Relationship save-reconciliation** (diffing `SaveResourceRequest` edge lists) + the
  **same-domain resource picker** (`ResourcePickerContract` + its sproc) → **#8**.
- All editor/details **UI** → **#6–9**.
- **HTTP controllers for editor CRUD** — not planned (server-hosted Blazor uses in-process
  services). Add thin `ApiControllerBase` wrappers only if an external caller ever needs them.

## Verification

1. **DB builds + publishes:** `MSBuild HTResourceMapperDb.sqlproj /t:Rebuild` (VS/full MSBuild) →
   dacpac, then `sqlpackage /Action:Publish … /p:DropObjectsNotInSource=True` to
   `(localdb)\MSSQLLocalDB\ResourceMapper`.
2. **Exercise the new sprocs via `sqlcmd`** (mirror slice #2's approach): seed a type + a couple
   tag defs; `Resource_Save` (create; update; **type-frozen error** on changed type);
   `ResourceTag_GetForResource` (after inserting tags — keys/contentType/IsPrimary correct);
   `Resource_CheckUnique` (unique; collision; excludes self by uid); `TagDefinition_Upsert`
   returns `@TagDefinitionId`.
3. **C# compiles + unit tests pass:** `dotnet build` the C# projects (not the sqlproj) and
   `dotnet test` the `Common.Server.Tests` project (existing 37 + the new `ResourceService` tests).
4. **In-process service smoke** (mirror slice #2's throwaway xUnit test against the live
   `(localdb)` DB, deleted before commit): resolve `IResourceService` and run the full flow —
   `SaveResourceAsync` (create → uid), `GetResourceDetailAsync` (detail; tags empty),
   `CheckUniquenessAsync` (collision on the just-saved key; unique for a new key),
   `CreateTagDefinitionAsync` (inline create → model; re-create same key → returns existing),
   `AddRelationshipAsync`/`RemoveRelationshipAsync` (between two saved resources),
   `DeleteResourceAsync` (gone; edges cascade). Confirms the full in-process surface the editor
   UI (#6–9) will bind to, ahead of those slices.

## Execution notes

- **All steps completed and verified.** DB dacpac built via full MSBuild + published via
  `sqlpackage /p:DropObjectsNotInSource=True`; all four sprocs (`Resource_Save`,
  `ResourceTag_GetForResource`, `Resource_CheckUnique`, `TagDefinition_Upsert`'s new output)
  exercised via `sqlcmd` — create/update/type-frozen-error, `IsPrimary` projection, unique/
  collision/self-exclude, and stable-id-on-reupsert all confirmed. `dotnet build` on the C#
  projects — zero `error CS` (the only build failure anywhere is the pre-existing, documented
  `HTResourceMapperDb.sqlproj` SSDT limitation under `dotnet build`, unrelated). `dotnet test` —
  37 existing + 14 new `ResourceService` tests, 51/51 passing. The in-process smoke test (a
  throwaway xUnit test against the live `(localdb)` DB, deleted before commit) ran the full
  save → detail → uniqueness → inline-tag-create → relationship add/remove → delete flow
  end-to-end successfully.
- **One addition beyond the original plan, made to close a real gap it left open:**
  `TagDefinitionModel.ContentType` needs the actual `"Text"`/`"Link"` code, but
  `TagDefinition_GetAll` only returned `TagContentTypeId` — there was no denormalized code to map
  from without either a SQL join or hardcoding the two seeded ids. Extended `TagDefinition_GetAll`
  with the same `INNER JOIN TagContentType` pattern `ResourceTag_GetForResource` already uses (an
  additive column; the existing `ImportSqlRepository` reader is unaffected since it selects
  columns by name) and added a plain `ContentType` property to the `TagDefinition` server model
  (safe — there is no EF Core `DbContext` anywhere in this codebase; the `[Table]`/`[Key]`
  attributes on these POCOs are vestigial, so adding a property has no ORM-mapping risk).
- **Found and fixed live during verification:** this exact sequencing bug — the dacpac was first
  published *before* the `TagDefinition_GetAll` extension was added, so the smoke test failed with
  `IndexOutOfRangeException: ContentType` until the dacpac was rebuilt and republished. Left as a
  reminder for slice #5+: **always republish after every sproc edit**, not just once at the end.
- **Scoping call:** the plan's "Key format (slug-safe)" validation was **not** implemented as a
  regex. No precedent for a slug-format check exists anywhere in the codebase (import validates
  only non-empty), and enforcing one now would risk rejecting legitimately-imported keys that
  don't happen to match a slug pattern. `SaveResourceAsync`/`CreateTagDefinitionAsync` validate
  required + max-length (matching column width) only; a stricter format check, if wanted, belongs
  in the UI (#6) as instant feedback, not a server-side hard rule.
- The dead "example" method `ResourceService.GetResourceByUid` (not part of `IResourceService`,
  referenced by nothing) was removed while expanding this file — it predated the real
  `GetResourceDetailAsync` this slice adds.

Then: flip slice #4 → Done and slice #5 → Planning in `00-implementation-plan-list.md`, and
commit (schema-consistent service/API layer). Per our pattern, planning is on the stronger model;
execution switches to the cheaper one.
