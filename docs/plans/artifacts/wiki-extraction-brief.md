# Wiki → Import JSON — Extraction Brief (standalone)

> Hand this entire file to a fresh Claude Code session running in this repo. It is self-contained.

## Mission

Convert the legacy team wiki (`docs/research-files/azure-wiki.md`) into a single **import JSON file**
that conforms to the HT.ResourceMapper import contract, so it can be imported into the app via its
Import feature. Produce a *representative, schema-valid* subset — **not** an exhaustive scrape.

## Critical framing (read first)

- **This is one-off, throwaway data-prep — NOT application code.** Do **not** create or modify any C#,
  SQL, schema, or project. Your *only* outputs are the files listed under Deliverables.
- **Goal is a CLEAN extraction, reached interactively — not a fast best-effort dump.** The wiki is messy,
  freeform markdown and contains genuine contradictions that a machine cannot resolve alone. **Do not guess
  past ambiguity. Pause, ask the human, and iterate** (see "Working mode" below).
- **Do not invent app behavior.** If the contract is ambiguous, follow the schema rules below literally.

## Working mode — pause, ask, iterate (human-in-the-loop)

Run this as a conversation, not a one-shot batch. You are running in a console the human is watching.

1. **First pass (sample, not everything):** parse the source, build the tree for a *small representative slice*
   (e.g. 3–5 resources spanning both families — one infra resource, the Alert Aggregator service, one tricky case).
2. **Surface open questions + the sample, then STOP.** Present a concise numbered list of ambiguities/decisions
   and your proposed handling for each, alongside the sample JSON. Wait for answers.
3. **Apply answers, extend to the next slice, re-surface anything new.** Repeat until no open questions remain.
4. **Only then produce the full `wiki-import.json`.** A clean, agreed result beats a complete-but-wrong one.

**Pause and ask whenever you hit (non-exhaustive):**
- A resource that appears in multiple tables with **conflicting** type, name, or links — which wins?
- A row you can't confidently place in **Family A vs Family B** (infrastructure vs service subsystem).
- A cell that is **neither a clear URL nor a clear code** (free text, a KQL expression, a note like "Search: …").
- **Identity matching** when labels differ slightly across tables (e.g. "App Service - vNext" vs "vNext").
- Whether a section/table is **in or out of scope**.
- Any place where following the rules literally would **drop data that looks important**.

Keep a running record of every question and its resolution in `wiki-extraction-notes.md`.

## Inputs

- **Source data:** `docs/research-files/azure-wiki.md` (read it fully).
- **Contract (authoritative):** `Modules/Common/ResourceMapper.Common.Shared/Import/Contracts/ImportContract.cs`
  and `docs/plans/ImportExportApiDesign.md` ("JSON Schema" + "Schema rules"). If they disagree, the C# contract wins.

## Deliverables

1. `docs/plans/artifacts/wiki-resource-tree.json` — an **intermediate tree** (see "Method" step 1): resources
   keyed by identity, each with accumulated tags. This is your working/iterable structure.
2. `docs/plans/artifacts/wiki-import.json` — the final import document (schema below), produced from the tree.
3. `docs/plans/artifacts/wiki-extraction-notes.md` — short log of decisions, ambiguities, sections skipped
   (and why), and rough counts (resources/types/tags).

## Method — build a resource tree first, THEN emit the import JSON

The wiki describes the **same resources from many angles**: one service (e.g. "Alert Aggregator") appears as a
row in several tables, each table adding different information. So:

**Step 1 — Build a tree keyed by resource identity.** One node per unique resource; aggregate tags from every
table onto the matching node:
```
resourceKey → { name, type, description, tags: [ { key, contentType, values: [...] } ] }
```
Because you key by identity, the same resource mentioned in five tables becomes ONE node with five sets of
tags — no duplicates. Match identity on the row label, normalized (e.g. "Alert Aggregator" → `alert-aggregator`).

**Step 2 — Flatten the tree into `wiki-import.json`** per the schema below.

## How to read the wiki tables (this is the part I got wrong before — read carefully)

There are **two families** of resources. Matrix tables are **NOT** "one resource per cell," and the rows here
are real services, **not** virtual resources.

### Family A — Infrastructure resources (have Azure portal links)
Source: the per-kind section "Resource" rows — `## App Configuration`, `## App Insights` (the *Application
Insights Resource* rows), `## App Service`, Redis, `## Service Bus`, the Service Fabric **cluster** row,
`## Storage Account`. The top `# Visibility Resources` table is an **index of these** — use the section tables
as the source of truth and **do not** create separate resources from the summary table.
- **One resource per row.** `type` = the kind (AppConfiguration, AppInsights, AppService, Redis, ServiceBus,
  ServiceFabric, StorageAccount).
- Non-Production / Production URL columns → **Link** tags `link-nonprod` / `link-prod`. A cell may hold several
  URLs (split on `<br>`) → multi-value. Empty/anchor-only link cells → omit that tag.

### Family B — Service / application subsystems (env-scoped tags, usually no portal link of their own)
Source: the rows that **recur across the matrix tables** — Alert Aggregator, Compliance Manager, Db Ingestor,
Rule Manager, Rule Processor, Signal Processor, Trigger Aggregator, Trigger Ingestor, Web UI, vNext Proxy, etc.
- **Each row is one resource** (`type` = `Service`). The SAME service appears in multiple matrix tables —
  aggregate all of its tags onto the one node.
