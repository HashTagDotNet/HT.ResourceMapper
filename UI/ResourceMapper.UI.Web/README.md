# ResourceMapper.UI.Web

Blazor Web App (server-rendered SSR + `InteractiveServer`) — the Phase 1 UI host: a resource grid
and a JSON Import page, served over the shared backend (`ResourceMapper.Common.Server`).

## Run it locally

Prerequisites: .NET 10 SDK, SQL Server LocalDB.

```powershell
# 1. Publish the database to LocalDB (produces & deploys the schema + sprocs)
sqllocaldb start MSSQLLocalDB
dotnet tool install --global microsoft.sqlpackage           # once
dotnet build Database/HTResourceMapperDb                    # in VS or via MSBuild if the .sqlproj is SSDT-format
sqlpackage /Action:Publish `
  /SourceFile:"Database\HTResourceMapperDb\bin\Debug\HTResourceMapperDb.dacpac" `
  /TargetServerName:"(localdb)\MSSQLLocalDB" /TargetDatabaseName:"ResourceMapper"

# 2. Point the app at the database (user secrets; this project's UserSecretsId is reused from the old host)
$cs = "Server=(localdb)\MSSQLLocalDB;Database=ResourceMapper;Integrated Security=true;TrustServerCertificate=true"
dotnet user-secrets set "ResourceMapper:ConnectionStrings:Database:RW" "$cs" --project UI/ResourceMapper.UI.Web
dotnet user-secrets set "ResourceMapper:ConnectionStrings:Database:RO" "$cs" --project UI/ResourceMapper.UI.Web

# 3. Run
dotnet run --project UI/ResourceMapper.UI.Web
```

> The connection strings are **required** — without them `GlobalConfig` throws at startup. They live in
> user secrets, never in appsettings.

## Load data

Open the app → **menu (☰) → Import…** → choose a contract JSON file (e.g.
`docs/plans/artifacts/wiki-import.json`) → **Import**. Or POST it to `POST /api/resources/import`.

## Browser verification checklist

- [ ] **Home grid** loads and lists resources (Name / Type / Description / Tags).
- [ ] **Menu (☰)** opens; **Import…** navigates to `/import`.
- [ ] **Link tags** render with a link icon, open the Azure URL in a **new tab**, and show a **copy icon on
      hover** that copies the URL ("Link copied" snackbar).
- [ ] **Text tags** render as `key: value` (e.g. `Environment: non-prod`).
- [ ] **Import page**: choosing a JSON file then **Import** shows a created/updated/skipped summary; a bad file
      shows a validation error table.
- [ ] **Sort** by Name/Type/Description works (single-column).
- [ ] First paint is **server-rendered** (view-source shows grid HTML, not an empty shell).

## Notes
- Static SSR globally; the routed app opts into `InteractiveServer` (menu, grid, import are interactive).
- Grid data loads **once** per page load (prerender result is reused on the interactive pass via
  `PersistentComponentState`).
