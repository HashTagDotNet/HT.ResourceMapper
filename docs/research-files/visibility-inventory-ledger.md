# Visibility (vNext / macropoint.com) inventory ledger

Raw discovery ledger. No recommendations, no model design.

Sources
- Code: `C:\src\1617006-maya-v140\vis\code` (worktree of macropoint.com, branch of story maya-v140). Paths below are relative to it.
- Config: `C:\src\ConfigFiles\usspi-apc-dt-mplt-07-2026-10-20.json`. App Config store `usspi-apc-dt-mplt-07` ("dt" = dev/test). 1122 items; labels present: (no label) 818, `localhost` 270, `Development` 34. **Only the dev/test store was exported, so ppd/prod resource names are not in this ledger.** No secret values are recorded; only resource identity and the key it came from.

Status legend: **V** = Verified (seen directly in file/config), **I** = Inferred.

Environment label conventions seen in the config (V): `Shared:EnvironmentCode` = `dev` (no label) / `lh` (label `localhost`); `Shared:Environment` = `Development` / `localhost`. Per-environment queue names embed `-dev-` or `-lh-`. Application codes use the form `<app>(<env>)`, taken from `...:ApplicationCode` and `...:Db:ApplicationName` keys.

---

## 1. Applications

How apps get config (V): every app's `appsettings.json` has an `AzureAppConfig:Prefixes` list (key prefixes loaded from the store) and an empty `Labels` list; the connection string comes from the env var `AppConfigConnectionString` / `APPCONFIG_CONNECTIONSTRING` (token `#{APPCONFIG_CONNECTIONSTRING_VNEXT}#` in ServiceManifest, e.g. `Services\MacroPoint.Services.FraudDetection\PackageRoot\ServiceManifest.xml`).

Hosting (V): Web.Server and AIAgents.Api deploy by zip-deploy to Azure App Service (`MacroPoint.com.yml`, `MacroPoint.AIAgent.yml`). The 11 services deploy to Service Fabric as stateless, singleton-partition, ExclusiveProcess services (`Services\Macropoint.SF` and `Services\MacropointAlerting.SF` ApplicationManifest.xml), placement constraint `(NodeType==azmpltbe)`. Pipelines: `Services\MPServices.yml`, `Services\MPAlertingServices.yml`. Cluster endpoint is blank in `PublishProfiles\Cloud.xml` (not in repo).

| # | Application | Project path | Kind | Subsystem | Hosted | App code (dev) | Status |
|---|---|---|---|---|---|---|---|
| 1 | Web.Server | `Web\MacroPoint.Web.Server` (Sdk.Web) | Web app (ASP.NET Core, gRPC / gRPC-Web endpoints, hosts the Blazor WASM client; port 43000 local) | Web UI/API host: ActionCenter, Admin, Alerting, ControlTower, Home, Red, Shipments, Common | Azure App Service; web app names pipeline vars `visibility-macropoint-com` and `visibility-macropoint-com-usaus` | `mpw(dev)` (`MacroPoint:Shared:Db:ApplicationName`) | V |
| 2 | Web.Client | `Web\MacroPoint.Web.Client` (BlazorWebAssembly) | WASM client served by Web.Server | UI of ActionCenter, Admin, Alerting, CarrierConnect, ControlTower, Red, Shipments | Served from Web.Server | `MacroPoint:Client:ApplicationCode` exists (value not recorded) | V |
| 3 | AIAgents.Api | `Web\MacroPoint.AIAgents.Api` (Sdk.Web; port 7086 local) | Web app (VAPI webhook/callback receiver, order-create API, shipment map tile endpoint) | AIAgents (+ Shipments, Common, ClassicProxy) | Azure App Service; pipeline var `aiagent-macropoint-com`; `dev-aiagent.macropoint.com` (`MacroPoint:Mcp:BasicTools:ApiBaseUrl`) | `aiapi(dev)` | V |
| 4 | AIAgents.Scheduler | `Services\MacroPoint.Services.AIAgents.Scheduler` (Exe worker) | SF service `AIAgentsScheduler`; sole owner of Quartz call loops; instance count must stay 1 (comment in `Macropoint.SF\ApplicationParameters\dev.xml`) | AIAgents | SF app `Macropoint.SF` | `ai-scheduler(dev)` | V |
| 5 | AIAgents.Processor | `Services\MacroPoint.Services.AIAgents.Processor` (Exe worker) | SF service `AIAgentsProcessor`; queue consumer for per-agent sender+callback queues | AIAgents | SF app `Macropoint.SF` | `ai-processor(dev)` | V |
| 6 | FraudDetection | `Services\MacroPoint.Services.FraudDetection` (Exe worker) | SF service `FraudDetection` (instances: 10 in prod.xml, 1 default) | FraudDetection (Modules\Alerting) + Alerting | SF app `Macropoint.SF` | `frddet({environmentcode})` | V |
| 7 | Alerting AlertAggregator | `Services\MacroPoint.Services.Alerting_AlertAggregator` | SF service | Alerting | SF app `MacropointAlerting.SF` | `alert_aa(dev)` as queue Identifier (no ApplicationCode key seen) | V |
| 8 | Alerting ComplianceManager | `Services\MacroPoint.Services.Alerting_ComplianceManager` | SF service | Alerting | `MacropointAlerting.SF` | Identifier `alert_cm(dev)` | V |
| 9 | Alerting HeavyIngestor | `Services\MacroPoint.Services.Alerting_HeavyIngestor` | SF service | Alerting | `MacropointAlerting.SF` | `alert-hi(dev)` | V |
| 10 | Alerting RuleManager | `Services\MacroPoint.Services.Alerting_RuleManager` | SF service; Quartz scheduler (cluster lock via Redis) | Alerting | `MacropointAlerting.SF` | `alert-rm(dev)` | V |
| 11 | Alerting RuleProcessor | `Services\MacroPoint.Services.Alerting_RuleProcessor` | SF service | Alerting | `MacropointAlerting.SF` | `alert-rp(dev)` per Identifier `alert_rp(dev)`; `alert-rp` seen in user notes only | V/I |
| 12 | Alerting SignalProcessor | `Services\MacroPoint.Services.Alerting_SignalProcessor` | SF service | Alerting | `MacropointAlerting.SF` | `alert-sp(dev)` | V |
| 13 | Alerting TriggerAggregator | `Services\MacroPoint.Services.Alerting_TriggerAggregator` | SF service | Alerting | `MacropointAlerting.SF` | Identifier `alert_ta` | V |
| 14 | Alerting TriggerIngestor | `Services\MacroPoint.Services.Alerting_TriggerIngestor` | SF service (instances: 2 in `MacropointAlerting.SF\ApplicationParameters\dev.xml`) | Alerting | `MacropointAlerting.SF` | `alert-ti(dev)` | V |
| 15 | VapiStore | `Modules\AIAgents\MacroPoint.AIAgents.VapiStore` (Exe) | CLI tool: VAPI config source control (README); calls `https://api.vapi.ai` | AIAgents | Not deployed (tool) | n/a | V |
| 16 | RemoveDuplicates | `RemoveDuplicates` (Exe) | Console utility | none seen | Not deployed (utility) | n/a | I (not opened beyond csproj) |
| 17 | Maps.Demo.Host | `__Demos_Utilities\MacroPoint Maps POC Solution\Maps.Demo.Host` (Sdk.Web) | POC demo web host | none (demo) | Not deployed | n/a | V (csproj only) |

