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

**Both write paths get this treatment (rubber-duck):** the editor's `Resource_Save` (slice #4)
receives the *same* domain-aware change as `Resource_Upsert`, so "Domain enters identity" holds
for editor-created resources too — not just imported ones. Otherwise the editor could mint
domain-less resources that no `SetForResource` would ever fix.

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
- **`Resource_Save.sql`** — **apply the identical domain-aware change** (rubber-duck finding: the
  editor write path must not be left domain-blind, or editor-created resources would be domain-less
  and identity would only be half-enforced). Add `@Domain`, resolve `@DomainTagDefId`, match the
  existing row by the domain-tag join in addition to `ResourceUid` (uid stays the primary anchor;
  the domain join guards the identity triple), and **write/refresh the domain `ResourceTag`** on
  insert (and on update if the value legitimately changed for the same uid). `SetForResource`
  domain-safety (below) then leaves it intact. The **editor UI** that supplies the domain value is
  slice #6; this slice only makes the sproc + `SaveResourceAsync` capable of it (add `@Domain`
  through `IResourceRepository.SaveResourceAsync` → `SaveResourceRequest.Domain` is already present
  from slice #4, currently ignored — wire it here). **Signature change → ripples to
  `IResourceRepository.SaveResourceAsync` + slice-#4 `ResourceService.SaveResourceAsync` + tests.**
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
- **Write phase:** pass the effective domain to `UpsertResourceAsync`; record each upserted
  resource's `(domain,key) → resourceId` into the multiplicity-aware resolution structure below
  (seeded from `GetAllResourceIdentitiesAsync` for pre-existing targets).
- **Dependencies → DependsOn (new):**
  - **Resolve/validate in the validation phase (rubber-duck finding — preserve "nothing written on
    a validation error").** Build the would-be identity set = existing triples (from
    `GetAllResourceIdentitiesAsync`) ∪ payload triples, and confirm every `Dependencies[]` entry
    resolves to **exactly one** target by **`(effective-domain + key)`**, rejecting
    self-references. Zero → `unresolved`, >1 → `ambiguous` (see Open Question). Failures are
    collected as `ImportError`s so the whole import fails cleanly before any write.
  - **Ambiguity detection needs multiplicity, not a last-writer map (rubber-duck finding).** Use a
    `(domain,key) → List<resourceId>` (or a count), **not** `Dictionary<(domain,key), id>` — a
    plain dict would silently overwrite the second same-key/different-type entry and hide the very
    ambiguity we must error on.
  - **Write the edges after the upsert loop** via `AddRelationshipAsync(thisId, targetId)`
    (out-edge = DependsOn). **Additive-only semantics** (rubber-duck decision): `_Add` is
    idempotent, so re-import is safe; import **does not remove** edges dropped from a re-imported
    `Dependencies[]` (reconciliation would also clobber edges added via the editor — deferred; see
    Open Question). Skip dependency writes for a **skipped** resource (`onConflict=skip`),
    consistent with skip-means-skip for tags.
  - Add a `ResourceRelationships` line to the import summary/response.
- **Validation tightening:**
  - **Type required** — `ValidateSchema` errors when `Type` is null/blank (identity-bearing; no
    silent `Unknown`). (Pure/DB-free — stays in `ValidateSchema`.)
  - **Content type** — validate each tag def's `ContentType ∈ {Text, Link}`; map the legacy
    default `"string"`/empty → `"Text"` (design §7). Fixes the stale `ImportTagDefinitionModel`
    default that now violates the Text/Link CHECK.
  - **Domain** — effective domain **required** (`RequirementLevel=Error`) and **∈ domain
    `AllowedValues`** (`prod`/`non-prod`; `AllowCustomValue=0`). Missing/invalid → hard error.
    **Canonicalize** the effective domain to the vocab's exact casing before matching/storing
    (rubber-duck finding: avoids `Prod` vs `prod` drift under the CI collation writing an
    off-canonical tag value).
  - **Duplicate detection** — resource dedup moves from **bare key** to the **`(domain+type+key)`
    triple** (bare-key dedup would wrongly reject the valid same-key/different-domain case).
  - **Conflict check** (`onConflict=fail`) — compare against the existing-**triple** set, not keys.
  - **Pipeline ordering (rubber-duck finding):** triple dedup, effective-domain resolution, domain
    vocab validation, and dependency resolvability all need the `IsDomainTag` definition + loaded
    reference data, so they run **after** the DB read (in `ValidateReferences` or a new
    post-load validation pass) — **not** in the pure `ValidateSchema`. Resolve the domain tag's
    **key** from the loaded `IsDomainTag=1` definition (generic; `"Domain"` in our seed).
  - **No domain definition (design §6 "Unused"):** if no `IsDomainTag=1` definition exists,
    **skip** the default/override/vocab/triple-domain logic entirely and fall back to `(Type+Key)`
    identity — mirrors the `Resource_Upsert`/`Resource_Save` sproc fallback. (Not our deployment,
    but keeps the domain concept genuinely optional as designed.)