- Each matrix table contributes a **dimension tag** whose **values are the per-environment cell contents,
  verbatim** (the environment is embedded in the value as the wiki wrote it). Multi-valued when several
  environments have values. **Skip** `--`, `n/a`, empty, and `(restricted)` cells.

| Wiki table | Tag key | contentType | Example value(s) |
|---|---|---|---|
| Database Application Names | `DbCode` | Text | `alert-aa(lh)`, `alert-aa(dev)`, `alert-aa(prod)` |
| Service Fabric (per-service rows) | `SF` | Text | `MacropointAlerting.SF-dev/AlertAggregator-dev` |
| App Insights → Query Filters | `AppInsightsQuery` | Link | the per-env portal query URL(s) |
| App Configuration → Key Scope/Prefix | `ConfigPrefix` | Text | `MacroPoint:Alerting:AlertAggregator` |

**Worked example** (Alert Aggregator, after aggregating across tables):
```json
{
  "key": "alert-aggregator",
  "name": "Alert Aggregator",
  "type": "Service",
  "tags": {
    "DbCode": ["alert-aa(lh)", "alert-aa(dev)", "alert-aa(ppd)", "alert-aa(prod)"],
    "SF": "MacropointAlerting.SF-dev/AlertAggregator-dev",
    "ConfigPrefix": "MacroPoint:Alerting:AlertAggregator",
    "AppInsightsQuery": ["https://portal.azure.com/...(lh)...", "https://portal.azure.com/...(dev)..."]
  }
}
```

### Out of scope — do NOT import
The `### Logging Configuration` section and all log-level / Serilog / `MacroPoint:Shared:...` **config-key**
tables; the `dbRestore Script` link; the `## Tools` section; the intra-wiki "(details)" anchor links in the
summary table (`#app-configuration` etc. — navigational, not resources); general prose. **Note:** App Insights
per-service *query links* ARE in scope (as `AppInsightsQuery` tags); only the logging *configuration* is out.

## Target JSON schema

```jsonc
{
  "version": "1.0",
  "policy": { "onConflict": "upsert" },

  "resourceTypes": {                       // define EVERY type you use
    "AppConfiguration": { "allowCustomTags": true },
    "AppInsights":      { "allowCustomTags": true },
    "AppService":       { "allowCustomTags": true },
    "Redis":            { "allowCustomTags": true },
    "ServiceBus":       { "allowCustomTags": true },
    "ServiceFabric":    { "allowCustomTags": true },
    "StorageAccount":   { "allowCustomTags": true },
    "Service":          { "allowCustomTags": true }   // the Family-B subsystems
  },

  "tagDefinitions": {                      // define EVERY tag key you use
    "link-nonprod":     { "contentType": "Link", "allowCustomValue": true, "isMultiValued": true  },
    "link-prod":        { "contentType": "Link", "allowCustomValue": true, "isMultiValued": true  },
    "DbCode":           { "contentType": "Text", "allowCustomValue": true, "isMultiValued": true  },
    "SF":               { "contentType": "Text", "allowCustomValue": true, "isMultiValued": true  },
    "ConfigPrefix":     { "contentType": "Text", "allowCustomValue": true, "isMultiValued": false },
    "AppInsightsQuery": { "contentType": "Link", "allowCustomValue": true, "isMultiValued": true  }
  },

  "resources": [ /* one object per tree node; see worked example above */ ]
}
```

## Hard schema rules (violating these makes the import fail)

- **Every** tag key on any resource MUST appear in `tagDefinitions`. No custom-tag exemption.
- **Every** `resource.type` MUST appear in `resourceTypes`.
- `key` values are unique and **case-insensitive** (`"A-b"` and `"a-b"` collide → not allowed).
- Tag value is a **string** (single) or **array** (multi). An array is legal only when that definition is
  `"isMultiValued": true`. (If a dimension has only one value for a resource, a plain string is fine.)
- `contentType` is free-form; use exactly **`"Link"`** for URLs and **`"Text"`** otherwise (the app's grid
  renders `"Link"` as a clickable link).
- **Omit `dependencies`** entirely this phase (relationships are a later phase).

## Self-validation before you finish

- [ ] Both `wiki-resource-tree.json` and `wiki-import.json` are valid JSON and parse.
- [ ] Each unique resource appears **once** (tree was aggregated by identity — no duplicate keys).
- [ ] Every tag key used exists in `tagDefinitions`; every `resource.type` exists in `resourceTypes`.
- [ ] `key`s are unique, case-insensitively.
- [ ] Array values appear only on `isMultiValued: true` definitions.
- [ ] No `dependencies` present anywhere.
- [ ] URLs are copied **verbatim** from the wiki (they need NOT be `portal.azure.com` — Health Check, Service
      Fabric Explorer, etc. are legitimately other hosts).
- [ ] `wiki-extraction-notes.md` lists decisions, skipped sections, and counts.

## Done =
**Every open question has been raised with the human and resolved** (no silent guesses), the deliverable files
exist, the JSON passes the self-validation checklist, and the notes file records each question and its resolution.
Do not run the app or the importer.
