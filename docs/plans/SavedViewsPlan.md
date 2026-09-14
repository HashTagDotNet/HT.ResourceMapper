# Saved Views Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Full CRUD for named grid queries, reached entirely from the application menu, owned by a single configured identity rather than a per-browser GUID.

**Architecture:** A `SavedView` table modelled on the existing `Diagram` table stores each query as an opaque query string. An `ICurrentIdentity` seam returns one configured `OwnerId`, replacing the anonymous per-browser id that `ClientSetting` and `Diagram` use today. The hamburger menu gains a `Saved Views` submenu (Save / Save As… / the list / Manage), and `/saved-views` is the manage page.

**Tech Stack:** .NET 10 / C# 13, Blazor Server (interactive), MudBlazor 9.5.0, SQL Server via `IDbConnector` + stored procedures, xUnit / Moq / FluentAssertions, Playwright (`playwright-core`, system Chrome).

**Spec:** `docs/plans/SavedViewsDesign.md` — read it before starting. This plan argues from it.

## Global Constraints

- **Do not upgrade Moq (4.18.4) or FluentAssertions (7.1.0).** Pinned deliberately.
- **Central Package Management** — all NuGet versions live in `Directory.Packages.props`; never put `Version=` in a `.csproj`.
- **Test conventions** (`test-conventions.md`): `MethodName_StateUnderTest_ExpectedBehavior` naming with no "Should" prefix; `// ReSharper disable InconsistentNaming` at the top of every test file; `[Trait]` at class level only, using the hierarchical namespace form; `[Trait("Category", "Unit")]` always present; Arrange-Act-Assert; helper methods in a `#region` at the bottom; **every FluentAssertions call takes a `because` clause**.
- **The SQL project is old-style SSDT.** `dotnet build` does **not** build it. Apply SQL to `(localdb)\MSSQLLocalDB\ResourceMapper` with `sqlcmd`, and add every new `.sql` file to `HTResourceMapperDb.sqlproj` as a `<Build Include=...>` item.
- **Stop the running app before `dotnet build`** — it locks `ResourceMapper.UI.Web.exe`. Symptom is `MSB3027 ... file is locked by`.
- **Run the app as:** `ASPNETCORE_ENVIRONMENT=localhost ASPNETCORE_URLS="http://localhost:5200" dotnet run --project UI/ResourceMapper.UI.Web --no-launch-profile`
- **UI titles are Title Case** — "Manage Saved Views", not "Manage saved views".
- **Browser-verify UI changes.** `dotnet build` passing is not evidence a UI change works; drive it with `tools/e2e` helpers. Do not commit ad-hoc drive scripts.
- **`sqlcmd` needs Windows-style paths** from Git Bash (`Database\...`), and `SET QUOTED_IDENTIFIER ON;`.
- **`SET QUOTED_IDENTIFIER ON` is not optional here, and it bites twice.** `SavedView` carries a
  filtered index, and (a) `CREATE INDEX` fails outright without it, silently leaving the table
  without its at-most-one-default guarantee, and (b) a **stored procedure captures its SET options
  at CREATE time** — a procedure created with it OFF fails at runtime on any UPDATE to that table,
  with `Msg 1934`. Prefix every `sqlcmd -i` application with `SET QUOTED_IDENTIFIER ON; SET
  ANSI_NULLS ON; GO`. An SSDT publish sets these correctly on its own; hand-applying does not.

---

## File Structure

**Created**

| File | Responsibility |
|---|---|
| `Modules/Common/ResourceMapper.Common.Server/Identity/ICurrentIdentity.cs` | The seam — one property, `OwnerId` |
| `Modules/Common/ResourceMapper.Common.Server/Identity/ConfiguredIdentity.cs` | Config-backed single owner |
| `Database/HTResourceMapperDb/Tables/SavedView.sql` | The table |
| `Database/HTResourceMapperDb/User Defined Types/SavedViewOrderList.sql` | TVP for reorder |
| `Database/HTResourceMapperDb/Stored Procedures/SavedView_{Upsert,ListForOwner,Delete,SetDefault,Reorder}.sql` | CRUD |
| `Modules/Common/ResourceMapper.Common.Shared/SavedViews/SavedViewModel.cs` | The DTO the UI binds to |
| `Modules/Common/ResourceMapper.Common.Shared/SavedViews/Contracts/SaveViewRequest.cs` | Save/Save As payload |
| `Modules/Common/ResourceMapper.Common.Server/SavedViews/Interfaces/ISavedViewRepository.cs` | Data contract |
| `Modules/Common/ResourceMapper.Common.Server/SavedViews/Interfaces/ISavedViewService.cs` | Service contract |
| `Modules/Common/ResourceMapper.Common.Server/SavedViews/SavedViewSqlRepository.cs` | Sproc calls |
| `Modules/Common/ResourceMapper.Common.Server/SavedViews/SavedViewService.cs` | Validation + orchestration |
| `UI/ResourceMapper.UI.Web/Components/Layout/SavedViewsMenu.razor` | The hamburger submenu |
| `UI/ResourceMapper.UI.Web/Components/SavedViews/SaveViewDialog.razor` | Name prompt for Save As |
| `UI/ResourceMapper.UI.Web/Components/Pages/SavedViews.razor` | The manage page |
| `UI/ResourceMapper.UI.Web/Controllers/Api/SavedViewsController.cs` | HTTP API for all five operations |
| `_Tests/.../Identity/ConfiguredIdentityTests.cs` | Seam tests |
| `_Tests/.../SavedViews/SavedViewServiceTests.cs` | Service tests |
| `tools/e2e/tests/saved-views.spec.js` | End-to-end round trip |

**Modified**

| File | Change |
|---|---|
| `Database/HTResourceMapperDb/Tables/{ClientSetting,Diagram}.sql` | `ClientId` → `OwnerId` |
| `Database/HTResourceMapperDb/Stored Procedures/{ClientSetting_*,Diagram_*}.sql` | `@ClientId` → `@OwnerId` |
| `Modules/Common/.../Settings/ClientSettingsSqlRepository.cs`, `.../Explorer/DiagramSqlRepository.cs` | Parameter rename |
| `Modules/Common/.../Utils/DependencyRegistration.cs` | Register identity + saved views |
| `UI/ResourceMapper.UI.Web/Components/Pages/Home.razor` | Startup resolution, menu wiring |
| `UI/ResourceMapper.UI.Web/Components/Pages/ResourceExplorer.razor` | Use `ICurrentIdentity`; delete the client-id chip |
| `UI/ResourceMapper.UI.Web/Components/Layout/MainLayout.razor` | Add the submenu |
| `UI/ResourceMapper.UI.Web/Explorer/ClientIdentity.cs` | **Deleted** |
| `Database/HTResourceMapperDb/HTResourceMapperDb.sqlproj` | `<Build Include>` for every new `.sql` |

---

### Task 1: The identity seam

**Files:**
- Create: `Modules/Common/ResourceMapper.Common.Server/Identity/ICurrentIdentity.cs`
- Create: `Modules/Common/ResourceMapper.Common.Server/Identity/ConfiguredIdentity.cs`
- Create: `_Tests/Common/Server/ResourceMapper.Common.Server.Tests/Identity/ConfiguredIdentityTests.cs`
- Modify: `Modules/Common/ResourceMapper.Common.Server/Utils/DependencyRegistration.cs`

**Interfaces:**
- Consumes: `IConfiguration`, and `GetString` from `HT.Microsoft.IConfiguration.Extensions`.
- Produces: `ICurrentIdentity.OwnerId` (a non-empty `string`). Every later task uses this instead of a client id.

- [ ] **Step 1: Write the failing test**

`_Tests/Common/Server/ResourceMapper.Common.Server.Tests/Identity/ConfiguredIdentityTests.cs`:

