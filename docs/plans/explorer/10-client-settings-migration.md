# Slice #10 — Client settings migration (home grid view → server)

## Context

Final slice of the [Resource Explorer build](./00-implementation-plan-list.md). See the master list.
This is the user's earlier request: move the **home-grid saved view** off browser `localStorage` onto
the **server**, keyed by the anonymous `clientId` (slice 6) — proving the `clientId` primitive is
reusable beyond diagrams. Back-end (`ClientSetting` table + service) plus a targeted `Home.razor`
rewire.

**Key decisions (this slice):**

- **General `ClientSetting` store** — `(ClientId, SettingKey, SettingJson)` with a unique
  `(ClientId, SettingKey)` — reusable for any future per-client setting. The home grid view is its
  first key (`home.gridView`).
- **`clientId` is a parameter** (as with diagrams); obtained in `Home.razor` via the slice-6
  `ClientIdentity.GetOrCreateAsync(JS)` helper.
- **Get returns success with a null `Value` when absent** (a missing setting is not an error).
- **One-time reset accepted:** existing users' `localStorage`-saved view does not migrate; the first
  server load is empty and re-saves on the next filter change. (Noted, minor.)
- Unit-tested (service vs. mocked repo); sproc applied + exercised via `sqlcmd`. `Home.razor` verified
  by driving.

---

## 1. SQL (`Database\HTResourceMapperDb\`)

**Table** `Tables\ClientSetting.sql`:

```sql
-- Generic per-client settings, keyed by the anonymous clientId (see design §7.1). First consumer:
-- the home grid's saved view ('home.gridView'). SettingJson is an opaque client string.
CREATE TABLE [HTResourceMapper].[ClientSetting]
(
    [ClientSettingId] INT NOT NULL IDENTITY(1,1)
        CONSTRAINT [PK_ClientSetting_ClientSettingId] PRIMARY KEY (ClientSettingId)
    ,[ClientId] VARCHAR(64) NOT NULL
    ,[SettingKey] VARCHAR(100) NOT NULL
    ,[SettingJson] NVARCHAR(MAX) NOT NULL
    ,[CreatedOn] DateTime2(0) NOT NULL
        CONSTRAINT [DF_ClientSetting_CreatedOn] DEFAULT (SYSUTCDATETIME())
    ,[UpdatedOn] DateTime2(0) NULL
    ,CONSTRAINT [UK_ClientSetting_Client_Key] UNIQUE (ClientId, SettingKey)
)
```

**Sproc** `Stored Procedures\ClientSetting_Get.sql`:

```sql
CREATE PROCEDURE [HTResourceMapper].[ClientSetting_Get]
    @ClientId VARCHAR(64),
    @SettingKey VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SettingJson AS Value
    FROM [HTResourceMapper].[ClientSetting]
    WHERE ClientId = @ClientId AND SettingKey = @SettingKey;
END
```

**Sproc** `Stored Procedures\ClientSetting_Upsert.sql`:

```sql
CREATE PROCEDURE [HTResourceMapper].[ClientSetting_Upsert]
    @ClientId VARCHAR(64),
    @SettingKey VARCHAR(100),
    @SettingJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM [HTResourceMapper].[ClientSetting] WHERE ClientId = @ClientId AND SettingKey = @SettingKey)
        UPDATE [HTResourceMapper].[ClientSetting]
        SET SettingJson = @SettingJson, UpdatedOn = SYSUTCDATETIME()
        WHERE ClientId = @ClientId AND SettingKey = @SettingKey;
    ELSE
        INSERT INTO [HTResourceMapper].[ClientSetting] (ClientId, SettingKey, SettingJson)
        VALUES (@ClientId, @SettingKey, @SettingJson);
END
```

Add three `<Build Include>` lines to `HTResourceMapperDb.sqlproj`:

```xml
<Build Include="Tables\ClientSetting.sql" />
<Build Include="Stored Procedures\ClientSetting_Get.sql" />
<Build Include="Stored Procedures\ClientSetting_Upsert.sql" />
```

- [ ] **Step 1:** Create the 3 files + `<Build Include>` lines, and apply to `(localdb)\MSSQLLocalDB\ResourceMapper` via `sqlcmd` (table `IF OBJECT_ID(...) IS NULL`; sprocs `CREATE OR ALTER`).

---

## 2. Shared DTO (`Modules\Common\ResourceMapper.Common.Shared\Settings\`)

