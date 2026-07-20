# Slice #5 — Diagram persistence (back-end)

## Context

Fifth slice of the [Resource Explorer build](./00-implementation-plan-list.md). See the
[design spec](./resource-explorer-design-v1.md) (§7) and the master list. **Back-end only — no UI**
(the Save/Open/Share UI that consumes this is slice 7). No dependency on the canvas slices; depends
only on the shared infrastructure.

This slice adds the **server-side diagram store**: a `Diagram` table owned by an anonymous
`clientId`, with a separate public `shareId`, plus the sprocs, repository, and `IDiagramService`
that Save / Save-As / Open-Recent / Get-by-share / Save-a-copy / Delete will call. The diagram
payload (`DiagramJson`) is treated as an **opaque string** here — slice 7 decides its shape.

**Key decisions (this slice):**

- **`clientId` is a plain parameter** to every service method (identity is obtained at the UI layer
  via `localStorage` in slice 6 — design §7.1). The service/repo never read `HttpContext`.
- **Private `DiagramUid` (owner handle) + separate public `ShareId`** per row (design §7.2/§7.3), both
  server-generated GUIDs (`Guid.NewGuid().ToString("N")`). Sharing exposes only the `ShareId`.
- **Sprocs return values via result-sets** (no output params) — consistent with slice 1's reader
  pattern; `Diagram_Upsert` returns the authoritative `Result`/`DiagramUid`/`ShareId` by selecting
  the row back, so update paths return the *real* existing `ShareId`.
- **`SeedResourceUid` is a loose reference** (no FK) — the diagram store is a decoupled, client-owned
  artifact; a deleted seed simply fails to load and is handled gracefully.
- **Open-Recent uses the `ShareId`**: `Diagram_ListForClient` returns each row's `ShareId`, so opening
  an own diagram reuses `Diagram_GetByShareId` (no separate owner-get sproc — matches the master list's
  4-sproc set).
- **Ownership on write:** `Diagram_Upsert`/`Diagram_Delete` match on `(DiagramUid, ClientId)`, so a
  caller can only update/delete their own rows.
- Unit-tested (service vs. mocked repo); sprocs applied to localdb + exercised via `sqlcmd`.

---

## 1. SQL — table (`Database\HTResourceMapperDb\Tables\`)

**File:** `Diagram.sql`

```sql
-- A saved explorer diagram, owned by an anonymous client (ClientId) with a separate public
-- share token (ShareId). DiagramJson is an opaque client-defined payload (nodes + positions +
-- expansion state); the server never inspects it. SeedResourceUid is a loose reference (no FK) —
-- the store is a decoupled client artifact. See docs/plans/explorer/resource-explorer-design-v1.md §7.
CREATE TABLE [HTResourceMapper].[Diagram]
(
    [DiagramId] INT NOT NULL IDENTITY(1,1)
        CONSTRAINT [PK_Diagram_DiagramId] PRIMARY KEY (DiagramId)
    ,[DiagramUid] VARCHAR(40) NOT NULL
        CONSTRAINT [UK_Diagram_DiagramUid] UNIQUE (DiagramUid)
    ,[ShareId] VARCHAR(40) NOT NULL
        CONSTRAINT [UK_Diagram_ShareId] UNIQUE (ShareId)
    ,[ClientId] VARCHAR(64) NOT NULL
    ,[Name] NVARCHAR(200) NOT NULL
    ,[SeedResourceUid] VARCHAR(40) NOT NULL
    ,[DisplayPreset] VARCHAR(20) NOT NULL
        CONSTRAINT [DF_Diagram_DisplayPreset] DEFAULT ('nameType')
    ,[DiagramJson] NVARCHAR(MAX) NOT NULL
    ,[CreatedOn] DateTime2(0) NOT NULL
        CONSTRAINT [DF_Diagram_CreatedOn] DEFAULT (SYSUTCDATETIME())
    ,[UpdatedOn] DateTime2(0) NULL
)
GO
-- Open-Recent lists a client's diagrams most-recent-first.
CREATE NONCLUSTERED INDEX [IX_Diagram_ClientId]
    ON [HTResourceMapper].[Diagram] ([ClientId]);
```

## 2. SQL — sprocs (`Database\HTResourceMapperDb\Stored Procedures\`)

**File:** `Diagram_Upsert.sql`

