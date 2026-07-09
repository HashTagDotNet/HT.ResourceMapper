# Slice #3 — Contracts & DTOs

## Context

Third implementation slice of the refreshed resource model (design:
`docs/plans/editor/resource-logical-model-and-editor-ux.md`; list:
`docs/plans/editor/00-implementation-plan-list.md`). Slices #1 (schema) and #2 (sprocs +
repository) are done and verified — the DB and data-access layer are now internally consistent.
This slice is **pure type definitions**: it lands the **transport/contract shapes** the editor +
details surface will bind to, so the build compiles and slices #4 (services/API) and #6–9 (UI)
have concrete models to project into and render. **No services, controllers, sprocs, or UI** —
except one forced compile-fix ripple (below).

**Layering decision (confirmed this session): "Common.Shared only".** The one working
end-to-end feature — the home-page grid — deliberately put its transport models in
`Common.Shared` (`ResourceGridItemModel`, `ResourceGridTagModel`, request/response in a
`Contracts` subfolder) and **never used** the `HT.ApiContracts/Models/ResourceMapper/` DTO
stubs. Those four stubs are **empty (0-byte) and referenced by zero code**. Rather than build a
parallel, unused DTO hierarchy, slice #3 **mirrors the home-page precedent**: all resource
transport models live in `Common.Shared`; the empty ApiContracts stubs are **deleted**;
`HT.ApiContracts` keeps only the envelope (`ApiResponse<T>`, `Message`, `MetaData`,
`CallStatusCode`) and `HT.Api.Service.Contracts` keeps `ApiServiceResponse<T>` /
`ServiceResponseBuilder<T>` — both unchanged.

**Naming/placement convention (matches home-page):** transport read/dictionary shapes use the
`…Model` suffix and sit at the feature root (`Common.Shared/Editor/`); request/response
envelopes go in `Common.Shared/Editor/Contracts/`. Change-tracked editor-state types keep the
`…Editor` / `…Preview` naming (like the existing `SingleValueEditor`). All are flat POCOs,
nullable-enabled, `public get/set`, camelCase on the wire (the API host uses reflection-based
`System.Text.Json` via `AddControllers()`; the home-page `ResourceGridResponse` is likewise
**not** registered in `ApiContractsJsonContext` and works — so **no `[JsonSerializable]`
edits**).

**Enum-ish fields stay strings on the wire.** `RequirementLevel` (Error|Suggested|Optional),
`ContentType` (Text|Link), and relationship `Direction` (DependsOn|DependentOn) are strings
everywhere in the data layer and in `ResourceGridTagModel.ContentType`. Keep them strings on
transport models (tolerant, matches the data + precedent); expose intent through **computed
helper bools** on the editor row types instead of typing the wire as an enum.

## Delete (housekeeping)

Delete the four empty, unreferenced stub files (they're not listed in any `.csproj` — SDK-style
globbing — so deleting the files is clean; nothing in code or `ApiContractsJsonContext`
references them):

- `Libraries/ApiContracts/HT.ApiContracts/Models/ResourceMapper/ResourceDto.cs`
- `…/ResourceTagDto.cs`
- `…/TagDefinitionDto.cs`
- `…/TagContentTypeDto.cs`

(The now-empty `Models/ResourceMapper/` folder can be removed too.)

## New transport models — `Common.Shared/Editor/` (ns `ResourceMapper.Common.Shared.Editor`)