## C# — contracts (`Common.Shared/Import/Contracts/ImportContract.cs`)

- Add `ImportDefaults Defaults { get; set; } = new();` to `ImportRequest`, with
  `ImportDefaults { public string? Domain { get; set; } }`.
- Fix `ImportTagDefinitionModel.ContentType` — drop the stale "auto-registered" doc and the
  `"string"` default (default to `"Text"` or leave empty + map in the service).
- `ImportResourceItem.Dependencies` (already `List<string>?`) — unchanged shape (see Open Question).
- Add an optional `ImportSectionSummary ResourceRelationships` to `ImportSummary` for the wired edges.

## Open questions / decisions to confirm (flagged — each carries a documented default)

**OQ1 — Dependency resolution against a flat key.** `dependencies` is `List<string>` (keys only),
but identity is `(domain+type+key)` and a key can recur across types within one domain (design's
own `orders` queue vs `orders` database). A bare dependency key is therefore ambiguous.

*Recommended default (implemented unless overridden):* resolve each dependency by
**`(same-domain + key)`** — dependencies are same-domain per design §12; **exactly one** match →
link; **zero** → error (`unresolved dependency`); **more than one** (same key, different types) →
error (`ambiguous dependency`). Self-references are rejected.

*Alternative if ambiguity is common:* extend the contract so a dependency can carry a type
(`{key,type}`), making it a full same-domain `(type+key)` match. This is a contract change; not
taken unless requested.

**OQ2 — Re-import edge semantics: additive vs reconciling (rubber-duck).** When a re-import
*drops* a dependency that was present before, should the stale edge be removed?

*Recommended default (implemented unless overridden):* **additive-only** — import adds edges
(idempotently) and never removes them. Reconciling to the payload's exact `DependsOn` set would
also delete edges a user added via the editor, and import isn't the sole edge author. Full
desired-state reconciliation is deferred; revisit if import becomes the authoritative edge source.

## Out of scope (later slices)

- Typed relationship catalog (`ProducesTo`/`ConsumesFrom`) — still deferred (design §10).
- Editor **UI** (General/Tags/Dependencies/etc.) → **#6–9**. The editor's domain **write
  capability** (domain-aware `Resource_Save` + `SaveResourceAsync` honoring `@Domain`) lands in
  **this** slice; slice #6 only wires the General-tab UI that supplies the value. Editor
  **dependency** editing (add/remove edges through the UI) is still #8.

## Tests

**`ImportServiceTests.cs`:**
- **Re-pin existing Moq setups** for the changed signatures: `UpsertResourceAsync` (+`domain`),
  and swap `GetExistingResourceKeysAsync` → `GetAllResourceIdentitiesAsync`.
- New cases: effective domain default vs per-resource override; missing domain → error; domain not
  in vocab → error; type required → error; content-type `"string"`→`Text` mapping; same-key/
  different-domain accepted (not a duplicate); triple idempotency (re-import → updated); dependency
  wired (`AddRelationshipAsync` called with resolved ids); self-reference rejected; ambiguous /
  unresolved dependency → error (and **nothing written** — assert no upsert happened, proving
  pre-write validation).

**`ResourceServiceTests.cs` (from the `Resource_Save` change):**
- Re-pin the `SaveResourceAsync` repo mock (+`domain`); add a case asserting the effective
  `Domain` from `SaveResourceRequest` is passed through to `SaveResourceAsync`.

## Verification

1. **DB builds + publishes** (full MSBuild dacpac → SqlPackage `/p:DropObjectsNotInSource=True`;
   republish after each sproc edit).
2. **`sqlcmd` exercise:** domain-aware `Resource_Upsert` **and `Resource_Save`** — same
   `(type+key)` with different `@Domain` creates **two** rows each with its domain tag; re-run same
   triple → `updated`, no dup; `ResourceTag_SetForResource` leaves the domain tag intact after a
   user-tag replace; `Resource_GetAllKeys` returns the `(id,domain,type,key)` tuples.
3. **C# build + unit tests** (`dotnet build` C# projects; `dotnet test` — existing + new import
   tests green).
4. **Import smoke** (throwaway xUnit test against live `(localdb)`, deleted before commit): import
   a doc with `defaults.domain`, a per-resource domain override, controlled-vocab tags, and
   `dependencies[]`; assert two same-key/different-domain resources are distinct, the domain tag is
   applied, edges are written, and a re-import is idempotent (all `updated`, no new rows/edges).

Then: flip slice #5 → Done and slice #6 → Planning in `00-implementation-plan-list.md`, and
commit. Per our pattern, planning is on the stronger model; execution switches to the cheaper one.