```sql
-- Create or update a diagram. Matches on (DiagramUid, ClientId) so a client can only update its
-- own rows; @ShareId is used only on insert. Returns the authoritative Result/DiagramUid/ShareId
-- by selecting the row back (so updates return the real existing ShareId).
CREATE PROCEDURE [HTResourceMapper].[Diagram_Upsert]
    @DiagramUid VARCHAR(40),
    @ShareId VARCHAR(40),
    @ClientId VARCHAR(64),
    @Name NVARCHAR(200),
    @SeedResourceUid VARCHAR(40),
    @DisplayPreset VARCHAR(20),
    @DiagramJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Result VARCHAR(10);

    IF EXISTS (SELECT 1 FROM [HTResourceMapper].[Diagram]
               WHERE DiagramUid = @DiagramUid AND ClientId = @ClientId)
    BEGIN
        UPDATE [HTResourceMapper].[Diagram]
        SET Name = @Name, SeedResourceUid = @SeedResourceUid, DisplayPreset = @DisplayPreset,
            DiagramJson = @DiagramJson, UpdatedOn = SYSUTCDATETIME()
        WHERE DiagramUid = @DiagramUid AND ClientId = @ClientId;
        SET @Result = 'updated';
    END
    ELSE
    BEGIN
        INSERT INTO [HTResourceMapper].[Diagram]
            (DiagramUid, ShareId, ClientId, Name, SeedResourceUid, DisplayPreset, DiagramJson)
        VALUES (@DiagramUid, @ShareId, @ClientId, @Name, @SeedResourceUid, @DisplayPreset, @DiagramJson);
        SET @Result = 'created';
    END

    SELECT @Result AS Result, DiagramUid, ShareId
    FROM [HTResourceMapper].[Diagram]
    WHERE DiagramUid = @DiagramUid AND ClientId = @ClientId;
END
```

**File:** `Diagram_GetByShareId.sql`

```sql
-- Resolve a diagram by its public share token (read-only share; also used by the owner to open
-- a recent diagram, since the list returns each row's ShareId).
CREATE PROCEDURE [HTResourceMapper].[Diagram_GetByShareId]
    @ShareId VARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        DiagramUid, ShareId, ClientId, Name, SeedResourceUid, DisplayPreset, DiagramJson,
        CONVERT(VARCHAR(33), COALESCE(UpdatedOn, CreatedOn), 126) AS UpdatedOnUtc
    FROM [HTResourceMapper].[Diagram]
    WHERE ShareId = @ShareId;
END
```

**File:** `Diagram_ListForClient.sql`

```sql
-- A client's diagrams for Open-Recent (metadata only; no DiagramJson). Includes ShareId so the
-- caller can open a chosen diagram via Diagram_GetByShareId.
CREATE PROCEDURE [HTResourceMapper].[Diagram_ListForClient]
    @ClientId VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        DiagramUid, ShareId, Name, SeedResourceUid,
        CONVERT(VARCHAR(33), COALESCE(UpdatedOn, CreatedOn), 126) AS UpdatedOnUtc
    FROM [HTResourceMapper].[Diagram]
    WHERE ClientId = @ClientId
    ORDER BY COALESCE(UpdatedOn, CreatedOn) DESC;
END
```

**File:** `Diagram_Delete.sql`

```sql
-- Delete a client's own diagram. Returns 1 if a row was deleted, else 0.
CREATE PROCEDURE [HTResourceMapper].[Diagram_Delete]
    @ClientId VARCHAR(64),
    @DiagramUid VARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM [HTResourceMapper].[Diagram]
    WHERE ClientId = @ClientId AND DiagramUid = @DiagramUid;

    SELECT CAST(CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END AS INT) AS Deleted;
END
```

**Register all five files** — add these lines (alphabetically) to the `<Build>` `<ItemGroup>` in
`HTResourceMapperDb.sqlproj`:

```xml
<Build Include="Tables\Diagram.sql" />
<Build Include="Stored Procedures\Diagram_Upsert.sql" />
<Build Include="Stored Procedures\Diagram_GetByShareId.sql" />
<Build Include="Stored Procedures\Diagram_ListForClient.sql" />
<Build Include="Stored Procedures\Diagram_Delete.sql" />
```

- [ ] **Step 1:** Create the table + 4 sproc files and add the 5 `<Build Include>` lines.
- [ ] **Step 2:** Apply all 5 to `(localdb)\MSSQLLocalDB\ResourceMapper` via `sqlcmd` — the table with a plain `CREATE TABLE` (guard with `IF OBJECT_ID(...) IS NULL`), the sprocs as `CREATE OR ALTER PROCEDURE`. (`sqlcmd`, not the SQL MCP.)

---

## 3. Server models (`Modules\Common\ResourceMapper.Common.Server\Explorer\Models\`)

