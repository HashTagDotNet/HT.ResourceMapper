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

            response.IsSuccess().Should().BeFalse(
                "because Name is NVARCHAR(200) and a longer value would be truncated by the database");
        }

        [Fact]
        public async Task SaveAsync_BlankQueryString_ReturnsValidation()
        {
            var request = new SaveViewRequest { Name = "View A", QueryString = "" };

            var response = await _sut.SaveAsync("steve", request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a view with no query would restore nothing");
        }

        [Fact]
        public async Task SaveAsync_QueryStringLongerThan2000Chars_IsAccepted()
        {
            var longQuery = "?n=100&f=tag:Consumer~eq~" +
                            string.Join(",", Enumerable.Range(0, 160).Select(i => "someLongValue" + i));
            _repo.Setup(r => r.UpsertAsync("steve", It.IsAny<string>(), "View A", longQuery,
                    It.IsAny<CancellationToken>()))
                 .ReturnsAsync(("created", "sv-1"));

            var request = new SaveViewRequest { Name = "View A", QueryString = longQuery };
            var response = await _sut.SaveAsync("steve", request, CancellationToken.None);

            longQuery.Length.Should().BeGreaterThan(2000,
                "because this test is worthless if the fixture is not actually over the old cap");
            response.IsSuccess().Should().BeTrue(
                "because a real four-filter query already exceeds 2000 characters and must not be rejected");
        }

        [Fact]
        public async Task SaveAsync_NoUidSupplied_GeneratesOneAndCreates()
        {
            string? capturedUid = null;
            _repo.Setup(r => r.UpsertAsync("steve", It.IsAny<string>(), "View A", It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                 .Callback<string, string, string, string, CancellationToken>((_, uid, _, _, _) => capturedUid = uid)
                 .ReturnsAsync(("created", "sv-generated"));

            var response = await _sut.SaveAsync("steve", Req("View A"), CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a create needs no caller-supplied uid");
            capturedUid.Should().StartWith("sv-", "because generated uids are prefixed like the other uid columns");
        }

        [Fact]
        public async Task SaveAsync_UidSupplied_IsPassedThroughSoTheRowIsUpdated()
        {
            _repo.Setup(r => r.UpsertAsync("steve", "sv-existing", "Renamed", It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                 .ReturnsAsync(("updated", "sv-existing"));

            var request = new SaveViewRequest
            {
                SavedViewUid = "sv-existing", Name = "Renamed", QueryString = "?n=100"
            };
            var response = await _sut.SaveAsync("steve", request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because supplying a uid updates rather than creates");
            _repo.Verify(r => r.UpsertAsync("steve", "sv-existing", "Renamed", "?n=100",
                    It.IsAny<CancellationToken>()), Times.Once,
                "because rename and overwrite both work by re-saving against an existing uid");
        }

        [Fact]
        public async Task SaveAsync_RepositoryDenies_ReturnsError()
        {
            _repo.Setup(r => r.UpsertAsync("steve", "sv-other", It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                 .ReturnsAsync(("denied", ""));

            var request = new SaveViewRequest
            {
                SavedViewUid = "sv-other", Name = "View A", QueryString = "?n=100"
            };
            var response = await _sut.SaveAsync("steve", request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because that uid belongs to a different owner");
        }

        [Fact]
        public async Task SaveAsync_NamePadded_IsTrimmedBeforeSaving()
        {
            _repo.Setup(r => r.UpsertAsync("steve", It.IsAny<string>(), "View A", It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                 .ReturnsAsync(("created", "sv-1"));

            await _sut.SaveAsync("steve", Req("  View A  "), CancellationToken.None);

            _repo.Verify(r => r.UpsertAsync("steve", It.IsAny<string>(), "View A", It.IsAny<string>(),
                    It.IsAny<CancellationToken>()), Times.Once,
                "because a padded name would otherwise defeat the per-owner unique constraint");
        }

        #endregion

        #region DeleteAsync

        [Fact]
        public async Task DeleteAsync_RepositoryDeletedNothing_ReturnsNotFound()
        {
            _repo.Setup(r => r.DeleteAsync("steve", "sv-gone", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(0);

            var response = await _sut.DeleteAsync("steve", "sv-gone", CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because deleting nothing means the view was not there");
        }

        [Fact]
        public async Task DeleteAsync_RowRemoved_Succeeds()
        {
            _repo.Setup(r => r.DeleteAsync("steve", "sv-1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(1);

            var response = await _sut.DeleteAsync("steve", "sv-1", CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the row was removed");
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

        [Fact]
        public async Task SetDefaultAsync_BlankOwnerId_ReturnsValidation()
        {
            var response = await _sut.SetDefaultAsync("", "sv-1", CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because the default is per owner");
        }

        #endregion

        #region ReorderAsync

        [Fact]
        public async Task ReorderAsync_EmptyList_SucceedsWithoutCallingRepo()
        {
            var response = await _sut.ReorderAsync("steve", new List<string>(), CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because reordering nothing is a no-op, not a failure");
            _repo.Verify(r => r.ReorderAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()), Times.Never, "because there is nothing to write");
        }

        [Fact]
        public async Task ReorderAsync_WithUids_PassesThemThroughInOrder()
        {
            var order = new List<string> { "sv-b", "sv-a" };

            var response = await _sut.ReorderAsync("steve", order, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the reorder was applied");
            _repo.Verify(r => r.ReorderAsync("steve", order, It.IsAny<CancellationToken>()), Times.Once,
                "because list position is what becomes SortOrder");
        }

        #endregion

        #region ListAsync

        [Fact]
        public async Task ListAsync_BlankOwnerId_ReturnsValidationAndDoesNotCallRepo()
        {
            var response = await _sut.ListAsync("", CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because views are listed per owner");
            _repo.Verify(r => r.ListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never,
                "because validation short-circuits before the query");
        }

        [Fact]
        public async Task ListAsync_RepositoryThrows_ReturnsErrorRatherThanPropagating()
        {
            _repo.Setup(r => r.ListAsync("steve", It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("database is down"));

            var response = await _sut.ListAsync("steve", CancellationToken.None);

            response.IsSuccess().Should().BeFalse(
                "because a failure to load saved views must not take the whole page down");
        }

        #endregion

        #region helpers

        private static SaveViewRequest Req(string name) =>
            new() { Name = name, QueryString = "?n=100&s=ResourceName:asc" };

        #endregion
    }
}
