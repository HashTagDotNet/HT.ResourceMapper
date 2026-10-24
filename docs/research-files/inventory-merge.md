# Visibility inventory — merge status

Where the inventory work stands, so it can be resumed.

## Files

- `inventory-ideas.md` — Steve's layers, recorded as given.
- `visibility-inventory-ledger.md` — Claude's raw discovery from the macropoint.com code (Maya worktree, Services/Web/Modules) and the dev App Configuration export. Rows marked Verified or Inferred; no secrets.
- `resource-data-draft.txt` — seed draft in the indented text format (`Type: Name`, then `env:` / `azure:` / `search:` lines): the wiki's non-prod top-section containers, the 8 alerting queues, the alerting blob container.
- `resource-data.json` — Steve's file; untouched.

## Ideas vs ledger

| Idea | Ledger evidence | Fit |
|---|---|---|
| Shared database: PingByPhone + Secondary | Azure SQL `fg-dt-mplt-03` + secondary; apps distinguished by `Db:ApplicationName` | **Confirmed** |
| Identity Service | SQL `MPX_Identity` (Red module), GLN SSO | **Confirmed** |
| LITE → vNext Proxy | `dev-vnextproxy.macropoint.com`, used by Web.Server, AIAgents.Api, Alerting | **Confirmed** |
| WebUI = Blazor app | Web.Server hosts Web.Client; App Service `visibility-macropoint-com` and `-usaus` | **Confirmed** |
| AIAgents = 2 services + API | Scheduler, Processor (Service Fabric); AIAgents.Api (App Service, `dev-aiagent.macropoint.com`) | **Confirmed** |
| Alerting = suite of services | 8 Service Fabric services chained by Service Bus queues | **Confirmed** |
| WebUI → App Config, Redis ×2, PingByPhone, Proxy, Access Control, Identity, App Insights | All present in Web.Server's config keys | **Confirmed** |
| WebUI → Service Bus(?), Storage(?) | No queue or blob registration found for Web.Server | **Likely not** |
| AIAgents → App Config, PingByPhone, Service Bus, Redis, App Insights, Storage | 8 per-agent queues, blob `usspistadtmplt04`, Redis Quartz lock | **Confirmed** |

Ledger items not in the ideas: FraudDetection (Service Fabric, built on the Alerting module); Azure SignalR `usspi-sigir-dt-mplt-01` (config only); Exporter storage `usspistadtmplt09` (config only); external calls (VAPI, text service, Power BI, HERE maps).

## Next

- Proposed merge: Steve's layers become the hierarchy (Product → subsystem → host per environment → resources); the ledger fills in names and links. Each host carries overview, DNS name and health links.
- Open question: does FraudDetection sit under Alerting or stand as its own subsystem?
- The ledger covers the dev store only; preprod/prod names are not in it.