New `Explorer\Models\` folder; namespace `ResourceMapper.Common.Server.Explorer.Models`.

**File:** `DiagramRow.cs`

```csharp
namespace ResourceMapper.Common.Server.Explorer.Models
{
    /// <summary>Full diagram row (Diagram_GetByShareId).</summary>
    public class DiagramRow
    {
        public string DiagramUid { get; set; } = string.Empty;
        public string ShareId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SeedResourceUid { get; set; } = string.Empty;
        public string DisplayPreset { get; set; } = string.Empty;
        public string DiagramJson { get; set; } = string.Empty;
        public string? UpdatedOnUtc { get; set; }
    }
}
```

**File:** `DiagramListRow.cs`

```csharp
namespace ResourceMapper.Common.Server.Explorer.Models
{
    /// <summary>Metadata row for Open-Recent (Diagram_ListForClient); no DiagramJson.</summary>
    public class DiagramListRow
    {
        public string DiagramUid { get; set; } = string.Empty;
        public string ShareId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SeedResourceUid { get; set; } = string.Empty;
        public string? UpdatedOnUtc { get; set; }
    }
}
```

**File:** `DiagramUpsertResult.cs`

```csharp
namespace ResourceMapper.Common.Server.Explorer.Models
{
    /// <summary>Result row from Diagram_Upsert.</summary>
    public class DiagramUpsertResult
    {
        public string Result { get; set; } = string.Empty;   // 'created' | 'updated'
        public string DiagramUid { get; set; } = string.Empty;
        public string ShareId { get; set; } = string.Empty;
    }
}
```

- [ ] **Step 3:** Create the three server models.

---

## 4. Shared DTOs (`Modules\Common\ResourceMapper.Common.Shared\Explorer\Diagrams\`)

New `Explorer\Diagrams\` folder; namespace `ResourceMapper.Common.Shared.Explorer.Diagrams`.

**File:** `DiagramModel.cs`

```csharp
namespace ResourceMapper.Common.Shared.Explorer.Diagrams
{
    /// <summary>A full saved diagram returned to the client (by share token).</summary>
    public class DiagramModel
    {
        public string DiagramUid { get; set; } = string.Empty;
        public string ShareId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SeedResourceUid { get; set; } = string.Empty;
        public string DisplayPreset { get; set; } = string.Empty;
        public string DiagramJson { get; set; } = string.Empty;
        public string? UpdatedOnUtc { get; set; }
    }
}
```

**File:** `DiagramListItem.cs`

```csharp
namespace ResourceMapper.Common.Shared.Explorer.Diagrams
{
    /// <summary>Open-Recent entry (metadata only).</summary>
    public class DiagramListItem
    {
        public string DiagramUid { get; set; } = string.Empty;
        public string ShareId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SeedResourceUid { get; set; } = string.Empty;
        public string? UpdatedOnUtc { get; set; }
    }
}
```

**File:** `Contracts\SaveDiagramRequest.cs`

```csharp
namespace ResourceMapper.Common.Shared.Explorer.Diagrams.Contracts
{
    /// <summary>Save (create when DiagramUid is null/empty, else update) a diagram.</summary>
    public class SaveDiagramRequest
    {
        public string? DiagramUid { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SeedResourceUid { get; set; } = string.Empty;
        public string DisplayPreset { get; set; } = "nameType";
        public string DiagramJson { get; set; } = string.Empty;
    }
}
```

**File:** `Contracts\SaveDiagramResponse.cs`

```csharp
namespace ResourceMapper.Common.Shared.Explorer.Diagrams.Contracts
{
    public class SaveDiagramResponse
    {
        public string DiagramUid { get; set; } = string.Empty;
        public string ShareId { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;   // 'created' | 'updated'
    }
}
```

- [ ] **Step 4:** Create the two DTOs + two contracts.

---

## 5. Repository (`Modules\Common\ResourceMapper.Common.Server\Explorer\`)

**File:** `Interfaces\IDiagramRepository.cs`

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ResourceMapper.Common.Server.Explorer.Models;

namespace ResourceMapper.Common.Server.Explorer.Interfaces
{
    public interface IDiagramRepository
    {
        Task<DiagramUpsertResult> UpsertAsync(string diagramUid, string shareId, string clientId,
            string name, string seedResourceUid, string displayPreset, string diagramJson,
            CancellationToken cancellationToken);

        Task<DiagramRow?> GetByShareIdAsync(string shareId, CancellationToken cancellationToken);

        Task<List<DiagramListRow>> ListForClientAsync(string clientId, CancellationToken cancellationToken);

        Task<bool> DeleteAsync(string clientId, string diagramUid, CancellationToken cancellationToken);
    }
}
```

**File:** `DiagramSqlRepository.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HT.Microsoft.SqlClient.Extensions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;
using ResourceMapper.Common.Server.Explorer.Interfaces;
using ResourceMapper.Common.Server.Explorer.Models;

namespace ResourceMapper.Common.Server.Explorer
{
    public class DiagramSqlRepository : IDiagramRepository
    {
        private readonly IDbConnector _db;

        public DiagramSqlRepository(IDbConnector db)
        {
            _db = db;
        }

        public async Task<DiagramUpsertResult> UpsertAsync(string diagramUid, string shareId, string clientId,
            string name, string seedResourceUid, string displayPreset, string diagramJson,
            CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].Diagram_Upsert")
                .AddVarchar("@DiagramUid", diagramUid)
                .AddVarchar("@ShareId", shareId)
                .AddVarchar("@ClientId", clientId)
                .AddNVarchar("@Name", name)
                .AddVarchar("@SeedResourceUid", seedResourceUid)
                .AddVarchar("@DisplayPreset", displayPreset)
                .AddNVarchar("@DiagramJson", diagramJson);

            var rows = await _db.Execute.ExecuteQueryAsync(cmd, dr => new DiagramUpsertResult
            {
                Result = dr.ReadString("Result"),
                DiagramUid = dr.ReadString("DiagramUid"),
                ShareId = dr.ReadString("ShareId")
            }, cancellationToken: cancellationToken);

            return rows.First();
        }

        public async Task<DiagramRow?> GetByShareIdAsync(string shareId, CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].Diagram_GetByShareId")
                .AddVarchar("@ShareId", shareId);

            var rows = await _db.Execute.ExecuteQueryAsync(cmd, dr => new DiagramRow
            {
                DiagramUid = dr.ReadString("DiagramUid"),
                ShareId = dr.ReadString("ShareId"),
                ClientId = dr.ReadString("ClientId"),
                Name = dr.ReadString("Name"),
                SeedResourceUid = dr.ReadString("SeedResourceUid"),
                DisplayPreset = dr.ReadString("DisplayPreset"),
                DiagramJson = dr.ReadString("DiagramJson"),
                UpdatedOnUtc = dr.ReadString("UpdatedOnUtc")
            }, cancellationToken: cancellationToken);

            return rows.FirstOrDefault();
        }

        public async Task<List<DiagramListRow>> ListForClientAsync(string clientId, CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].Diagram_ListForClient")
                .AddVarchar("@ClientId", clientId);

            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new DiagramListRow
            {
                DiagramUid = dr.ReadString("DiagramUid"),
                ShareId = dr.ReadString("ShareId"),
                Name = dr.ReadString("Name"),
                SeedResourceUid = dr.ReadString("SeedResourceUid"),
                UpdatedOnUtc = dr.ReadString("UpdatedOnUtc")
            }, cancellationToken: cancellationToken);
        }

        public async Task<bool> DeleteAsync(string clientId, string diagramUid, CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].Diagram_Delete")
                .AddVarchar("@ClientId", clientId)
                .AddVarchar("@DiagramUid", diagramUid);

            var rows = await _db.Execute.ExecuteQueryAsync(cmd, dr => dr.ReadInt("Deleted"),
                cancellationToken: cancellationToken);

            return rows.FirstOrDefault() > 0;
        }
    }
}
```

- [ ] **Step 5:** Create `IDiagramRepository.cs` and `DiagramSqlRepository.cs`.

---

## 6. Service (`Modules\Common\ResourceMapper.Common.Server\Explorer\`)

**File:** `Interfaces\IDiagramService.cs`

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HT.Api.Service.Contracts;
using ResourceMapper.Common.Shared.Explorer.Diagrams;
using ResourceMapper.Common.Shared.Explorer.Diagrams.Contracts;

namespace ResourceMapper.Common.Server.Explorer.Interfaces
{
    public interface IDiagramService
    {
        Task<ApiServiceResponse<SaveDiagramResponse>> SaveAsync(string clientId, SaveDiagramRequest request, CancellationToken cancellationToken = default);
        Task<ApiServiceResponse<List<DiagramListItem>>> ListForClientAsync(string clientId, CancellationToken cancellationToken = default);
        Task<ApiServiceResponse<DiagramModel>> GetByShareIdAsync(string shareId, CancellationToken cancellationToken = default);
        Task<ApiServiceResponse<SaveDiagramResponse>> SaveCopyAsync(string clientId, string shareId, string? newName, CancellationToken cancellationToken = default);
        Task<ApiServiceResponse<bool>> DeleteAsync(string clientId, string diagramUid, CancellationToken cancellationToken = default);
    }
}
```

**File:** `DiagramService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HT.Api.Client.Contracts.Models;                 // CallStatusCode
using HT.Api.Service.Contracts;                       // ApiServiceResponse<T>
using HT.Api.Service.Contracts.BuildersOfT;           // ServiceResponseBuilder<T>
using ResourceMapper.Common.Server.Explorer.Interfaces;
using ResourceMapper.Common.Shared.Explorer.Diagrams;
using ResourceMapper.Common.Shared.Explorer.Diagrams.Contracts;

namespace ResourceMapper.Common.Server.Explorer
{
    public class DiagramService : IDiagramService
    {
        private readonly IDiagramRepository _repo;