```csharp
// ReSharper disable InconsistentNaming

using Microsoft.Extensions.Configuration;
using ResourceMapper.Common.Server.Identity;

namespace ResourceMapper.Common.Server.Tests.Identity
{
    [Trait("Category", "Unit")]
    [Trait("Category", "ResourceMapper")]
    [Trait("Category", "ResourceMapper/Common")]
    [Trait("Category", "ResourceMapper/Common/Server")]
    [Trait("Category", "ResourceMapper/Common/Server/Identity")]
    [Trait("Category", "ResourceMapper/Common/Server/Identity/ConfiguredIdentity")]
    public class ConfiguredIdentityTests
    {
        [Fact]
        public void OwnerId_KeyConfigured_ReturnsConfiguredValue()
        {
            var sut = new ConfiguredIdentity(Config("steve"));

            sut.OwnerId.Should().Be("steve", "because the configured owner wins over the default");
        }

        [Fact]
        public void OwnerId_KeyMissing_ReturnsAnonymous()
        {
            var sut = new ConfiguredIdentity(Config(null));

            sut.OwnerId.Should().Be("anonymous",
                "because there is no identity provider yet and the default must still be a stable owner");
        }

        [Fact]
        public void OwnerId_KeyBlank_ReturnsAnonymous()
        {
            var sut = new ConfiguredIdentity(Config("   "));

            sut.OwnerId.Should().Be("anonymous", "because whitespace is not an owner");
        }

        #region helpers

        private static IConfiguration Config(string? ownerId)
        {
            var values = new Dictionary<string, string?>();
            if (ownerId is not null) values["ResourceMapper:Identity:OwnerId"] = ownerId;
            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        #endregion
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~ConfiguredIdentityTests"`
Expected: FAIL — `ConfiguredIdentity` does not exist (compile error).

- [ ] **Step 3: Write the implementation**

`ICurrentIdentity.cs`:

```csharp
namespace ResourceMapper.Common.Server.Identity
{
    /// <summary>
    /// Who owns the personal artefacts: saved views, explorer diagrams, grid settings.
    /// There is no identity provider yet, so the only implementation returns a single configured
    /// owner. This exists so ownership has the right shape now; swapping in a real provider later
    /// is one implementation rather than a schema change across three tables.
    /// </summary>
    public interface ICurrentIdentity
    {
        string OwnerId { get; }
    }
}
```

`ConfiguredIdentity.cs`:

```csharp
using Microsoft.Extensions.Configuration;

namespace ResourceMapper.Common.Server.Identity
{
    /// <summary>
    /// Single-owner identity, read from ResourceMapper:Identity:OwnerId. Falls back to
    /// 'anonymous' when unset, which is accurate: every request really is the same person until
    /// an identity provider exists.
    /// </summary>
    public class ConfiguredIdentity : ICurrentIdentity
    {
        public const string ConfigKey = "ResourceMapper:Identity:OwnerId";
        public const string DefaultOwnerId = "anonymous";

        public ConfiguredIdentity(IConfiguration configuration)
        {
            var configured = configuration[ConfigKey];
            OwnerId = string.IsNullOrWhiteSpace(configured) ? DefaultOwnerId : configured.Trim();
        }

        public string OwnerId { get; }
    }
}
```

- [ ] **Step 4: Register it**

In `DependencyRegistration.cs`, beside the other `TryAddScoped` calls:

```csharp
services.TryAddSingleton<ICurrentIdentity, ConfiguredIdentity>();
```

Singleton, not scoped: the value is configuration, and it cannot vary per request while there is one owner.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~ConfiguredIdentityTests"`
Expected: PASS, 3 tests.

- [ ] **Step 6: Commit**

```bash
git add Modules/Common/ResourceMapper.Common.Server/Identity _Tests/Common/Server/ResourceMapper.Common.Server.Tests/Identity Modules/Common/ResourceMapper.Common.Server/Utils/DependencyRegistration.cs
git commit -m "feat(identity): single configured owner behind ICurrentIdentity"
```

---

### Task 2: Move the existing surfaces onto the owner

Diagrams and grid settings currently key on a per-browser GUID. This moves them onto `ICurrentIdentity` so there is one identity model, and deletes the UI that existed to work around not having one.

**Files:**
- Modify: `Database/HTResourceMapperDb/Tables/ClientSetting.sql`, `Tables/Diagram.sql`
- Modify: `Database/HTResourceMapperDb/Stored Procedures/ClientSetting_Get.sql`, `ClientSetting_Upsert.sql`, `Diagram_Upsert.sql`, `Diagram_Delete.sql`, `Diagram_ListForClient.sql`
- Modify: `Modules/Common/.../Settings/ClientSettingsSqlRepository.cs`, `Settings/ClientSettingsService.cs` (+ their interfaces), `Explorer/DiagramSqlRepository.cs`, `Explorer/DiagramService.cs` (+ interfaces)
- Modify: `UI/ResourceMapper.UI.Web/Components/Pages/Home.razor`, `Components/Pages/ResourceExplorer.razor`
- Delete: `UI/ResourceMapper.UI.Web/Explorer/ClientIdentity.cs`
- Modify: `_Tests/.../Explorer/DiagramServiceTests.cs` (parameter renames)

**Interfaces:**
- Consumes: `ICurrentIdentity.OwnerId` from Task 1.
- Produces: repositories and services whose first parameter is `string ownerId`. Task 6 mirrors these signatures.

- [ ] **Step 1: Rename the columns**

`Tables/ClientSetting.sql` — rename `[ClientId]` to `[OwnerId]` and `UK_ClientSetting_Client_Key` to `UK_ClientSetting_Owner_Key`.
`Tables/Diagram.sql` — rename `[ClientId]` to `[OwnerId]` and `IX_Diagram_ClientId` to `IX_Diagram_OwnerId`.

Leave `ShareId` alone — it is a share token, not an owner.

- [ ] **Step 2: Rename `Diagram_ListForClient` and update all five procedures**

`git mv "Database/HTResourceMapperDb/Stored Procedures/Diagram_ListForClient.sql" "Database/HTResourceMapperDb/Stored Procedures/Diagram_ListForOwner.sql"`, rename the procedure inside it, then replace `@ClientId` with `@OwnerId` and `ClientId` with `OwnerId` throughout all five procedure files. Update the `<Build Include>` entry for the renamed file in `HTResourceMapperDb.sqlproj`.

- [ ] **Step 3: Apply to localdb**

Because these are `CREATE PROCEDURE` scripts, apply them with `CREATE OR ALTER`, and the tables with explicit `sp_rename`:

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -d ResourceMapper -Q "SET QUOTED_IDENTIFIER ON;
EXEC sp_rename 'HTResourceMapper.ClientSetting.ClientId', 'OwnerId', 'COLUMN';
EXEC sp_rename 'HTResourceMapper.Diagram.ClientId', 'OwnerId', 'COLUMN';"
```

Then apply each procedure file with its `CREATE PROCEDURE` swapped to `CREATE OR ALTER PROCEDURE`.

- [ ] **Step 4: Rename through the C# layers**

Rename the `clientId` parameter to `ownerId` in `IClientSettingsRepository`, `IClientSettingsService`, `IDiagramRepository`, `IDiagramService` and their implementations, and change the `.AddVarchar("@ClientId", ...)` calls to `"@OwnerId"`. This is a rename, not a behaviour change — no logic moves.

- [ ] **Step 5: Switch the call sites and delete the workaround UI**

In `Home.razor` and `ResourceExplorer.razor`: `@inject ICurrentIdentity Identity`, replace `_clientId = await ClientIdentity.GetOrCreateAsync(JS);` with `_clientId = Identity.OwnerId;` (rename the field to `_ownerId`), and delete the `using ResourceMapper.UI.Web.Explorer;` import.

In `ResourceExplorer.razor`, delete the client-id block at `:71` — the `Client: xxxxxxxx…` caption, its `MudTooltip`/copy button, and the `CopyClientIdAsync` method. It exists only to carry a browser id to another browser, which is the problem being removed.

Delete `UI/ResourceMapper.UI.Web/Explorer/ClientIdentity.cs`.

- [ ] **Step 6: Fix the diagram service tests**

`DiagramServiceTests.cs` refers to client ids in test names and `because` clauses. Rename `SaveAsync_BlankClientId_ReturnsValidationAndDoesNotCallRepo` to `SaveAsync_BlankOwnerId_ReturnsValidationAndDoesNotCallRepo` and update its `because` text to say owner. Behaviour is unchanged.

- [ ] **Step 7: Run the full test suite**

Run: `dotnet test`
Expected: PASS, except the two known `NotFound`→404 mapping failures (PL-01). Any other red is a real signal.

- [ ] **Step 8: Verify in the browser**

Start the app, open `/explorer`, confirm the client-id caption is gone and a diagram still saves and lists. Then open `/` and confirm the grid still resumes its last view.

- [ ] **Step 9: Commit**

```bash
git add Database Modules UI _Tests
git commit -m "refactor(identity): key settings and diagrams on OwnerId, drop the browser client id"
```

---

### Task 3: Prove the nested menu renders

The whole UI depends on a `MudMenu` nested inside the hamburger `MudMenu`. MudBlazor 9.5 supports it, but nothing in this codebase does it yet. Prove it before building on it — if it does not work, the design changes to a flyout or a dedicated page and the later tasks change shape.

**Files:**
- Modify: `UI/ResourceMapper.UI.Web/Components/Layout/MainLayout.razor`

**Interfaces:**
- Produces: a confirmed-working submenu structure the Task 8 component copies.

- [ ] **Step 1: Add a throwaway nested menu**

In `MainLayout.razor`, inside the existing `MudMenu` and after the `Import…` item:

```razor
<MudMenu Label="Saved Views" Icon="@Icons.Material.Outlined.BookmarkBorder"
         ActivationEvent="@MouseEvent.MouseOver" AnchorOrigin="Origin.TopRight">
    <MudMenuItem>Spike Item A</MudMenuItem>
    <MudMenuItem>Spike Item B</MudMenuItem>
