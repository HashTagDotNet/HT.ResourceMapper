# Resource Logical Model

## 1. Context & purpose

This is the settled **logical resource model** for HT.ResourceMapper, consolidated across
several design passes. It is intended as the **foundation for a full end-to-end build** — the
exercise goal is to take this model to a *near-perfect working stack* — so it aims to be
internally consistent and implementation-ready. Implementation is a later phase; this document
is design only.

**What the app is for — the yardstick for every decision below:** it **replaces wiki-style
link pages** — a browser bookmark manager for cloud resources. The *primary* job is to **find
a resource and jump to its links** (Overview / Logs / Kusto / …). Relationships between
components are a **secondary** concern. It is a **static reference site**, so the model favors
**limited-but-useful** structure over speculative generality.

**Surfaces:**

- **Home page (grid) = the bookmark manager** — find a resource; name-click opens its
  **primary resource URL**; external-links menu; pinned Domain column; `DisplayName` +
  `DisplayOrder` tag ordering; filtering/faceting with controlled-vocabulary picklists.
- **Details page + editor** — create/edit resources and their tags/links; where dependency
  relationships surface. **Greenfield: neither exists yet.** The **manual editor is the next
  build** (required before release) — the reason this model must be complete now.

## 2. Overview (ER)

```
                    ┌──────────────┐        ┌────────────────────────────┐
                    │ ResourceType │ 1----* │ ResourceTypeTag            │
                    │  TypeName    │        │  (entry-point template)    │
                    │ AllowCustom… │        │  IsDefaultPrimary          │
                    └──────┬───────┘        └───────────────┬────────────┘
                         1 │                               * │ (FK)
                           │ (required; part of identity)    │
                         * │                               1 ▼
     ┌───────────────┐  * ┌▼─────────┐ *   ┌──────────────────────────────┐
     │ ResourceTag   ├───►│ Resource │◄───►│ ResourceRelationship         │
     │ (applied val) │    │  (node)  │self │  from→to, DependsOn, no loops │
     └──────┬────────┘    └────┬─────┘ ref └──────────────────────────────┘
          * │                  │ PrimaryTagDefinitionId ──┐
            │ (FK)             └──────────────────────────┼──► (a Link TagDefinition)
          1 ▼                                             │
     ┌──────────────┐ *                                   │
     │ TagDefinition│◄────────────────────────────────────┘
     │ (dictionary) │ 1
     └──────┬───────┘
         *  │ (FK)
          1 ▼
     ┌───────────────┐
     │ TagContentType│  Text | Link  (constrained)
     └───────────────┘
```

Relationships:

- `Resource *–1 ResourceType` — every resource has exactly one type (**required**; part of identity).
- `Resource 1–* ResourceTag *–1 TagDefinition` — applied tag values reference shared definitions.
- `TagDefinition *–1 TagContentType` — content/render type (`Text | Link`).
- `Resource –(PrimaryTagDefinitionId)→ TagDefinition` — the default click-through Link tag.
- `ResourceType 1–* ResourceTypeTag *–1 TagDefinition` — per-type entry-point template.
- `Resource *–* Resource` via `ResourceRelationship` — directed `DependsOn` edges, self-referential.

**Placement & identity are expressed through tags:** the **Domain** and **Environment** are
`TagDefinition`s applied via `ResourceTag`. Domain additionally *participates in identity*
(§5) and is the designated **boundary tag** (§6). Dropped from the legacy schema: `TagValueType`.

## 3. Consolidated virtual JSON