Not applications: `Web\MacroPoint.WebHost.Common` (library: `MacroPointServiceBuilder`, queue/OAuth/OTel/health-check registration helpers, used by AIAgents.Api, Scheduler, Processor). Service Fabric app projects `Macropoint.SF` and `MacropointAlerting.SF` are packaging containers (V).

Alerting service pattern (V): all 8 Alerting hosts plus FraudDetection call `RegisterAlertingProcessorsDependencies` -> `RegisterCommonServerDependencies` + `RegisterAlertingDependencies` + `RegisterReadWriteQueueClient`, and then `RegisterQueueWorker<TMsg,TProcessor>(QueueConfigSections.<X>Queue)` for their own queue.

---

## 2. Subsystems (from `Modules\`)

Common root: "Visibility Application". Each Module is generally a Client/Server/Shared triplet.

| Subsystem | Projects (under `Modules\<Name>`) | Used by applications | Status |
|---|---|---|---|
| ActionCenter | ActionCenter.Client, .Server, .Shared | Web.Server (Server), Web.Client (Client) | V (csproj refs) |
| Admin | Admin.Client, .Server, .Shared | Web.Server, Web.Client; Admin.Server+Shared also referenced by all 8 Alerting services | V |
| AIAgents | AIAgents.Common, AIAgents.VapiStore (Exe) | AIAgents.Api, Scheduler, Processor (Common); VapiStore standalone | V |
| Alerting | Alerting.Client, .Server, .Shared, FraudDetection.Server | Web.Server, Web.Client (Client); all 8 Alerting services and FraudDetection (Server); FraudDetection.Server only by FraudDetection | V |
| CarrierConnect | CarrierConnect.Client, .Shared | Web.Client | V |
| ClassicProxy | ClassicProxy, .Abstractions, .Client.Http | Reached via `RegisterProxyServices` from Alerting dependencies, AIAgents.Api, Web host; config `MacroPoint:ConnectionStrings:LegacyProxy:*` | I (registration seen; csproj refs not traced) |
| Common | MacroPoint, Common.Client, Common.ResourceFiles, Common.Server, Common.Shared, Common.Telemetry | Referenced by all Alerting services, FraudDetection, WebHost.Common (so AIAgents.* hosts), Web.Server | V |
| ControlTower | ControlTower.Client, .Server, .Shared | Web.Server, Web.Client | V |
| Home | Home.Client, .Server, .Shared | Web.Server (also PowerBI endpoint); Home.Server produces to `CarrierManagement:CarrierPartnerFileQueue` | V |
| Red | Red.Client, .Server, .Shared | Web.Server, Web.Client; Red.Server reads `IdentityServerDb`, uses SMC3 client | V |
| Shipments | Shipments.Client, .Server, .Shared | Web.Server, Web.Client, AIAgents.Api | V |
| Telemetry | Telemetry.Client, MacroPointTelemetry.Shared | Not traced | I |