New `Settings\` folder; namespace `ResourceMapper.Common.Shared.Settings`.

**File:** `ClientSettingModel.cs`

```csharp
namespace ResourceMapper.Common.Shared.Settings
{
    public class ClientSettingModel
    {
        public string SettingKey { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}
```

- [ ] **Step 2:** Create it.

---

## 3. Server: model, repo, service (`Modules\Common\ResourceMapper.Common.Server\Settings\`)

New `Settings\` folder; namespace `ResourceMapper.Common.Server.Settings` (+ `Interfaces`, `Models`).

**File:** `Models\ClientSettingRow.cs`

```csharp
namespace ResourceMapper.Common.Server.Settings.Models
{
    public class ClientSettingRow
    {
        public string? Value { get; set; }
    }
}
```

**File:** `Interfaces\IClientSettingsRepository.cs`

```csharp
using System.Threading;
using System.Threading.Tasks;

namespace ResourceMapper.Common.Server.Settings.Interfaces
{
    public interface IClientSettingsRepository
    {
        Task<string?> GetAsync(string clientId, string settingKey, CancellationToken cancellationToken);
        Task UpsertAsync(string clientId, string settingKey, string settingJson, CancellationToken cancellationToken);
    }
}
```

**File:** `ClientSettingsSqlRepository.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HT.Microsoft.SqlClient.Extensions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;
using ResourceMapper.Common.Server.Settings.Interfaces;
using ResourceMapper.Common.Server.Settings.Models;

namespace ResourceMapper.Common.Server.Settings
{
    public class ClientSettingsSqlRepository : IClientSettingsRepository
    {
        private readonly IDbConnector _db;

        public ClientSettingsSqlRepository(IDbConnector db)
        {
            _db = db;
        }

        public async Task<string?> GetAsync(string clientId, string settingKey, CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].ClientSetting_Get")
                .AddVarchar("@ClientId", clientId)
                .AddVarchar("@SettingKey", settingKey);

            var rows = await _db.Execute.ExecuteQueryAsync(cmd, dr => new ClientSettingRow
            {
                Value = dr.ReadString("Value")
            }, cancellationToken: cancellationToken);

            return rows.FirstOrDefault()?.Value;
        }

        public async Task UpsertAsync(string clientId, string settingKey, string settingJson, CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].ClientSetting_Upsert")
                .AddVarchar("@ClientId", clientId)
                .AddVarchar("@SettingKey", settingKey)
                .AddNVarchar("@SettingJson", settingJson);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
        }
    }
}
```

**File:** `Interfaces\IClientSettingsService.cs`

```csharp
using System.Threading;
using System.Threading.Tasks;
using HT.Api.Service.Contracts;
using ResourceMapper.Common.Shared.Settings;

namespace ResourceMapper.Common.Server.Settings.Interfaces
{
    public interface IClientSettingsService
    {
        Task<ApiServiceResponse<ClientSettingModel>> GetAsync(string clientId, string settingKey, CancellationToken cancellationToken = default);
        Task<ApiServiceResponse<object>> SetAsync(string clientId, string settingKey, string? value, CancellationToken cancellationToken = default);
    }
}
```

**File:** `ClientSettingsService.cs`

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using HT.Api.Client.Contracts.Models;                 // CallStatusCode
using HT.Api.Service.Contracts;                       // ApiServiceResponse<T>
using HT.Api.Service.Contracts.BuildersOfT;           // ServiceResponseBuilder<T>
using ResourceMapper.Common.Server.Settings.Interfaces;
using ResourceMapper.Common.Shared.Settings;

namespace ResourceMapper.Common.Server.Settings
{
    public class ClientSettingsService : IClientSettingsService
    {
        private readonly IClientSettingsRepository _repo;

        public ClientSettingsService(IClientSettingsRepository repo)
        {
            _repo = repo;
        }

        public async Task<ApiServiceResponse<ClientSettingModel>> GetAsync(string clientId, string settingKey, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<ClientSettingModel>();
            try
            {
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    builder.Validation.AddValidation("clientId", "Client id is required");
                    return builder.BuildResponse();
                }
                if (string.IsNullOrWhiteSpace(settingKey))
                {
                    builder.Validation.AddValidation("settingKey", "Setting key is required");
                    return builder.BuildResponse();
                }

                var value = await _repo.GetAsync(clientId, settingKey, cancellationToken);
                builder.Data.Set(new ClientSettingModel { SettingKey = settingKey, Value = value });
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<object>> SetAsync(string clientId, string settingKey, string? value, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<object>();
            try
            {
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    builder.Validation.AddValidation("clientId", "Client id is required");
                    return builder.BuildResponse();
                }
                if (string.IsNullOrWhiteSpace(settingKey))
                {
                    builder.Validation.AddValidation("settingKey", "Setting key is required");
                    return builder.BuildResponse();
                }

                await _repo.UpsertAsync(clientId, settingKey, value ?? string.Empty, cancellationToken);
                builder.Data.Set(new object());
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }
    }
}
```

