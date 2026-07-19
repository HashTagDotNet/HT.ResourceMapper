# Slice #1 — Explorer neighbor read

## Context

First slice of the [Resource Explorer build](./00-implementation-plan-list.md); implements the
bottom layer the canvas stands on. See the [design spec](./resource-explorer-design-v1.md)
(§4.2, §5.3) and the master list for the full slice sequence.

This slice makes one thing real: **given a `ResourceUid`, return that resource's display node
plus its one-hop `DependsOn` / `DependentOn` neighbors, each carrying the neighbor's `Domain`
and `PrimaryUrl`.** That is the single server call the canvas issues to seed a diagram and to
expand any node. Nothing else in the explorer works until this exists.

It is a pure read path — new sproc, repo, service, and shared DTOs — with **no UI** and **no
writes**. It deliberately does **not** touch the editor's `ResourceRelationship_GetForResource`
sproc or its models (smaller blast radius; the editor path stays exactly as-is).

**Key decisions (this slice):**

- **New sproc, not an extension of the editor's.** `Resource_GetForExplorer` takes a
  **`@ResourceUid`** (the canvas never handles internal int ids) and returns a **single
  UNION-ALL result set** with a `Direction` discriminator (`'Self'` = the center node,
  `'DependsOn'` / `'DependentOn'` = neighbors) — matching the existing single-result reader
  pattern (`ExecuteQueryAsync<T>`), avoiding any multi-result-set API.
