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
            var response = await _sut.GetNodeAsync("   ", null, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a blank uid is a validation failure");
            _repo.Verify(r => r.GetForExplorerAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>?>(), It.IsAny<CancellationToken>()),
                Times.Never, "because validation must short-circuit before the repo call");
        }

        [Fact]
        public async Task GetNodeAsync_NoSelfRow_ReturnsNotFound()
        {
            _repo.Setup(r => r.GetForExplorerAsync("missing", It.IsAny<IReadOnlyCollection<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ExplorerNodeRow>());

            var response = await _sut.GetNodeAsync("missing", null, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because an unknown uid yields no 'Self' row and maps to NotFound");
        }

        [Fact]
        public async Task GetNodeAsync_SelfAndNeighbors_MapsCenterFieldsAndSplitsNeighbors()
        {
            _repo.Setup(r => r.GetForExplorerAsync("root", It.IsAny<IReadOnlyCollection<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ExplorerNodeRow>
                {
                    Row("Self", "root", "root-key", "Root", "Service", "prod", "https://portal/root", "APP", "web"),
                    Row("DependsOn", "dep1", "dep1-key", "Dep One", "Queue", "prod", "https://portal/dep1"),
                    Row("DependentOn", "up1", "up1-key", "Upstream One", "App", null, null)
                });

            var response = await _sut.GetNodeAsync("root", null, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a 'Self' row was present");
            var data = response.ApiResponse.Data!;
            data.ResourceUid.Should().Be("root", "because the Self row is the center node");
            data.PrimaryUrl.Should().Be("https://portal/root", "because the center node's primary url is projected");
            data.ShortCode.Should().Be("APP", "because the center node's type short code is projected");
            data.IconKey.Should().Be("web", "because the center node's type icon key is projected");
            data.Neighbors.Should().HaveCount(2, "because both non-Self rows become neighbours");
            data.Neighbors.Should().Contain(n => n.ResourceUid == "dep1" && n.Direction == "DependsOn",
                "because out-edges are DependsOn neighbours");
            data.Neighbors.Should().Contain(n => n.ResourceUid == "up1" && n.Direction == "DependentOn" && n.PrimaryUrl == null,
                "because in-edges are DependentOn neighbours and a missing primary url stays null");
        }

        [Fact]
        public async Task GetNodeAsync_SelfWithNoNeighbors_ReturnsEmptyNeighborList()
        {
            _repo.Setup(r => r.GetForExplorerAsync("lonely", It.IsAny<IReadOnlyCollection<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ExplorerNodeRow>
                {
                    Row("Self", "lonely", "lonely-key", "Lonely", "Service", null, null)
                });

            var response = await _sut.GetNodeAsync("lonely", null, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the resource exists even with no edges");
            response.ApiResponse.Data!.Neighbors.Should().BeEmpty("because it has no relationships");
        }

        [Fact]
        public async Task GetNodeAsync_EdgeRows_ProjectsEdgesAndKeepsThemOutOfNeighbors()
        {
            _repo.Setup(r => r.GetForExplorerAsync("root", It.IsAny<IReadOnlyCollection<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ExplorerNodeRow>
                {
                    Row("Self", "root", "root-key", "Root", "Service", "prod", null),
                    Row("DependsOn", "dep1", "dep1-key", "Dep One", "Queue", "prod", null),
                    Row("DependentOn", "up1", "up1-key", "Upstream One", "App", "prod", null),
                    Edge("root", "dep1"),
                    Edge("up1", "root"),
                    Edge("up1", "dep1")
                });

            var response = await _sut.GetNodeAsync("root", null, CancellationToken.None);

            var data = response.ApiResponse.Data!;
            data.Edges.Should().HaveCount(3, "because every 'Edge' row is projected");
            data.Edges.Should().Contain(e => e.FromUid == "up1" && e.ToUid == "dep1",
                "because a relationship between two neighbours of the center must be drawable — it is exactly the edge no center-incident query returns");
            data.Neighbors.Should().HaveCount(2, "because 'Edge' rows are not nodes and must not become neighbours");
            data.Neighbors.Should().NotContain(n => string.IsNullOrEmpty(n.ResourceUid),
                "because the blank node columns on an 'Edge' row would otherwise leak in as an empty neighbour");
        }

        [Fact]
        public async Task GetNodeAsync_EdgeRowMissingAnEndpoint_IsDropped()
        {
            _repo.Setup(r => r.GetForExplorerAsync("root", It.IsAny<IReadOnlyCollection<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ExplorerNodeRow>
                {
                    Row("Self", "root", "root-key", "Root", "Service", null, null),
                    Edge("root", null),
                    Edge(null, "root"),
                    Edge("root", "dep1")
                });

            var response = await _sut.GetNodeAsync("root", null, CancellationToken.None);

            response.ApiResponse.Data!.Edges.Should().ContainSingle(
                "because an edge with a missing endpoint cannot be drawn and is discarded rather than sent as a half-edge");
        }

        [Fact]
        public async Task GetNodeAsync_KnownUidsSupplied_ArePassedThroughToTheRepository()
        {
            var known = new[] { "already-drawn-1", "already-drawn-2" };
            _repo.Setup(r => r.GetForExplorerAsync("root", It.IsAny<IReadOnlyCollection<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ExplorerNodeRow>
                {
                    Row("Self", "root", "root-key", "Root", "Service", null, null)
                });

            await _sut.GetNodeAsync("root", known, CancellationToken.None);

            _repo.Verify(r => r.GetForExplorerAsync("root", known, It.IsAny<CancellationToken>()), Times.Once,
                "because the read needs the caller's on-canvas set to backfill edges to nodes already drawn");
        }

        #endregion

        #region helpers

        private static ExplorerNodeRow Row(string direction, string uid, string key, string name,
            string type, string? domain, string? primaryUrl, string? shortCode = null, string? iconKey = null) => new()
        {
            Direction = direction,
            ResourceUid = uid,
            ResourceKey = key,
            ResourceName = name,
            ResourceType = type,
            ShortCode = shortCode,
            IconKey = iconKey,
            Domain = domain,
            PrimaryUrl = primaryUrl
        };

        // An 'Edge' row: node columns blank, endpoints in From/ToResourceUid.
        private static ExplorerNodeRow Edge(string? fromUid, string? toUid) => new()
        {
            Direction = "Edge",
            ResourceUid = string.Empty,
            ResourceKey = string.Empty,
            ResourceName = string.Empty,
            ResourceType = string.Empty,
            FromResourceUid = fromUid,
            ToResourceUid = toUid
        };

        #endregion
    }
}
