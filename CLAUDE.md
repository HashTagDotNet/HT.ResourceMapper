# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

HT.ResourceMapper is a cloud resource management application — a searchable catalog of resources across multiple cloud environments with advanced tagging, metadata management, and dependency tracking. Conceptually an enterprise-grade bookmark manager for cloud infrastructure.

## Development Commands

### Build and Test
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build UI/ResourceMapper.UI.Server

# Run the server (hosts Blazor WebAssembly client)
dotnet run --project UI/ResourceMapper.UI.Server

# Run all tests
dotnet test

# Run tests for a specific class
dotnet test --filter "FullyQualifiedName~ResourceMapper.Common.Server.Tests.ResourceService"

# Run a single test method
dotnet test --filter "FullyQualifiedName=Namespace.ClassName.MethodName"
```

> **Note**: The SQL database project (`Database/HTResourceMapperDb/HTResourceMapperDb.sqlproj`) is an **old-style SSDT / DACPAC** project (`TargetFrameworkVersion v4.7.2`; imports `Microsoft.Data.Tools.Schema.SqlTasks.targets`). It builds in **Visual Studio 2026** or **full MSBuild** — **not** `dotnet build` — to produce a `.dacpac`, and is published to `(localdb)\MSSQLLocalDB\ResourceMapper` via **SqlPackage** or VS Publish using `localhost.publish.xml`. It is **declarative**: edit the `.sql` files (desired state) and the publish engine diffs against the target. Reference/system seed lives in `Scripts/Script.PostDeployment1.sql` (a `PostDeploy` item; idempotent `MERGE`). Because it is not SDK-style, a solution-wide `dotnet build` does not build the DB project — build/publish it separately.

## Solution Structure

```
Libraries/       HT.* shared utility libraries (net8.0/net9.0)
Modules/         Feature modules as Client/Server/Shared triplets
UI/              Blazor WASM client + ASP.NET Core server host
Database/        SQL Server database project (old-style SSDT / DACPAC; builds in VS / full MSBuild)
_Tests/          Test projects mirroring source structure
__ProjectNotes/  Architectural notes and user stories
```

## Architecture

### Module Pattern (Client/Server/Shared)
Each feature is a triplet of projects under `Modules/`:
- **`*.Shared`** — Contracts, DTOs, and editor models shared across boundaries
- **`*.Server`** — Business logic, repositories, `IResourceService` implementations
- **`*.Client`** — Razor components, MudBlazor UI, client-side only code

Modules register dependencies via an extension method in `*/Utils/DependencyRegistration.cs`:
```csharp
// Called from UI.Server/Program.cs
builder.Services.RegisterCommonDependencies();
```
This registers `GlobalConfig`, `IDbConnector`, `IResourceRepository`, and `IResourceService` using `TryAdd` semantics.

### API Response Pattern
Two-layer contract system:

**`HT.Api.Client.Contracts`** — Wire format (`ApiResponse<T>`, `Message`, `MetaData`, `CallStatusCode`). Used by Blazor client. Contains resource DTOs (`ResourceDto`, `ResourceTagDto`, etc.).

**`HT.Api.Service.Contracts`** — Server-side wrapper (`ApiServiceResponse<T>`) plus a fluent `ServiceResponseBuilder<T>`:
```csharp
var builder = ServiceResponseBuilder<MyDto>.Create();
builder.Validation.AddValidation("fieldName", "error message");
builder.Errors.AddError(CallStatusCode.NotFound, "detail", "property");
var response = builder.BuildResponse();  // auto-sets CallStatus from errors
```
`ApiServiceResponse<T>.IsOk` checks errors, `MetaData.CallStatus`, and HTTP status code.

### Controllers
All controllers inherit `ApiControllerBase` which provides `MapServiceResponseToActionResult<T>(ApiServiceResponse<T>)` to translate service responses to appropriate HTTP `ActionResult` with status codes and custom reason phrases.

### Database Access
`IDbConnector` abstraction (from `HT.Microsoft.SqlClient.Extensions`) exposes separate read-only (`.RO`) and read-write (`.Execute`) connections. Repositories use extension methods for sproc calls:
```csharp
await _db.RO.SprocCommand("Resource_GetItems")
    .AddNVarchar("@SearchFor", request.SearchFor)
    .ExecuteQueryAsync<ResourceRow>(cancellationToken);
```
Connection strings are read from `ResourceMapper:ConnectionStrings:Database:RO` and `ResourceMapper:ConnectionStrings:Database:RW`.

### Editor Model Pattern
`SingleValueEditor` in `Modules/Common/ResourceMapper.Common.Shared/Editor/` tracks change state for form fields:
```csharp
public class SingleValueEditor {
    public string OriginalValue { get; set; }
    public string EditedValue { get; set; }        // bind MudTextField to this
    public bool IsChanged => ...;
    public List<EditorMessage> Messages { get; set; }
}
```
`ResourceEditorModel` composes multiple `SingleValueEditor` instances (Code, Name, Notes, ResourceType). Razor components bind to `.EditedValue`:
```razor
<MudTextField @bind-Value="_editorModel.Code.EditedValue"
              For="@(()=>_editorModel.Code.EditedValue)" />
```

## Test Conventions

Tests use **xUnit** + **Moq 4.18.4** (do not upgrade) + **FluentAssertions 7.1.0** (do not upgrade).

- Test method naming: `MethodName_StateUnderTest_ExpectedBehavior` (no "Should" prefix)
- `[Trait]` attributes at class level only; use hierarchical namespace traits
- `// ReSharper disable InconsistentNaming` at top of each test file
- Always include `[Trait("Category", "Unit")]`
- Arrange-Act-Assert pattern; helper methods in a `#region` at the bottom
- Mock concrete dependencies only when they have interfaces; use concrete implementations otherwise
- Always add a `"because"` clause to FluentAssertions

See `test-conventions.md` (root) and `docs/test-conventions.md` for the full reference.

## Key Technologies

- **.NET 10.0** / C# 13, nullable reference types enabled everywhere; `$(HTTargetFramework)` in `Directory.Build.props` sets TFM for all projects
- **Central Package Management** via `Directory.Packages.props` — all NuGet versions live there, no `Version=` in individual `.csproj` files
- **Blazor WebAssembly** hosted by ASP.NET Core, MudBlazor 9.x component library
- **SQL Server** with stored procedures; `Microsoft.Data.SqlClient`
- **Serilog** with daily rolling file sink (`Logs/`)
- **xUnit / Moq / FluentAssertions** for testing

## Configuration

`HT.Microsoft.IConfiguration.Extensions` provides type-safe access:
```csharp
config.GetString("ResourceMapper:ConnectionStrings:Database:RW")
config.GetTypedSection<MyOptions>("Section:Key")
config.IsDevelopment()
config.ValidateRequiredKeys("key1", "key2")
```
User secrets are configured for `UI.Server` (for local connection strings).

## Project Notes

`__ProjectNotes/` contains architectural decisions and user stories. Key files:
- `Concept1.md` — Core architectural concepts
- `UserStories.md` — Feature requirements (US-001 through US-010)
- `UserStoryMapping.md` — Workflow analysis
- `HomePageGridDesign.md` / `SearchBehaviorDesign.md` — Feature-specific design
