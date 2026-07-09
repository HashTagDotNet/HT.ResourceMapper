# Slice #5 — Import wiring — Domain

## Context

Fifth implementation slice (design: `docs/plans/editor/resource-logical-model-and-editor-ux.md`
§5–§8; list: `docs/plans/editor/00-implementation-plan-list.md`). Slices #1–#4 are done. This is
the slice where **Domain finally enters identity end-to-end**: identity becomes the full
`(Domain + Type + Key)` triple (design §5), the import gains a batch-default domain + per-resource
override, idempotency moves to the triple, `dependencies[]` are wired to `ResourceRelationship`
edges (deferred since slice #2), and the deferred validation tightening (type required,
content-type constrained, controlled-vocab incl. domain) lands.

It is the **most identity-sensitive slice** so far, because **Domain is stored as a *tag*
(`ResourceTag` → `TagDefinition` with `IsDomainTag=1`), not a column on `Resource`.** So
"matching on domain" means matching through a tag join, which drives the central design decision
below.

**Seed context (slice #1, unchanged):** the `Domain` tag is `TagDefinitionKey='Domain'`,
`DisplayName='Subscription'`, `IsDomainTag=1`, `IsSystemTag=1`, `AllowCustomValue=0`,
`AllowedValues='["prod","non-prod"]'`, `RequirementLevel='Error'`, `ContentType='Text'`.

## Central design decision — how domain participates in matching

**`Resource_Upsert` becomes domain-aware and *owns* the domain tag.** The import writes tags per
resource as `UpsertResourceAsync` → `SetResourceTagsAsync` (upsert first, tags after), so a domain
value written only by the later `SetForResource` wouldn't be visible to the upsert's own matching.
Resolution:

- `Resource_Upsert` takes a new `@Domain`, resolves the single `IsDomainTag=1` definition, and
  **matches existing rows by joining `ResourceTag` (domain def, `TagValue=@Domain`) with
  `(ResourceTypeId, ResourceKey)`** — the full triple. On **insert** it writes the resource **and
  its domain `ResourceTag` row atomically**; on update the domain tag already matches, so nothing
  more is needed.
- `ResourceTag_SetForResource` gains **domain-safety**: it must **not** delete the domain tag
  (it's written/owned by the upsert) and must **skip** any domain-keyed row in its input TVP.
  Concretely: delete only `WHERE TagDefinitionId <> @DomainTagDefId`, and exclude the domain def
  from the insert join.
- Genericity: if no `IsDomainTag=1` definition exists (design §6 "Unused" state), `Resource_Upsert`
  falls back to `(Type + Key)` matching and ignores `@Domain` — no domain tag written.

This keeps the two same-`(type+key)`-different-domain resources distinct (design §5 example:
`orders-queue`/non-prod vs `orders-queue`/prod), makes re-import idempotent on the triple, and
keeps the domain tag a real, queryable `ResourceTag` row (so the grid/editor read it like any tag).

## SQL — changed sprocs (`Database\HTResourceMapperDb\Stored Procedures\`)

Declarative SSDT: rebuild dacpac via full MSBuild, publish via SqlPackage **with
`/p:DropObjectsNotInSource=True`**, **and republish after *every* sproc edit** (the slice-#4
lesson — a stale publish silently breaks readers).

- **`Resource_Upsert.sql`** — add `@Domain NVARCHAR(...) = NULL`. Resolve `@DomainTagDefId`
  (`IsDomainTag=1`). Change the find-existing predicate to also join the domain tag on `TagValue =
  @Domain` (when `@DomainTagDefId` is non-null). On insert, after `SCOPE_IDENTITY()`, insert the
  domain `ResourceTag (@ResourceId, @DomainTagDefId, @Domain)`. Keep the existing type-guard
  (`@Result='error'` when the type is unresolvable). **Signature change → ripples to
  `IImportRepository.UpsertResourceAsync` + the test mocks.**
- **`Resource_GetAllKeys.sql`** — return `(ResourceId, ResourceKey, TypeName, Domain)` tuples
  (`INNER JOIN ResourceType`, `LEFT JOIN` the domain `ResourceTag`), replacing the bare-key list.
  Feeds both the conflict check on the triple and dependency resolution (below).
- **`ResourceTag_SetForResource.sql`** — domain-safety: preserve the domain tag (delete only
  non-domain rows; skip domain-keyed TVP rows). Also protects the domain tag when the **editor**
  later saves user tags (slices #7).

## C# — repository layer

- **`IImportRepository` / `ImportSqlRepository`:**
  - `UpsertResourceAsync(...)` — add a `string? domain` parameter, pass `@Domain`.
  - Replace `GetExistingResourceKeysAsync` (bare `HashSet<string>`) with
    `GetAllResourceIdentitiesAsync` → `List<ResourceIdentity>` (a new small model:
    `ResourceId, Domain, Type, Key`) over the widened `Resource_GetAllKeys`. Used for triple
    conflict-checking **and** dependency-target resolution.
- **Relationship writes reuse the existing `IResourceRepository.AddRelationshipAsync(fromId,
  toId,…)`** (already injected into `ImportService` as `_resourceRepo`) — no new repo method.

## C# — import service (`ImportService.cs`)

- **Effective domain per resource** = the resource's own domain-tag value in `Tags[<domainKey>]`
  (override) ?? `request.Defaults.Domain` (batch default). Resolve `<domainKey>` from the
  `IsDomainTag=1` definition's key (generic; `"Domain"` in our seed). **Pull the domain entry out
  of `Tags`** before `NormalizeTags` so it isn't double-written by `SetForResource` (the upsert
  owns it).
- **Write phase:** pass the effective domain to `UpsertResourceAsync`; build a
  `Dictionary<(domain,key), resourceId>` as resources upsert (seeded from
  `GetAllResourceIdentitiesAsync` for pre-existing targets).
- **Dependencies → DependsOn (new):** after all resources are upserted (targets exist), for each
  resource with `Dependencies[]`, resolve each key to a target by **`(effective-domain + key)`**
  (see Open Question), reject self-references, and call `AddRelationshipAsync(thisId, targetId)`
  (out-edge = DependsOn). Add a `ResourceRelationships` line to the import summary/response.
- **Validation tightening:**
  - **Type required** — `ValidateSchema` errors when `Type` is null/blank (identity-bearing; no
    silent `Unknown`).
  - **Content type** — validate each tag def's `ContentType ∈ {Text, Link}`; map the legacy
    default `"string"`/empty → `"Text"` (design §7). Fixes the stale `ImportTagDefinitionModel`
    default that now violates the Text/Link CHECK.
  - **Domain** — effective domain **required** (`RequirementLevel=Error`) and **∈ domain
    `AllowedValues`** (`prod`/`non-prod`; `AllowCustomValue=0`). Missing/invalid → hard error.
  - **Duplicate detection** — `AddDuplicateKeyErrors` for resources moves from **bare key** to the
    **`(domain+type+key)` triple** (bare-key dedup would wrongly reject the valid same-key/
    different-domain case).
  - **Conflict check** (`onConflict=fail`) — compare against the existing-**triple** set, not keys.

## C# — contracts (`Common.Shared/Import/Contracts/ImportContract.cs`)

- Add `ImportDefaults Defaults { get; set; } = new();` to `ImportRequest`, with
  `ImportDefaults { public string? Domain { get; set; } }`.
- Fix `ImportTagDefinitionModel.ContentType` — drop the stale "auto-registered" doc and the
  `"string"` default (default to `"Text"` or leave empty + map in the service).
- `ImportResourceItem.Dependencies` (already `List<string>?`) — unchanged shape (see Open Question).
- Add an optional `ImportSectionSummary ResourceRelationships` to `ImportSummary` for the wired edges.

## Open question / decision to confirm (flagged — carries a documented default)

**Dependency resolution against a flat key.** `dependencies` is `List<string>` (keys only), but
identity is `(domain+type+key)` and a key can recur across types within one domain (design's own
`orders` queue vs `orders` database). A bare dependency key is therefore ambiguous.

**Recommended default (implemented unless overridden):** resolve each dependency by
**`(same-domain + key)`** — dependencies are same-domain per design §12; **exactly one** match →
link; **zero** → error (`unresolved dependency`); **more than one** (same key, different types) →
error (`ambiguous dependency`). Self-references are rejected.

*Alternative if ambiguity is common:* extend the contract so a dependency can carry a type
(`{key,type}`), making it a full same-domain `(type+key)` match. This is a contract change; not
taken unless requested.

## Out of scope (later slices)

- Typed relationship catalog (`ProducesTo`/`ConsumesFrom`) — still deferred (design §10).
- Editor **UI** (General/Tags/Dependencies/etc.) → **#6–9**. The editor's own domain **write**
  path (a user picking a domain in the General tab) rides on the same domain-aware
  `Resource_Save` semantics; slice #6 wires the UI, reusing this slice's `SetForResource`
  domain-safety.

## Tests (`_Tests\...\ResourceMapper.Common.Server.Tests\Resources\ImportServiceTests.cs`)

- **Re-pin existing Moq setups** for the changed signatures: `UpsertResourceAsync` (+`domain`),
  and swap `GetExistingResourceKeysAsync` → `GetAllResourceIdentitiesAsync`.
- New cases: effective domain default vs per-resource override; missing domain → error; domain not
  in vocab → error; type required → error; content-type `"string"`→`Text` mapping; same-key/
  different-domain accepted (not a duplicate); triple idempotency (re-import → updated); dependency
  wired (`AddRelationshipAsync` called with resolved ids); self-reference rejected; ambiguous /
  unresolved dependency → error.

## Verification

1. **DB builds + publishes** (full MSBuild dacpac → SqlPackage `/p:DropObjectsNotInSource=True`;
   republish after each sproc edit).
2. **`sqlcmd` exercise:** domain-aware `Resource_Upsert` — same `(type+key)` with different
   `@Domain` creates **two** rows each with its domain tag; re-upsert same triple → `updated`, no
   dup; `ResourceTag_SetForResource` leaves the domain tag intact; `Resource_GetAllKeys` returns
   the `(id,domain,type,key)` tuples.
3. **C# build + unit tests** (`dotnet build` C# projects; `dotnet test` — existing + new import
   tests green).
4. **Import smoke** (throwaway xUnit test against live `(localdb)`, deleted before commit): import
   a doc with `defaults.domain`, a per-resource domain override, controlled-vocab tags, and
   `dependencies[]`; assert two same-key/different-domain resources are distinct, the domain tag is
   applied, edges are written, and a re-import is idempotent (all `updated`, no new rows/edges).

Then: flip slice #5 → Done and slice #6 → Planning in `00-implementation-plan-list.md`, and
commit. Per our pattern, planning is on the stronger model; execution switches to the cheaper one.
