# Wiki Cutover Plan

## Summary (plain language)

Replace the wiki page "vNext - Azure Resource Links" (MacroPoint.wiki) with the hosted catalog before its Azure
links go dead. Priority one is **resource links** (find a resource, open its new Azure portal link); priority two
is **relationships** (what depends on what).

The app's Beta features are built. What is missing is everything around them: a place to run it, a way for the
team to sign in, and the data. The plan runs four workstreams in parallel (Seed, Link data, Hosting, Merge), loads
the data, then adds relationships and points the wiki page at the app. No new features.

## Where we are

### Verified

| Area | State | How to check |
|---|---|---|
| Features | **Done** — every Beta slice marked Done, acceptance walk recorded | `docs/plans/Beta-Plan.md`, Status table |
| Build | **Green** — web host builds; the DB project builds only in VS / full MSBuild | `dotnet build UI/MacroPoint.Azure.ResourceExplorer.UI.Web` |
| Branch | **Unmerged** — all work on `home-page`, not on `main`; part of it not pushed | `git rev-list --count main..home-page`, `git rev-list --count origin/home-page..home-page` |
| Hosting | **None** — no pipeline, no infrastructure, only a localhost publish profile | no `*.yml` / `*.bicep` in the repo; `localhost.publish.xml` |
| Access | **Single user** — every request is the configured owner; multi-user auth is Post-Beta | `ConfiguredIdentity`, `ICurrentIdentity`; `__ProjectNotes/HardReview-Simplify.md` §0 |
| Catalog data | **Empty** — local DB has no resources and no resource types (seed not applied) | `select count(*) from ResourceExplorer.ResourceType` on `(localdb)\MSSQLLocalDB\ResourceExplorer` |
| Import files | **Partial** — `wiki-import.json` covers the wiki's top table only and uses `Link`, not `AzureResourceId`; `resource-data.json` is empty; `resource-data-draft.txt` is partial and has no loader | the files under `docs/plans/artifacts` and `docs/research-files` |
| Relationships | **Discovered, not loaded** — Visibility dev only | `docs/research-files/visibility-inventory-ledger.md` §4 |

### Assumed (verify first)

- The acceptance walk still passes (`tools/e2e` `accept.spec.js`; not re-run for this plan).
- The new Azure resources already exist and can be listed with Azure Resource Graph (`az graph query`).
- The import endpoint (`ResourceImportController.Import`) takes a multi-valued `AzureResourceId` and cross-tier
  edges. Prove it with one POST before the data shards scale out.

## Decisions needed before work starts

1. **Source of the new ids.** Which subscription(s) hold the new resources, and are they already created?
2. **Hosting target.** Which subscription and resource group get the App Service and Azure SQL database, and who
   creates the Entra app registration and the pipeline service connection?
3. **Day-one scope.** Do non-prod and prod links both have to be live at cutover?

## Workstreams

| Workstream | Who | Depends on | Done when |
|---|---|---|---|
| Seed | agent | — | local DB republished; resource types and the `AzureResourceId` tag definition exist |
| Link data (5 shards) | one agent per shard | Seed; decision 1 | each shard's import file loads locally and every resource is found by name and by ARM id |
| Hosting | agent + Steve | decision 2 | the app runs in Azure behind Entra sign-in, against an Azure SQL catalog |
| Merge | Steve | — | `home-page` merged to `main` (push and PR only on Steve's go) |
| Relationships | agent | Link data loaded | ledger §4 edges load; Used By / Depends On show them |
| Cutover | Steve + agent | all of the above | acceptance below passes; wiki page points at the app |

### Seed

Republish the DB project to localdb (VS / MSBuild, then SqlPackage with `localhost.publish.xml`) so
`Script.PostDeployment1.sql` applies. Every data shard validates against this database.

### Link data, one shard per wiki section

| Shard | Wiki sections |
|---|---|
| Stores | App Configuration; App Insights (the stores) |
| Compute and cache | App Services; Azure Cache for Redis; Storage Account |
| Messaging | Service Bus (namespaces and queues) |
| Service Fabric | Service Fabric (clusters and apps) |
| App codes | Database Application Names; App Insights query filters |

Each shard reads its section from the wiki (REST API) and writes `docs/import/<shard>.json` in the existing import
format: one resource per logical thing, old and new `AzureResourceId` values, the `Environment` tag. New ids come
from Resource Graph. No code changes. A shard is done when its file imports cleanly into the seeded local DB and
home search finds each resource by name and by ARM id. The query-filter rows are KQL links that embed the old
store's resource id, so the App codes shard rebuilds them against the new store as `Link` values on the
application resource.

### Hosting

App Service (.NET 10) plus an Azure SQL database; one pipeline that builds the web host, builds the dacpac on a
Windows agent with MSBuild, and publishes it with SqlPackage. Team access through App Service Authentication
(Entra), which needs no code change. App settings carry the RO/RW connection strings and the configured owner.
Steve creates the resources, the app registration and the service connection.

Fallback, only if hosting is blocked at the end of day one: render the catalog as the wiki page's tables so the
page itself carries the new links from the same data.

### Relationships

Turn the ledger's §4 edges (application → queue, database, Redis, storage, external system) into one import file.
LITE and prod edges follow after cutover.

## Cutover acceptance

- Every link on the current wiki page has a catalog resource that home search finds by name, and its new portal
  link opens the new resource.
- Pasting a new ARM id, or its trailing name segment, lands on the same resource.
- A teammate other than Steve signs in and searches.
- The wiki page's body is replaced by a pointer to the app.

## Timeline

| When | What |
|---|---|
| Day 1, morning | Decisions; Seed, Link data shards, Hosting and Merge start in parallel |
| Day 1, end | All shard files load locally; Azure resources and pipeline exist |
| Day 2, morning | Deploy `main`; load the merged import files; acceptance walk |
| Day 2, afternoon | Relationships load; wiki page replaced |

## Out of scope

Everything §0 of `__ProjectNotes/HardReview-Simplify.md` lists as Post-Beta or Cut: multi-user identity in code,
saved views, the graph, MDLP.
