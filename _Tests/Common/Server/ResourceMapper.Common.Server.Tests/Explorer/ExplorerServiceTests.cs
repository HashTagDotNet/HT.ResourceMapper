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
                    Row("Self", "root", "root-key", "Root", "Service", "prod", "https://portal/root", "APP", "web"),
                    Row("DependsOn", "dep1", "dep1-key", "Dep One", "Queue", "prod", "https://portal/dep1"),
                    Row("DependentOn", "up1", "up1-key", "Upstream One", "App", null, null)
                });

            var response = await _sut.GetNodeAsync("root", CancellationToken.None);

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

        #endregion
    }
}