</MudMenu>
```

- [ ] **Step 2: Build and drive it**

Stop the app, `dotnet build UI/ResourceMapper.UI.Web`, restart, then open the hamburger and hover `Saved Views` in a real browser.

Expected: the submenu opens beside the parent, both spike items are visible and clickable, and the parent menu does not close when moving onto the submenu.

**RESULT (verified 2026-09-14): it works** — the submenu opens on hover, the parent stays open
while the pointer moves onto it, and items are clickable.

**Use `StartIcon`, not `Icon`.** With `Icon`, MudBlazor 9.5 renders an icon-only activator and
drops `Label` entirely, so the entry appears as a bare bookmark glyph with no text and looks like
nesting failed. The working form is:

```razor
<MudMenu Label="Saved Views" StartIcon="@Icons.Material.Outlined.BookmarkBorder"
         ActivationEvent="@MouseEvent.MouseOver" AnchorOrigin="Origin.TopRight"
         FullWidth="true">
```

- [ ] **Step 3: Record the outcome**

If it works, keep the structure and move on. If it does not, **stop and report** — the design's menu-only placement is the thing at risk, and the remedy (a flyout panel, or promoting the list onto the manage page) is a design decision, not an implementation detail.

- [ ] **Step 4: Revert the spike**

Remove the two spike items; leave the `MudMenu` shell in place for Task 6.

- [ ] **Step 5: Commit**

```bash
git add UI/ResourceMapper.UI.Web/Components/Layout/MainLayout.razor
git commit -m "chore(nav): add the Saved Views submenu shell"
```

---

### Task 4: The table and the reorder type

**Files:**
- Create: `Database/HTResourceMapperDb/Tables/SavedView.sql`
- Create: `Database/HTResourceMapperDb/User Defined Types/SavedViewOrderList.sql`
- Modify: `Database/HTResourceMapperDb/HTResourceMapperDb.sqlproj`

**Interfaces:**
- Produces: `[HTResourceMapper].[SavedView]` and `[HTResourceMapper].[SavedViewOrderList]`, both consumed by Task 5.

- [ ] **Step 1: Write the table**

```sql
-- A named grid query, owned by ICurrentIdentity.OwnerId. QueryString is the grid's own
-- '?n=...&s=...&f=...' payload and is OPAQUE to the server -- it is never parsed here, exactly as
-- Diagram.DiagramJson is not. That is why the filter format can change without a migration.
-- NVARCHAR(MAX) is deliberate: four filters over high-cardinality tags already exceed 2000 chars.
CREATE TABLE [HTResourceMapper].[SavedView]
(
    [SavedViewId] INT NOT NULL IDENTITY(1,1)
        CONSTRAINT [PK_SavedView_SavedViewId] PRIMARY KEY (SavedViewId)
    ,[SavedViewUid] VARCHAR(40) NOT NULL
        CONSTRAINT [UK_SavedView_SavedViewUid] UNIQUE (SavedViewUid)
    ,[OwnerId] VARCHAR(64) NOT NULL
    ,[Name] NVARCHAR(200) NOT NULL
    ,[QueryString] NVARCHAR(MAX) NOT NULL
    ,[SortOrder] INT NOT NULL
        CONSTRAINT [DF_SavedView_SortOrder] DEFAULT (0)
    ,[IsDefault] BIT NOT NULL
        CONSTRAINT [DF_SavedView_IsDefault] DEFAULT (0)
    ,[CreatedOn] DateTime2(0) NOT NULL
        CONSTRAINT [DF_SavedView_CreatedOn] DEFAULT (SYSUTCDATETIME())
    ,[UpdatedOn] DateTime2(0) NULL
    ,CONSTRAINT [UK_SavedView_Owner_Name] UNIQUE ([OwnerId], [Name])
)
GO
-- The menu lists an owner's views on every page load.
CREATE NONCLUSTERED INDEX [IX_SavedView_OwnerId]
    ON [HTResourceMapper].[SavedView] ([OwnerId]);
GO
-- At most one default per owner, enforced by the database rather than by code remembering to
-- clear the previous one. Same pattern as UX_ResourceTypeTag_DefaultPrimary.
CREATE UNIQUE NONCLUSTERED INDEX [UX_SavedView_Default]
    ON [HTResourceMapper].[SavedView] ([OwnerId])
    WHERE [IsDefault] = 1;
```

- [ ] **Step 2: Write the reorder type**

```sql
-- One row per view, carrying its new position. Lets a drag commit in a single round trip.
CREATE TYPE [HTResourceMapper].[SavedViewOrderList] AS TABLE
(
    [SavedViewUid] VARCHAR(40) NOT NULL,
    [SortOrder]    INT         NOT NULL
);
```

- [ ] **Step 3: Register both in the project**

Add to `HTResourceMapperDb.sqlproj` beside the other `<Build Include>` items:

```xml
<Build Include="Tables\SavedView.sql" />
<Build Include="User Defined Types\SavedViewOrderList.sql" />
```

- [ ] **Step 4: Apply to localdb and verify the default constraint bites**

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -d ResourceMapper -i "Database\HTResourceMapperDb\Tables\SavedView.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d ResourceMapper -i "Database\HTResourceMapperDb\User Defined Types\SavedViewOrderList.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d ResourceMapper -Q "SET QUOTED_IDENTIFIER ON;
INSERT INTO HTResourceMapper.SavedView (SavedViewUid, OwnerId, Name, QueryString, IsDefault)
VALUES ('sv-t1','t','A','?n=100',1),('sv-t2','t','B','?n=100',1);"
```

Expected: the second row is REJECTED with a duplicate key error on `UX_SavedView_Default`. That failure is the proof the constraint works.

Then clean up: `sqlcmd -S "(localdb)\MSSQLLocalDB" -d ResourceMapper -Q "DELETE FROM HTResourceMapper.SavedView WHERE OwnerId='t';"`

- [ ] **Step 5: Commit**

```bash
git add Database/HTResourceMapperDb
git commit -m "feat(db): SavedView table and reorder table type"
```

---

### Task 5: The stored procedures

**Files:**
- Create: `Database/HTResourceMapperDb/Stored Procedures/SavedView_Upsert.sql`, `SavedView_ListForOwner.sql`, `SavedView_Delete.sql`, `SavedView_SetDefault.sql`, `SavedView_Reorder.sql`
- Modify: `Database/HTResourceMapperDb/HTResourceMapperDb.sqlproj`