Libraries (under `Libraries\`, V by folder listing): AccessControl, Api, BlobStorage, Buffer, Caching (Memory, Redis, AMR, FusionCache), Compression, Configuration, Cryptography, GLN (Token, Accounts, Seals), Grpc, HealthChecks (Database, DownstreamAPI, IdentityServer, Redis, Configuration), Hosts (IHostBuilder), HttpConnector, Logging, MacroPoint.Visibility.Entities, Mdlp, OAuth, Queuing (Azure.ServiceBus), Redis (+Pooling), ReferencesServices (TimeZones), Serialization, Smtp, SqlClient, Texting, Vapi.

AIAgents agent names (V from config + Program.cs): Ava, Chuck, Debbie, Maya (each with sender + callback queue).

---

## 3. Azure / external resources

All names from the dev/test store. "Key" is the App Config key (or code reference). Secret parts never recorded.

### 3.1 Service Bus (namespace `usspi-sbn-dt-mplt-02`)

Namespace key (V): `Shared:ConnectionStrings:ServiceBus:Consumer` (access policy name `handler-consumer`), `...:Producer` (`handler-producer`), `...:ReadWrite` (`ProducerConsumer`). Also `ServiceBus:Client:Default:ConnectionString` = placeholder text only. Queue defaults: `Shared:ServiceBus:Defaults:*` (PeekLock, MaxConcurrentCalls 5, auto-complete). Queue-name keys: `Shared:ServiceBus:Queues:<path>:EntityName`; label `(none)` = dev names, label `localhost` = `-lh-` names.

| Queue (dev name) | localhost name | Config section / key | Status |
|---|---|---|---|
| aiagent-dev-vapi-ava-sender | aiagent-lh-vapi-ava-sender | `Queues:AIAgent:Ava:VapiSender` | V |
| aiagent-dev-vapi-ava-callback | aiagent-lh-vapi-ava-callback | `Queues:AIAgent:Ava:VapiCallback` | V |
| aiagent-dev-vapi-chuck-sender / -callback | aiagent-lh-vapi-chuck-sender / -callback | `Queues:AIAgent:Chuck:VapiSender` / `VapiCallback` | V |
| aiagent-dev-vapi-debbie-sender / -callback | aiagent-lh-vapi-debbie-sender / -callback | `Queues:AIAgent:Debbie:VapiSender` / `VapiCallback` | V |
| aiagent-dev-vapi-maya-sender / -callback | aiagent-lh-vapi-maya-sender / -callback | `Queues:AIAgent:Maya:VapiSender` / `VapiCallback` | V |
| aiagent-dev-vapi-sender / aiagent-dev-vapi-callback | aiagent-lh-vapi-sender / -callback | `Queues:AIAgent:VapiSender` / `VapiCallback` (generic, pre-per-agent) | V (config); consumer not seen in code (I: legacy) |
| macropoint-alerting-dev-trigger-ingestor | ...-lh-trigger-ingestor | `Alerts:TriggerIngestor` (identifier `alert_ti`) | V |
| macropoint-alerting-dev-heavy-ingestor | ...-lh-heavy-ingestor | `Alerts:HeavyIngestor` | V |
| macropoint-alerting-dev-trigger-aggregator | ...-lh-trigger-aggregator | `Alerts:TriggerAggregator` | V |
| macropoint-alerting-dev-rule-manager | ...-lh-rule-manager | `Alerts:RuleManager` | V |
| macropoint-alerting-dev-rule-processor | ...-lh-rule-processor | `Alerts:RuleProcessor` | V |
| macropoint-alerting-dev-alert-aggregator | ...-lh-alert-aggregator | `Alerts:AlertAggregator` | V |
| macropoint-alerting-dev-compliance-manager | ...-lh-compliance-manager | `Alerts:ComplianceManager` | V |
| macropoint-alerting-dev-signal-processor | ...-lh-signal-processor | `Alerts:SignalProcessor` | V |
| webhook-dev-nonsession-normal | webhook-lh-nonsession-normal | `Alerts:WebHookNonSessionNormal` (code const `WebHookRequestQueue`) | V |
| macropoint-alerting-lh-trigger-ingestor (single name, no dev variant) | n/a | `Alerts:TestTriggerIngestor` | V |
| macropoint-services-dev-fraud-detection | macropoint-services-lh-fraud-detection | `Services:FraudDetection` and duplicate `FraudDetection` key | V |
| macropoint-services-dev-exporter | macropoint-services-lh-exporter | `Services:Exporter` | V (config); no consumer in this repo |
| carrierpartner-file-queue | (no localhost label) | `CarrierManagement:CarrierPartnerFileQueue` | V |

### 3.2 SQL Server (Azure SQL logical server `fg-dt-mplt-03.ncus1e0d342a36313.database.windows.net`, secondary `fg-dt-mplt-03.secondary...`)

| Database | Config key(s) | localhost | Status |
|---|---|---|---|
| PingByPhone (primary visibility/LITE DB) | `Shared:ConnectionStrings:PingByPhone`, `:ReadOnly`, `:ReadWrite`, `:Secondary` (secondary host `fg-dt-mplt-03.secondary...`) | `localhost\MSSQLSERVER01` / `.` / `localhost`, DB `PingByPhone` | V |
| MPX_Identity (IdentityServer DB) | `Shared:ConnectionStrings:IdentityServerDb`; code read in `Modules\Red\MacroPoint.Red.Server\RegisterDependencies.cs` | `localhost`, DB `MPX_Identity` | V |

Notes (V): SQL login name in connection strings is the dev app user (name not recorded). Per-app attribution is via `Db:ApplicationName` (e.g. `mpw(dev)`, `aiapi(dev)`, `alert-hi(dev)`), i.e. the same database is shared by all apps.

### 3.3 Redis

| Resource | Config key(s) | Status |
|---|---|---|
| Azure Cache for Redis `usspi-red-dt-mplt-01.redis.cache.windows.net:6380` | `Shared:ConnectionStrings:Redis`, `...:Redis:LegacyManaged` (`ServiceHost` = `AzureCacheRedis`) | V |
| Azure Managed Redis `usspi-red-dt-mplt-04.northcentralus.redis.azure.net:10000` | `Shared:ConnectionStrings:Redis:Managed` (`ServiceHost` = `AzureManagedRedis`, PrincipalId present, redacted), `Shared:ConnectionStrings:RedisAmr` | V |
| Which Redis a cache group resolves to | `MacroPoint:Shared:Caching:Group:Redis:Default:ResolvedConnectionString` = `Redis:Managed`; `MacroPoint:Shared:Caching:Group:Default:Redis:ResolvedConnectionString` = `Redis`; `MacroPoint:Alerting:Shared:Caching:Group:Default:Redis:ResolvedConnectionString` = `Redis:Managed` (no label) and `Redis` (localhost); `MacroPoint:Api:Caching:Group:Default:...` = `Redis` | V (two groups with different targets: see Unknowns) |
| Redis pools / FusionCache backplane | `MacroPoint:Shared:Caching:RedisPool:Default|Packed`, `...:Redis:Fusion`, `FusionBackplane`, `Hybrid`; client names `{appcode}`, `{appcode}-fc`, `-fc-backplane`, `-hybrid`, `-default`, `-packed` | V |
| Quartz cluster lock (Redis) | `MacroPoint:AIAgent:Service:Scheduler:QuartzCluster:*`, `MacroPoint:Alerting:RuleManager:QuartzCluster:*` (LockHandler = Redis, key prefix `quartz:lock:`; scheduler names `MacroPoint.AIAgents.Scheduler`, `MacroPoint.Alerting.RuleManager`) | V |

### 3.4 Storage accounts

| Account | Container / use | Config key(s) | Status |
|---|---|---|---|
| `usspistadtmplt04` | default container `ftp`; email attachments container `vnext-attachments` | `MacroPoint:Shared:BlobStorage:AccountName`, `:DefaultContainerName`, `:EmailAttachments` (AccountKey redacted) | V |
| `usspistadtmpltservice01` | container `carrier-auto-partner` (carrier partner file upload) | `MacroPoint:Shared:CarrierPartnerFileStorage:*` | V |
| `usspistadtmplt09` (DNS `dev-download.macropoint.com`) | exports download | `MacroPoint:Services:Exporter:ExportStorage:AccountName`, `:DnsName` | V (config); consumer not in this repo |

Code: `AddMacroPointBlobStorage(configuration.GetSection("BlobStorage"))` in AIAgents.Api, Scheduler, Processor and `RegisterAlertingDependencies` (V). Section name `BlobStorage` vs config key `MacroPoint:Shared:BlobStorage` (prefix stripping presumed, I).

### 3.5 Telemetry

| Resource | Config key | Status |
|---|---|---|
| Application Insights (ingestion `northcentralus-0.in.applicationinsights.azure.com`, live endpoint `northcentralus.livediagnostics...`); instrumentation key/connection string redacted | `Shared:APPLICATIONINSIGHTS_CONNECTION_STRING`, `Shared:APPINSIGHTS_INSTRUMENTATIONKEY`, `MacroPoint:Shared:Client:APPINSIGHTS_INSTRUMENTATIONKEY`, `MacroPoint:Shared:Serilog:WriteTo:0:Args:endpoint` (Serilog AI sink) | V |
| Resource name of the AI store behind it | not in config | Unknown |
| OpenTelemetry flags | `MacroPoint:Shared:Telemetry:Otel:*`, `MacroPoint:Alerting:Shared:Telemetry:*`; apps call `AddMacroPointOpenTelemetry` | V (flags); exporter target not seen |
| Logging sink | Serilog (`MacroPoint:Shared:Serilog:*`, per-app `...:Serilog:*`) | V |

### 3.6 Azure SignalR, Azure AI, other Azure

| Resource | Config key | Status |
|---|---|---|
| Azure SignalR Service `usspi-sigir-dt-mplt-01` (`usspi-sigir-dt-mplt-01.service.signalr.net`) | `MacroPoint:Alerting:ConnectionStrings:SignalR` (access key redacted); also `MacroPoint:Alerting:SignalR:InitialHubServerConnectionCount`; feature flag `MacroPoint/SignalRAlerting` | V (config); consumer code not located (see Unknowns) |
| Azure AI Foundry project `usvir-aif-dt-mplt-dev-visibilityai2-macropoint-com` / project `Visibility-dev-EastUS2` | `MacroPoint:Api:AiFoundryAgentEndpoint` | V |
| Bot Framework Direct Line `directline.botframework.com` | `MacroPoint:Api:AiDirectLineEndpoint`, `MacroPoint:Api:AiGenerateTokenEndpoint` | V |
| Power BI (`api.powerbi.com`, resource `analysis.windows.net/powerbi/api`) | `MacroPoint:Api:PowerBi:ApiUrl`, `:ResourceUrl` (+ credentials keys, not recorded); code `Web\MacroPoint.Web.Server\EndPoints\Reporting\PowerBiEndPoint.cs`, `Modules\Admin\MacroPoint.Admin.Server\Accounting\AccountingService.cs` | V |
| Entra ID app | `Shared:Identity:EntraId:ClientId`, `:PrincipalId` (redacted) | V (existence only) |

### 3.7 Other MacroPoint / Descartes systems (HTTP/gRPC endpoints)

| System | Host / identity | Config key | Status |
|---|---|---|---|
| vNext proxy (ClassicProxy target) | `dev-vnextproxy.macropoint.com` (localhost `https://localhost:7009`) | `MacroPoint:ConnectionStrings:LegacyProxy:Url` (+ RetryCount, TimeoutTs) | V |
| LITE web | `dev.macropoint-lite.com` (admin page `secure/Company.aspx`) | `Shared:ConnectionStrings:Lite:Root`, `:AdminPage` | V |
| MPAccess / Access Control | `dev-mpaccess.macropoint-lite.com` | `Shared:ConnectionStrings:AccessControlUrl` | V |
| IdentityServer | `dev-isserver.macropoint-lite.com` | `Shared:ConnectionStrings:IdentityServerUrl` | V |
| Text service (MPX texting) | `dev-mptext.macropoint-lite.com` | `Shared:ConnectionStrings:TextService` | V |
| DSG SMS API | `wwwtest.myvan.descartes.com/SmsApi/v1/` | `Shared:ConnectionStrings:DsgSmsApi` | V |
| Capacity | `dev-capacity.macropoint.com` | `Shared:ConnectionStrings:Capacity:Url` | V |
| Mobile app link host | `dev.mpnt.co` | `Shared:ConnectionStrings:MobileApp` | V |
| Visibility web (self) | `dev-visibility.macropoint.com`; localhost `https://localhost:43000` | `MacroPoint:Shared:ApiBaseUrl`, `WebBaseUrl`, `MacroPoint:Api:Sso:HostBaseUrl` | V |
| Visibility API (public) | `dev-visibility-api.macropoint.com` | `MacroPoint.Visibility.Api:PublicBaseUrl`, `MacroPoint:Client:CarrierConnect:ApiBaseUrl` | V |
| Driver opportunity API (LITE.NET side) | `dev-api.macropoint.com/v1.0/driver/opportunity` | `MacroPoint:AIAgent:DriverOpportunityApi:BaseUrl` | V |
| AIAgent API (MCP basic tools target) | `dev-aiagent.macropoint.com`; localhost `http://host.docker.internal:7085` | `MacroPoint:Mcp:BasicTools:ApiBaseUrl` | V |
| Dev FTP host | `dev-ftp.macropoint-lite.com` (credential embedded in value; not recorded) | `MacroPoint:Alerting:SignalProcessor:Tmp:ClientFtpUrl` | V |
| GLN SSO (Descartes) | `auth.gln.com:8443`; test `testauth.gln.com` (Mdlp login) | `Red:MacroPoint.Visibility.CarrierNetwork:Aljex:Api:TicketUrl`, `MacroPoint:Api:Mdlp:GlnSsoUrl`, `:MdlpUrl`; code `Libraries\GLN`, `AddGlnSsoTicketAuthentication` | V |
| Aljex shipments API | `aljex.descartes.com:3001/:tenant/api/v1/shipments` | `Red:...:Aljex:Api:ShipmentUrl` | V |
| SMC3 carrier network | (host not in config; client id/secret redacted) | `Red:MacroPoint.Visibility.CarrierNetwork:Smc3:*`; code `Modules\Red\MacroPoint.Red.Server\RegisterDependencies.cs` (`RegisterCarrierNetworkSmc3Client`) | V |
| VAPI (voice agents) | `https://api.vapi.ai` (code const); per-org credential key sets `MacroPoint:AIAgent:Vapi:{Develop,LocalHost,PreProd,Production}:*` (secrets not recorded), `MacroPoint:AIAgent:VapiApiKey` | `Modules\AIAgents\MacroPoint.AIAgents.VapiStore\Vapi\VapiOrgReader.cs`, `OrgWriter.cs`; config keys listed | V |
| Aha knowledge base / academy links | `descartessystems.identity.aha.io`, `customer.descartesconnect.com/academy-visibility` | `MacroPoint:AhaKnowledgeBase:*` | V |
| Other UI links | `macropoint-lite.com/Connect.aspx`, monday.com form, `docs.macropoint.com` | `MacroPoint:Client:MacroPointConnectUrl`, `ConnectTMSUrl`, `MacroPoint:Shared:PartnerFileUploadTemplateUrl` | V |
| HERE maps API | key present, host not in config | `MacroPoint:Client:HereApiKey`; AIAgents.Api named HttpClient `ShipmentMapImageConnector` | V/I |

### 3.8 Feature flags (App Config, `.appconfig.featureflag/...`)

V: `MacroPoint/Accounting`, `ActionCenter`, `Alerting`, `Alerting/PreFilter`, `DocUpload`, `FraudGuard`, `OpsForce` (disabled), `SecureIdRetry`, `ShipmentEditor`, `ShowPreviewContent`, `SignalRAlerting`, `TempTracking`, `TwoWayChat`, `UseMacroPointTicket`, `UserManager`, `Services/FraudDetection`; plus test flags `TestShared3` (and one key with a leading space: ` MacroPoint/FraudGuard`).

### 3.9 Deployment / build infrastructure (from repo)

| Resource | Evidence | Status |
|---|---|---|
| Service Fabric cluster (node type `azmpltbe`) | `Services\*.SF\ApplicationPackageRoot\ApplicationManifest.xml` `PlacementConstraint1` | V (node type); cluster name Unknown |
| Azure App Service web apps | pipeline vars `visibility-macropoint-com`, `visibility-macropoint-com-usaus` (Web.Server); `aiagent-macropoint-com` (AIAgents.Api) | V (variable names); actual resource names are in ADO variable groups (`visibility-nodeploy` group seen) |
| Azure DevOps pipelines | `MacroPoint.com.yml`, `MacroPoint.AIAgent.yml`, `Services\MPServices.yml`, `Services\MPAlertingServices.yml` | V |
| App Configuration store | `usspi-apc-dt-mplt-07` (dev/test); the apps read it via env var `AppConfigConnectionString` | V |

---

## 4. Relationships

"Registered producer" means the `AddServiceBusProducer<T>` call is present in the host's dependency set; it does not prove the host actually sends in a given code path (I for use).

### 4.1 Application -> Service Bus queue

| Application | Relation | Queue (config section) | Evidence | Status |
|---|---|---|---|---|
| Alerting TriggerIngestor | consumes | `Alerts:TriggerIngestor` | `Services\MacroPoint.Services.Alerting_TriggerIngestor\Program.cs` RegisterQueueWorker | V |
| Alerting TriggerIngestor | registered producer | `Alerts:TriggerAggregator`, `Alerts:TriggerIngestor`, `Alerts:TestTriggerIngestor` | `AlertingDependencyInitializer.AddTriggerIngestorProcessor` | V |
| Alerting HeavyIngestor | consumes | `Alerts:HeavyIngestor` | its Program.cs | V |
| Alerting HeavyIngestor | registered producer | `Alerts:HeavyIngestor`, `Alerts:TriggerAggregator`, `Alerts:WebHookNonSessionNormal` | `AddHeavyIngestorProcessor` | V |
| Alerting TriggerAggregator | consumes | `Alerts:TriggerAggregator` | its Program.cs | V |
| Alerting TriggerAggregator | registered producer | `Alerts:RuleProcessor` | `AddTriggerAggregatorProcessor` | V |
| Alerting RuleProcessor | consumes | `Alerts:RuleProcessor` | its Program.cs | V |
| Alerting RuleProcessor | registered producer | `Alerts:AlertAggregator` | `AddRuleProcessor` | V |
| Alerting AlertAggregator | consumes | `Alerts:AlertAggregator` | its Program.cs | V |
| Alerting AlertAggregator | registered producer | `Alerts:ComplianceManager` | `AddAlertAggregatorProcessor` | V |
| Alerting ComplianceManager | consumes | `Alerts:ComplianceManager` | its Program.cs | V |
| Alerting ComplianceManager | registered producer | `Alerts:SignalProcessor` | `AddComplianceProcessor` | V |
| Alerting SignalProcessor | consumes | `Alerts:SignalProcessor` | its Program.cs | V |
| Alerting SignalProcessor | registered producer | `Alerts:WebHookNonSessionNormal`, `Alerts:TriggerAggregator` (via AddSignalProcessor region; exact block not fully read) | `AlertingDependencyInitializer` | V/I |
| Alerting RuleManager | consumes | `Alerts:RuleManager` | its Program.cs | V |
| Alerting RuleManager | registered producer | `Alerts:TriggerAggregator` (message `AlertMessageBase`), `Alerts:RuleManager` | Program.cs, `AddRuleManagerProcessor` | V |
| Every Alerting host + FraudDetection + Web.Server (any host that calls `RegisterAlertingDependencies`) | registered producer to all of the above | all `Alerts:*` queues listed in the section above | `RegisterAlertingDependencies` calls all `Add*Processor` | V (registration), I (usage) |
| FraudDetection | consumes | `Services:FraudDetection` | `Services\MacroPoint.Services.FraudDetection\Program.cs` | V |
| FraudDetection | registered producer | `Services:FraudDetection`, `Alerts:TriggerAggregator`, `Alerts:TriggerIngestor`, `Alerts:HeavyIngestor`, `Alerts:TestTriggerIngestor` | same Program.cs | V |
| AIAgents.Processor | consumes | per-agent sender and callback queues for Debbie, Chuck, Ava, Maya (8 queues) | `Services\MacroPoint.Services.AIAgents.Processor\Program.cs` | V |
| AIAgents.Processor | registered producer | same 8 queues | same | V |
| AIAgents.Scheduler | registered producer | Debbie/Chuck/Ava/Maya sender queues; Maya callback queue | `Services\MacroPoint.Services.AIAgents.Scheduler\Program.cs` | V |
| AIAgents.Api | produces | Debbie/Chuck/Ava/Maya callback queues (EndOfCallReport payloads) | `Web\MacroPoint.AIAgents.Api\Program.cs` AddServiceBusProducer | V |
| Web.Server (via Home.Server) | registered producer | `CarrierManagement:CarrierPartnerFileQueue` | `Modules\Home\MacroPoint.Home.Server\Utils\HomeDependencyInitializer.cs` | V (module); Web.Server inclusion I |
| (no host in this repo) | consumer | `CarrierManagement:CarrierPartnerFileQueue`, `Services:Exporter` | no `RegisterQueueWorker` found | Unknown consumer |
| All apps using Service Bus | namespace | `usspi-sbn-dt-mplt-02` | `Shared:ConnectionStrings:ServiceBus:*`, `AddReadWriteQueueClient` / `RegisterReadWriteQueueClient` | V/I |

### 4.2 Application -> other resources

| Application(s) | Relation | Resource | Evidence | Status |
|---|---|---|---|---|
| All Alerting hosts, FraudDetection, AIAgents.* , Web.Server | reads/writes | SQL `PingByPhone` | `Shared:ConnectionStrings:PingByPhone*`; `AddDatabaseConnector`, `RegisterCommonServerDependencies`; `Db:ApplicationName` keys per app | V (keys, registration), I (every app actually connects) |
| Web.Server (Red module), AIAgents.Api (via shared auth) | reads | SQL `MPX_Identity` (IdentityServerDb) | `Modules\Red\...\RegisterDependencies.cs`; `AddGlnSsoTicketAuthentication` test config includes IdentityServerDb | V (Red), I (AIAgents.Api) |
| All apps using caching | uses | Redis (legacy `usspi-red-dt-mplt-01` and/or managed `usspi-red-dt-mplt-04`) | Section 3.3 keys | V (keys), I (per-app target) |
| AIAgents.Scheduler, Alerting RuleManager | uses (cluster lock) | Redis | `QuartzCluster:LockHandler = Redis` | V |
| AIAgents.Api, Scheduler, Processor, Alerting hosts | uses | Blob storage `usspistadtmplt04` | `AddMacroPointBlobStorage` + `MacroPoint:Shared:BlobStorage:*` | V (registration), I (account) |
| All apps | send logs/telemetry | Application Insights; Serilog AI sink | Section 3.5 | V |
| AIAgents.* hosts | call | VAPI (`api.vapi.ai`), Text service, LITE/driver-opportunity API, ClassicProxy | `MacroPoint:AIAgent:Vapi*`, `DriverOpportunityApi:BaseUrl`, `AddOAuthClient`, Texting library | V (config), I (which host calls which) |
| AIAgents.Api | calls | HERE map image API | `AddHttpClient(nameof(ShipmentMapImageConnector))` | V |
| Web.Server | calls | Power BI, Azure AI Foundry / Direct Line, SMC3, Aljex, GLN SSO, text service, DSG SMS, LITE, Capacity, MPAccess | Sections 3.6-3.7 keys; `Web.Server` loads prefixes `MacroPoint:`, `MacroPoint:Api:`, `Red:`, `Shared:` | V (keys), I (calls) |
| Web.Server | receives from | Web.Client (WASM) over gRPC-Web | `UseGrpcWeb`, `MacroPoint:Client:Grpc:*` | V |
| Alerting SignalProcessor | reads/writes | Dev FTP host, Azure SignalR (alerting) | `Tmp:ClientFtpUrl`; `MacroPoint:Alerting:ConnectionStrings:SignalR` | I (key under SignalProcessor/Alerting prefix; consumer code not confirmed) |
| Web.Server, AIAgents.Api, Alerting hosts | call | vNext proxy `dev-vnextproxy.macropoint.com` via `RegisterProxyServices` | `AlertingDependencyInitializer`, AIAgents.Api Program.cs | V (registration), I (target) |

### 4.3 Application -> subsystem

| Application | Subsystems (project references, V from csproj unless noted) |
|---|---|
| Web.Server | ActionCenter.Server, Admin.Server, Alerting.Server, Common.Client, ControlTower.Server, Home.Server, Red.Server, Shipments.Server, Web.Client |
| Web.Client | ActionCenter.Client, Admin.Client, Alerting.Client, CarrierConnect.Client, ControlTower.Client, Red.Client, Shipments.Client |
| AIAgents.Api | AIAgents.Common, Shipments.Server, WebHost.Common (-> Common.Server) |
| AIAgents.Scheduler / Processor | AIAgents.Common, WebHost.Common (-> Common.Server), Blob storage lib |
| 8 Alerting services | Alerting.Server, Admin.Server (not TriggerIngestor: Admin.Shared only), Common.Server, Common.Shared |
| FraudDetection | Alerting.Server, FraudDetection.Server, Admin.Shared, Common.Server, Common.Shared |
| VapiStore | AIAgents (itself) |

### 4.4 Application -> config prefix (which App Config keys each loads, V from appsettings.json)

| Application | Extra prefixes beyond `Shared:`, `MacroPoint:`, `MacroPoint:Shared:`, `MacroPoint:Api:` |
|---|---|
| Web.Server | `MacroPoint:AIAgent:Shared:`, `MacroPoint:AIAgent:Service:`, `MacroPoint:Alerting:Shared:`, `Red:` |
| AIAgents.Api | `MacroPoint:Alerting:Shared:`, `MacroPoint:AIAgent:`, `MacroPoint:AIAgent:Api:` |
| AIAgents.Scheduler | `MacroPoint:Alerting:Shared:`, `MacroPoint:AIAgent:`, `MacroPoint:AIAgent:Service:`, `...:Service:Scheduler:` |
| AIAgents.Processor | same as Scheduler with `...:Service:Processor:` |
| Alerting `<X>` (8 hosts) | `MacroPoint:AIAgent:Shared:` (not TriggerIngestor), `MacroPoint:AIAgent:Service:` (not TriggerIngestor), `MacroPoint:Alerting:Shared:`, `MacroPoint:Alerting:<X>:` |
| FraudDetection | `MacroPoint:Alerting:Shared:`, `MacroPoint:AIAgent:Shared:`, `MacroPoint:AIAgent:Service:`, `MacroPoint:FraudDetection:` |

---

## 5. Unknowns / ambiguities

1. Only the dev/test App Config store was provided. ppd and prod resource names (namespaces, DB servers, Redis, storage, SignalR, SF cluster) are not covered. Labels in this export are only (none)/`localhost`/`Development`; the unlabelled values are the dev values, not a shared default across environments.
2. App Insights resource name behind `Shared:APPLICATIONINSIGHTS_CONNECTION_STRING` is not stated in config; only the regional ingestion endpoint (`northcentralus`) is.
3. Two Redis targets coexist (`usspi-red-dt-mplt-01` classic and `usspi-red-dt-mplt-04` Azure Managed Redis); cache groups resolve to `Redis` vs `Redis:Managed` depending on group key (`Group:Default:Redis` vs `Group:Redis:Default`) and the key `MacroPoint:Alerting:Shared:Caching:...` has `Redis:Managed` unlabelled but `Redis` under `localhost`. Which app uses which was not traced in code.
4. Config exists for components with no project in this repo: `MacroPoint:Services:Exporter:*` (queue `macropoint-services-*-exporter`, storage `usspistadtmplt09`), `Alerting:DbIngestor:*` (app code `alert-db(dev)`; no `DbIngestor` identifier in code), consumer of `CarrierManagement:CarrierPartnerFileQueue`. Possibly hosted in another repo (LITE.NET or lite.com) or retired.
5. Generic queues `aiagent-dev-vapi-sender` / `-callback` (no agent name) exist in config; code registers only per-agent queues.
6. Azure SignalR (`usspi-sigir-dt-mplt-01`): consumer code was not located by grep; feature flag `SignalRAlerting` and `SignalR:InitialHubServerConnectionCount` suggest Web.Server hosts the hub (I).
7. Alerting app-code naming is inconsistent in config: queue Identifiers use underscores (`alert_aa(dev)`), `ApplicationCode` keys use hyphens (`alert-rm(dev)`); SignalProcessor and WebHookNonSessionNormal share Identifier `alert_sp(dev)`; AlertAggregator, ComplianceManager, TriggerAggregator have no `ApplicationCode` key seen.
8. `Shared:ServiceBus:Queues:FraudDetection` and `...:Services:FraudDetection` both exist with the same queue name; code uses `Services:FraudDetection`.
9. Hosting of Web.Server per environment: pipeline deploys to two web apps (`visibility-macropoint-com` and `...-usaus`); region/slot mapping and actual resource names are in ADO variable groups, not in repo.
10. SF cluster name/endpoint blank in `PublishProfiles\Cloud.xml`; node type `azmpltbe` only. Instance counts: FraudDetection 10 in prod.xml, TriggerIngestor 2 in Alerting dev.xml; other params not all read.
11. `MacroPoint.Services.Alerting_*` host-level producer usage (as opposed to registration) was not traced per code path; all Alerting hosts register all alerting producers.
12. `Telemetry` module, `RemoveDuplicates`, ClassicProxy project references and `MacroPoint:Client:ApplicationCode` value were not opened in depth.
13. Config contains test/fixture keys (`TestShared:*`, `TestApp:*`, `TestShared2*`) and a feature flag with a leading-space id; treated as noise, not resources.
14. Web.Server `Properties\launchSettings.json` has six profiles all on `https://localhost:43000` (Debug Wasm and Api, Dashboard, Alerting, Action centre, Tech info, Web.Api); AIAgents.Api on `https://localhost:7086`; the Mcp config points localhost at `7085` (a different port, host not identified).