        public DiagramService(IDiagramRepository repo)
        {
            _repo = repo;
        }

        public async Task<ApiServiceResponse<SaveDiagramResponse>> SaveAsync(string clientId, SaveDiagramRequest request, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<SaveDiagramResponse>();
            try
            {
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    builder.Validation.AddValidation("clientId", "Client id is required");
                    return builder.BuildResponse();
                }
                if (request is null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return builder.BuildResponse();
                }

                var invalid = false;
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    builder.Validation.AddValidation("request.Name", "Name is required");
                    invalid = true;
                }
                if (string.IsNullOrWhiteSpace(request.SeedResourceUid))
                {
                    builder.Validation.AddValidation("request.SeedResourceUid", "Seed resource is required");
                    invalid = true;
                }
                if (invalid) return builder.BuildResponse();

                var isNew = string.IsNullOrWhiteSpace(request.DiagramUid);
                var diagramUid = isNew ? NewId() : request.DiagramUid!;
                var shareCandidate = NewId();   // used only if inserting; Upsert returns the real one

                var result = await _repo.UpsertAsync(diagramUid, shareCandidate, clientId,
                    request.Name, request.SeedResourceUid,
                    string.IsNullOrWhiteSpace(request.DisplayPreset) ? "nameType" : request.DisplayPreset,
                    request.DiagramJson ?? string.Empty, cancellationToken);

