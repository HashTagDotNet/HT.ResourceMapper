# Editor / Details — Implementation Plan List (master)

Master list of implementation slices that turn the design in
[`resource-logical-model-and-editor-ux.md`](./resource-logical-model-and-editor-ux.md) into a
working end-to-end stack. Layers ripple bottom-up (data → sprocs → contracts → services →
import → UI), so slices are ordered by dependency. Each slice gets its own plan doc
(`NN-<slug>.md`) as we start it.

## Slices

| # | Slice | Scope (summary) | Depends on | Status |
|---|---|---|---|---|
| 1 | **[Data foundation](./01-data-foundation.md)** | Schema: `Resource.ResourceTypeId` (NOT NULL) + `PrimaryTagDefinitionId`; drop `UK_Resource_ResourceKey`; `TagDefinition` props (`DisplayName`, `RequirementLevel`, `IsDomainTag`, `DisplayOrder`, `IsSystemTag`→bit); constrain + seed `TagContentType` (Text/Link); rename `ResourceDependency`→`ResourceRelationship` (+ UNIQUE, no-self-loop CHECK); **drop** `TagValueType`; redesign `ResourceTypeTag` (FK→`TagDefinition`, `IsDefaultPrimary`); seed `Domain` tag (`Subscription`, prod/non-prod) via post-deploy. Housekeeping: delete stale DB trees + orphaned project, fix CLAUDE.md. | — | **Done** ✓ |
| 2 | **[Sprocs & repository](./02-sprocs-and-repository.md)** | `Resource_Upsert`/`TagDefinition_Upsert` type-guard + `(type+key)` identity (Domain deferred to #5); dropped dead `Resource_Create`; removed dead "blank ResourceType" branches from `Resource_GetItems`/`Resource_GetFilterValues`; new `Resource_Delete` (cascade), `ResourceRelationship_Add`/`_Remove`/`_GetForResource`. C# POCOs (`ResourceDependency.cs`→`ResourceRelationship.cs`, delete `TagValueType.cs`, `TagDefinition.IsSystemTag` int→bool + new props, `Resource.ResourceTypeId` int?→int + `PrimaryTagDefinitionId`) + repo methods (`GetResourceByUidAsync`, relationship CRUD, `DeleteResourceAsync`) + `ImportSqlRepository` fix. | 1 | **Done** ✓ |
| 3 | **[Contracts & DTOs](./03-contracts-and-dtos.md)** | **Deleted** the 4 empty/unused ApiContracts DTO stubs (`ResourceDto`/`ResourceTagDto`/`TagDefinitionDto`/`TagContentTypeDto`) — mirrors the home-page precedent instead: new transport models (`TagContentTypeModel`, `TagDefinitionModel`, `ResourceTagModel`, `ResourceDetailModel`, `ResourceRelationshipModel`, `ResourceTypeEntryPointModel`) + expanded change-tracked editor models (`ResourceEditorModel`, `TagRowEditor`, `DependencyRowEditor`, `IdentityPreview`) all in `Common.Shared/Editor`; expanded `OpenEditorContract` + new `ResourceIdentityContract`/`ResourcePickerContract`. | 2 | **Done** ✓ |
| 4 | **[Services & API](./04-services-and-api.md)** | `IResourceService` (in-process — server-hosted Blazor, **no HTTP controllers**): get / create-update (uid-keyed `Resource_Save`) / delete / uniqueness-check; tag-definition CRUD (inline create); relationship CRUD. New sprocs `Resource_Save`, `ResourceTag_GetForResource` (closes #3 tags-read gap), `Resource_CheckUnique` + `TagDefinition_Upsert` id output; also extended `TagDefinition_GetAll` with a denormalized `ContentType` join. Repo/service methods + `SaveResource`/`CreateTagDefinition` contracts. General + primary persisted; applied-tag/edge save deferred to #7/#8. | 3 | **Done** ✓ |
| 5 | **Import wiring — Domain** | **Domain now enters identity**: `Resource_Upsert`/`Resource_GetAllKeys` gain the domain-tag join → full `(domain+type+key)` triple; `import.defaults.domain` + per-resource override; idempotency on the triple; `ResourceTag_SetForResource` domain-safety; wire `dependencies`→`DependsOn`; validation updates (type required, content-type, vocab). | 2–4 | Planning |
| 6 | **Editor/Details UI — core** | Tabbed component; Create (wizard) / Edit (property-sheet) / View (read-only + Edit) modes; **General** tab (type/domain/name/auto-key/description, identity preview, early uniqueness); **Review** tab + validation gate; save + success toast; routes `/resources`, `/resources/<uid>`; "Create Resource" in hamburger. | 4 | Todo |
| 7 | **Editor UI — Tags tab** | Pre-seeded entry-point rows from type template; content-aware value editors (Link/URL validation, controlled-vocab pickers, multi-valued chips); inline **full** tag-definition create dialog (search-first, no system/domain flags); primary single-select; empty-row drop on save. | 6 | Todo |
| 8 | **Editor UI — Dependencies / Dependent On** | Searchable resource picker (same-domain only); both directions editable (system writes far side); nested "Create new…" target (domain-locked) with pop-back; persistence timing. | 6, 2 | Todo |
| 9 | **Navigation, dirty guard & delete** | Nested editors as history-pushing routes; `RegisterLocationChangingHandler` dirty guard + Mud Save/Discard/Cancel; plain Mud dialogs (not in history); delete action + confirm dialog (dependents) + cascade-delete edges. | 6–8 | Todo |

## Deferred (not in this build)

Typed relationship catalog (`ProducesTo`/`ConsumesFrom`); per-type `DisplayOrder` override;
audit actor (`CreatedBy`/`UpdatedBy`); graphical resource explorer; History tab (scaffolded,
commented out). See the design doc's *Deferred / next phases*.

## Notes

- **Verify each slice end-to-end** before starting the next (build, tests, and — for UI — drive
  the actual app).
- The DB project is `Database/HTResourceMapperDb`; app DB is `(localdb)\MSSQLLocalDB\ResourceMapper`.
- Per-slice plan docs will be added here as `NN-<slug>.md` and linked from the table.