- [ ] **Step 3:** Create the model, repo (interface + impl), and service (interface + impl).

---

## 4. Dependency registration

Add to `RegisterCommonDependencies` in `DependencyRegistration.cs` (with the matching `using`s
`ResourceMapper.Common.Server.Settings` / `...Settings.Interfaces`):

```csharp
services.TryAddScoped<IClientSettingsRepository, ClientSettingsSqlRepository>();
services.TryAddScoped<IClientSettingsService, ClientSettingsService>();
```

- [ ] **Step 4:** Register the pair.

---

## 5. Tests (`_Tests\Common\Server\ResourceMapper.Common.Server.Tests\Settings\`)

New folder; namespace `ResourceMapper.Common.Server.Tests.Settings`.

**File:** `ClientSettingsServiceTests.cs`

```csharp
// ReSharper disable InconsistentNaming

using System.Threading;
using System.Threading.Tasks;
using ResourceMapper.Common.Server.Settings;
using ResourceMapper.Common.Server.Settings.Interfaces;

namespace ResourceMapper.Common.Server.Tests.Settings
{
    [Trait("Category", "Unit")]
    [Trait("Category", "ResourceMapper")]
    [Trait("Category", "ResourceMapper/Common")]
    [Trait("Category", "ResourceMapper/Common/Server")]
    [Trait("Category", "ResourceMapper/Common/Server/Settings")]
    [Trait("Category", "ResourceMapper/Common/Server/Settings/ClientSettingsService")]
    public class ClientSettingsServiceTests
    {
        private readonly Mock<IClientSettingsRepository> _repo;
        private readonly ClientSettingsService _sut;

        public ClientSettingsServiceTests()
        {
            _repo = new Mock<IClientSettingsRepository>();
            _sut = new ClientSettingsService(_repo.Object);
        }

        #region GetAsync

        [Fact]
        public async Task GetAsync_BlankClientId_ReturnsValidationAndDoesNotCallRepo()
        {
            var response = await _sut.GetAsync("", "home.gridView", CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a client id is required");
            _repo.Verify(r => r.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never, "because validation short-circuits");
        }

        [Fact]
        public async Task GetAsync_Existing_ReturnsValue()
        {
            _repo.Setup(r => r.GetAsync("c1", "home.gridView", It.IsAny<CancellationToken>()))
                .ReturnsAsync("?q=web");

            var response = await _sut.GetAsync("c1", "home.gridView", CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the read succeeds");
            response.ApiResponse.Data!.Value.Should().Be("?q=web", "because the stored value is returned");
        }

        [Fact]
        public async Task GetAsync_Missing_ReturnsSuccessWithNullValue()
        {
            _repo.Setup(r => r.GetAsync("c1", "home.gridView", It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);

            var response = await _sut.GetAsync("c1", "home.gridView", CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a missing setting is not an error");
            response.ApiResponse.Data!.Value.Should().BeNull("because there is no stored value");
        }

        #endregion

        #region SetAsync

        [Fact]
        public async Task SetAsync_BlankKey_ReturnsValidationAndDoesNotCallRepo()
        {
            var response = await _sut.SetAsync("c1", "", "v", CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a setting key is required");
            _repo.Verify(r => r.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never, "because validation short-circuits");
        }

        [Fact]
        public async Task SetAsync_Valid_UpsertsAndSucceeds()
        {
            var response = await _sut.SetAsync("c1", "home.gridView", "?q=web", CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a valid set succeeds");
            _repo.Verify(r => r.UpsertAsync("c1", "home.gridView", "?q=web", It.IsAny<CancellationToken>()),
                Times.Once, "because the value is persisted via the repo");
        }

        [Fact]
        public async Task SetAsync_NullValue_PersistsEmptyString()
        {
            var response = await _sut.SetAsync("c1", "home.gridView", null, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a null value is coalesced to empty");
            _repo.Verify(r => r.UpsertAsync("c1", "home.gridView", string.Empty, It.IsAny<CancellationToken>()),
                Times.Once, "because null is stored as empty, not passed through as null");
        }

        #endregion
    }
}
```

