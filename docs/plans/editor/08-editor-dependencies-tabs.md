# Slice #8 — Editor UI — Dependencies / Dependent On tabs

## Context

Eighth implementation slice (design: `docs/plans/editor/resource-logical-model-and-editor-ux.md`
§4.5 + §12 "Dependencies / Dependent On tabs (agreed)"; list:
`docs/plans/editor/00-implementation-plan-list.md`). Slices #6/#7 shipped the editor shell +
General/Review/Tags tabs and wired the deferred tag write. This slice adds the **Dependencies**
(out-edges) and **Dependent On** (in-edges) tabs and finally wires the **relationship edge write on
save** — the last deferred write in the editor.

Scope (from the master list, **as re-scoped by this plan — see OQ1**): a searchable resource
picker restricted to the **same domain**, over **existing** resources; **both directions editable**
(editing the in-edge side makes the system write the edge on the *other* resource); edge
**reconciliation on the parent's Save**; server-side **same-domain / self-loop / exists**
validation. **The nested "Create new…" target flow is deferred to #9**, where the history-pushing
nav-stack + dirty-guard it depends on actually land (OQ1). **Delete + dirty-guard + nav-stack →
#9.**

This slice spans: a **DB** read (a new picker search sproc + a domain column added to the existing
relationship read), a **service** write (relationship reconciliation, previously ignored) + a new
search method, and a **UI** (two tabs sharing a reusable direction component + a reusable picker).
The contracts it needs already exist from slice #3 (`ResourcePickerRequest/Item/Response`,
`DependencyRowEditor`, `SaveResourceRequest.DependsOnUids`/`DependentOnUids`,
`ResourceEditorModel.DependsOn`/`DependentOn`).

**Additionally (mid-planning decision):** #8 stands up the project's first **committed Playwright
regression suite** (`@playwright/test`) under `tools/e2e/`, with DB seed/teardown fixtures — the
permanent E2E home future slices add specs to, replacing the drive-once-then-discard scripts used
in #6/#7. See *E2E regression suite* below. This roughly doubles #8's surface (feature work **plus**
a test-infra workstream) — flag if you'd rather split it into #8a (feature) / #8b (E2E harness);
default is to keep both in #8 per your call.

## Verified starting state

- **Relationship sprocs exist** (slices #2/#4): `ResourceRelationship_Add` (idempotent, no-op on
  dup; self-loop blocked by `CK_ResourceRelationship_NoSelfLoop`), `_Remove` (delete by from+to),
  `_GetForResource` (both directions in one result set via `UNION ALL`, `Direction` =
  `'DependsOn'` | `'DependentOn'`).
- **Repo methods exist:** `GetRelationshipsForResourceAsync(resourceId)`,
  `AddRelationshipAsync(fromId, toId)`, `RemoveRelationshipAsync(fromId, toId)` (all int-keyed).
- **Service methods exist but are standalone** and **not** used by the editor:
  `AddRelationshipAsync(fromUid, toUid)` / `RemoveRelationshipAsync(fromUid, toUid)` (each resolves
  both uids and validates self-loop + endpoints-exist). **`SaveResourceAsync` ignores
  `request.DependsOnUids` / `request.DependentOnUids`.**
- **Editor model is wired for reads:** `BuildEditEditorModel` already splits
  `GetRelationshipsForResourceAsync` into `model.DependsOn` / `model.DependentOn` lists of
  `DependencyRowEditor`. **`ResourceEditor.razor` renders neither tab yet.**
- **Gap — no domain on relationship rows:** `ResourceRelationship_GetForResource` does **not**
  return the other resource's domain, and `BuildDependencyRowEditor` hard-sets
  `OtherDomain = null // #5`. Domain is a `ResourceTag` value (joined via the `IsDomainTag=1`
  `TagDefinition`), not a `Resource` column — the same pattern `Resource_GetByResourceUid` /
  `Resource_CheckUnique` / `Resource_GetAllKeys` already use.