```jsonc
{
  // ── Tag DICTIONARY (shared, setup-time; TagDefinition + TagContentType) ──────────────
  "tagDefinitions": [
    { "key": "Domain", "displayName": "Subscription",     // stable internal key vs. shown label
      "contentType": "Text", "isDomainTag": true,          // the boundary/identity tag
      "requirementLevel": "Error",                          // required; missing = hard error
      "isSystemTag": true, "isMultiValued": false,          // system-managed, single value
      "allowCustomValue": false, "allowedValues": ["prod","non-prod"], "displayOrder": 10 },

    { "key": "Environment", "displayName": "Environment",
      "contentType": "Text", "requirementLevel": "Suggested",
      "isMultiValued": true,                                // MULTI-valued = discrete rows
      "allowCustomValue": false,
      "allowedValues": ["prod","preprod","dev","develop","test","integrationtest","localhost"],
      "displayOrder": 20 },

    { "key": "Overview", "displayName": "Overview", "contentType": "Link",
      "requirementLevel": "Optional", "isMultiValued": false, "displayOrder": 30 }
    // …Logs, Kusto, Application, etc.
  ],

  // ── A ResourceType with its entry-point TEMPLATE (ResourceTypeTag) ───────────────────
  "resourceType": {
    "typeName": "AppService", "allowCustomTags": true,
    "entryPoints": [                                        // rows in ResourceTypeTag (FK → TagDefinition)
      { "tag": "Overview",      "isDefaultPrimary": true, "requirementLevel": "Suggested" },
      { "tag": "Configuration" },
      { "tag": "Logs" },
      { "tag": "Kusto" }
    ]
  },

  // ── A RESOURCE (shared Redis — the interesting multi-environment case) ───────────────
  "resource": {
    "resourceUid":  "a1b2c3d4-…",     // VARCHAR(40) globally-unique surrogate (URLs/API)
    "resourceKey":  "orders-redis",   // RAW key; unique per (Domain + Type), NOT global
    "resourceName": "Orders Redis",   // NOT NULL
    "resourceTypeId": 12,             // NOT NULL — required, and part of identity
    "primaryTagDefinitionId": 30,     // → the "Overview" Link tag (single-valued)
    "createdOn": "2026-01-04T12:00:00Z", "updatedOn": null,   // CreatedBy/UpdatedBy deferred

    "tags": [                          // ResourceTag rows
      { "key": "Domain",      "value": "non-prod" },                    // required boundary tag
      { "key": "Environment", "value": "dev" },                         // ┐ multi-valued =
      { "key": "Environment", "value": "test" },                        // ┘ DISCRETE rows
      { "key": "Overview",    "value": "https://portal…/overview" },    // Link tag (its own link)
      { "key": "Logs",        "value": "https://portal…/logs" }
    ],

    "dependsOn": [],                   // ResourceRelationship (from = this); build deferred
    "dependedOnBy": []                 // in-edges; read view
  }
}
```

Identity of this row = `("non-prod" + AppService + "orders-redis")`. A second `orders-redis`
of the same type in the `prod` domain is a **distinct** resource.

## 4. Entities

### 4.1 Resource (the node)

| Field | Type | Notes |
|---|---|---|
| `ResourceId` | INT IDENTITY, PK | internal surrogate |
| `ResourceUid` | VARCHAR(40), UNIQUE | globally-unique external surrogate (URLs/API), write-once |
| `ResourceKey` | NVARCHAR(250) | **raw** business key; unique per `(Domain + Type)`, **not** global |
| `ResourceTypeId` | INT **NOT NULL**, FK → ResourceType | required; **part of identity** |
| `ResourceName` | NVARCHAR(250) **NOT NULL** | display name (resolves the old SQL/EF nullability mismatch) |
| `Description` | NVARCHAR(2000), NULL | |
| `PrimaryTagDefinitionId` | INT NULL, FK → TagDefinition | the default click-through **Link** tag |
| `CreatedOn` | DateTime2(0) NOT NULL | default `SYSUTCDATETIME()` |
| `UpdatedOn` | DateTime2(0) NULL | |

- **No lifecycle/status field**; **no standalone tier flag** (tier = the Domain value).
- Audit **who** (`CreatedBy`/`UpdatedBy`) is **deferred** (needs auth).
- The old global-unique `UK_Resource_ResourceKey` is **relaxed**; `(Domain + Type + Key)`
  uniqueness is enforced in code (§5).

### 4.2 ResourceType + entry-point template

**ResourceType** (lookup/classification):

| Field | Type | Notes |
|---|---|---|
| `ResourceTypeId` | INT IDENTITY, PK | |
| `ResourceTypeUid` | VARCHAR(40), UNIQUE | |
| `TypeName` | NVARCHAR(250), UNIQUE | e.g. `AppService`, `Queue`, `Database` |
| `AllowCustomTags` | BIT | may resources of this type carry ad-hoc tags |
| `CreatedOn` / `UpdatedOn` | DateTime2(0) | |

