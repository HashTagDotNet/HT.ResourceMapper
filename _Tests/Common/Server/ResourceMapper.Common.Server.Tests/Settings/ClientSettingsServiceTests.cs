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