Read / dictionary shapes the service (#4) projects the slice-#2 repo models into. Flat POCOs.

**`TagContentTypeModel.cs`** (← `TagContentType`) — `int TagContentTypeId`, `string Code`
(`"Text"|"Link"`). Lightweight lookup for the tag-def create dialog (#7).

**`TagDefinitionModel.cs`** (← `TagDefinition` + `TagContentType` nav) — the tag "render
rulebook": `int TagDefinitionId`, `string TagDefinitionUid`, `string TagDefinitionKey`,
`string? DisplayName`, `int TagContentTypeId`, `string ContentType` (denormalized `TagCode`),
`string RequirementLevel`, `bool IsMultiValued`, `bool AllowCustomValue`,
`List<string>? AllowedValues` (the JSON-array string is parsed in the #4 mapper — the only
non-1:1 projection), `bool IsDomainTag`, `bool IsSystemTag`, `int DisplayOrder`.

**`ResourceTagModel.cs`** (← applied `ResourceTag` + its `TagDefinition`) — `int TagDefinitionId`,
`string TagDefinitionKey`, `string? DisplayName`, `string ContentType`, `string? Value`
(Link ⇒ URL), `bool IsPrimary` (set in projection when `Resource.PrimaryTagDefinitionId` ==
this def). Carries key/contentType/value inline so the client renders without the dictionary
(mirrors `ResourceGridTagModel`).

**`ResourceDetailModel.cs`** (← `ResourceDetail` + resolved type + tag rows + relationships) —
the details/view read shape: `int ResourceId`, `string ResourceUid`, `string ResourceKey`,
`string ResourceName`, `string? Description`, `int ResourceTypeId`, `string ResourceTypeName`,
`string? Domain` (computed; **null until slice #5** lands the domain-tag join — matches the
#2/#5 sequencing), `int? PrimaryTagDefinitionId`, `string? PrimaryLinkUrl` (computed),
`string? IdentityDisplay` (`"domain / type / key"`, computed), `List<ResourceTagModel> Tags`,
`List<ResourceRelationshipModel> Relationships`, `DateTime CreatedOn`, `DateTime? UpdatedOn`.
Acyclic (no back-references).

**`ResourceRelationshipModel.cs`** (← `ResourceRelationshipItem`) — `int RelationshipId`,
`string Direction` (`"DependsOn"` out-edge | `"DependentOn"` in-edge), `string OtherResourceUid`,
`string OtherResourceKey`, `string OtherResourceName`, `string OtherResourceType`,
`string? OtherDomain` (resolved in #5).

**`ResourceTypeEntryPointModel.cs`** (← `ResourceTypeTag`) — per-type entry-point template:
`int ResourceTypeId`, `int TagDefinitionId`, `bool IsDefaultPrimary`, `string? RequirementLevel`.
Drives the Tags-tab pre-seeded rows + default primary.

## Change-tracked editor models — `Common.Shared/Editor/`

State the editor binds to. **Reuse `SingleValueEditor`** (Original/EditedValue, `IsChanged`,
`Messages`, `Level`) for every change-tracked scalar; **compose** the transport models above
for render rules.

**`ResourceEditorModel.cs`** (EXPAND — replaces the stale stub):
- General scalars (all `SingleValueEditor`): `ResourceType` (read-only after save; drives
  template), `Domain` (select prod/non-prod; required), `Name`, `Key` (auto `slug(Name)`,
  editable, required), `Description` (multiline, optional).
  → **renames**: stub `Code`→`Key`, `Notes`→`Description`; **remove** `NewTagName`/`NewTagValue`.
- State/identity: `string ResourceUid` (immutable anchor; new Guid for Create),
  `IdentityPreview IdentityPreview`, `string Mode` (`Create|Edit|View`), `bool IsPersisted`
  (freezes Type; enables Save-anywhere in Edit), `int? ResourceTypeId`,
  `int? PrimaryTagDefinitionId`, `DateTime? CreatedOn`, `DateTime? UpdatedOn`.
- Collections: `List<TagRowEditor> Tags`, `List<DependencyRowEditor> DependsOn`,
  `List<DependencyRowEditor> DependentOn`.
- `bool IsDirty` (computed): any scalar `IsChanged` or any row dirty (drives the #9 dirty guard).

**`TagRowEditor.cs`** (NEW) — one editable applied-tag row:
- `TagDefinitionModel Definition` (rulebook), `int TagDefinitionId`,
  `List<SingleValueEditor> Values` (1 for single-valued; N chips for multi-valued),
  `bool IsPrimary` (single-select among Link tags), `bool IsPreSeeded` (from template; empty
  pre-seeded rows dropped on save), `bool IsRemoved` (soft-remove), `List<EditorMessage>
  RowMessages`, computed `EditorFieldLevel RowLevel`.
- Computed helpers off `Definition` (no new state): `IsLink` (`ContentType=="Link"`),
  `IsControlledVocab` (`!AllowCustomValue && AllowedValues?.Count>0`), `CanBePrimary`
  (`IsLink && !IsMultiValued`), `IsRequired` (`RequirementLevel=="Error"`), `IsSuggested`
  (`=="Suggested"`), `IsEditable` (`!IsSystemTag`).

**`DependencyRowEditor.cs`** (NEW) — one editable relationship row (both tabs):
- `int? RelationshipId` (null = not-yet-persisted), `string Direction`, `string OtherResourceUid`,
  `string OtherResourceName`, `string OtherResourceType`, `string? OtherDomain`, `bool IsNew`
  (edge persists on parent Save), `bool IsRemoved` (edge deleted on parent Save),
  `List<EditorMessage> RowMessages` (cross-domain / self-loop guard).

**`IdentityPreview.cs`** (NEW) — read-only preview + early-uniqueness surface:
- `string? Domain`, `string? ResourceType`, `string? Key`, `string Display`
  (`"{domain} / {type} / {key}"`, omits null parts), `bool? IsUnique` (null = unchecked),
  `string? UniquenessMessage`.

## Contracts — `Common.Shared/Editor/Contracts/` (ns `…Editor.Contracts`)

**`OpenEditorContract.cs`** (EXPAND) — bootstrap the editor in one round trip (low-friction /
validate-early):
- `OpenEditorRequest`: `string Mode`, `string? ResourceUid` (null/empty for Create).
- `OpenEditorResponse`: existing `ResourceEditorModel EditorModel` +
  `List<KeyValuePair<string,string>> ResourceTypes`; **add** `List<TagDefinitionModel>
  TagDictionary` ("Add tag" search + render rules), `List<ResourceTypeEntryPointModel>
  EntryPointTemplates` (per-type pre-seed; enables client-side re-seed on type change without a
  round trip), `List<string> DomainAllowedValues` (prod/non-prod for the Domain select).

**`ResourceIdentityContract.cs`** (NEW) — the General-tab early uniqueness check:
- `ResourceUniquenessRequest`: `string? Domain`, `int ResourceTypeId`, `string ResourceKey`,
  `string? ExcludeResourceUid` (exclude self on edit).
- `ResourceUniquenessResponse`: `bool IsUnique`, `string? Message`, `string? IdentityDisplay`.
- Backing service/sproc in #4 (check `type+key`, widen to `domain+type+key` in #5, mirroring the
  #2 "Domain deferred" call). Slice #3 defines the shape only.

**`ResourcePickerContract.cs`** (NEW) — the same-domain dependency picker (Dependencies /
Dependent On tabs). *Note on the master-list wording:* it says "facet DTOs", but the picker
needs same-domain **search**, not column faceting — the home page already has
`ResourceGridFacet*` (`Common.Shared/HomePage/Contracts`) to reuse if a faceted picker is ever
wanted; **don't duplicate** facet DTOs here.
- `ResourcePickerRequest`: `string? SearchFor`, `string? Domain` (same-domain constraint),
  `int? ResourceTypeId`, `string? ExcludeResourceUid`, `int Skip`, `int Take`.
- `ResourcePickerItem`: `string ResourceUid`, `string ResourceName`, `string ResourceKey`,
  `string ResourceTypeName`, `string? Domain`.
- `ResourcePickerResponse`: `List<ResourcePickerItem> Items`, `int TotalItems`.
- Backing sproc added in #8; slice #3 defines the shape only.

## Forced compile-fix ripple (the only existing-code change)

`Modules/Common/ResourceMapper.Common.Server/Resources/ResourceService.cs` —
`GetResourceEditorModelAsync` (≈lines 163–170) constructs `ResourceEditorModel { Code=…,
Notes=… }`. After the rename it must set `Key`, `Name`, `Description`, `ResourceType`, `Domain`
(all `new()`), `ResourceUid`, and `Mode`. No behavior change beyond the field renames; the
new `OpenEditorResponse` collections default to empty (populated in #4). This is the **only**
consumer of the editor stubs (verified: the interface signature is unchanged; no test
constructs the editor shape).

## Out of scope (later slices)

- **Projecting** repo models → these models, and the actual service/controller methods → **#4**
  (`GetResourceByUid`, save/create/update/delete, uniqueness check, picker, tag-def CRUD).
- **A read for a resource's applied tags** — *gap flagged*: slice #2's `ResourceDetail` carries
  no tags and there's no `ResourceTag_GetForResource` sproc/repo method. `ResourceTagModel` +
  `ResourceDetailModel.Tags` define the **target**; the backing read is added in **#4/#6**.
- **Domain** value resolution (`Domain`/`OtherDomain`/`DomainAllowedValues` population) → **#5**.
- All UI rendering/binding, primary single-select behavior, chips, nested create → **#6–9**.

## Also update (doc accuracy — mirrors the slice-#1 CLAUDE.md fix)

`CLAUDE.md` "API Response Pattern" section currently says
`HT.Api.Client.Contracts` "Contains resource DTOs (ResourceDto, ResourceTagDto, etc.)". After
deletion that's inaccurate — update it to state that `HT.ApiContracts` holds only the wire
**envelope**, and resource transport models live per-feature in `Common.Shared`
(`…Shared/HomePage`, `…Shared/Editor`), following the home-page precedent.

## Verification (build-only — this is a types slice)

1. **Compiles:** `dotnet build` the solution (or at least `HT.Api.Client.Contracts`,
   `HT.Api.Service.Contracts`, `ResourceMapper.Common.Shared`, `ResourceMapper.Common.Server`,
   and `ResourceMapper.UI.Web`). The only cross-project ripple is the `ResourceService`
   rename fix; deleting the four empty stubs must leave `HT.Api.Client.Contracts` building
   clean (confirmed: zero references, not in the `.csproj`, not in `ApiContractsJsonContext`).
2. **Tests:** `dotnet test` the `Common.Server.Tests` project → still green (grep confirms no
   test constructs the old `ResourceEditorModel` shape; re-pin if any editor-shape test surfaces).
3. **No JSON change:** camelCase round-trip is unaffected (no `ApiContractsJsonContext` edit),
   consistent with how the home-page `ResourceGridResponse` already serializes.

## Execution notes

- **All steps completed and verified.** `dotnet build HT.ResourceMapper.slnx` — zero `error CS`/
  `error MSB` across the C# projects; the only build error is the pre-existing, documented
  `HTResourceMapperDb.sqlproj` SSDT failure under `dotnet build` (unrelated — that project builds
  via VS/full MSBuild only, per the CLAUDE.md note added in slice #1).
- `dotnet test` on `Common.Server.Tests` — **37/37 passed**, no test referenced the old
  `ResourceEditorModel` shape (`Code`/`Notes`/`NewTagName`/`NewTagValue`), so no re-pinning needed.
- The four empty ApiContracts stubs were confirmed unreferenced (not in any `.csproj` item list —
  SDK-style globbing — and not in `ApiContractsJsonContext`) before deletion; deleting them and
  the now-empty `Models/ResourceMapper/` folder left `HT.Api.Client.Contracts` building clean.

Then: move this plan to `docs/plans/editor/03-contracts-and-dtos.md`, flip slice #3 → Done and
slice #4 → Planning in `00-implementation-plan-list.md`, and commit (schema-consistent contract
layer). Per our pattern, switch to Sonnet for execution once approved.