**Interfaces:**
- Consumes: the table and TVP from Task 4.
- Produces: `SavedView_Upsert` returns one row `(Result VARCHAR(10), SavedViewUid VARCHAR(40))` where Result is `created` | `updated` | `denied`. `SavedView_ListForOwner` returns `SavedViewUid, Name, QueryString, SortOrder, IsDefault, CreatedOn, UpdatedOn`. Task 6's repository maps exactly these.

- [ ] **Step 1: Write `SavedView_Upsert`**

```sql
-- Create or update one saved view. Matches on (SavedViewUid, OwnerId) so an owner can only
-- update its own rows; a uid owned by someone else is refused rather than blind-inserted into a
-- unique-key violation. Also serves rename: Name is just another updated column.
CREATE PROCEDURE [HTResourceMapper].[SavedView_Upsert]
    @SavedViewUid VARCHAR(40),
    @OwnerId VARCHAR(64),
    @Name NVARCHAR(200),
    @QueryString NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Result VARCHAR(10);

    IF EXISTS (SELECT 1 FROM [HTResourceMapper].[SavedView]
               WHERE SavedViewUid = @SavedViewUid AND OwnerId = @OwnerId)
    BEGIN
        UPDATE [HTResourceMapper].[SavedView]
        SET Name = @Name, QueryString = @QueryString, UpdatedOn = SYSUTCDATETIME()
        WHERE SavedViewUid = @SavedViewUid AND OwnerId = @OwnerId;
        SET @Result = 'updated';
    END
    ELSE IF EXISTS (SELECT 1 FROM [HTResourceMapper].[SavedView] WHERE SavedViewUid = @SavedViewUid)
    BEGIN
        SET @Result = 'denied';
    END
    ELSE
    BEGIN
        -- New views land at the end of the owner's list.
        DECLARE @NextOrder INT =
            ISNULL((SELECT MAX(SortOrder) + 1 FROM [HTResourceMapper].[SavedView] WHERE OwnerId = @OwnerId), 0);

        INSERT INTO [HTResourceMapper].[SavedView] (SavedViewUid, OwnerId, Name, QueryString, SortOrder)
        VALUES (@SavedViewUid, @OwnerId, @Name, @QueryString, @NextOrder);
        SET @Result = 'created';
    END

    IF @Result = 'denied'
        SELECT @Result AS Result, CAST('' AS VARCHAR(40)) AS SavedViewUid;
    ELSE
        SELECT @Result AS Result, @SavedViewUid AS SavedViewUid;
END
```

- [ ] **Step 2: Write `SavedView_ListForOwner`**

```sql
-- An owner's saved views, in manual order then by name. Drives both the menu and the manage page.
CREATE PROCEDURE [HTResourceMapper].[SavedView_ListForOwner]
    @OwnerId VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT SavedViewUid, Name, QueryString, SortOrder, IsDefault, CreatedOn, UpdatedOn
    FROM [HTResourceMapper].[SavedView] WITH(NOLOCK)
    WHERE OwnerId = @OwnerId
    ORDER BY SortOrder, Name;
END
```

- [ ] **Step 3: Write `SavedView_Delete`**

```sql
-- Delete one of the owner's views. Deleting the default simply leaves the owner with none, which
-- the grid treats as "fall back to the resume setting".
CREATE PROCEDURE [HTResourceMapper].[SavedView_Delete]
    @OwnerId VARCHAR(64),
    @SavedViewUid VARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM [HTResourceMapper].[SavedView]
    WHERE OwnerId = @OwnerId AND SavedViewUid = @SavedViewUid;

    SELECT @@ROWCOUNT AS DeletedCount;
END
```

- [ ] **Step 4: Write `SavedView_SetDefault`**

```sql
-- Make one view the owner's default, or clear the default entirely when @SavedViewUid is NULL.
-- The clear MUST happen before the set: UX_SavedView_Default permits only one IsDefault=1 row per
-- owner, so doing it the other way round violates the index. Both statements are in one
-- transaction so a failure cannot leave the owner with no default when they asked for one.
CREATE PROCEDURE [HTResourceMapper].[SavedView_SetDefault]
    @OwnerId VARCHAR(64),
    @SavedViewUid VARCHAR(40) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

        UPDATE [HTResourceMapper].[SavedView]
        SET IsDefault = 0, UpdatedOn = SYSUTCDATETIME()
        WHERE OwnerId = @OwnerId AND IsDefault = 1;

        IF @SavedViewUid IS NOT NULL
            UPDATE [HTResourceMapper].[SavedView]
            SET IsDefault = 1, UpdatedOn = SYSUTCDATETIME()
            WHERE OwnerId = @OwnerId AND SavedViewUid = @SavedViewUid;

    COMMIT TRANSACTION;
END
```

- [ ] **Step 5: Write `SavedView_Reorder`**

```sql
-- Apply a whole new ordering in one round trip. Rows not named in @Order are left alone, and uids
-- belonging to another owner are ignored by the join rather than erroring.
CREATE PROCEDURE [HTResourceMapper].[SavedView_Reorder]
    @OwnerId VARCHAR(64),
    @Order [HTResourceMapper].[SavedViewOrderList] READONLY
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE sv
    SET SortOrder = o.SortOrder, UpdatedOn = SYSUTCDATETIME()
    FROM [HTResourceMapper].[SavedView] sv
    INNER JOIN @Order o ON o.SavedViewUid = sv.SavedViewUid
    WHERE sv.OwnerId = @OwnerId;

    SELECT @@ROWCOUNT AS UpdatedCount;
END
```

- [ ] **Step 6: Register all five and apply them**

Add a `<Build Include="Stored Procedures\SavedView_*.sql" />` line per file, then apply each to localdb with `CREATE PROCEDURE` swapped to `CREATE OR ALTER PROCEDURE`.

- [ ] **Step 7: Verify the default switch by hand**

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -d ResourceMapper -Q "SET QUOTED_IDENTIFIER ON;
EXEC HTResourceMapper.SavedView_Upsert 'sv-a','t','A','?n=100';
EXEC HTResourceMapper.SavedView_Upsert 'sv-b','t','B','?n=100';
EXEC HTResourceMapper.SavedView_SetDefault 't','sv-a';
EXEC HTResourceMapper.SavedView_SetDefault 't','sv-b';
SELECT Name, IsDefault, SortOrder FROM HTResourceMapper.SavedView WHERE OwnerId='t' ORDER BY SortOrder;"
```

Expected: exactly one row with `IsDefault = 1`, and it is B. `SortOrder` is 0 then 1. Then delete the `OwnerId='t'` rows.

- [ ] **Step 8: Commit**

```bash
git add Database/HTResourceMapperDb
git commit -m "feat(db): SavedView CRUD stored procedures"
```

---

### Task 6: Contracts, repository and service

**Files:**
- Create: `Modules/Common/ResourceMapper.Common.Shared/SavedViews/SavedViewModel.cs`
- Create: `Modules/Common/ResourceMapper.Common.Shared/SavedViews/Contracts/SaveViewRequest.cs`
- Create: `Modules/Common/ResourceMapper.Common.Server/SavedViews/Interfaces/ISavedViewRepository.cs`, `Interfaces/ISavedViewService.cs`
- Create: `Modules/Common/ResourceMapper.Common.Server/SavedViews/SavedViewSqlRepository.cs`, `SavedViewService.cs`
- Create: `_Tests/Common/Server/ResourceMapper.Common.Server.Tests/SavedViews/SavedViewServiceTests.cs`
- Modify: `Modules/Common/ResourceMapper.Common.Server/Utils/DependencyRegistration.cs`

**Interfaces:**
- Consumes: the procedures from Task 5; `ICurrentIdentity` from Task 1.
- Produces: `ISavedViewService` with `ListAsync(string ownerId, CancellationToken)`, `SaveAsync(string ownerId, SaveViewRequest, CancellationToken)`, `DeleteAsync(string ownerId, string uid, CancellationToken)`, `SetDefaultAsync(string ownerId, string? uid, CancellationToken)`, `ReorderAsync(string ownerId, IReadOnlyList<string> uidsInOrder, CancellationToken)`. All return `ApiServiceResponse<T>`. Tasks 8-10 call exactly these.

- [ ] **Step 1: Write the shared types**

`SavedViewModel.cs`:

```csharp
namespace ResourceMapper.Common.Shared.SavedViews
{
    /// <summary>One saved grid query, as the UI sees it.</summary>
    public class SavedViewModel
    {
        public string SavedViewUid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        /// <summary>The grid's own '?n=...&amp;s=...&amp;f=...' payload. Opaque to the server.</summary>
        public string QueryString { get; set; } = string.Empty;