**ResourceTypeTag** (redesigned — the per-type entry-point template; was dead code):

| Field | Type | Notes |
|---|---|---|
| `ResourceTypeTagId` | INT IDENTITY, PK | |
| `ResourceTypeId` | INT, FK → ResourceType | |
| `TagDefinitionId` | INT, **FK → TagDefinition** | (was a free string — now a real FK) |
| `IsDefaultPrimary` | BIT | ≤1 per type; seeds a new resource's `PrimaryTagDefinitionId` |
| `RequirementLevel` | enum, NULL | optional per-type override of the definition's default |

A type declares its standard entry-point Link tags + the default primary; new resources
inherit, and may override their own `PrimaryTagDefinitionId`. (*Per-type `DisplayOrder`
override is deferred — a single global order suffices initially.*)

### 4.3 Tag dictionary: TagDefinition + TagContentType

**TagDefinition** (the single source of tag metadata):

| Field | Type | Notes |
|---|---|---|
| `TagDefinitionId` | INT IDENTITY, PK | |
| `TagDefinitionUid` | VARCHAR(40), UNIQUE | |
| `TagDefinitionKey` | NVARCHAR(50), UNIQUE | **stable internal key** (used for identity/import/wire) |
| `DisplayName` | NVARCHAR | **user-facing label** (NEW), decoupled from the key |
| `TagContentTypeId` | INT, FK → TagContentType | |
| `RequirementLevel` | enum (NEW) | `Error` \| `Suggested` \| `Optional` (replaces a required bool) |
| `IsMultiValued` | BIT | multi-valued ⇒ **discrete `ResourceTag` rows** (never a joined string) |
| `AllowCustomValue` | BIT | false ⇒ value must be in `AllowedValues` |
| `AllowedValues` | NVARCHAR(2000), NULL | JSON array (controlled vocabulary) |
| `IsDomainTag` | BIT (NEW) | designates the single **boundary tag** (≤1; §6) |
| `IsSystemTag` | BIT | **editability/provenance**: system-managed = user can't edit, still visible (was `int` → `bit`) |
| `DisplayOrder` | INT (NEW) | admin-defined grid ordering |
| `CreatedOn` / `UpdatedOn` | DateTime2(0) | |

**TagContentType** (constrained lookup):

| Field | Type | Notes |
|---|---|---|
| `TagContentTypeId` | INT, PK | |
| `TagCode` | VARCHAR(50), UNIQUE | **constrained set: `Text` \| `Link`** (extensible). No longer free-form. |

### 4.4 ResourceTag (applied values)

| Field | Type | Notes |
|---|---|---|
| `ResourceTagId` | INT IDENTITY, PK | |
| `ResourceId` | INT, FK → Resource | |
| `TagDefinitionId` | INT, FK → TagDefinition | |
| `TagValue` | NVARCHAR(2000), NULL | for Link tags = the URL |
| `CreatedOn` / `UpdatedOn` | DateTime2(0) | |

- **Multi-valued** = multiple rows with the same `TagDefinitionId` (e.g. two `Environment` rows).
- **Link tags are effectively single-valued per key**: each link is its own tag key; the
  label is the tag's shared `DisplayName`; multiple links ⇒ multiple keys (no per-value title).
- Saved atomically via `ResourceTag_SetForResource` (delete-all-then-reinsert). Because
  `PrimaryTagDefinitionId` points at a *definition* (not a `ResourceTagId`), it survives re-save.

### 4.5 ResourceRelationship (edges)

| Field | Type | Notes |
|---|---|---|
| `RelationshipId` | INT IDENTITY, PK | |
| `FromResourceId` | INT, FK → Resource | tail (the dependent) |
| `ToResourceId` | INT, FK → Resource | head (the dependency) |
| `CreatedOn` / `UpdatedOn` | DateTime2(0) | |
| — | `UNIQUE (FromResourceId, ToResourceId)` | no duplicate edges |
| — | `CHECK (FromResourceId <> ToResourceId)` | no self-loops |

- **Renamed** from `ResourceDependency`. **Untyped now** (`DependsOn` implied). Direction =
  `tail→head`; the reverse view ("depended on by") is a read, not a second row.