- [ ] **Step 5:** Write the tests; `dotnet test` the `ClientSettingsServiceTests`.

---

## 6. Rewire `Home.razor` (`UI\ResourceMapper.UI.Web\Components\Pages\Home.razor`)

**Read the file first.** Replace the two `localStorage` uses of the grid-view with the service +
`clientId`. Concretely:

### 6a. Usings + injection + fields

Add usings and an inject:

```razor
@using ResourceMapper.Common.Server.Settings.Interfaces
@using ResourceMapper.UI.Web.Explorer
@inject IClientSettingsService ClientSettings
```

Add a client-id field and rename the storage-key const to a setting key. Find the existing
`ViewStorageKey` const and replace it with:

```csharp
    private const string GridViewSettingKey = "home.gridView";
    private string _clientId = string.Empty;
```
(Update the two references from `ViewStorageKey` to `GridViewSettingKey`.)

### 6b. Restore (in `OnAfterRenderAsync`, first-render block)

**Before** the current restore logic reads the saved value, obtain the client id:

```csharp
        _clientId = await ClientIdentity.GetOrCreateAsync(JS);
```

Replace the localStorage read line
(`saved = await JS.InvokeAsync<string?>("localStorage.getItem", ViewStorageKey);`) with:

```csharp
            try
            {
                var settingResp = await ClientSettings.GetAsync(_clientId, GridViewSettingKey);
                saved = settingResp.IsSuccess() ? settingResp.ApiResponse.Data?.Value : null;
            }
            catch { /* settings unavailable — fall back to the default view */ }
```

### 6c. Persist (`PersistViewAsync`)

Replace the body:

```csharp
    private async Task PersistViewAsync(string query)
    {
        if (string.IsNullOrEmpty(_clientId)) return;
        try { await ClientSettings.SetAsync(_clientId, GridViewSettingKey, query); }
        catch { /* settings unavailable — best-effort persistence */ }
    }
```

(The `JS`/`IJSRuntime` inject stays — it's still used by `ClientIdentity.GetOrCreateAsync` and other
grid interop.)

- [ ] **Step 6:** Apply 6a–6c. Confirm no remaining `localStorage`-based grid-view read/write and that
  `ViewStorageKey` is fully replaced.

---

## UI verification hook (visible slice)

- [ ] **Step 7: Build + run.** `dotnet build` clean; `dotnet run --project UI/ResourceMapper.UI.Web`.
- [ ] **Step 8: Drive the home grid** (`/`):
  1. Apply a filter / search / sort, then navigate away and back (or reload with a bare `/` URL) →
     the view **resumes** (same as before — but now from the server).
  2. Confirm via `sqlcmd` that a `[HTResourceMapper].[ClientSetting]` row exists for your `clientId`
     with `SettingKey = 'home.gridView'` and the query string as `SettingJson`.
  3. Clear `localStorage` (dev-tools) but keep the same `clientId` cookie/entry → the saved view still
     resumes (it's server-side now). (Note: clearing the `rm_client_id` localStorage entry yields a new
     client and a fresh view — accepted.)
- [ ] **Step 9 (optional): Playwright** — set a filter, reload, assert it resumes; assert the row lands
  in `ClientSetting`.

---

## Verification

1. **DB:** apply table + 2 sprocs via `sqlcmd`; exercise: `ClientSetting_Upsert` then `ClientSetting_Get`
   returns the value; upsert again updates it; `ClientSetting_Get` for an unknown key returns 0 rows.
2. `dotnet build` clean; `dotnet test` — the new `ClientSettingsServiceTests` pass, no regression.
3. Manual grid drive per Steps 7–8.

---

## Out of scope

- Migrating *other* localStorage state (there is none of consequence besides the grid view and the
  explorer's own client id).
- A settings UI — this is a transparent persistence swap.

---

## Execution notes

_(Written after execution — record the `sqlcmd` exercise, the `Home.razor` reconciliation (exact
lines replaced), the grid resume-from-server behavior observed, test results, and commit hash(es).
Then mark slice #10 done in the master list — completing the plan — and commit.)_