        public int SortOrder { get; set; }
        public bool IsDefault { get; set; }
    }
}
```

`Contracts/SaveViewRequest.cs`:

```csharp
namespace ResourceMapper.Common.Shared.SavedViews.Contracts
{
    /// <summary>
    /// Save or Save As. An empty SavedViewUid means "create"; a populated one means "update that
    /// view", which is also how rename and overwrite-on-duplicate-name are expressed.
    /// </summary>
    public class SaveViewRequest
    {
        public string SavedViewUid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string QueryString { get; set; } = string.Empty;
    }
}
```

- [ ] **Step 2: Write the failing service tests**

`_Tests/Common/Server/ResourceMapper.Common.Server.Tests/SavedViews/SavedViewServiceTests.cs`:

```csharp
// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ResourceMapper.Common.Server.SavedViews;
using ResourceMapper.Common.Server.SavedViews.Interfaces;
using ResourceMapper.Common.Shared.SavedViews;
using ResourceMapper.Common.Shared.SavedViews.Contracts;

namespace ResourceMapper.Common.Server.Tests.SavedViews
{
    [Trait("Category", "Unit")]
    [Trait("Category", "ResourceMapper")]
    [Trait("Category", "ResourceMapper/Common")]
    [Trait("Category", "ResourceMapper/Common/Server")]
    [Trait("Category", "ResourceMapper/Common/Server/SavedViews")]
    [Trait("Category", "ResourceMapper/Common/Server/SavedViews/SavedViewService")]
    public class SavedViewServiceTests
    {
        private readonly Mock<ISavedViewRepository> _repo;
        private readonly SavedViewService _sut;

        public SavedViewServiceTests()
        {
            _repo = new Mock<ISavedViewRepository>();
            _sut = new SavedViewService(_repo.Object);
        }

        #region SaveAsync