- **Deferred (typed multigraph):** `relationshipTypeId` FK → `RelationshipType(code,
  displayName, inverseLabel)`; the `UNIQUE` then widens to include the type. Added when the
  producer/consumer incident view is built (`ProducesTo` / `ConsumesFrom`).
- Lean edges — `(from, to)` + audit only; no edge attributes. **No node-type → edge-type
  legality constraint.** **`DependsOn` is wired in the editor build** (the details/editor is the
  next build); the typed multigraph catalog remains deferred.

## 5. Identity & uniqueness

- **Natural identity = `(Domain + ResourceType + ResourceKey)`, enforced in code** (DB
  enforcement not required — internal app). Business names collide across domains *and* recur
  within a domain across resource types (a queue `orders` and a database `orders`), so type is
  part of identity.
- `ResourceKey` / `ResourceName` stay the **raw** name — no domain/type qualification munging.
- The global-unique `UK_Resource_ResourceKey` is **relaxed**; uniqueness of the **triple** is
  enforced in the service/import layer.
- **Domain and type are mandatory** at create and import (domain via batch-default + optional
  per-resource override; type per resource — no silent `Unknown` fallback, since type is
  identity-bearing).
- **Idempotency** (a confirmed requirement) matches on `(domain + type + key)`:
  `Resource_Upsert` gains domain + type params; `Resource_GetAllKeys` returns
  `(domain, type, key)` tuples.
- `ResourceUid` remains the globally-unique system surrogate (URLs/API), untouched.
- **`ResourceUid` is the immutable identity anchor**; `(Domain + Type + Key)` is a *mutable*
  natural/matching key (uniqueness + import matching). Relationships store `ResourceId` (FK), so
  **renaming `Name`/`Key` is safe** (no edges break); only `Type` is frozen after save. Import
  matches the *current* key — renaming realigns a record to its real source key.

## 6. Placement: domain & environment

**Grain — one row per real instance.** A resource row = one real cloud resource (one portal
presence → one set of entry-point links). A dedicated app deployment carries one environment;
shared infra (Redis, SQL Server, AppConfig) carries several. No per-environment "deployment"
child is needed.

**Placement = flat tags, no enforcement** (internal app):

- **`Domain`** (the **boundary tag**; displayed "Subscription" for us) — **required**,
  **single-valued**; the partition within which keys are unique. In our setup its value *is*
  the prod/non-prod tier, so there is no separate tier flag. It is a **designated tag**, not a
  hardcoded concept — a `TagDefinition` with `IsDomainTag=1` (≤1 across all definitions), shown
  under whatever `DisplayName` a deployment picks (Azure *subscription*, AWS *account*, …).
  It is a **system tag with a restricted vocabulary**: users *select* from `AllowedValues`,
  cannot invent inline; adding a domain is a setup action.
- **`Environment`** — **optional, multi-valued**. Multi-valued = **discrete `(key,value)`
  rows** (`Environment=dev` *and* `Environment=test` as two `ResourceTag` rows), never a joined
  `"dev,test"` string — discrete rows are what `Resource_GetItems` / `Resource_GetFilterValues`
  facet on. Vocabulary is a **global superset list** (not enforced per domain — see G5).
- Optional grouping: an `Application` tag relates the separate per-environment rows of one
  logical app (facet on it). Low priority.

**Domain requirement states** (from `IsDomainTag` + `RequirementLevel`):

| State | Expression | Identity |
|---|---|---|
| Required (Error) | `IsDomainTag=1, RequirementLevel=Error` | `(domain + type + key)`; missing domain = hard error |
| Suggested / Optional | `IsDomainTag=1, RequirementLevel=Suggested\|Optional` | `(domain + type + key)` if domain present, else `(type + key)` |
| Unused | no `IsDomainTag=1` | `(type + key)` |

**Our Azure deployment seeds** the `Domain` tag as: `TagDefinitionKey='Domain'`,
`DisplayName='Subscription'`, `IsDomainTag=1`, `RequirementLevel=Error`, `ContentType=Text`,
`IsSystemTag=1`, `AllowCustomValue=0`, `AllowedValues=['prod','non-prod']`.

## 7. Tag model details

- **One dictionary** (`TagDefinition`); the legacy `TagValueType` is **dropped** (its
  requiredness → `RequirementLevel`; its value-type notion → `ContentType`).
