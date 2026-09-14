# Live catalog seed

The live MacroPoint resource catalog, extracted from two Azure DevOps wiki pages:

| Page | Path | Contributes |
|---|---|---|
| 11783 | `/Visibility vNext/vNext - Azure Resource Links` | Azure infrastructure, systems, applications |
| 13215 | `/Visibility/Queues <--> Services` | Queues with their producers and consumers |

Unlike the `Demo_*` scripts, this is intended to become part of the release.

## Run order

| Step | File | What it does |
|---|---|---|
| 1 | `Live_Purge_Catalog.sql` | Clears the demo catalog. Destructive. |
| 2 | `Live_Seed_Vocabulary.sql` | Resource types, tag definitions, per-type templates. |
| 3 | `LiveCatalog.json` | Uploaded through the app's Import page at `/import`. |
| 4 | `Live_Seed_PostImport.sql` | Cross-domain edges and primary link wiring. |

Steps 1, 2 and 4 run against `(localdb)\MSSQLLocalDB\ResourceMapper` via sqlcmd or SSMS. Step 3
goes through the running application, not the database.

## Why the work is split across a JSON file and two SQL scripts

The import document format is deliberately narrow. It carries resources, their tags and their
same-domain dependencies — and nothing else. Everything below is real catalog state that the
format cannot express, which is why the SQL scripts exist:

- **`ResourceType.ShortCode` / `.IconKey`** — `ResourceType_Upsert` ignores both on update.
- **`TagDefinition.DisplayName`, `.RequirementLevel`, `.IsDomainTag`, `.IsSystemTag`,
  `.DisplayOrder`** — preserved on update, but never set by import.
- **`ResourceTypeTag`** — the per-type entry-point template has no place in the format at all.
- **Cross-domain dependency edges** — `ImportService.ValidateAndResolveDependencies` resolves a
  dependency key *inside the dependent's own domain*. An Application (domain `shared`) depending
  on an App Service (`prod`/`non-prod`) is not merely dropped, it fails the entire import.
- **`Resource.PrimaryTagDefinitionId`** — set by neither `Resource_Upsert` nor `ImportService`,
  so without step 4 no resource has a click-through link.

## Regenerating the JSON

```
python build-live-catalog.py
```

Reads `wiki-vnext-azure-resource-links.md` and `wiki-queues-services.md` (snapshots of the two
wiki pages, checked in so the catalog is reproducible without network access) and writes
`LiveCatalog.json` plus `cross-domain-edges.json`. To refresh a snapshot:

```
az rest --method get --resource 499b84ac-1321-427f-aa17-267ca6975798 \
  --url "https://dev.azure.com/dsgrandd/MacroPoint/_apis/wiki/wikis/MacroPoint.wiki/pages/<id>?includeContent=true&api-version=7.0"
```

Every extraction decision lives in the named tables at the top of `build-live-catalog.py`
(`SUBS`, `PROVIDER_TYPE`, `ENV_MAP`, `SYSTEMS`, `APPS`, `ALIASES`, `NOT_APPLICATIONS`,
`CORRECTIONS`), so the script is also the decision record.

## The model

**Two families.** Real Azure resources carry a portal link and live in one subscription.
Logical resources — `System` and `Application` — describe the software and are deployed nowhere.

**Identity** is `Domain + Type + Key`. `Domain` displays as **Tier** and holds `prod`,
`non-prod`, or `shared`. Tier follows the *subscription*, so everything in `DSG-ProdPreprod` is
`prod` — including the preprod and preview environments, which are distinguished by the
`Environment` tag rather than by tier. The `shared` value exists for the logical resources.

**Three separate concepts** that are easy to confuse, so each has its own tag:

| Tag | Values |
|---|---|
| `Domain` (Tier) | prod, non-prod, shared |
| `Subscription` | DSG-DevTestMSDN, DSG-ProdPreprod |
| `Environment` | localhost, development, pentest, preprod, preview, production |

**Per-environment values are multi-valued tags, not per-environment tag keys.** The values
already name their own environment (`alert-aa(dev)`, `macropoint-alerting-prod-rule-manager`,
`MacropointAlerting.SF-dev/RuleManager-dev`), so one `DbAppName` tag holding four values beats
four tag keys or four duplicate resources.

**Names and types are derived from the portal URL**, not from the wiki's link text, which is
sometimes a label rather than a resource name. The URL also yields `ResourceGroup`,
`Subscription` and therefore `Domain`.

### Queues

A queue is **one resource per tier**, because the prod and non-prod namespaces hold genuinely
separate Azure objects that happen to share a name. Identity is `Domain + Type + Key`, so the
same queue name in two tiers is two rows, each linked to its own namespace — and because a queue
and its namespace share a tier, those edges *are* expressible in the JSON.

`Producer` and `Consumer` are multi-valued tags holding **application names**, because
`ResourceRelationship` is untyped and an edge cannot record which way messages flow. The edges
exist as well, so the explorer still connects applications to queues; the roles live in the tags.
`Live_Seed_PostImport.sql` builds those edges by joining the tag values onto
`Resource.ResourceName`, so **the tag values must stay spelled exactly as the applications are
named** — it reports any that fail to match.

The wiki lists 28 queues out of roughly 700 in each namespace, so it is a curation rather than an
inventory. All 28 were verified to exist in prod; four do not exist in non-prod
(`geocodelonglockqueue`, plus three named for the prod environment whose non-prod twins carry
different names).

