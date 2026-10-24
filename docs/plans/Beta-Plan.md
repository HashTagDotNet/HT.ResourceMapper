# Beta Plan

## Summary (plain language)

Beta is the smallest app that answers the Azure migration question both ways: find "PingByPhone", see the Azure
resource id(s) it points to (old `usspi*` and new), and see everything impacted, transitively; or paste an ARM id
and land on the same resource. One person must be able to keep the catalog current with a quick add form.

No model change. The Azure id is a new multi-valued tag (`AzureResourceId`). The graph UI is removed. The scope,
acceptance criteria and evidence are in `__ProjectNotes/HardReview-Simplify.md` (§0 is the Beta target); the
target screens are `__ProjectNotes/mockups/home.html`, `resource.html` and `quick-add.html`.

**Semantic target:** the wiki page "vNext - Azure Resource Links" (MacroPoint.wiki page 11783), ignoring its prose
(logging configuration, tools). Beta must be able to express everything its App Configuration, App Insights, App
Services, Database Application Names, Service Bus and Service Fabric sections express. Judge each slice against
it; loading the page's content is not a Beta slice.

Out of scope: everything §0 lists as Post-Beta, including MDLP integration with cookie authentication.

## Decisions (accepted by Steve 2026-10-23: all four recommendations)

| Decision | Question | Recommendation |
|---|---|---|
| Cross-tier edges | Does the same-domain rule on relationships survive Beta? (review §5 gap 4) | **Relax it** — impact crosses tiers; code-only change in `ResourceService.ValidateAndResolveRelationshipsAsync` and `ImportService.ValidateAndResolveDependencies` |
| Old vs new id | How is a value marked old or new? (gap 2) | **Configured legacy markers** (subscription ids / name prefixes) matched per value; no sibling tag |
| Region search | Region tag, or region means RG/name-prefix search? (gap 7) | **Name-prefix search** for Beta; Region tag Post-Beta |
| Default domain | Which configured domain value does quick add use? (gap 8) | A configured setting; value chosen at deploy |

## Slices

Each slice ends with a build, the unit tests, and a background review; each fix is its own commit.

| # | Slice | Contents (review section) |
|---:|---|---|
| 1 | Remove the graph | Everything in §1, plus the icon-key/short-code UI and seed (§2) |
| 2 | Remove dead code | `UxLab.razor`, Sample service + its two sprocs, `Libraries\Collections` (§2) |
| 3 | Fix defects | H1 publish wipes domain values; H2 domain not required server-side; H4 RO string unused; H5 search truncation; H6 `async void`; H7 tag-key match; H3 single transactional save sproc (§4) |
| 4 | Hide the flexibility | Saved Views menu + restore path hidden; ClientSetting resume cut; Review tab, wizard footer, Dependent On tab hidden; filters and display count behind Advanced (§2, §3) |
| 5 | Azure id tag | Seed `AzureResourceId` in `Script.PostDeployment1.sql` and every Azure type template; old/new rule; import sets the primary link (§5 gaps 1, 2, 9) |
| 6 | Resource view | Read-only view per `resource.html`: Azure ids first, Depends On / Used By with hop-by-hop expand, cycle-safe recursive read (§5 gaps 3, 5) |
| 7 | Home | Per `home.html`: one search box, Azure Id column with matched-value highlight, + Add Resource (§5 gap 6) |
| 8 | Quick add | Per `quick-add.html`: one form, More Options disclosure, dialog for "Create new…"; delete `EditorNavStack` once it lands (§3 quick entry) |
| 9 | Beta acceptance | Drive every §0 acceptance criterion in the browser with `tools/e2e`; record results below |

## Status

| # | Slice | State | Notes |
|---:|---|---|---|
| 1 | Remove the graph | **Done** — committed with Slice 2 | 527 tests pass; 2 Api.Service.Contracts failures pre-exist at b19b4df |
| 2 | Remove dead code | **Done** | `Libraries\Collections` kept: `Logging.Extensions` uses `AddIfNotExists` |
| 3 | Fix defects | **Done** — 5 commits | 221 server tests pass; dacpac builds; new save sproc not yet run against a DB |
| 4 | Hide the flexibility | **Done** | e2e 6 pass / 6 skipped (delete, saved-views skipped) |
| 5 | Azure id tag | **Done** | published to localdb, save sproc smoke-tested; export still drops cross-tier edges |
| 6 | Resource view | **Done** | `/resources/{uid}/view`; e2e 7 pass / 6 skipped; legacy markers not yet configured |
| 7 | Home | **Done** | e2e 8 pass / 6 skipped |
| 8 | Quick add | **Done** | `/resources/new`; nav stack deleted; e2e 7 pass / 6 skipped |
| 9 | Beta acceptance | **Done** — all 9 criteria pass | `accept.spec.js`; mp-code-review run 001 fixed (8 commits); Environment added to all templates |

## How to check this plan's claims

- Graph inventory: `git grep -n "Diagram\|IExplorer\|/explore\|cytoscape\|rm-explorer"`.
- H1: `Script.PostDeployment1.sql`, the `WHEN MATCHED` branch setting `AllowedValues`.
- H4: `DependencyRegistration.cs`, `new DbConnector(` with `DatabaseReadWrite` twice.
- No Azure id tag yet: `git grep -n AzureResourceId -- Database`.
- Unverified review claims are listed in review §7.