- **`ContentType` is a constrained enum** (`Text` + `Link` to start), driving both render and
  validation — fixing today's free-form `"string"` bug (import default must map to `Text`).
- **`RequirementLevel`** encodes enforcement directly: `Error` blocks, `Suggested` warns,
  `Optional` neither. Set at setup, ~static. Distinct axis from `IsSystemTag` (editability).
- **`DisplayName` shown everywhere** (grid + editor) instead of the raw key.
- **`DisplayOrder`** gives admin-defined ordering (Domain/Environment first); grid order =
  `DisplayOrder` → then the sproc-computed `Priority`/`TagRank`.
- **Link URL validation** — Link values must be well-formed `http`/`https` on write (import +
  editor) and safe to render (no `javascript:`).
- **`AllowedValues`** — when `AllowCustomValue=false`, hard-restrict + drive a picklist; when
  `true`, act as suggestions (autocomplete), not a constraint.

## 8. Import

- Shape: `{ version, policy{ onConflict }, defaults{ domain }, resources[ … ] }`. A
  **batch-level default domain** applies to all resources; any resource may override with its
  own `Domain` tag.
- Each `ImportResourceItem`: `key`, `name`, **`type` (required)**, `description?`, `tags{}`
  (string = single, array = multi-valued), `dependencies[]` (flat key list = `DependsOn`).
- **Idempotent** by `(domain + type + key)` — re-import updates in place. Import validates
  `IsMultiValued`, `AllowCustomValue`/`AllowedValues`; resolves relationship targets by
  identity and rejects self-references. (The relationship *write* is wired when the build is
  un-deferred.)

```jsonc
{
  "version": "1.0",
  "policy": { "onConflict": "upsert" },
  "defaults": { "domain": "non-prod" },              // batch default
  "resources": [
    { "key": "orders-queue", "name": "orders-queue", "type": "Queue",
      "tags": { "Environment": ["dev","test","integrationtest"] } },     // inherits non-prod
    { "key": "orders-queue", "name": "orders-queue", "type": "Queue",
      "tags": { "Domain": "prod", "Environment": "prod" } }              // override → distinct resource
  ]
}
```

## 9. Key decisions (summary)

| Area | Decision |
|---|---|
| Lifecycle/status | None (stateless entity) |
| Resource type | Required; **part of identity** |
| Identity | `(Domain + Type + Key)`, code-enforced; `ResourceKey` global-unique relaxed |
| Primary link | `PrimaryTagDefinitionId` → a single-valued Link tag (no URL duplication) |
| Entry points | Per-type template (`ResourceTypeTag` → `TagDefinition`, `IsDefaultPrimary`) |
| Placement | Flat tags; Domain (required, single), Environment (optional, multi-valued discrete) |
| Domain | Designated boundary tag (`IsDomainTag`); generic; `DisplayName`-labelled; `prod`/`non-prod` |
| Tags | One dictionary; `ContentType` = `Text`\|`Link`; `RequirementLevel` = Error\|Suggested\|Optional |
| Ordering | Stored global `DisplayOrder` |
| Links | Each link = its own tag; shared `DisplayName` label; validated http/https |
| Relationships | `ResourceRelationship (from,to)`; `DependsOn` only; typed catalog deferred |
| Audit (who) | Deferred (needs auth) |
| Import | Batch-default domain + override; idempotent by `(domain+type+key)`; type required |

## 10. Deferred / next phases

- **Manual editor + details page — the next build** (required before release). UI concurrency
  control is intentionally **out of scope** (*decision to reverify later*).
- **`DependsOn` relationship** — **wired in the editor build** (no longer deferred; the
  editor/details is the next build).
- **Typed relationships** — `relationshipTypeId` + `RelationshipType` catalog
  (`ProducesTo`/`ConsumesFrom`, `inverseLabel`) + traversal reads — when the producer/consumer
  incident view is built.
- **Graphical resource explorer** (rough concept — may change): click a resource to open its
  dependencies; right-click to open everything it depends on.
- **Per-type `DisplayOrder` override**; **audit actor** (`CreatedBy`/`UpdatedBy`); **Owner /
  Environment as first-class columns** (staying tags for now).

## 11. Adversarial review — findings & resolutions

**Resolved (model changed):**