**Convention-named queues come from Azure, not the wiki.** The Alerting and Services queues follow
`macropoint-{alerting|services}-{lh|dev|ppd|pvw|prod}-{application}`, so the name yields both the
environment and the application it serves. These were enumerated with `az servicebus queue list`
against both namespaces (verified 2026-09-13), because the wiki's Service Bus table is wrong here:
it names queues `…-db-aggregator` that do not exist in Azure at all. Two sets are deliberately
excluded — `macropoint-alerting-{prod,ppd}-db-ingestor`, superseded by `heavy-ingestor` (prod has
not finished the rename; non-prod has), and `macropoint-alerting-shyam-trigger-ingestor`, a
developer's personal queue.

Together that gives 86 queue resources. Because the queues are now first-class resources with
edges back to their applications, the applications no longer carry a `ServiceBusQueue` tag — it
would be duplicated data that could drift out of step with the resources.

## Known gaps and deviations from the wiki

These are deliberate. Fixes belong in the wiki, after which the snapshot can be refreshed.

**Wiki errors corrected in the catalog:**

| Where | Wiki says | Catalog says |
|---|---|---|
| Service Bus / Rule Manager / production | `macropont-alerting-prod-rule-manager` | `macropoint-…` |
| Service Bus / Trigger Aggregator | dev and prod values are swapped, and the dev cell also misspells `aggreagor` | unswapped and corrected |
| Index table / Storage Account | `Storage Account (Blob))` — stray bracket | `Storage Account (Blob)` |
| Index table / App Service | `App Service - Proxy`, where the App Services table and the application both say vNext Proxy | `App Service - vNext Proxy`, so one site family has one description |
| Index table / Redis | the row is labelled `Azure Cache for Redis` but its second link is the Enterprise SKU | that resource is described `Azure Managed Redis` |

**Not carried into the catalog:**

- **`Health Check`** (App Service - vNext, prod column) — not a portal link, and its URL embeds
  an `hctoken` query parameter that must not be committed to source control.
- **`LITE Ftp Processor`'s App Insights filter** — a markdown link broken across a cell boundary
  in the wiki source (`[union traces,exceptions \`), unusable as extracted.
- **`integration test`** — retired environment; its two app services are not catalogued.
- **Logging Configuration and Tools sections** — configuration reference and external tool links,
  not catalog resources.
- **Shared config scopes** (`Alerting (shared)`, `MacroPoint Shared`, `System Shared`) — config
  namespaces rather than deployable applications.
- **Uncertain queue consumers.** `location-process-queue` (`LocationHandler?`) and
  `order-status-callback-queue` (`Notification? BulkLoad?`) carry question marks in the wiki, so
  those two queues have no `Consumer` tag and no consumer edge rather than a guess.

**Known-incomplete producer lists.** Two queues name a producer and then a wildcard:
`email-sender-intake` (`Web, *`) and `phone-notification-intake` (`OrdersNeedingUpdate, *`). Only
the named producer is tagged; the `*` is dropped, so treat both lists as partial.

**Placeholder application.** `AppConfiguration (handlers)` is the producer for eight queues. It is
catalogued as a LITE application so the edges exist, but whether it is one deployable application
or a description of a config-driven mechanism is unconfirmed.

**Producer still unknown.** `macropoint-services-prod-fraud-detection` is consumed by the
`FraudDetection` application (Visibility); its producer is a LITE application, not yet identified.

**Applications adopted from the queue page.** Consumers and producers named there become
applications in the `LITE` system by default. Two were matched to applications this catalog
already had — `ComplianceManager` and `TriggerAggregator` are the Alerting system's
`Compliance Manager` and `Trigger Aggregator`, which is consistent with their queues being named
`macropoint-alerting-prod-*`.

**Wiki content that is stale.** The App Configuration key-scope table lists 13 prefixes. The
actual store holds considerably more, including `MacroPoint:AIAgent` (230 keys in non-prod, more
than `MacroPoint:Client`), `MacroPoint:FraudDetection`, `MacroPoint:Mcp`, `MacroPoint:Messaging`,
`MacroPoint:Server`, `MacroPoint:AhaKnowledgeBase` and `MacroPoint:FraudGuard`. None of these
appear as applications in this catalog, because this pass is scoped to the wiki. Reconciling them
is follow-up work.

**Renames in flight.** The application the wiki calls *Db Ingestor* (and *Db Aggregator*, in the
Service Bus table alone) is now **Heavy Ingestor**. Its config prefix is recorded as
`MacroPoint:Alerting:HeavyIngestor`; note that `MacroPoint:Alerting:DbIngestor` still holds four
live keys in both stores, so the rename is not finished. The wiki's Service Bus table compounds
this by naming its queues `…-db-aggregator` — those do not exist in Azure. The real queues are
`macropoint-alerting-{lh,dev}-heavy-ingestor` in non-prod, while prod still carries both
`…-{prod,ppd}-heavy-ingestor` and the superseded `…-{prod,ppd}-db-ingestor`.

**Applications merged.** `Api`, `Client` and `Web UI` are facets of one application, `vNext` —
the first two are config scopes, and `Web UI`'s telemetry filter is the API's own app name.
`Proxy` in the database table is the same application as `vNext Proxy` elsewhere.

**`Web` and `Mobile Web` are NOT the same as `Web UI`.** They are LITE applications, named as
queue producers on the Queues page, and are deliberately separate from `vNext` — into which the
vNext page's `Web UI` was folded. The names read alike but the things are different, so do not
merge them.

**Not yet linked.** The two `usaus-` (second region) app services appear only in the wiki's index
table, not in the App Services matrix, so no Application depends on them. If the vNext and vNext
Proxy applications do run there, those edges belong in `Live_Seed_PostImport.sql`.