                builder.Data.Set(new SaveDiagramResponse
                {
                    DiagramUid = result.DiagramUid,
                    ShareId = result.ShareId,
                    Result = result.Result
                });
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<List<DiagramListItem>>> ListForClientAsync(string clientId, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<List<DiagramListItem>>();
            try
            {
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    builder.Validation.AddValidation("clientId", "Client id is required");
                    return builder.BuildResponse();
                }

                var rows = await _repo.ListForClientAsync(clientId, cancellationToken);
                builder.Data.Set(rows.Select(r => new DiagramListItem
                {
                    DiagramUid = r.DiagramUid,
                    ShareId = r.ShareId,
                    Name = r.Name,
                    SeedResourceUid = r.SeedResourceUid,
                    UpdatedOnUtc = r.UpdatedOnUtc
                }).ToList());
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<DiagramModel>> GetByShareIdAsync(string shareId, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<DiagramModel>();
            try
            {
                if (string.IsNullOrWhiteSpace(shareId))
                {
                    builder.Validation.AddValidation("shareId", "Share id is required");
                    return builder.BuildResponse();
                }

                var row = await _repo.GetByShareIdAsync(shareId, cancellationToken);
                if (row is null)
                {
                    builder.Errors.AddError(CallStatusCode.NotFound, "Diagram Not Found",
                        $"No diagram for share id '{shareId}'.", "shareId");
                    return builder.BuildResponse();
                }

                builder.Data.Set(ToModel(row));
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<SaveDiagramResponse>> SaveCopyAsync(string clientId, string shareId, string? newName, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<SaveDiagramResponse>();
            try
            {
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    builder.Validation.AddValidation("clientId", "Client id is required");
                    return builder.BuildResponse();
                }
                if (string.IsNullOrWhiteSpace(shareId))
                {
                    builder.Validation.AddValidation("shareId", "Share id is required");
                    return builder.BuildResponse();
                }

                var src = await _repo.GetByShareIdAsync(shareId, cancellationToken);
                if (src is null)
                {
                    builder.Errors.AddError(CallStatusCode.NotFound, "Diagram Not Found",
                        $"No diagram for share id '{shareId}'.", "shareId");
                    return builder.BuildResponse();
                }

                var name = string.IsNullOrWhiteSpace(newName) ? src.Name + " (copy)" : newName!;
                var result = await _repo.UpsertAsync(NewId(), NewId(), clientId,
                    name, src.SeedResourceUid, src.DisplayPreset, src.DiagramJson, cancellationToken);

                builder.Data.Set(new SaveDiagramResponse
                {
                    DiagramUid = result.DiagramUid,
                    ShareId = result.ShareId,
                    Result = result.Result
                });
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<bool>> DeleteAsync(string clientId, string diagramUid, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<bool>();
            try
            {
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    builder.Validation.AddValidation("clientId", "Client id is required");
                    return builder.BuildResponse();
                }
                if (string.IsNullOrWhiteSpace(diagramUid))
                {
                    builder.Validation.AddValidation("diagramUid", "Diagram id is required");
                    return builder.BuildResponse();
                }

                var deleted = await _repo.DeleteAsync(clientId, diagramUid, cancellationToken);
                if (!deleted)
                {
                    builder.Errors.AddError(CallStatusCode.NotFound, "Diagram Not Found",
                        $"No diagram '{diagramUid}' owned by this client.", "diagramUid");
                    return builder.BuildResponse();
                }

                builder.Data.Set(true);
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        private static string NewId() => Guid.NewGuid().ToString("N");

        private static DiagramModel ToModel(Models.DiagramRow row) => new()
        {
            DiagramUid = row.DiagramUid,
            ShareId = row.ShareId,
            Name = row.Name,
            SeedResourceUid = row.SeedResourceUid,
            DisplayPreset = row.DisplayPreset,
            DiagramJson = row.DiagramJson,
            UpdatedOnUtc = row.UpdatedOnUtc
        };
    }
}
```

> `builder.Data.Set(...)`, `builder.Validation.AddValidation(...)`, `builder.Errors.AddError(CallStatusCode.X, title, detail, prop)`, `builder.BuildResponse()`, and `response.IsSuccess()` are the exact APIs used by the existing `ResourceService`/`ExplorerService` — mirror them.

- [ ] **Step 6:** Create `IDiagramService.cs` and `DiagramService.cs`.

---

## 7. Dependency registration

Add to `RegisterCommonDependencies` in `DependencyRegistration.cs` (beside the explorer pair from
slice 1):

```csharp
services.TryAddScoped<IDiagramRepository, DiagramSqlRepository>();
services.TryAddScoped<IDiagramService, DiagramService>();
```

(The `ResourceMapper.Common.Server.Explorer` / `.Explorer.Interfaces` usings are already present from
slice 1.)

- [ ] **Step 7:** Register the repo + service.

---

## 8. Tests (`_Tests\Common\Server\ResourceMapper.Common.Server.Tests\Explorer\`)

Add `DiagramServiceTests.cs` (namespace `ResourceMapper.Common.Server.Tests.Explorer`), service vs.
mocked `IDiagramRepository`.

```csharp
// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ResourceMapper.Common.Server.Explorer;
using ResourceMapper.Common.Server.Explorer.Interfaces;
using ResourceMapper.Common.Server.Explorer.Models;
using ResourceMapper.Common.Shared.Explorer.Diagrams.Contracts;

namespace ResourceMapper.Common.Server.Tests.Explorer
{
    [Trait("Category", "Unit")]
    [Trait("Category", "ResourceMapper")]
    [Trait("Category", "ResourceMapper/Common")]
    [Trait("Category", "ResourceMapper/Common/Server")]
    [Trait("Category", "ResourceMapper/Common/Server/Explorer")]
    [Trait("Category", "ResourceMapper/Common/Server/Explorer/DiagramService")]
    public class DiagramServiceTests
    {
        private readonly Mock<IDiagramRepository> _repo;
        private readonly DiagramService _sut;

        public DiagramServiceTests()
        {
            _repo = new Mock<IDiagramRepository>();
            _sut = new DiagramService(_repo.Object);
        }

        #region SaveAsync

        [Fact]
        public async Task SaveAsync_BlankClientId_ReturnsValidationAndDoesNotCallRepo()
        {
            var response = await _sut.SaveAsync("", Req("Diagram A", "seed1"), CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a client id is required");
            _repo.Verify(r => r.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never, "because validation short-circuits");
        }

        [Fact]
        public async Task SaveAsync_MissingNameOrSeed_ReturnsValidation()
        {
            var response = await _sut.SaveAsync("client1", Req("", ""), CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because name and seed are required");
        }

        [Fact]
        public async Task SaveAsync_NoDiagramUid_GeneratesIdsAndCreates()
        {
            _repo.Setup(r => r.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), "client1",
                    "Diagram A", "seed1", "nameType", "{}", It.IsAny<CancellationToken>()))
                .ReturnsAsync((string uid, string share, string _, string __, string ___, string ____, string _____, CancellationToken _______) =>
                    new DiagramUpsertResult { Result = "created", DiagramUid = uid, ShareId = share });

            var response = await _sut.SaveAsync("client1",
                new SaveDiagramRequest { DiagramUid = null, Name = "Diagram A", SeedResourceUid = "seed1", DisplayPreset = "nameType", DiagramJson = "{}" },
                CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a valid create was requested");
            var data = response.ApiResponse.Data!;
            data.Result.Should().Be("created", "because no DiagramUid means a new diagram");
            data.DiagramUid.Should().NotBeNullOrWhiteSpace("because the service generates a new uid");
            data.ShareId.Should().NotBeNullOrWhiteSpace("because the service generates a new share id");
        }

        [Fact]
        public async Task SaveAsync_WithDiagramUid_UpdatesWithThatUid()
        {
            string? capturedUid = null;
            _repo.Setup(r => r.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), "client1",
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback((string uid, string share, string _, string __, string ___, string ____, string _____, CancellationToken ______) => capturedUid = uid)
                .ReturnsAsync(new DiagramUpsertResult { Result = "updated", DiagramUid = "existing-uid", ShareId = "existing-share" });

            var response = await _sut.SaveAsync("client1",
                new SaveDiagramRequest { DiagramUid = "existing-uid", Name = "Diagram A", SeedResourceUid = "seed1" },
                CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a valid update was requested");
            capturedUid.Should().Be("existing-uid", "because an existing DiagramUid is passed straight through");
            response.ApiResponse.Data!.ShareId.Should().Be("existing-share", "because Upsert returns the authoritative existing share id");
        }

        #endregion

        #region GetByShareIdAsync / SaveCopyAsync

        [Fact]
        public async Task GetByShareIdAsync_Missing_ReturnsNotFound()
        {
            _repo.Setup(r => r.GetByShareIdAsync("nope", It.IsAny<CancellationToken>())).ReturnsAsync((DiagramRow?)null);

            var response = await _sut.GetByShareIdAsync("nope", CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because an unknown share id is NotFound");
        }

        [Fact]
        public async Task SaveCopyAsync_ClonesSourceUnderCallerWithNewIds()
        {
            _repo.Setup(r => r.GetByShareIdAsync("src-share", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DiagramRow { DiagramUid = "src-uid", ShareId = "src-share", ClientId = "owner", Name = "Orig", SeedResourceUid = "seed9", DisplayPreset = "detailed", DiagramJson = "{\"n\":1}" });
            _repo.Setup(r => r.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), "me",
                    It.IsAny<string>(), "seed9", "detailed", "{\"n\":1}", It.IsAny<CancellationToken>()))
                .ReturnsAsync((string uid, string share, string _, string __, string ___, string ____, string _____, CancellationToken ______) =>
                    new DiagramUpsertResult { Result = "created", DiagramUid = uid, ShareId = share });

            var response = await _sut.SaveCopyAsync("me", "src-share", null, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the source exists and is cloned");
            response.ApiResponse.Data!.Result.Should().Be("created", "because a copy is a new row");
            _repo.Verify(r => r.UpsertAsync(It.Is<string>(u => u != "src-uid"), It.IsAny<string>(), "me",
                "Orig (copy)", "seed9", "detailed", "{\"n\":1}", It.IsAny<CancellationToken>()),
                Times.Once, "because the clone reuses the source payload under the caller with a new uid and defaulted name");
        }

        [Fact]
        public async Task SaveCopyAsync_MissingSource_ReturnsNotFound()
        {
            _repo.Setup(r => r.GetByShareIdAsync("gone", It.IsAny<CancellationToken>())).ReturnsAsync((DiagramRow?)null);

            var response = await _sut.SaveCopyAsync("me", "gone", null, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because you can't copy a diagram that isn't there");
        }

        #endregion

        #region DeleteAsync

        [Fact]
        public async Task DeleteAsync_RepoReportsDeleted_ReturnsSuccess()
        {
            _repo.Setup(r => r.DeleteAsync("client1", "d1", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var response = await _sut.DeleteAsync("client1", "d1", CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the owned diagram was deleted");
        }

        [Fact]
        public async Task DeleteAsync_NothingDeleted_ReturnsNotFound()
        {
            _repo.Setup(r => r.DeleteAsync("client1", "d1", It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var response = await _sut.DeleteAsync("client1", "d1", CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because no owned row matched");
        }

        #endregion

        #region helpers

        private static SaveDiagramRequest Req(string name, string seed) => new()
        {
            DiagramUid = null, Name = name, SeedResourceUid = seed, DisplayPreset = "nameType", DiagramJson = "{}"
        };

        #endregion
    }
}
```

> If the compiler disagrees on `response.IsSuccess()` / `response.ApiResponse.Data` / builder members,
> open `ResourceServiceTests.cs` / `ResourceService.cs` and match exactly — do not invent members.

- [ ] **Step 8:** Write `DiagramServiceTests.cs`.
- [ ] **Step 9:** Run `dotnet test --filter "FullyQualifiedName~ResourceMapper.Common.Server.Tests.Explorer.DiagramServiceTests"` → all pass; then `dotnet build` + full `dotnet test` (no regressions).

---

## Verification

1. **DB:** apply the table + 4 sprocs to `(localdb)\MSSQLLocalDB\ResourceMapper` via `sqlcmd`.
2. **Sproc exercise via `sqlcmd`:**
   - `Diagram_Upsert` with a fresh `@DiagramUid` → returns `Result='created'` + the ids; run it again with the **same** uid/clientId and changed `@Name` → `Result='updated'`, **same ShareId** returned, row updated.
   - `Diagram_ListForClient` for that client → the row (metadata, most-recent first).
   - `Diagram_GetByShareId` with the returned ShareId → the full row incl. `DiagramJson`.
   - `Diagram_Delete` with a wrong clientId → `Deleted=0`; with the right one → `Deleted=1`.
   - Clean up any rows you insert (report what you added).
3. **Build + unit tests:** `dotnet build`, then `dotnet test` → all green (the 2 pre-existing `HT.Api.Service.Contracts.Tests` failures remain, unrelated).

---

## Out of scope (later slices)

- **Any UI** (toolbar, Save/Save-As/Open-Recent/Delete dialogs, share link, "Save a copy to mine" button, canvas ⇄ `DiagramJson` serialization) → **slice 7**.
- **Obtaining the `clientId`** (localStorage helper) → **slice 6**. This slice only *accepts* it as a parameter.
- **`DiagramJson` shape / schema** — opaque here; defined by slice 7.
- **Full VS/MSBuild DACPAC publish** — the `.sql` are added to the project as declarative source; a full publish reconciles later.

---

## Execution notes

- **Reconciliation:** the brief's repo code (readers/param helpers, `_db.RO`/`_db.RW.SprocCommand`,
  `ExecuteQueryAsync`) matched `ResourceSqlRepository.cs`/`ExplorerSqlRepository.cs` verbatim — no
  changes needed. One required deviation: `ApiServiceResponse<TApiPayload>` is constrained
  `where TApiPayload : class, new()`, so the brief's `ApiServiceResponse<bool>` for `DeleteAsync`
  did not compile (CS0452). Fixed by following the existing `IResourceService.DeleteResourceAsync`
  pattern: `DeleteAsync` returns `ApiServiceResponse<object>`, with `builder.Data.Set(new object())`
  on success. No test changes were needed (the delete tests only assert `IsSuccess()`).
- **Build:** `dotnet build` clean across all C# projects; only the expected `MSB4278` on
  `HTResourceMapperDb.sqlproj` (old-style SSDT, not built via `dotnet build`).
- **Tests:** targeted filter → 9/9 new `DiagramServiceTests` pass. Full `dotnet test` → no
  regressions; the only failures are the 2 pre-existing, out-of-scope
  `HT.Api.Service.Contracts.Tests.BuildersOfTTests` failures.
- **DB apply + sqlcmd exercise** against `(localdb)\MSSQLLocalDB\ResourceMapper`:
  - Table applied via guarded `IF OBJECT_ID(...) IS NULL` `CREATE TABLE` + index; sprocs applied as
    `CREATE OR ALTER PROCEDURE`. All 5 objects confirmed present in `sys.objects`.
  - `Diagram_Upsert` create (`DiagramUid='test-diag-uid-001'`) → `Result='created'` +
    `ShareId='test-share-id-001'`.
  - `Diagram_Upsert` again, same `DiagramUid`/`ClientId`, changed `Name`/`DisplayPreset`/`DiagramJson`,
    and a deliberately **different bogus** `@ShareId` passed in → `Result='updated'`, and the
    **same** `ShareId='test-share-id-001'` returned (the sproc correctly ignored the bogus input
    ShareId on update and selected the authoritative row back).
  - `Diagram_ListForClient('test-client-001')` → the renamed row (metadata only, no `DiagramJson`).
  - `Diagram_GetByShareId('test-share-id-001')` → full row incl. `DiagramJson`.
  - `Diagram_Delete` with wrong `ClientId` → `Deleted=0` (row confirmed still present); with the
    correct `ClientId` → `Deleted=1` (row confirmed gone). This same call served as cleanup — the
    table was confirmed empty (`COUNT(*) = 0`) afterward, no separate cleanup needed.
- Full transcript of every command + output is in the task's scratch report
  (`task-5-report.md`), including the connectivity check and object-listing query.

Commit: see repository log for slice #5's commit hash on `home-page`.

Slice #5 **Done** ✓ / slice #6 **Next**.