- **G1 — Identity → `(Domain + ResourceType + ResourceKey)`.** Names recur within a domain
  across types; type joins identity. Also resolves **G8** — type is identity-bearing, so import
  `Type` is effectively required (no silent `Unknown`).
- **G2 — Each link is its own tag; no per-value title.** Also resolves **G7** — a single-valued
  primary link is unambiguous.
- **G3 — Manual editor is the *next build*.** The model exists to build it against, so the
  editor-dependent machinery (templates, `RequirementLevel`, vocab, `DisplayName`) is **kept**.
  UI concurrency out of scope (reverify later).

**Resolved (defaults adopted):**

- **G4 — Link rot: accepted for now.** No per-link health marker; rely on `UpdatedOn` +
  periodic re-import. Active URL checking is a future possibility only.
- **G5 — Environment vocabulary: global superset list**, not enforced per domain.
- **G6 — Domain values are stable keys**, not editable labels; rename/merge is a migration.
  The editor locks domain-value edits.

**Reviewed and kept (not extraneous, given the editor is the next build):** entry-point
templates, `RequirementLevel`, vocabulary behaviors, `DisplayName`, and domain genericity
(intended hybrid future-proofing).

**Deferred:** typed-relationship catalog; per-type `DisplayOrder` override. (`DependsOn` is now
wired in the editor build.)

## 12. Editor / Details UX

### Background — original raw notes

> Background only. The **authoritative spec is below** (*Editor / Details UX — agreed design*
> onward); these bullets are the original brain-dump, kept for traceability.

- I like the Azure App Service "Create Web App" design with tabs/sections; each tab edits
  related fields with some help text as needed. Tabs edit a single object, with action and
  navigation buttons (e.g. Previous / Next / Review + create). Use this as
  inspiration/consideration but do not attempt to mimic it exactly. Use native Mud* controls.
  (Reference image: Azure "Create Web App" wizard — Basics/Database/Deployment/Networking/
  Monitor+secure/Tags/Review+create tabs; grouped sections like Project Details / Instance
  Details; fields with `*` required markers and info tooltips; footer with Review+create,
  Previous, Next buttons.)

- The user should be led in such a way to make edits and additions very easy. So provide
  things like drop downs, select lists, etc. Reduce entry and maintenance friction as much
  as possible.

- Use a compact format like the screenshot provided. Avoid extraneous borders, lines, etc.

- Validate as quickly as possible so the user's flow isn't broken by late error discovery.

- The user should be able to create new tags while editing a resource (via popup or inline
  or … TBD) without exiting the current form.

- The user should be able to create a dependency (parent or child) resource then pop back to
  the current form.

- The link could be `/resources` for a new resource and `/resources/<uid>` for edit.

- Deleting a resource is via an action button (not shown on the screenshot). The user should
  be shown a confirmation dialog: "The following resources depend on this service <if
  applicable>. Are you sure you want to delete <xxx>".

- Possible editor tabs: General, Tags, Dependencies, Dependent On, History, Review. TBD as we
  focus on design.

- First field should be Resource Type. Cannot be changed once the resource is saved
  (read-only after save).

- (Open question) Do we need a way to rename/edit resource names (e.g. fix spelling, better
  alignment to mapped resource, etc.)?

- Possible blocking error at stages (e.g. unique-name-per-design validation) before
  continuing the edit. The record isn't officially saved until the "Save" button is
  successful.

- The browser back button pops an item off the stack. For instance: creating a resource, then
  a dependency where I need to create a dependent resource (push onto a logical stack); in that
  secondary editor I create a tag. When I hit back on the browser, I expect to go
  tag → secondary editor → primary editor → home. If there are unsaved edits then I want to be
  notified and given the opportunity to save.

- Use Mud dialog (not JavaScript/browser-native) for dialogs.

- Indicate required fields with a "*" indicator.

- Add help text at the top of each tab. Add hint text for important fields, or use an info
  icon with tooltip.

- The form sections (i.e. tabs) should be as compact as good design allows.

- On save/return, the resource list should show the newly added record. (This might not be
  possible if the filters hide the new resource. I'm OK with it not showing, but it would be
  nice.)

### Editor / Details UX — agreed design

