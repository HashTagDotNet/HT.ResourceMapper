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
