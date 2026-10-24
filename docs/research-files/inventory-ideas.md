# Inventory ideas (Steve)

- Three major products: Visibility, LITE, Telematics. Top nodes of hives.
- All share the same database.
- PingByPhone, PingByPhone-Secondary, and some kind of database group.
- Under LITE we have vNext Proxy Application.
- Under Visibility: WebUI, AIAgents, Alerting.
- WebUI is the Blazor app.
- AIAgents: two services and an API.
- Alerting: a suite of services.
- Then there are additional layers.
- Not trying to be comprehensive: just a quick way for users to find resources. Everybody still has the Azure Portal if we miss something.
- There are web apps/APIs for each environment, with different links. Maybe differentiate by name(?).
- Each host/API might have links to: the overview page, the DNS name, and the health page.
- WebUI => App Config, both Redis caches, PingByPhone, vNext Proxy, Access Control, Identity Service, Service Bus(?), Storage Accounts(?), App Insights.
- AIAgents => App Config, PingByPhone, Service Bus, Redis, App Insights, Storage Accounts.
- The AIAgents sub-system has a suite of services under it.