- **Gap — no picker read:** `ResourcePickerRequest/Item/Response` contracts exist (slice #3), but
  there is **no picker sproc, no repo method, and no service method**. `Resource_GetItems` (the
  grid sproc) is too heavy and has no same-domain filter.

## DB

- **`Resource_SearchForPicker.sql`** (NEW; + `<Build>` item in `.sqlproj`): backs the picker.
  Params `@SearchFor NVARCHAR(255)=NULL, @Domain NVARCHAR(2000)=NULL, @ResourceTypeId INT=NULL,
  @ExcludeResourceUid VARCHAR(40)=NULL, @Skip INT=0, @Take INT=20, @TotalRecords INT OUTPUT`.
  Returns `(ResourceUid, ResourceName, ResourceKey, ResourceTypeName, Domain)`. Domain via the
  `LEFT JOIN ResourceTag dt ON dt.ResourceId=r.ResourceId AND dt.TagDefinitionId=@DomainTagDefId`
  pattern. **Same-domain filter:** `((@Domain IS NULL AND dt.TagValue IS NULL) OR dt.TagValue =
  @Domain)` — matches when both sides have the same domain, *or* both have none (the "Unused"
  state, §6); a parent-with-domain never matches a target-without, and vice-versa. `@SearchFor`
  matches `ResourceName` **or** `ResourceKey` via `LIKE '%'+@SearchFor+'%'`; `@ExcludeResourceUid`
  drops self; `@ResourceTypeId` optional type narrow. Paged with `OFFSET/FETCH`; `@TotalRecords`
  = full match count (for a "showing N of M" hint). **No BIT columns in the result → the
  untyped-`CASE`→INT/BIT trap doesn't apply here** (RD12), but keep it in mind for any future edit.
- **`ResourceRelationship_GetForResource.sql`** (MODIFY): add `other`'s domain to **both** `UNION
  ALL` branches — `LEFT JOIN ResourceTag odt ON odt.ResourceId = other.ResourceId AND
  odt.TagDefinitionId = @DomainTagDefId`, select `odt.TagValue AS OtherDomain`. Declare
  `@DomainTagDefId` once at the top. Closes the `// #5` TODO and lets the tabs show each row's
  domain.
- **Build/publish:** rebuild the dacpac via full MSBuild → publish via SqlPackage
  `/p:DropObjectsNotInSource=True`, and **republish after every sproc edit** (the recurring
  #4/#6/#7 lesson — a stale publish silently broke reads twice before). Verify both sprocs via
  `sqlcmd` (`SET QUOTED_IDENTIFIER ON;` prefix — the DB has filtered indexes).

## Service + repository

- **`ResourceRelationshipItem`** (model): add `string? OtherDomain`.
- **`IResourceRepository` / `ResourceSqlRepository`:**
  - `GetRelationshipsForResourceAsync` — read the new `OtherDomain` column into the item.
  - `SearchResourcesForPickerAsync(ResourcePickerRequest, ct)` → `(List<ResourcePickerItem>, int
    Total)` via `Resource_SearchForPicker`.
- **`IResourceService` / `ResourceService`:**
  - `SearchResourcesForPickerAsync(ResourcePickerRequest, ct)` →
    `ApiServiceResponse<ResourcePickerResponse>` (validate `Take` cap, forward to repo).
  - `BuildDependencyRowEditor` — populate `OtherDomain` from the item (drop the `// #5` null).
  - **`SaveResourceAsync`** — wire relationship reconciliation (the deferred piece). **Two-phase,
    because reconciliation needs the parent's `resourceId` which doesn't exist until `Resource_Save`
    in Create mode (RD2):**
    1. **Pre-write validation** (before `Resource_Save`, alongside tag validation — *all* validation
       before *any* write): dedupe `DependsOnUids` / `DependentOnUids`; reject any uid equal to the
       parent's own uid (self-loop, RD5); resolve every desired uid via `GetResourceByUidAsync`
       (reject not-found); reject any target whose domain ≠ the **parent's effective domain**
       (same-domain, treating both-null as equal — RD4). *Effective domain* = the **persisted**
       domain (`existing.Domain`) in Edit, `request.Domain` in Create — **not** blindly
       `request.Domain`, since the frozen-domain guard only rejects a *changed* request domain when
       both sides are non-empty, so a Save that omitted Domain in Edit would otherwise validate
       targets against an empty domain (RD14). Collect as validation errors under a stable
       `request.DependsOnUids` / `request.DependentOnUids` key → return before writing. Cache the
       resolved uid→(id) map.
    2. **Post-write reconciliation** (after `SetResourceTagsAsync`): load current edges via
       `GetRelationshipsForResourceAsync(resourceId)`, split by `Direction`. **Full-set replace per
       direction (RD3), scoped so each direction only touches its own edges (RD1):**
       - *DependsOn* (out, `from=this`): `toAdd = desiredIds − currentToIds` →
         `AddRelationshipAsync(this, target)`; `toRemove = currentToIds − desiredIds` →
         `RemoveRelationshipAsync(this, target)`.
       - *DependentOn* (in, `to=this`): `toAdd = desiredIds − currentFromIds` →
         `AddRelationshipAsync(target, this)` (**system writes the far side**, RD7); `toRemove` →
         `RemoveRelationshipAsync(target, this)`.
  - **Non-atomic note (RD8):** `Resource_Save` + `SetResourceTagsAsync` + N relationship sprocs are
    not one transaction — matches the import/#7 precedent. Documented; a single orchestrating sproc
    is future hardening, not this slice.
- Keep the standalone `AddRelationshipAsync(uid,uid)`/`RemoveRelationshipAsync(uid,uid)` service
  methods (tests depend on them); the Save path does **not** route through them (it uses the
  int-keyed repo methods with ids resolved once).

## UI — components

- **`Components/Editor/ResourcePicker.razor`** — a reusable same-domain picker. A
  `MudAutocomplete<ResourcePickerItem>` with a `(string, CancellationToken) => Task<IEnumerable<…>>`
  `SearchFunc` calling `SearchResourcesForPickerAsync` (the exact pattern proven in #7's "Add
  existing tag"). Params: the parent `Domain`, the `ExcludeResourceUid`, and the set of
  already-added uids to exclude; `ToStringFunc` shows `Name · Type · Domain`. Emits the chosen
  `ResourcePickerItem`. **Gated on domain being set (OQ2):** disabled with a hint ("Choose a
  Subscription first") until the parent has a domain — a same-domain search with no domain is
  meaningless.
- **`Components/Editor/DependencyTab.razor`** — one reusable component, used twice (Direction =
  `"DependsOn"` / `"DependentOn"`), each with its own help line ("What this depends on" /
  "What depends on this"). Renders the direction's non-removed rows: `Name` · `Type` · `Domain` ·
  remove; below them the `ResourcePicker` (hidden in View). Add via picker → append a
  `DependencyRowEditor` (`IsNew=true`) unless a non-removed row with that uid already exists; if a
  **removed** row with that uid exists, un-remove it instead of duplicating (RD10). Remove → drop
  `IsNew` rows outright, else set `IsRemoved=true` (mirror of #7's tag-row removal). `ReadOnly`
  param (View → no picker/remove).
- **`ResourceEditor.razor` wiring:**
  - Add **Dependencies** and **Dependent On** `MudTabPanel`s between **Tags** and **Review**.
  - **Include in the save request:** flatten each direction's non-removed rows →
    `DependsOnUids` / `DependentOnUids` (deduped).
  - **Domain-change-in-Create clears added dependency rows (RD6):** when the General Domain changes
    in Create, clear both `DependsOn`/`DependentOn` lists — they were validated same-domain against
    the *old* domain and could now be cross-domain. Needs a callback from `GeneralTab.OnDomainChanged`
    up to the parent (add `OnDomainChanged`, analogous to #7's `OnResourceTypeChanged`). In Edit the
    domain is frozen, so this never fires there.
  - **`ApplyServerErrors` routes the relationship keys:** extend the existing handler so
    `request.DependsOnUids` / `request.DependentOnUids` validation errors surface (via snackbar,
    exactly as #7 did for `request.Tags` — they don't map cleanly onto a single field). The picker
    enforces same-domain client-side, so these are a server backstop for a stale client, not the
    normal path.
- **`ReviewTab.razor`** — extend the roll-up to list the Dependencies / Dependent On rows (name ·
  type), matching how #7 added the tags roll-up.
- **`app.css`** — a small `rm-deps-editor` block (compact rows, picker layout) in the `rm-` style.

## E2E regression suite (committed — `@playwright/test`)

Promotes `tools/e2e` from a drive-script toolkit into a committed, re-runnable regression suite.
The MudBlazor locator helpers (`mud-helpers.js` — `clickSelect`, `clickSelectInScope`,
`pickOption`) stay the shared core; the runner + auto-managed server + DB fixtures are new.

- **Runner + config.** Add `@playwright/test` (devDependency) to `tools/e2e/package.json` (it
  re-exports `chromium` and pulls `playwright-core` transitively) + a `"test:e2e": "playwright test"`
  script. `tools/e2e/playwright.config.js`:
  - `use: { baseURL, channel: 'chrome', headless: true, launchOptions: { args: ['--no-sandbox'] },
    screenshot: 'only-on-failure', trace: 'retain-on-failure' }` — **system Chrome, no bundled
    Chromium download** (`channel: 'chrome'`, the proven #7 setup — so `npx playwright install` is
    NOT needed).
  - `webServer: { command: 'dotnet run --project UI/ResourceMapper.UI.Web --no-launch-profile',
    cwd: <repo root>, url: baseURL, env: { ASPNETCORE_ENVIRONMENT: 'Development', ASPNETCORE_URLS },
    reuseExistingServer: !process.env.CI, timeout: 120_000 }` — the runner starts/stops the app and
    waits for it, replacing the manual `dotnet run &` / `taskkill` dance (RD19).
  - `workers: 1`, `fullyParallel: false` — a single shared localdb has no parallel isolation (RD17).
  - `testDir: './tests'`.
- **DB seed/teardown.** `tools/e2e/sql/seed.sql` + `tools/e2e/sql/cleanup.sql`, invoked from a
  `globalSetup` (cleanup-then-seed — idempotent even after a crashed prior run) and `globalTeardown`
  (cleanup) that **shell out to `sqlcmd`** — reliable for `(localdb)\MSSQLLocalDB`; the `mssql` npm
  client's localdb named-pipe handling is fiddly, avoid it (RD18). Both scripts start with
  `SET QUOTED_IDENTIFIER ON;` (filtered indexes).
- **Fixture data.** A dedicated E2E `ResourceType` (`E2eDepType`) + a Link tag def + a
  controlled-vocab multi-valued tag def + entry-point templates (also usable by a future #7 spec),
  and picker candidates: **2 resources in `non-prod` + 1 in `prod`**, each with a Domain
  `ResourceTag` row **referencing the existing shared domain `TagDefinition`** (value from its
  `AllowedValues`) — never creating or deleting that shared system row (RD15).
- **Isolation model.** Picker candidates are a **read-only baseline** seeded once; each spec's
  **resource-under-edit** is created via the UI's Create flow so mutating specs don't depend on each
  other; `globalTeardown` wipes all E2E-prefixed rows regardless. `workers:1` + the fixed E2E prefix
  bound any leakage (RD17).
- **First spec — `tests/dependencies.spec.js`.** The slice-#8 flow, now as assertions (see Tests):
  picker offers only same-domain targets; add DependsOn + DependentOn; Save; reload asserts
  round-trip; open the DependentOn target asserts the far-side edge (RD7); remove one → Save →
  asserts removed.
- **README + package.** Update `tools/e2e/README.md` for the runner (`npm install`, `npm run
  test:e2e`, the seed/cleanup model, "system Chrome — no `playwright install`"), and note the suite
  targets **local localdb** and assumes the dacpac is published; **CI wiring is out of scope** (a
  pipeline would need Chrome + localdb + a published dacpac — OQ6). `example-drive-editor.js` stays
  for quick ad-hoc pokes.

## Rubber-duck — bugs / logic traps caught up front

- **RD1 — per-direction reconciliation scope.** DependsOn (out, `from=this`) and DependentOn (in,
  `to=this`) are disjoint edge sets (a self-loop is forbidden). Each direction's full-set-replace
  must only add/remove within its own set — reconciling DependsOn must never remove an in-edge, and
  vice-versa. A mutual dependency `(this→X)` + `(X→this)` is two distinct edges, each owned by its
  own direction; reconciling one leaves the other untouched.
- **RD2 — validate-before-write, but reconcile-after-write (two-phase).** In Create the parent has
  no `resourceId` until `Resource_Save`, so edges can't be written until after it. But we must not
  persist a resource whose relationships are invalid. Split: resolve + validate *all* desired
  targets (exists / same-domain / not-self) **before** any write; reconcile edges **after** the
  resource + tags are written, using the new `resourceId` and the cached resolutions.
- **RD3 — full-set-replace ⇒ empty list wipes that direction.** The request carries the *complete*
  desired set per direction (not a delta), so an empty `DependsOnUids` removes all out-edges — same
  semantics as #7's tag write. The editor always re-sends the full loaded set, so a normal Save is
  safe; a caller that omits the lists would wipe edges. Documented, matching #7; the non-nullable
  `List<string>` contract can't express "don't touch this direction" (see OQ3).
- **RD4 — same-domain enforced server-side, not just in the picker.** The picker only offers
  same-domain targets, but a stale/tampered client could send a cross-domain uid. Reject any
  newly-added target whose domain ≠ the parent's on save. Treat both-null domains as equal (the
  "Unused" state). Existing edges are assumed already-valid (validated when created), so only
  **newly-added** targets are domain-checked.
- **RD5 — self-loop.** Reject any desired uid equal to the parent's own uid before writing (the DB
  `CK_ResourceRelationship_NoSelfLoop` is the backstop, but a clean validation error beats a SQL
  exception). In Create the parent isn't persisted, so it can't appear in picker results anyway.
- **RD6 — domain change in Create invalidates added deps.** Rows added under domain A become
  cross-domain if the user then switches to domain B. Clear the added dependency rows on Domain
  change in Create (analogous to #7 RD6's tag re-seed on Type change). Edit freezes domain, so no
  issue there.
- **RD7 — DependentOn writes the FAR side.** Adding "X depends on me" writes edge `(from=X,
  to=this)`; removing it deletes that edge — i.e. editing the in-edge tab mutates the *other*
  resource's out-edges. This is the design's explicit behavior ("the system writes the edge on the
  other resource's side"). Direction math verified against `ResourceRelationship_GetForResource`
  (DependentOn rows carry `OtherResourceId = FromResourceId`).
- **RD8 — non-atomic save.** Resource + tags + N edge sprocs are not one transaction; a mid-save
  failure can leave a partially-reconciled graph. Documented; matches import/#7; hardening deferred.
- **RD9 — picker exclusions.** Exclude self (`ExcludeResourceUid`) and rows already present in the
  *same* direction (avoid dup rows / the sproc/DB would no-op a dup edge anyway). Do **not** exclude
  a resource already linked in the *other* direction — mutual dependencies (2-cycles) are valid
  (the model forbids only self-loops and duplicate `(from,to)` edges, not cycles).
- **RD10 — client re-add of a removed row.** Re-adding a target whose row is flagged `IsRemoved`
  should un-remove that row, not append a duplicate (mirror of #7's tag re-add).
- **RD11 — `OtherDomain` was hard-null (`// #5`).** Populate it via the domain join added to
  `ResourceRelationship_GetForResource` so rows can show domain and the same-domain story is
  visible. Sproc edit ⇒ **republish**.
- **RD12 — untyped `CASE`→INT vs C# `ReadBoolean` (BIT) trap.** Bit that hid for two slices; the new
  picker sproc returns no BIT columns so it's not at risk, but flagged because it only ever fires on
  a *non-empty* result set — easy to miss until real data exists.
- **RD14 — Edit-mode same-domain uses the persisted domain, not `request.Domain`.** The
  frozen-domain guard only errors on a *changed* request domain when both existing and request
  domains are non-empty; a Save that sent an empty/omitted Domain in Edit would slip past it and
  then validate relationship targets against an empty domain (falsely rejecting real same-domain
  targets, or falsely accepting cross-domain ones). Use `existing.Domain` (persisted) as the
  effective domain in Edit; `request.Domain` in Create.
- **RD13 — "Create new…" depends on #9.** The design's nested-target create is a **route that
  pushes browser history**, popping back to the parent with the child linked (persistence timing:
  child persists immediately, edge on the parent's Save). That machinery — the nav-stack +
  `RegisterLocationChangingHandler` dirty-guard — is slice #9. Building it in #8 would pull #9
  forward; building a throwaway dialog-based nested editor would contradict the design ("nested
  editors are routes; dialogs are only for tag-create"). **Defer to #9** (OQ1); #8's picker
  empty-state simply says the target must exist first.

### E2E-suite traps

- **RD15 — cleanup must preserve shared system rows.** The domain `TagDefinition` (`IsDomainTag=1`,
  key `Domain`), the `TagContentType` rows, and any real seed are shared app state — deleting the
  domain def would break the whole app (identity, picker, tags). The E2E fixture **references** them
  by lookup and cleanup targets **only** E2E-prefixed rows (by the `E2eDepType` id + the E2E
  tag-def keys), never the shared rows.
- **RD16 — FK-safe cleanup order + cleanup-first seed.** Delete children before parents:
  `ResourceRelationship` (edges touching E2E resources) → `ResourceTag` (of E2E resources) →
  `Resource` (of `E2eDepType`) → `ResourceTypeTag` (of `E2eDepType`) → `TagDefinition` (E2E keys
  only) → `ResourceType` (`E2eDepType`). `globalSetup` runs cleanup **then** seed so a crashed prior
  run leaves no residue.
- **RD17 — no parallel isolation on a shared localdb.** `workers:1` + `fullyParallel:false`; a
  read-only candidate baseline plus a per-spec subject resource keeps mutating specs independent.
- **RD18 — use `sqlcmd`, not the `mssql` node client, for localdb.** `(localdb)\MSSQLLocalDB` needs
  the instance named pipe, which the `mssql` package handles poorly; `sqlcmd` is the proven path.
  `SET QUOTED_IDENTIFIER ON;` in every batch (filtered indexes).
- **RD19 — `webServer` cwd + reuse.** `webServer.cwd` must be the **repo root**, not `tools/e2e`
  (else `dotnet run --project UI/...` can't resolve the path — the exact bug hit while smoke-testing
  the #7 tooling). `reuseExistingServer: !CI` so a dev server already on the port is reused, not
  double-started (port clash).
- **RD20 — `channel: 'chrome'` (no browser download) is a hard requirement here.** The environment
  has system Chrome but the ~150 MB `playwright install` download is undesirable/blocked; the config
  must pin `channel: 'chrome'`. CI would additionally need Chrome + localdb + a published dacpac —
  out of scope (OQ6).

## Open questions / decisions (documented defaults)

- **OQ1 — defer "Create new…" to #9 (recommended).** The nested-create target needs #9's
  history-pushing nav-stack + dirty-guard (RD13). *Default:* scope #8 to the picker over existing
  same-domain resources + both-direction reconciliation; move the nested "Create new…" target
  (domain-locked, pop-back, persistence-timing) into #9 alongside the nav-stack it requires. This
  plan updates the master-list scope cells for #8/#9 to match. Flag if you'd rather build a
  stopgap nested create in #8.
- **OQ2 — picker gated on domain (Create).** *Default:* disable the picker with a hint until the
  parent's Domain is set (a same-domain search with no domain is meaningless). In Edit the domain is
  always set.
- **OQ3 — full-set-replace vs. null-means-skip.** *Default:* full-set-replace per direction (match
  #7's tag semantics; the editor always sends the full set). A nullable `DependsOnUids?` /
  `DependentOnUids?` where `null` = "leave this direction alone" would be safer for partial/API
  saves but is a contract change — deferred.
- **OQ4 — allow mutual dependencies (2-cycles).** *Default:* yes — the model forbids only
  self-loops and duplicate edges, not cycles; the picker doesn't cross-exclude the other direction.
- **OQ5 — target resolution: per-uid vs batch.** *Default:* resolve each desired uid via
  `GetResourceByUidAsync` (gives id + domain in one call, reused for validation). Chatty for large
  N; a batch `Resource_GetByUids` is a future optimization.
- **OQ6 — E2E suite is local-only for now.** *Default:* the committed suite is a **local** regression
  home (run manually / pre-commit); CI integration (Chrome + localdb + a published dacpac in the
  pipeline) is a separate future task, not a #8 blocker.
- **OQ7 — backfilling #6/#7 flows as specs.** *Default:* #8 stands up the harness + the one
  dependency spec; retroactively porting the General/Tags flows into specs is out of scope for #8
  (the harness makes it easy to add later).
- **OQ8 — sizing / split.** *Default:* keep the feature work and the E2E harness both in #8 per your
  request; flag that they're separable (#8a feature / #8b harness) if the slice balloons in
  execution.

## Out of scope (later slices)

- **Nested "Create new…" target + nav-stack + dirty-guard + delete → #9** (OQ1/RD13).
- **Typed relationship catalog** (`ProducesTo`/`ConsumesFrom`, `RelationshipType`, inverse labels)
  — design-deferred.
- **UI concurrency / lost-update** on a stale relationship set (e.g. someone adds a dependent
  elsewhere after load, then this Save's full-replace drops it) — design says UI concurrency is out
  of scope.
- **Acyclicity enforcement** — the model permits cycles by design.

## Tests

- **`ResourceServiceTests`** (mocked repo):
  - `SaveResourceAsync` reconciles **DependsOn**: adds missing (`AddRelationshipAsync(this,
    target)`), removes extra (`RemoveRelationshipAsync(this, target)`); asserts the from/to ids.
  - `SaveResourceAsync` reconciles **DependentOn**: writes the far side (`AddRelationshipAsync(
    target, this)`) — RD7 direction check.
  - Empty desired list removes all edges in that direction; a non-empty other direction is
    untouched (RD1).
  - Cross-domain target → validation error, **no** edge write (RD4).
  - Self-referential uid → validation error, no write (RD5).
  - Unknown target uid → validation error, no write.
  - Duplicate uids in a direction collapse (dedup).
  - `GetResourceEditorModelAsync` populates `OtherDomain` on relationship rows (needs the modified
    read + a repo stub returning `OtherDomain`).
  - `SearchResourcesForPickerAsync` forwards `Domain`/`ExcludeResourceUid`/`Take` to the repo.
  - **Test-fixture note:** add a default `GetRelationshipsForResourceAsync(...)` → empty-list stub
    in the test ctor (like #7 did for tags) so existing happy-path save tests don't NRE now that
    Save reconciles relationships.
- **Committed E2E spec** — `tools/e2e/tests/dependencies.spec.js`, run via `npm run test:e2e` (the
  runner starts the app + seeds via `globalSetup`, tears down via `globalTeardown` — no manual
  `dotnet run`/`taskkill`/`sqlcmd` dance). The flow, as assertions against the seeded baseline (2
  `non-prod` candidates + 1 `prod`): create/open a `non-prod` subject → **Dependencies**: the picker
  offers only `non-prod` targets (the `prod` one absent); add one → **Dependent On**: add another →
  **Save** → reload asserts both round-trip; open the *target* of the DependentOn edge and assert it
  now lists the subject under **Dependencies** (the far-side write, RD7); remove one dep → Save →
  assert removed. This spec is the permanent regression for the slice; ad-hoc pokes can still use
  `example-drive-editor.js`.

## Verification

1. DB builds + publishes (full MSBuild dacpac → SqlPackage `/p:DropObjectsNotInSource=True`;
   **republish after both the new sproc and the `ResourceRelationship_GetForResource` edit**); both
   sprocs verified via `sqlcmd`.
2. `dotnet build HT.ResourceMapper.slnx` clean (only the known non-SDK `sqlproj` MSB4278);
   `dotnet test` green (existing 73 server + new relationship tests; the 2 pre-existing unrelated
   `HT.Api.Service.Contracts.Tests` failures remain out of scope).
3. `cd tools/e2e && npm install && npm run test:e2e` green — the committed suite starts the app
   (webServer), seeds localdb, runs `dependencies.spec.js`, and cleans up. Confirm it also passes on
   a **second** run (idempotent seed/teardown, RD16) and leaves no E2E rows behind.

Then: flip slice #8 → Done and slice #9 → Planning in `00-implementation-plan-list.md`, add an
"Execution notes" section here, and commit. Per our pattern, planning is on the stronger model;
execution switches to the cheaper one.
