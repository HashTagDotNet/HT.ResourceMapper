# Wiki extraction — notes & decisions

## Scope done
- **Visibility Resources** table (top of `azure-wiki.md`) → `wiki-import.json` (21 resources, 7 types).
- Imported live to LocalDB `ResourceMapper`: 21 created, renders in the grid.

## Model used (confirmed with user)
Each **Azure-link cell** in this table is its own resource:
- `key` = slug of the link text; `name` = the link text (Azure id) verbatim.
- `description` = the row's left-column label (e.g. "App Configuration - Visibility").
- `type` = inferred from the row (AppConfiguration, AppInsights, AppService, Redis, ServiceBus, ServiceFabric, StorageAccount).
- Tags: **`Environment`** (Text) = `non-prod` | `prod` (from the column), and **`Link`** (Link contentType) = the URL.
  Environment and Link are two distinct tags.

## Edge-case decisions (please confirm)
1. **Multi-link cells → one resource per link.** App Service vNext/Proxy non-prod and Azure Cache for Redis
   non-prod each had two links → two resources each.
2. **Non-id link labels kept as `name` verbatim:** `Health Check`, `MacropointAlerting.SF-Prod`,
   `USSPI-RSG-DT-MPLT-01 - Azure Managed Redis`, `usaus-...-com/appServices`.
3. **Service Bus** non-prod cell's "Search: macropoint-alerting" note → captured as a `Search` (Text) tag.
4. **Skipped (no Azure-link cell in this table):** `Database Filters` (its data lives in the per-environment
   matrix tables, extracted separately) and `Tools` (out of scope).
5. **Empty prod cells** (App Service - Proxy, Azure Cache for Redis) → no prod resource produced.
6. Tag key `Environment` matches the earlier fixture's lowercase `environment` (keys are case-insensitive).

## Not yet extracted
- Section tables (`## App Configuration`, `## App Insights`, etc.) — overlap the above resources (dedupe by key).
- Per-environment matrix tables (Database Application Names → `DbCode`; Service Fabric per-service → `SF`;
  App Insights Query Filters → `AppInsightsQuery`): **row = service resource**, cells = env-scoped tags.
- Out of scope: logging/Serilog config, KQL config text.

## Demo data hygiene
- The DB also still holds the 2 earlier smoke-test fixtures (`app-config-visibility`, `alert-aggregator`).
  Purge if a clean demo set is wanted.