        [Fact]
        public async Task SaveAsync_BlankOwnerId_ReturnsValidationAndDoesNotCallRepo()
        {
            var response = await _sut.SaveAsync("", Req("View A"), CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because an owner is required to save against");
            _repo.Verify(r => r.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never,
                "because validation short-circuits before any write");
        }

        [Fact]
        public async Task SaveAsync_BlankName_ReturnsValidation()
        {
            var response = await _sut.SaveAsync("steve", Req("   "), CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a saved view is identified by its name");
        }

        [Fact]
        public async Task SaveAsync_NameLongerThanLimit_ReturnsValidation()
        {
            var response = await _sut.SaveAsync("steve", Req(new string('x', 201)), CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because Name is NVARCHAR(200) and a longer value would truncate");
        }

        [Fact]
        public async Task SaveAsync_BlankQueryString_ReturnsValidation()
        {
            var response = await _sut.SaveAsync("steve",
                new SaveViewRequest { Name = "View A", QueryString = "" }, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a view with no query would restore nothing");
        }

        [Fact]
        public async Task SaveAsync_VeryLongQueryString_IsAccepted()
        {
            var longQuery = "?n=100&f=tag:Consumer~eq~" +
                string.Join(",", Enumerable.Range(0, 80).Select(i => "value" + i));
            _repo.Setup(r => r.UpsertAsync("steve", It.IsAny<string>(), "View A", longQuery, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(("created", "sv-1"));

            var response = await _sut.SaveAsync("steve",
                new SaveViewRequest { Name = "View A", QueryString = longQuery }, CancellationToken.None);

            response.IsSuccess().Should().BeTrue(
                "because a real four-filter query already exceeds 2000 characters and must not be capped");
        }

        [Fact]
        public async Task SaveAsync_RepoDenies_ReturnsError()
        {
            _repo.Setup(r => r.UpsertAsync("steve", "sv-other", "View A", "?n=100", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(("denied", ""));

            var response = await _sut.SaveAsync("steve",
                new SaveViewRequest { SavedViewUid = "sv-other", Name = "View A", QueryString = "?n=100" },
                CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because that uid belongs to a different owner");
        }

        #endregion

        #region SetDefaultAsync

        [Fact]
        public async Task SetDefaultAsync_NullUid_ClearsWithoutError()
        {
            var response = await _sut.SetDefaultAsync("steve", null, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because clearing the default is a legitimate request");
            _repo.Verify(r => r.SetDefaultAsync("steve", null, It.IsAny<CancellationToken>()), Times.Once,
                "because the null flows through to the procedure that clears it");
        }

        #endregion

        #region ReorderAsync

        [Fact]
        public async Task ReorderAsync_EmptyList_DoesNotCallRepo()
        {
            var response = await _sut.ReorderAsync("steve", new List<string>(), CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because reordering nothing is a no-op, not a failure");
            _repo.Verify(r => r.ReorderAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()), Times.Never, "because there is nothing to write");
        }

        #endregion

        #region helpers

        private static SaveViewRequest Req(string name) =>
            new() { Name = name, QueryString = "?n=100&s=ResourceName:asc" };

        #endregion
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~SavedViewServiceTests"`
Expected: FAIL — `SavedViewService` does not exist.

- [ ] **Step 4: Write the interfaces**

```csharp
// ISavedViewRepository.cs
using ResourceMapper.Common.Shared.SavedViews;

namespace ResourceMapper.Common.Server.SavedViews.Interfaces
{
    public interface ISavedViewRepository
    {
        Task<List<SavedViewModel>> ListAsync(string ownerId, CancellationToken cancellationToken);

        /// <summary>Returns (Result, SavedViewUid) where Result is created | updated | denied.</summary>
        Task<(string Result, string SavedViewUid)> UpsertAsync(
            string ownerId, string savedViewUid, string name, string queryString, CancellationToken cancellationToken);

        Task<int> DeleteAsync(string ownerId, string savedViewUid, CancellationToken cancellationToken);
        Task SetDefaultAsync(string ownerId, string? savedViewUid, CancellationToken cancellationToken);
        Task ReorderAsync(string ownerId, IReadOnlyList<string> uidsInOrder, CancellationToken cancellationToken);
    }
}
```

```csharp
// ISavedViewService.cs
using HT.Api.Service.Contracts;
using ResourceMapper.Common.Shared.SavedViews;
using ResourceMapper.Common.Shared.SavedViews.Contracts;

namespace ResourceMapper.Common.Server.SavedViews.Interfaces
{
    public interface ISavedViewService
    {
        Task<ApiServiceResponse<List<SavedViewModel>>> ListAsync(string ownerId, CancellationToken cancellationToken);
        Task<ApiServiceResponse<SavedViewModel>> SaveAsync(string ownerId, SaveViewRequest request, CancellationToken cancellationToken);
        Task<ApiServiceResponse<bool>> DeleteAsync(string ownerId, string savedViewUid, CancellationToken cancellationToken);
        Task<ApiServiceResponse<bool>> SetDefaultAsync(string ownerId, string? savedViewUid, CancellationToken cancellationToken);
        Task<ApiServiceResponse<bool>> ReorderAsync(string ownerId, IReadOnlyList<string> uidsInOrder, CancellationToken cancellationToken);
    }
}
```

- [ ] **Step 5: Write the repository**

```csharp
public class SavedViewSqlRepository : ISavedViewRepository
{
    private const string SavedViewOrderListType = "[HTResourceMapper].[SavedViewOrderList]";

    private readonly IDbConnector _db;
    public SavedViewSqlRepository(IDbConnector db) => _db = db;

    public async Task<List<SavedViewModel>> ListAsync(string ownerId, CancellationToken cancellationToken)
    {
        using var cmd = _db.RO.SprocCommand("[HTResourceMapper].SavedView_ListForOwner")
            .AddVarchar("@OwnerId", ownerId);

        return await _db.Execute.ExecuteQueryAsync(cmd, dr => new SavedViewModel
        {
            SavedViewUid = dr.ReadString("SavedViewUid"),
            Name         = dr.ReadString("Name"),
            QueryString  = dr.ReadString("QueryString"),
            SortOrder    = dr.ReadInt("SortOrder"),
            IsDefault    = dr.ReadBoolean("IsDefault")
        }, cancellationToken: cancellationToken);
    }

    public async Task<(string Result, string SavedViewUid)> UpsertAsync(
        string ownerId, string savedViewUid, string name, string queryString, CancellationToken cancellationToken)
    {
        using var cmd = _db.RW.SprocCommand("[HTResourceMapper].SavedView_Upsert")
            .AddVarchar("@SavedViewUid", savedViewUid)
            .AddVarchar("@OwnerId", ownerId)
            .AddNVarchar("@Name", name)
            .AddNVarchar("@QueryString", queryString);

        var rows = await _db.Execute.ExecuteQueryAsync(cmd, dr => new
        {
            Result = dr.ReadString("Result"),
            Uid    = dr.ReadString("SavedViewUid")
        }, cancellationToken: cancellationToken);

        var row = rows.FirstOrDefault();
        return (row?.Result ?? "denied", row?.Uid ?? string.Empty);
    }

    public async Task<int> DeleteAsync(string ownerId, string savedViewUid, CancellationToken cancellationToken)
    {
        using var cmd = _db.RW.SprocCommand("[HTResourceMapper].SavedView_Delete")
            .AddVarchar("@OwnerId", ownerId)
            .AddVarchar("@SavedViewUid", savedViewUid);

        var rows = await _db.Execute.ExecuteQueryAsync(cmd,
            dr => dr.ReadInt("DeletedCount"), cancellationToken: cancellationToken);
        return rows.FirstOrDefault();
    }

    public async Task SetDefaultAsync(string ownerId, string? savedViewUid, CancellationToken cancellationToken)
    {
        using var cmd = _db.RW.SprocCommand("[HTResourceMapper].SavedView_SetDefault")
            .AddVarchar("@OwnerId", ownerId)
            .AddVarchar("@SavedViewUid", savedViewUid);   // null clears the default

        await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
    }

    public async Task ReorderAsync(string ownerId, IReadOnlyList<string> uidsInOrder, CancellationToken cancellationToken)
    {
        using var table = new DataTable();
        table.Columns.Add("SavedViewUid", typeof(string));
        table.Columns.Add("SortOrder", typeof(int));
        for (var i = 0; i < uidsInOrder.Count; i++)
            table.Rows.Add(uidsInOrder[i], i);          // position in the list IS the sort order

        using var cmd = _db.RW.SprocCommand("[HTResourceMapper].SavedView_Reorder")
            .AddVarchar("@OwnerId", ownerId)
            .AddTvp("@Order", SavedViewOrderListType, table);

        await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
    }
}
```

`AddTvp` is the same helper `ResourceSqlRepository` uses for `ResourceFilterList` (`ResourceSqlRepository.cs:149`) — do not hand-roll a `SqlParameter`.

- [ ] **Step 6: Write the service**

Validation, then delegate. Mirror `DiagramService`'s use of `ServiceResponseBuilder<T>`:

```csharp
public async Task<ApiServiceResponse<SavedViewModel>> SaveAsync(
    string ownerId, SaveViewRequest request, CancellationToken cancellationToken)
{
    var builder = new ServiceResponseBuilder<SavedViewModel>();

    if (string.IsNullOrWhiteSpace(ownerId))
        builder.Validation.AddValidation("ownerId", "Is required");
    if (request is null)
    {
        builder.Validation.AddValidation("request", "Is required");
        return builder.BuildResponse();
    }

    var name = (request.Name ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(name))
        builder.Validation.AddValidation("name", "Is required");
    else if (name.Length > MaxNameLength)
        builder.Validation.AddValidation("name", $"Must be {MaxNameLength} characters or fewer");

    // QueryString is deliberately NOT length-capped: four filters over high-cardinality tags
    // already exceed 2000 characters, and the column is NVARCHAR(MAX).
    if (string.IsNullOrWhiteSpace(request.QueryString))
        builder.Validation.AddValidation("queryString", "Is required");

    if (!builder.IsOk) return builder.BuildResponse();

    var uid = string.IsNullOrWhiteSpace(request.SavedViewUid)
        ? "sv-" + Guid.NewGuid().ToString("N")
        : request.SavedViewUid;

    var (result, savedUid) = await _repo.UpsertAsync(ownerId, uid, name, request.QueryString, cancellationToken);

    if (result == "denied")
    {
        builder.Errors.AddError(CallStatusCode.NotFound, "That saved view belongs to someone else.");
        return builder.BuildResponse();
    }

    builder.Data.Set(new SavedViewModel { SavedViewUid = savedUid, Name = name, QueryString = request.QueryString });
    return builder.BuildResponse();
}
```

Use the same shape for the others. `ReorderAsync` returns success without calling the repository when the list is empty.

- [ ] **Step 7: Register both**

```csharp
services.TryAddScoped<ISavedViewRepository, SavedViewSqlRepository>();
services.TryAddScoped<ISavedViewService, SavedViewService>();
```

- [ ] **Step 8: Run the tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~SavedViewServiceTests"`
Expected: PASS, 8 tests.

- [ ] **Step 9: Commit**

```bash
git add Modules _Tests
git commit -m "feat(saved-views): contracts, repository and service"
```

---

### Task 7: The HTTP API

Every CRUD operation gets an endpoint, so saved views are reachable from outside the Blazor
circuit — scripts, another client, or a future front end. The Blazor components still call
`ISavedViewService` in-process (this is server-side Blazor; a loopback HTTP hop would be waste),
so the controller is a second caller of the same service, never a layer the UI goes through.

**Files:**
- Create: `UI/ResourceMapper.UI.Web/Controllers/Api/SavedViewsController.cs`

**Interfaces:**
- Consumes: `ISavedViewService` (Task 6) and `ICurrentIdentity` (Task 1).
- Produces: five endpoints under `api/saved-views`, each returning `ApiResponse<T>`.

| Verb + route | Operation |
|---|---|
| `GET    api/saved-views` | List |
| `POST   api/saved-views` | Create or update (also rename) |
| `DELETE api/saved-views/{savedViewUid}` | Delete |
| `PUT    api/saved-views/{savedViewUid}/default` | Set the default |
| `PUT    api/saved-views/default` | Clear the default |
| `PUT    api/saved-views/order` | Reorder |

- [ ] **Step 1: Write the controller**

```csharp
using HT.Api.Client.Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using ResourceMapper.Common.Server.Identity;
using ResourceMapper.Common.Server.SavedViews.Interfaces;
using ResourceMapper.Common.Shared.SavedViews;
using ResourceMapper.Common.Shared.SavedViews.Contracts;

namespace ResourceMapper.UI.Web.Controllers.Api;

/// <summary>
/// CRUD for saved grid views. The owner is resolved server-side from ICurrentIdentity and is
/// never taken from the request -- a caller cannot ask for someone else's views by naming them.
/// </summary>
[Route("api/saved-views")]
[Produces("application/json")]
[Tags("Saved Views")]
public class SavedViewsController : ApiControllerBase
{
    private readonly ISavedViewService _svc;
    private readonly ICurrentIdentity _identity;

    public SavedViewsController(ISavedViewService service, ICurrentIdentity identity)
    {
        _svc = service;
        _identity = identity;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SavedViewModel>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<List<SavedViewModel>>), 500)]
    public async Task<ActionResult<ApiResponse<List<SavedViewModel>>>> List(
        CancellationToken cancellationToken = default)
        => MapServiceResponseToActionResult(
            await _svc.ListAsync(_identity.OwnerId, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SavedViewModel>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SavedViewModel>), 400)]
    [ProducesResponseType(typeof(ApiResponse<SavedViewModel>), 500)]
    public async Task<ActionResult<ApiResponse<SavedViewModel>>> Save(
        [FromBody] SaveViewRequest request,
        CancellationToken cancellationToken = default)
        => MapServiceResponseToActionResult(
            await _svc.SaveAsync(_identity.OwnerId, request, cancellationToken));

    [HttpDelete("{savedViewUid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        string savedViewUid,
        CancellationToken cancellationToken = default)
        => MapServiceResponseToActionResult(
            await _svc.DeleteAsync(_identity.OwnerId, savedViewUid, cancellationToken));

    [HttpPut("{savedViewUid}/default")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<ActionResult<ApiResponse<bool>>> SetDefault(
        string savedViewUid,
        CancellationToken cancellationToken = default)
        => MapServiceResponseToActionResult(
            await _svc.SetDefaultAsync(_identity.OwnerId, savedViewUid, cancellationToken));

    /// <summary>Clears the default entirely, leaving the owner with none.</summary>
    [HttpPut("default")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<ActionResult<ApiResponse<bool>>> ClearDefault(
        CancellationToken cancellationToken = default)
        => MapServiceResponseToActionResult(
            await _svc.SetDefaultAsync(_identity.OwnerId, null, cancellationToken));

    [HttpPut("order")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<ActionResult<ApiResponse<bool>>> Reorder(
        [FromBody] List<string> uidsInOrder,
        CancellationToken cancellationToken = default)
        => MapServiceResponseToActionResult(
            await _svc.ReorderAsync(_identity.OwnerId, uidsInOrder, cancellationToken));
}
```

- [ ] **Step 2: Build**

Stop the app, then `dotnet build UI/ResourceMapper.UI.Web`.
Expected: `Build succeeded.`

- [ ] **Step 3: Exercise every endpoint against the running app**

```bash
BASE=http://localhost:5200/api/saved-views
curl -s -X POST $BASE -H "Content-Type: application/json"      -d '{"name":"Api Test","queryString":"?n=100&s=ResourceName:asc"}'
curl -s $BASE
UID=$(curl -s $BASE | python -c "import sys,json; print(json.load(sys.stdin)['data'][0]['savedViewUid'])")
curl -s -X PUT $BASE/$UID/default
curl -s -X PUT $BASE/order -H "Content-Type: application/json" -d "[\"$UID\"]"
curl -s -X DELETE $BASE/$UID
curl -s $BASE
```

Expected: create returns the view with a uid; list shows it; set-default succeeds; reorder
succeeds; delete removes it; the final list no longer contains it.

**Deleting a missing uid returns 422, not 404** — that is the pre-existing PL-01 mapping defect
(`CallStatusCode.NotFound` -> `UnprocessableEntity`), the same one behind the two known failing
tests in `HT.Api.Service.Contracts`. Not a bug in this controller; do not "fix" it here.

- [ ] **Step 4: Check the validation path returns 400, not 500**

```bash
curl -s -o /dev/null -w "%{http_code}
" -X POST $BASE      -H "Content-Type: application/json" -d '{"name":"","queryString":""}'
```

Expected: `400`. A 500 means the service threw instead of validating, which is a bug in Task 6.

- [ ] **Step 5: Commit**

```bash
git add UI/ResourceMapper.UI.Web/Controllers/Api/SavedViewsController.cs
git commit -m "feat(saved-views): HTTP API for list, save, delete, default and reorder"
```

---

### Task 8: The menu

**Files:**
- Create: `UI/ResourceMapper.UI.Web/Components/Layout/SavedViewsMenu.razor`
- Create: `UI/ResourceMapper.UI.Web/Components/SavedViews/SaveViewDialog.razor`
- Modify: `UI/ResourceMapper.UI.Web/Components/Layout/MainLayout.razor`
- Modify: `UI/ResourceMapper.UI.Web/Components/Pages/Home.razor`

**Interfaces:**
- Consumes: `ISavedViewService` from Task 6; the submenu structure proven in Task 3.
- Produces: a `SavedViewsMenu` component; `Home.razor` exposes the current query string and the open view's uid for it.

- [ ] **Step 1: Write the name dialog**

`SaveViewDialog.razor` — copy `Components/Explorer/DiagramNameDialog.razor` and change the title to **"Save View As"** (Title Case) and the field label to "View Name". It returns the entered name via `MudDialog.Close(DialogResult.Ok(name))`.

- [ ] **Step 2: Write the menu component**

`SavedViewsMenu.razor`, rendered inside the hamburger:

```razor
<MudMenu Label="Saved Views" Icon="@Icons.Material.Outlined.BookmarkBorder"
         ActivationEvent="@MouseEvent.MouseOver" AnchorOrigin="Origin.TopRight">
    <MudMenuItem Disabled="@(!CanSave)" OnClick="OnSave">Save</MudMenuItem>
    <MudMenuItem OnClick="OnSaveAs">Save As…</MudMenuItem>
    <MudDivider />
    @foreach (var view in _views)
    {
        <MudMenuItem OnClick="@(() => OnOpen(view))">
            @(view.IsDefault ? "★ " : "")@view.Name
        </MudMenuItem>
    }
    @if (_views.Count > 0) { <MudDivider /> }
    <MudMenuItem Href="/saved-views">Manage Saved Views…</MudMenuItem>
</MudMenu>
```

**This task introduces the open-view state in `Home.razor`**, which Task 9 later sets when it
resolves a default view:

```csharp
private string _openViewUid = string.Empty;    // the saved view currently open, if any
private string _openViewName = string.Empty;   // its name, so Save can re-save without prompting
private string _openViewQuery = string.Empty;  // its query string as last saved
private List<SavedViewModel> _views = new();   // the list the menu renders

// Save targets an open view and is pointless when nothing has changed since.
private bool CanSave =>
    !string.IsNullOrEmpty(_openViewUid) &&
    !string.Equals(_lastQuery, _openViewQuery, StringComparison.Ordinal);
```

Both Save and Save As funnel through one helper, so there is a single place that writes and then
refreshes the list the menu renders from:

```csharp
private async Task SaveViewAsync(string savedViewUid, string name)
{
    var request = new SaveViewRequest
    {
        SavedViewUid = savedViewUid,        // empty => create, populated => update
        Name = name,
        QueryString = _lastQuery
    };

    var response = await SavedViews.SaveAsync(_ownerId, request, CancellationToken.None);
    if (!response.IsSuccess())
    {
        Snackbar.Add("That view could not be saved.", Severity.Warning);
        return;
    }

    _openViewUid = response.ApiResponse.Data?.SavedViewUid ?? string.Empty;
    _openViewQuery = _lastQuery;            // saved and current now agree, so Save goes quiet
    await ReloadViewsAsync();               // the menu list must show the new or renamed view
}
```

`OnSave` is then `SaveViewAsync(_openViewUid, _openViewName)` — it already has a uid, so it
updates in place and never prompts.

`ReloadViewsAsync` calls `SavedViews.ListAsync(_ownerId, ...)` and assigns `_views`. Call it once
in `OnInitializedAsync` as well, so the menu is populated before the first render. `Snackbar` is
already injected in `Home.razor`; `SavedViews` is the new `ISavedViewService` injection.

- [ ] **Step 3: Wire Save As, including the overwrite path**

```csharp
private async Task OnSaveAs()
{
    var dialog = await DialogService.ShowAsync<SaveViewDialog>("Save View As");
    var result = await dialog.Result;
    if (result is null || result.Canceled) return;

    var name = (result.Data as string ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(name)) return;

    // Match against the list already loaded for the menu. The server also rejects duplicates,
    // but SavedView_Upsert keys on UID and the validation error carries none - so the UI could
    // not turn that error into an overwrite. Resolve the uid here instead.
    var existing = _views.FirstOrDefault(v =>
        string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));

    var targetUid = string.Empty;
    if (existing is not null)
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Replace Saved View",
            $"A view named \"{existing.Name}\" already exists. Replace it?",
            yesText: "Replace", cancelText: "Cancel");
        if (confirmed != true) return;
        targetUid = existing.SavedViewUid;   // same uid => Upsert updates rather than creates
    }

    await SaveViewAsync(targetUid, name);
}
```

Do not rely on the server's duplicate-name validation to drive this: `SavedView_Upsert` keys on uid and the validation error carries no uid, so the UI could not act on it. The server check remains the backstop for a concurrent create.

- [ ] **Step 4: Mount it**

In `MainLayout.razor`, replace the Task 3 shell with `<SavedViewsMenu />`.

- [ ] **Step 5: Build and drive it**

Stop the app, build, restart. Save a view, change a filter, confirm `Save` becomes enabled, save again, then open the view from the list and confirm the grid returns to it.

- [ ] **Step 6: Commit**

```bash
git add UI
git commit -m "feat(saved-views): Save, Save As and the view list in the app menu"
```

---

### Task 9: Startup resolution

The riskiest task in this plan. `Home.razor` already gates its startup on `_resumeChecked` so the grid's default initial load cannot overwrite a saved view before the restore runs. Adding a default view makes that a three-way decision.

**Files:**
- Modify: `UI/ResourceMapper.UI.Web/Components/Pages/Home.razor`

**Interfaces:**
- Consumes: `ISavedViewService.ListAsync` from Task 6.

- [ ] **Step 1: Implement the three-way resolution**

The existing block sits in `OnAfterRenderAsync(firstRender)` and already guards on the URL
carrying no query of its own. Insert the default-view branch ahead of the resume lookup, inside
that same guard, so branch 1 (an explicit URL) keeps winning exactly as it does today:

```csharp
if (string.IsNullOrEmpty(new Uri(Navigation.Uri).Query))
{
    // 2. The owner's default view, if one is set.
    SavedViewModel? defaultView = null;
    try
    {
        var viewsResp = await SavedViews.ListAsync(_ownerId, CancellationToken.None);
        defaultView = viewsResp.IsSuccess()
            ? viewsResp.ApiResponse.Data?.FirstOrDefault(v => v.IsDefault)
            : null;
    }
    catch { /* saved views unavailable - fall through to the resume setting */ }

    if (defaultView is not null && !string.IsNullOrWhiteSpace(defaultView.QueryString))
    {
        var defaultState = ResourceFilterState.FromQueryString(defaultView.QueryString);
        _state = defaultState;
        _urlOrderBy = defaultState.OrderBy;
        _urlOrderDirection = defaultState.OrderDirection;
        _openViewUid = defaultView.SavedViewUid;
        _openViewQuery = defaultState.ToQueryString();
        _lastQuery = _openViewQuery;      // set BEFORE NavigateTo so the self-echo guard ignores it
        Navigation.NavigateTo(new Uri(Navigation.Uri).AbsolutePath + _lastQuery,
                              forceLoad: false, replace: true);
        restored = true;
    }

    // 3. Otherwise the existing home.gridView resume, unchanged.
    if (!restored)
    {
        // ... the current saved-setting block stays exactly as it is ...
    }
}
```

Two details that are load-bearing, both copied from how the existing resume branch works:
`_lastQuery` is assigned **before** `NavigateTo` so `OnLocationChanged` recognises the navigation
as self-inflicted and does not reload, and `replace: true` keeps the bare URL out of the back
stack so Back does not bounce between bare and resolved.

Setting `_openViewUid` and `_openViewQuery` is what makes `Save` in the menu (Task 8) target the
default view rather than behaving as if nothing is open.

- [ ] **Step 2: Stop the dead resume write**

`PersistViewAsync` currently writes on every navigation. With a default set, branch 3 is unreachable, so that write is never read. Skip it when a default exists. While in this method, fix the fire-and-forget call (PL-A10) — `_ = PersistViewAsync(_lastQuery)` swallows failures and races the next navigation; await it.

- [ ] **Step 3: Verify each branch by hand**

Three checks, in a real browser:

| Given | Visit | Expect |
|---|---|---|
| A default is set | `/?n=100&f=Type~eq~Queue` | the URL's query, not the default |
| A default is set | `/` | the default view's query |
| No default set | `/` | the last view you were on |

Watch specifically for a default that renders then gets replaced by an unfiltered load — that is the failure mode this ordering exists to prevent.

- [ ] **Step 4: Commit**

```bash
git add UI/ResourceMapper.UI.Web/Components/Pages/Home.razor
git commit -m "feat(saved-views): open the default view on a bare URL"
```

---

### Task 10: The manage page

**Files:**
- Create: `UI/ResourceMapper.UI.Web/Components/Pages/SavedViews.razor`

**Interfaces:**
- Consumes: `ISavedViewService` — `ListAsync`, `SaveAsync` (rename), `DeleteAsync`, `SetDefaultAsync`, `ReorderAsync`.

- [ ] **Step 1: Build the page**

`@page "/saved-views"`, titled **"Saved Views"**, following `ResourceTypes.razor`'s structure — `PageHeader`, a `MudTable`, per-row actions. Columns: drag handle, name (inline rename), a star toggle for the default, and delete.

- [ ] **Step 2: Wire rename**

Rename calls `SaveAsync` with the row's existing uid, its new name and its **existing** `QueryString` — the request carries all three, so omitting the query would blank it.

- [ ] **Step 3: Wire delete**

Behind the same confirm dialog pattern the rest of the app uses. Deleting the default leaves the owner with none, which the grid handles by falling back to resume.

- [ ] **Step 4: Wire the star and reorder**

Star calls `SetDefaultAsync(uid)`, or `SetDefaultAsync(null)` when un-starring the current default. Drag calls `ReorderAsync` with the uids in their new order.

- [ ] **Step 5: Add the search box**

A `MudTextField` above the table filtering the loaded list client-side on name,
case-insensitively. The list is already fully loaded for the menu, so this is a filter over
`_views`, not another round trip. Placeholder: "Filter saved views…".

- [ ] **Step 6: Drive it in a browser**

Rename a view, star a different one, reorder two, delete one — then reopen the hamburger and confirm the menu reflects every change, since it reads the same list.

- [ ] **Step 7: Commit**

```bash
git add UI/ResourceMapper.UI.Web/Components/Pages/SavedViews.razor
git commit -m "feat(saved-views): manage page"
```

---

### Task 11: End-to-end coverage

**Files:**
- Create: `tools/e2e/tests/saved-views.spec.js`
- Modify: `tools/e2e/sql/seed.sql`, `tools/e2e/sql/cleanup.sql`

**Interfaces:**
- Consumes: everything above.

- [ ] **Step 1: Extend the fixture**

`cleanup.sql` must delete `SavedView` rows the spec creates. Prefix their names `E2e` and delete on that prefix, matching how the existing fixture scopes itself. **Do not delete by owner** — with a single owner that would wipe real saved views.

- [ ] **Step 2: Write the spec**

One test covering the round trip: save a view, change filters, reopen it from the menu, rename it on the manage page, star it, reload a bare URL and land on it, then delete it and confirm the menu no longer lists it.

- [ ] **Step 3: Run the suite**

Run: `cd tools/e2e && npm run test:e2e`
Expected: all specs pass, including the pre-existing 8.

- [ ] **Step 4: Commit**

```bash
git add tools/e2e
git commit -m "test(e2e): saved views round trip"
```

---

## Notes for whoever executes this

**Task 3 is a real gate.** If nested `MudMenu` does not work in MudBlazor 9.5, stop rather than improvising. The design deliberately put everything in the menu and removed the Open dialog because of it; the fallback is a design decision.

**Task 9 is where the bugs will be.** Startup ordering in this component already needed a `_resumeChecked` gate to avoid clobbering saved state. A third branch is exactly the kind of change that produces a flash-then-replace.

**Do not commit ad-hoc Playwright drive scripts** into `tools/e2e`. There is prior art for that mistake in the history (`755eb1a chore(e2e): drop scratch drivers committed by mistake`).