**Principles & conventions**
- Azure "Create Web App"–style **tabbed single-object editor** with action + navigation
  buttons (inspiration, not a mimic). **Native Mud\* controls** only; **Mud dialogs** (never
  browser-native).
- **Compact** layout; avoid extraneous borders/lines; tabs as compact as good design allows.
- **Low friction:** dropdowns/select lists over free text; lead the user; reduce entry &
  maintenance effort.
- **Validate early** (per field / per tab) so flow isn't broken by late errors; the record is
  **not saved until the Save button succeeds**.
- **Required fields marked `*`**; **help text at the top of each tab**; hint / info-icon
  tooltips on important fields.

**Modes & validation**
- Three modes in one component: **Create** = linear wizard (per-stage blocking + final
  **Review** gate); **Edit** = property-sheet (jump any tab, inline validation, **Save from
  anywhere** once valid, no forced Review); **View** = read-only + **Edit** toggle.
- Validate early (field / tab) so flow isn't broken by late errors; the record is **not saved
  until Save succeeds**.

**Layout, actions & navigation**
- Tabs (working set): **General, Tags, Dependencies, Dependent On, History (hidden), Review**.
- Routes: `/resources` = new, `/resources/<uid>` = view/edit. **"Create Resource"** in the
  hamburger menu is the primary entry to a new resource; the **details/view** page is reached
  from a home-grid **row action** (grid name-click opens the primary external link, not details).