- **`PrimaryUrl` is sourced by joining the resource's `PrimaryTagDefinitionId` → its
  `ResourceTag.TagValue`** (the same "primary Link tag" mechanism `ProjectResourceDetailAsync`
  derives in C# today), applied per node in SQL.
- **Own repo + service** (`IExplorerRepository` / `IExplorerService`) under a new `Explorer/`
  folder, registered alongside the resource pair in `RegisterCommonDependencies`.
- **Unknown uid → `NotFound`** (no `'Self'` row ⇒ the service returns a NotFound response).

---

## SQL — new sproc (`Database\HTResourceMapperDb\Stored Procedures\`)

**File:** `Resource_GetForExplorer.sql`

```sql
-- Explorer canvas read: the center resource (@ResourceUid) plus its one-hop neighbours, in a
-- single result set discriminated by Direction: 'Self' = the resource itself; 'DependsOn' = an
-- out-edge (what it depends on); 'DependentOn' = an in-edge (what depends on it). Each row also
-- carries the node's Domain (IsDomainTag tag value) and PrimaryUrl (the resource's primary Link
-- tag value, via PrimaryTagDefinitionId) so the canvas can render node content + the external
-- link without extra calls. Uid-keyed because the canvas never handles internal int ids.
-- Unknown uid returns an empty set (no 'Self' row) -> the service treats it as NotFound.
CREATE PROCEDURE [HTResourceMapper].[Resource_GetForExplorer]
    @ResourceUid VARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ResourceId INT =
        (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceUid = @ResourceUid);

    IF @ResourceId IS NULL
        RETURN;

    DECLARE @DomainTagDefId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE IsDomainTag = 1);

    -- Center node
    SELECT
        'Self' AS Direction,
        r.ResourceUid,
        r.ResourceKey,
        r.ResourceName,
        rt.TypeName    AS ResourceType,
        dt.TagValue    AS Domain,
        purl.TagValue  AS PrimaryUrl
    FROM [HTResourceMapper].[Resource] r
    INNER JOIN [HTResourceMapper].[ResourceType] rt ON rt.ResourceTypeId = r.ResourceTypeId
    LEFT JOIN [HTResourceMapper].[ResourceTag] dt
        ON dt.ResourceId = r.ResourceId AND dt.TagDefinitionId = @DomainTagDefId
    LEFT JOIN [HTResourceMapper].[ResourceTag] purl
        ON purl.ResourceId = r.ResourceId AND purl.TagDefinitionId = r.PrimaryTagDefinitionId
    WHERE r.ResourceId = @ResourceId

    UNION ALL

    -- Out-edges: what this resource depends on
    SELECT
        'DependsOn' AS Direction,
        other.ResourceUid,
        other.ResourceKey,
        other.ResourceName,
        rt.TypeName    AS ResourceType,
        dt.TagValue    AS Domain,
        purl.TagValue  AS PrimaryUrl
    FROM [HTResourceMapper].[ResourceRelationship] rel
    INNER JOIN [HTResourceMapper].[Resource] other ON other.ResourceId = rel.ToResourceId
    INNER JOIN [HTResourceMapper].[ResourceType] rt ON rt.ResourceTypeId = other.ResourceTypeId
    LEFT JOIN [HTResourceMapper].[ResourceTag] dt
        ON dt.ResourceId = other.ResourceId AND dt.TagDefinitionId = @DomainTagDefId
    LEFT JOIN [HTResourceMapper].[ResourceTag] purl
        ON purl.ResourceId = other.ResourceId AND purl.TagDefinitionId = other.PrimaryTagDefinitionId
    WHERE rel.FromResourceId = @ResourceId

    UNION ALL

    -- In-edges: what depends on this resource
    SELECT
        'DependentOn' AS Direction,
        other.ResourceUid,
        other.ResourceKey,
        other.ResourceName,
        rt.TypeName    AS ResourceType,
        dt.TagValue    AS Domain,
        purl.TagValue  AS PrimaryUrl
    FROM [HTResourceMapper].[ResourceRelationship] rel
    INNER JOIN [HTResourceMapper].[Resource] other ON other.ResourceId = rel.FromResourceId
    INNER JOIN [HTResourceMapper].[ResourceType] rt ON rt.ResourceTypeId = other.ResourceTypeId
    LEFT JOIN [HTResourceMapper].[ResourceTag] dt
        ON dt.ResourceId = other.ResourceId AND dt.TagDefinitionId = @DomainTagDefId
    LEFT JOIN [HTResourceMapper].[ResourceTag] purl
        ON purl.ResourceId = other.ResourceId AND purl.TagDefinitionId = other.PrimaryTagDefinitionId
    WHERE rel.ToResourceId = @ResourceId

    ORDER BY Direction, ResourceName;
END
```

**Register it in the project** — add this line, alphabetically, to the `<Build>` `<ItemGroup>` in
`Database\HTResourceMapperDb\HTResourceMapperDb.sqlproj`:

```xml
<Build Include="Stored Procedures\Resource_GetForExplorer.sql" />
```

- [ ] **Step 1: Write the sproc file** `Resource_GetForExplorer.sql` with the SQL above.
- [ ] **Step 2: Add the `<Build Include>` line** to `HTResourceMapperDb.sqlproj`.
- [ ] **Step 3: Apply the sproc directly to the app DB** via `sqlcmd` (dual-track — the project file keeps `CREATE PROCEDURE`; apply a `CREATE OR ALTER PROCEDURE` variant so it's re-runnable):

```
sqlcmd -S "(localdb)\MSSQLLocalDB" -d ResourceMapper -Q "CREATE OR ALTER PROCEDURE [HTResourceMapper].[Resource_GetForExplorer] @ResourceUid VARCHAR(40) AS BEGIN ... END"
```
(Use the exact body from the `.sql` file with the leading verb changed to `CREATE OR ALTER`. `sqlcmd`, not the SQL MCP, per the app-DB note.) A full VS/MSBuild DACPAC publish is **not** required to verify this slice.
- [ ] **Step 4: Exercise via `sqlcmd`** against a resource that has both dependency directions and a primary link:

```
sqlcmd -S "(localdb)\MSSQLLocalDB" -d ResourceMapper -Q "EXEC [HTResourceMapper].[Resource_GetForExplorer] @ResourceUid='<a-known-uid>'"
```
Expected: one `Self` row (with `PrimaryUrl` populated if that resource has a primary Link tag) plus one row per neighbor, each with `Direction` `DependsOn`/`DependentOn`. An unknown uid returns **no rows**.

---

## C# — server row model (`Modules\Common\ResourceMapper.Common.Server\Resources\Models\`)

**File:** `ExplorerNodeRow.cs` — flat repo row mirroring the sproc's columns.

```csharp
namespace ResourceMapper.Common.Server.Resources.Models
{
    /// <summary>
    /// One row from Resource_GetForExplorer. Direction is 'Self' for the center resource, or
    /// 'DependsOn' / 'DependentOn' for a one-hop neighbour. Domain and PrimaryUrl may be null.
    /// </summary>
    public class ExplorerNodeRow
    {
        public string Direction { get; set; } = string.Empty;
        public string ResourceUid { get; set; } = string.Empty;
        public string ResourceKey { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string? Domain { get; set; }
        public string? PrimaryUrl { get; set; }
    }
}
```

- [ ] **Step 5: Create `ExplorerNodeRow.cs`.**

---

## Shared — client DTOs (`Modules\Common\ResourceMapper.Common.Shared\Explorer\`)

New feature folder + namespace `ResourceMapper.Common.Shared.Explorer` (parallels
`...Shared.Editor`). These cross the wire to Blazor and are serialized to the canvas.

**File:** `ExplorerNeighborModel.cs`

```csharp
namespace ResourceMapper.Common.Shared.Explorer
{
    /// <summary>
    /// A one-hop neighbour of an explorer node. Direction is 'DependsOn' (this node depends on
    /// the neighbour) or 'DependentOn' (the neighbour depends on this node).
    /// </summary>
    public class ExplorerNeighborModel
    {
        public string Direction { get; set; } = string.Empty;
        public string ResourceUid { get; set; } = string.Empty;
        public string ResourceKey { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string? Domain { get; set; }
        public string? PrimaryUrl { get; set; }
    }
}
```

**File:** `ExplorerNodeModel.cs`

```csharp
using System.Collections.Generic;

namespace ResourceMapper.Common.Shared.Explorer
{
    /// <summary>
    /// The center resource for an explorer read plus its one-hop neighbours. Returned by
    /// IExplorerService.GetNodeAsync for both the initial seed load and each node expansion.
    /// </summary>
    public class ExplorerNodeModel
    {
        public string ResourceUid { get; set; } = string.Empty;
        public string ResourceKey { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string? Domain { get; set; }
        public string? PrimaryUrl { get; set; }
        public List<ExplorerNeighborModel> Neighbors { get; set; } = new();
    }
}
```

- [ ] **Step 6: Create `ExplorerNeighborModel.cs` and `ExplorerNodeModel.cs`.**

---

## Repository (`Modules\Common\ResourceMapper.Common.Server\Explorer\`)

New `Explorer/` folder in the server project. Interface in `Explorer\Interfaces\`.

**File:** `Interfaces\IExplorerRepository.cs`

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ResourceMapper.Common.Server.Resources.Models;

namespace ResourceMapper.Common.Server.Explorer.Interfaces
{
    public interface IExplorerRepository
    {
        Task<List<ExplorerNodeRow>> GetForExplorerAsync(string resourceUid, CancellationToken cancellationToken);
    }
}
```

**File:** `ExplorerSqlRepository.cs` (mirrors `ResourceSqlRepository`'s `SprocCommand`/reader style)

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HT.Microsoft.SqlClient.Extensions;                       // SprocCommand / AddVarchar / ExecuteQueryAsync
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces; // IDbConnector
using ResourceMapper.Common.Server.Explorer.Interfaces;
using ResourceMapper.Common.Server.Resources.Models;

namespace ResourceMapper.Common.Server.Explorer
{
    public class ExplorerSqlRepository : IExplorerRepository
    {
        private readonly IDbConnector _db;

        public ExplorerSqlRepository(IDbConnector db)
        {
            _db = db;
        }

        public async Task<List<ExplorerNodeRow>> GetForExplorerAsync(string resourceUid, CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].Resource_GetForExplorer")
                .AddVarchar("@ResourceUid", resourceUid);

            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new ExplorerNodeRow
            {
                Direction = dr.ReadString("Direction"),
                ResourceUid = dr.ReadString("ResourceUid"),
                ResourceKey = dr.ReadString("ResourceKey"),
                ResourceName = dr.ReadString("ResourceName"),
                ResourceType = dr.ReadString("ResourceType"),
                Domain = dr.ReadString("Domain"),
                PrimaryUrl = dr.ReadString("PrimaryUrl")
            }, cancellationToken: cancellationToken);
        }
    }
}
```

- [ ] **Step 7: Create `IExplorerRepository.cs` and `ExplorerSqlRepository.cs`.**

---

## Service (`Modules\Common\ResourceMapper.Common.Server\Explorer\`)

**File:** `Interfaces\IExplorerService.cs`

```csharp
using System.Threading;
using System.Threading.Tasks;
using HT.Api.Service.Contracts;
using ResourceMapper.Common.Shared.Explorer;

namespace ResourceMapper.Common.Server.Explorer.Interfaces
{
    public interface IExplorerService
    {
        Task<ApiServiceResponse<ExplorerNodeModel>> GetNodeAsync(string resourceUid, CancellationToken cancellationToken = default);
    }
}
```

**File:** `ExplorerService.cs`

```csharp
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HT.Api.Client.Contracts.Models;                 // CallStatusCode
using HT.Api.Service.Contracts;                       // ApiServiceResponse<T>
using HT.Api.Service.Contracts.BuildersOfT;           // ServiceResponseBuilder<T>
using ResourceMapper.Common.Server.Explorer.Interfaces;
using ResourceMapper.Common.Shared.Explorer;

namespace ResourceMapper.Common.Server.Explorer
{
    public class ExplorerService : IExplorerService
    {
        private readonly IExplorerRepository _repo;

        public ExplorerService(IExplorerRepository repo)
        {
            _repo = repo;
        }

        public async Task<ApiServiceResponse<ExplorerNodeModel>> GetNodeAsync(string resourceUid, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<ExplorerNodeModel>();
            try
            {
                if (string.IsNullOrWhiteSpace(resourceUid))
                {
                    builder.Validation.AddValidation("resourceUid", "Resource UID is required");
                    return builder.BuildResponse();
                }

                var rows = await _repo.GetForExplorerAsync(resourceUid, cancellationToken);
                var self = rows.FirstOrDefault(r => r.Direction == "Self");
                if (self == null)
                {
                    builder.Errors.AddError(CallStatusCode.NotFound, "Resource Not Found",
                        $"Resource with UID '{resourceUid}' was not found.", "resourceUid");
                    return builder.BuildResponse();
                }

                var model = new ExplorerNodeModel
                {
                    ResourceUid = self.ResourceUid,
                    ResourceKey = self.ResourceKey,
                    ResourceName = self.ResourceName,
                    ResourceType = self.ResourceType,
                    Domain = self.Domain,
                    PrimaryUrl = self.PrimaryUrl,
                    Neighbors = rows
                        .Where(r => r.Direction != "Self")
                        .Select(r => new ExplorerNeighborModel
                        {
                            Direction = r.Direction,
                            ResourceUid = r.ResourceUid,
                            ResourceKey = r.ResourceKey,
                            ResourceName = r.ResourceName,
                            ResourceType = r.ResourceType,
                            Domain = r.Domain,
                            PrimaryUrl = r.PrimaryUrl
                        })
                        .ToList()
                };

                builder.Data.Set(model);
                return builder.BuildResponse();
            }
            catch (System.Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }
    }
}
```

- [ ] **Step 8: Create `IExplorerService.cs` and `ExplorerService.cs`.**

---

## Dependency registration

Add the two pairs to `RegisterCommonDependencies` in
`Modules\Common\ResourceMapper.Common.Server\Utils\DependencyRegistration.cs` (after the resource
pair), plus the two new `using`s:

```csharp
using ResourceMapper.Common.Server.Explorer;
using ResourceMapper.Common.Server.Explorer.Interfaces;
```
```csharp
services.TryAddScoped<IExplorerRepository, ExplorerSqlRepository>();
services.TryAddScoped<IExplorerService, ExplorerService>();
```

- [ ] **Step 9: Register the repo + service.**

---

## Tests (`_Tests\Common\Server\ResourceMapper.Common.Server.Tests\Explorer\`)

Unit-test the **service** against a mocked `IExplorerRepository` (repos are never unit-tested
against a live DB — see the master-list Notes). New folder mirrors the source; namespace
`ResourceMapper.Common.Server.Tests.Explorer`.

**File:** `ExplorerServiceTests.cs`

```csharp
// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ResourceMapper.Common.Server.Explorer;
using ResourceMapper.Common.Server.Explorer.Interfaces;
using ResourceMapper.Common.Server.Resources.Models;

namespace ResourceMapper.Common.Server.Tests.Explorer
{
    [Trait("Category", "Unit")]
    [Trait("Category", "ResourceMapper")]
    [Trait("Category", "ResourceMapper/Common")]
    [Trait("Category", "ResourceMapper/Common/Server")]
    [Trait("Category", "ResourceMapper/Common/Server/Explorer")]
    [Trait("Category", "ResourceMapper/Common/Server/Explorer/ExplorerService")]
    public class ExplorerServiceTests
    {
        private readonly Mock<IExplorerRepository> _repo;
        private readonly ExplorerService _sut;

        public ExplorerServiceTests()
        {
            _repo = new Mock<IExplorerRepository>();
            _sut = new ExplorerService(_repo.Object);
        }

        #region GetNodeAsync

        [Fact]
        public async Task GetNodeAsync_BlankUid_ReturnsValidationErrorAndDoesNotCallRepo()
        {
            var response = await _sut.GetNodeAsync("   ", CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a blank uid is a validation failure");
            _repo.Verify(r => r.GetForExplorerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never, "because validation must short-circuit before the repo call");
        }

        [Fact]
        public async Task GetNodeAsync_NoSelfRow_ReturnsNotFound()
        {
            _repo.Setup(r => r.GetForExplorerAsync("missing", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ExplorerNodeRow>());

            var response = await _sut.GetNodeAsync("missing", CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because an unknown uid yields no 'Self' row and maps to NotFound");
        }

        [Fact]
        public async Task GetNodeAsync_SelfAndNeighbors_MapsCenterFieldsAndSplitsNeighbors()
        {
            _repo.Setup(r => r.GetForExplorerAsync("root", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ExplorerNodeRow>
                {
                    Row("Self", "root", "root-key", "Root", "Service", "prod", "https://portal/root"),
                    Row("DependsOn", "dep1", "dep1-key", "Dep One", "Queue", "prod", "https://portal/dep1"),
                    Row("DependentOn", "up1", "up1-key", "Upstream One", "App", null, null)
                });

            var response = await _sut.GetNodeAsync("root", CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a 'Self' row was present");
            var data = response.ApiResponse.Data!;
            data.ResourceUid.Should().Be("root", "because the Self row is the center node");
            data.PrimaryUrl.Should().Be("https://portal/root", "because the center node's primary url is projected");
            data.Neighbors.Should().HaveCount(2, "because both non-Self rows become neighbours");
            data.Neighbors.Should().Contain(n => n.ResourceUid == "dep1" && n.Direction == "DependsOn",
                "because out-edges are DependsOn neighbours");
            data.Neighbors.Should().Contain(n => n.ResourceUid == "up1" && n.Direction == "DependentOn" && n.PrimaryUrl == null,
                "because in-edges are DependentOn neighbours and a missing primary url stays null");
        }

        [Fact]
        public async Task GetNodeAsync_SelfWithNoNeighbors_ReturnsEmptyNeighborList()
        {
            _repo.Setup(r => r.GetForExplorerAsync("lonely", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ExplorerNodeRow>
                {
                    Row("Self", "lonely", "lonely-key", "Lonely", "Service", null, null)
                });

            var response = await _sut.GetNodeAsync("lonely", CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the resource exists even with no edges");
            response.ApiResponse.Data!.Neighbors.Should().BeEmpty("because it has no relationships");
        }

        #endregion

        #region helpers

        private static ExplorerNodeRow Row(string direction, string uid, string key, string name,
            string type, string? domain, string? primaryUrl) => new()
        {
            Direction = direction,
            ResourceUid = uid,
            ResourceKey = key,
            ResourceName = name,
            ResourceType = type,
            Domain = domain,
            PrimaryUrl = primaryUrl
        };

        #endregion
    }
}
```

> `response.IsSuccess()` (method on `ApiServiceResponse<T>`, `ApiServiceResponseOfT.cs`) and
> `response.ApiResponse.Data!` are the exact assertion surface the existing `ResourceServiceTests`
> use — mirror them.

- [ ] **Step 10: Write `ExplorerServiceTests.cs` (the four tests above).**
- [ ] **Step 11: Run the tests — expect FAIL to compile/red first** (types don't exist yet if written test-first; if writing service alongside, expect them to drive the mapping):

```
dotnet test --filter "FullyQualifiedName~ResourceMapper.Common.Server.Tests.Explorer.ExplorerServiceTests"
```

- [ ] **Step 12: Implement/adjust until green**, then run the full suite once:

```
dotnet build
dotnet test
```
Expected: all green; no changes to editor tests (this slice didn't touch that path).

---

## Verification

1. **DB:** build the dacpac (VS/full MSBuild) and publish to `(localdb)\MSSQLLocalDB\ResourceMapper` with `/p:DropObjectsNotInSource=True`.
2. **Sproc, happy path:** `sqlcmd` EXEC against a uid with both directions + a primary link → one `Self` row (PrimaryUrl set) + neighbour rows with correct `Direction`.
3. **Sproc, edges of data:** a resource with **no** relationships → only the `Self` row; a resource with **no** primary tag → `Self` row with `PrimaryUrl = NULL`; an **unknown** uid → zero rows.
4. **Build + unit tests:** `dotnet build` then `dotnet test` → all green.
5. **Throwaway smoke (optional, deleted before commit):** a temporary xUnit test resolving the real `IExplorerService` (real `ExplorerSqlRepository` over `(localdb)`) that calls `GetNodeAsync("<known-uid>")` and asserts a populated `ExplorerNodeModel`. **Delete it before committing** (matches the editor slices' verification style).

---

## Out of scope (later slices)

- **All UI** — the `/explore/{ResourceUid}` page, the Cytoscape canvas, and the grid "Explore" action → **slice 4**. This slice is **back-end only**, verified by unit tests + `sqlcmd` (per the master-list rule that pure back-end slices need no UI stub).
- **One-hop expand/collapse, layout, presets, node actions** → slices 4–6.
- **Diagram persistence, client identity, sharing, export** → slices 2, 3, 7, 8.
- **No change to the editor's `ResourceRelationship_GetForResource`, `ResourceRelationshipModel`, or `DependencyRowEditor`** — the editor path is intentionally untouched.

---

## Execution notes

_(Written after execution — record what actually happened, any deviations, bugs found + fixed,
and the exact `using`/member-name reconciliations against `ResourceService.cs`/`ResourceServiceTests.cs`.)_

Then: mark slice #1 **Done** ✓ / slice #2 **Planning** in the master list, and commit.