- **Delete** = action button; Mud confirm dialog ("The following resources depend on this
  service <if applicable>. Are you sure you want to delete <xxx>?"). Delete **cascade-deletes all
  `ResourceRelationship` edges** referencing the resource (either direction); allowed with
  warning, not blocked.
- **Save feedback:** explicit success toast ("Saved <name>"), independent of whether grid
  filters then show the row; on save/return the list shows the new record when filters allow.
- **Rename (resolved):** `Name` editable anytime; `Key` editable + validated; only `Type` freezes
  after save (`ResourceUid` is the immutable anchor).

### Editor design — General tab (agreed)

- Compact, one help line at top, native Mud controls; required fields marked `*`.
- Field order / flow: **① Resource Type** (`MudSelect`, required, **read-only after save**;
  drives the Tags entry-point template) → **② Subscription/Domain** (`MudSelect` prod/non-prod,
  required) → **③ Name** (`MudTextField`) → **④ Key** (`MudTextField`, auto-derived
  `slug(Name)`, editable, `*`) → **⑤ Description** (multiline, optional).
- **Stored `Key` = raw `slug(Name)`** (e.g. `orders-api`); Domain & Type stay separate
  identity parts. Identity = system-combined `(Domain + Type + Key)`.
- Show a **read-only identity preview**, e.g. `non-prod / AppService / orders-api`, so the
  user sees what makes the record unique.
- **Early validation:** once Domain + Type + Key are all set (Key blur / Domain-or-Type
  change), fire a uniqueness check on `(Domain + Type + Key)` and show an inline error
  immediately — collisions caught on General, not at Save.
- **Post-save mutability:** `Type` is **read-only after save**; **`Name` and `Key` stay
  editable and validated** (`ResourceUid` is the immutable anchor; edges are by `ResourceId`).
  Uniqueness re-checks on edit, excluding self.

### Editor design — Tags tab (agreed)

- **Pre-seed empty rows for all entry-point tags** from the type template (e.g. AppService →
  Overview, Configuration, Logs, Kusto) on a new resource; default-primary pre-marked. User
  fills values or clears unused rows.
- Compact tag row: `DisplayName` label · value editor · "primary" toggle (Link tags) · remove;
  `*`/hint cues per `RequirementLevel`.
- Value editor adapts to content type + vocabulary: `Link` → URL field with live http/https
  validation; controlled-vocab (`AllowCustomValue=false`) → select/autocomplete from
  `AllowedValues`; free text → text field. Multi-valued (e.g. Environment) → multiple discrete
  values (chips).
- "Add tag" → pick an existing `TagDefinition` from a searchable list, or **create a new tag
  definition inline** (Mud dialog) without leaving the form.
- Inline tag-definition creation uses the **full definition editor** (all props), with defaults
  / empty optionals prefilled — the **same editor experience** whether reached inline or from a
  setup screen. It is **search-existing-first** (surface close matches to avoid duplicate
  definitions); `IsDomainTag`/`IsSystemTag` cannot be set inline (setup-only).
- **Primary** is single-select among Link tags (radio-style; only a Link tag can be primary;
  setting one clears the others).
- **Empty pre-seeded rows are dropped on save** (not persisted); an empty *Suggested* row raises
  no Review warning unless engaged.

### Editor design — Dependencies / Dependent On tabs (agreed)

- **Dependencies** = out-edges ("what this depends on"); **Dependent On** = in-edges ("what
  depends on this"). **Both are editable**; when the user edits the in-edge side, the system
  writes the edge on the *other* resource's side. (`DependsOn` edges are **wired in this build**.)
- Add via a searchable resource picker (Name / Domain / Type); rows show Name · Type · Domain
  · remove. Target not found → **"Create new…"** pushes a nested editor (logical stack); on
  save, pop back with the new resource linked.
- **Cross-domain restriction:** dependencies are **restricted to the same domain** — the
  picker only offers same-domain targets (so edges never cross prod/non-prod).
- Nested "Create new…" target **inherits and locks the parent's domain** (same-domain rule).
- **Persistence timing:** creating a target **persists that resource immediately**; the **edge
  persists only on the parent's Save**. Discarding the parent leaves the child as a standalone
  (unlinked) resource.

### Editor design — History / Review tabs (agreed)

- **History:** build the tab but **comment it out in code** (doesn't render) — a scaffold for
  when audit-who/versioning lands. Show `CreatedOn`/`UpdatedOn` on Review/General for now.
- **Review:** **Azure-style "Review" before Save** — read-only roll-up of all tabs, runs full
  validation, Save enabled only when clean. (Consistent with "not saved until Save succeeds".)

### Details (view) surface (agreed)

- **Same tabbed component, view-first + Edit toggle.** `/resources/<uid>` opens the tabbed
  screen **read-only** (compact summary, tags/links, relationships visible); an **Edit** button
  flips fields editable. One component serves both view and edit. `/resources` = new (edit).

### Navigation stack, back button & unsaved changes (agreed)

- **Nested editors are routes that push browser history.** Creating a dependency's target
  pushes a secondary editor; browser-back pops **secondary editor → primary editor → home**.
  On save/pop, control returns to the parent with the new child linked.
- **Dialogs (e.g. tag-create) are plain Mud modals** — closed by their own Save/Cancel; **not**
  history entries (back does not pop them).
- **Dirty guard:** intercept navigation via Blazor `RegisterLocationChangingHandler`; if the
  current editor level is dirty, show a **Mud "Save / Discard / Cancel"** dialog before leaving.
- **Refresh / deep-link mid-stack** loses the in-progress logical stack (unsaved state); a
  deep-linked nested editor falls back to **home** as its parent.

### Provenance (rubber-duck review)

This spec incorporates a rubber-duck adversarial review; its decisions are folded into the
sections above. Four forks were raised and resolved — **V** (per-mode validation: wizard for
Create, Save-anywhere for Edit), **REL** (wire `DependsOn` now), **DEL** (cascade-delete edges,
allow-with-warning), **KEY** (`Name`/`Key` editable, `Type` frozen; `ResourceUid` is the
immutable anchor) — plus adopted detail decisions: inline tag-def hygiene (search-first, no
system/domain flags inline), nested-create domain lock + persistence timing, details reached via
a grid row action, save-success toast, primary single-select, empty-row drop on save, and
uniqueness-check-excludes-self on edit.

## 13. Verification

Design-only — nothing to run yet. Verification = confirming the model captures intent:

1. Walk the consolidated JSON against real examples: an `AppService` with Overview/Logs/Kusto
   entry points; a shared Redis spanning several non-prod environments; two same-named
   resources of the same type in different domains (must be distinct); a resource whose primary
   Link tag is later removed (primary nulls out / warns).
2. Confirm no contradiction remains (type required + identity; relaxed key uniqueness vs.
   idempotent import on the triple).

When implemented, end-to-end verification will include: a migration backfilling type +
`(domain,type,key)` uniqueness; `Resource_Upsert` taking domain + type; idempotent re-import;
and a UI check that name-click follows the primary link. Those are **future** steps.
