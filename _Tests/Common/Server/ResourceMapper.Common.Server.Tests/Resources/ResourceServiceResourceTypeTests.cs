using HT.Api.Client.Contracts.Models;
using ResourceMapper.Common.Server.Resources;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Server.Resources.Models;
using ResourceMapper.Common.Shared.ResourceTypes.Contracts;

// ReSharper disable InconsistentNaming

namespace ResourceMapper.Common.Server.Tests.Resources
{
    /// <summary>
    /// Resource-type CRUD behaviour of ResourceService (PL-28): validation guards, the mapping of
    /// the repository's sproc result strings onto typed errors, and the blocked-delete rule that
    /// keeps FK_Resource_ResourceTypeId from ever surfacing as a raw SQL failure.
    /// </summary>
    [Trait("Category", "Unit")]
    [Trait("Category", "ResourceMapper")]
    [Trait("Category", "ResourceMapper/Common")]
    [Trait("Category", "ResourceMapper/Common/Server")]
    [Trait("Category", "ResourceMapper/Common/Server/Resources")]
    [Trait("Category", "ResourceMapper/Common/Server/Resources/ResourceService")]
    public class ResourceServiceResourceTypeTests
    {
        private readonly Mock<IResourceRepository> _repo;
        private readonly ResourceService _sut;

        public ResourceServiceResourceTypeTests()
        {
            _repo = new Mock<IResourceRepository>();
            _sut = new ResourceService(_repo.Object);
        }

        #region GetResourceTypesAsync

        [Fact]
        public async Task GetResourceTypesAsync_RepositoryReturnsRows_ProjectsEveryFieldIncludingUsageCounts()
        {
            SetupUsageRows(MakeUsageRow(7, "Azure Redis", "RDS", "memory", resourceCount: 12, entryPointTagCount: 3));

            var response = await _sut.GetResourceTypesAsync(CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a plain read with no validation guards should succeed");

            var model = response.ApiResponse.Data.Should().ContainSingle("because the repository returned one row").Subject;
            model.ResourceTypeId.Should().Be(7, "because the surrogate id identifies the row for later edit/delete calls");
            model.TypeName.Should().Be("Azure Redis", "because the type name is projected verbatim");
            model.ShortCode.Should().Be("RDS", "because ShortCode is what the explorer prints inside the node");
            model.IconKey.Should().Be("memory", "because IconKey is what the explorer colours the node by");
            model.ResourceCount.Should().Be(12, "because the dependent count drives whether delete is offered");
            model.EntryPointTagCount.Should().Be(3, "because the tag-template size is shown on the management screen");
        }

        [Fact]
        public async Task GetResourceTypesAsync_RepositoryThrows_ReturnsInternalErrorRatherThanPropagating()
        {
            _repo.Setup(r => r.GetResourceTypesWithUsageAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var response = await _sut.GetResourceTypesAsync(CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because the repository failed");
            HasError(response.ApiResponse.Errors, CallStatusCode.InternalError).Should().BeTrue(
                "because an unexpected exception is reported as an internal error, not thrown at the component");
        }

        #endregion

        #region SaveResourceTypeAsync: validation

        [Fact]
        public async Task SaveResourceTypeAsync_NullRequest_FailsValidationAndSkipsRepository()
        {
            var response = await _sut.SaveResourceTypeAsync(null!, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a null request cannot be saved");
            VerifySaveNeverCalled();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SaveResourceTypeAsync_BlankTypeName_FailsValidationAndSkipsRepository(string typeName)
        {
            var response = await _sut.SaveResourceTypeAsync(MakeRequest(typeName: typeName), CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because TypeName is NOT NULL and uniquely constrained");
            HasErrorForField(response.ApiResponse.Errors, "request.TypeName").Should().BeTrue(
                "because the message must point the dialog at the name field");
            VerifySaveNeverCalled();
        }

        [Fact]
        public async Task SaveResourceTypeAsync_TypeNameLongerThanColumn_FailsValidationAndSkipsRepository()
        {
            var request = MakeRequest(typeName: new string('x', 251));

            var response = await _sut.SaveResourceTypeAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because TypeName is NVARCHAR(250) and would be truncated");
            HasErrorForField(response.ApiResponse.Errors, "request.TypeName").Should().BeTrue(
                "because the over-length message belongs on the name field");
            VerifySaveNeverCalled();
        }

        [Fact]
        public async Task SaveResourceTypeAsync_ShortCodeLongerThanColumn_FailsValidationAndSkipsRepository()
        {
            var request = MakeRequest(shortCode: new string('A', 11));

            var response = await _sut.SaveResourceTypeAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because ShortCode is VARCHAR(10)");
            HasErrorForField(response.ApiResponse.Errors, "request.ShortCode").Should().BeTrue(
                "because the message must point at the short-code field");
            VerifySaveNeverCalled();
        }

        [Fact]
        public async Task SaveResourceTypeAsync_IconKeyLongerThanColumn_FailsValidationAndSkipsRepository()
        {
            var request = MakeRequest(iconKey: new string('k', 41));

            var response = await _sut.SaveResourceTypeAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because IconKey is VARCHAR(40)");
            HasErrorForField(response.ApiResponse.Errors, "request.IconKey").Should().BeTrue(
                "because the message must point at the icon field");
            VerifySaveNeverCalled();
        }

        #endregion

        #region SaveResourceTypeAsync: repository call shape

        [Fact]
        public async Task SaveResourceTypeAsync_ZeroResourceTypeId_SendsNullIdAndAGeneratedUidToRepository()
        {
            int? capturedId = -1;
            var capturedUid = string.Empty;
            CaptureSave((id, uid, _, _, _, _) => { capturedId = id; capturedUid = uid; }, ("created", 42));
            SetupUsageRows(MakeUsageRow(42, "New Type", "NEW", "web"));

            await _sut.SaveResourceTypeAsync(MakeRequest(resourceTypeId: 0, typeName: "New Type"), CancellationToken.None);

            capturedId.Should().BeNull("because id 0 means create, and the sproc keys creation off a null id");
            Guid.TryParse(capturedUid, out _).Should().BeTrue(
                "because a create must mint the immutable ResourceTypeUid rather than leaving it blank");
        }

        [Fact]
        public async Task SaveResourceTypeAsync_ExistingResourceTypeId_SendsThatIdSoRenameIsAnUpdate()
        {
            int? capturedId = null;
            var capturedName = string.Empty;
            CaptureSave((id, _, name, _, _, _) => { capturedId = id; capturedName = name; }, ("updated", 7));
            SetupUsageRows(MakeUsageRow(7, "Renamed Type", "RNM", "web"));

            await _sut.SaveResourceTypeAsync(MakeRequest(resourceTypeId: 7, typeName: "Renamed Type"), CancellationToken.None);

            capturedId.Should().Be(7,
                "because the editor path keys on the surrogate id — unlike the import path's ResourceType_Upsert, " +
                "which keys on TypeName and therefore can never rename");
            capturedName.Should().Be("Renamed Type", "because the new name must reach the repository");
        }

        [Fact]
        public async Task SaveResourceTypeAsync_NegativeResourceTypeId_IsTreatedAsAnUpdateTarget()
        {
            int? capturedId = null;
            CaptureSave((id, _, _, _, _, _) => capturedId = id, ("updated", -6));
            SetupUsageRows(MakeUsageRow(-6, "Azure ServiceBus", "SB", "bus"));

            await _sut.SaveResourceTypeAsync(MakeRequest(resourceTypeId: -6, typeName: "Azure ServiceBus"), CancellationToken.None);

            capturedId.Should().Be(-6, "because seeded system types carry negative ids and must still be editable");
        }

        [Fact]
        public async Task SaveResourceTypeAsync_PaddedValues_AreTrimmedBeforeReachingRepository()
        {
            var capturedName = string.Empty;
            string? capturedShortCode = null;
            string? capturedIconKey = null;
            CaptureSave((_, _, name, code, icon, _) =>
            {
                capturedName = name;
                capturedShortCode = code;
                capturedIconKey = icon;
            }, ("created", 3));
            SetupUsageRows(MakeUsageRow(3, "Padded Type", "PAD", "web"));

            var request = MakeRequest(typeName: "  Padded Type  ", shortCode: "  PAD  ", iconKey: "  web  ");
            await _sut.SaveResourceTypeAsync(request, CancellationToken.None);

            capturedName.Should().Be("Padded Type", "because a stray space would create a near-duplicate type name");
            capturedShortCode.Should().Be("PAD", "because the node badge would render the padding");
            capturedIconKey.Should().Be("web", "because a padded key would not match any glyph in the icon map");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task SaveResourceTypeAsync_BlankOptionalFields_AreSentAsNullNotEmptyString(string? blank)
        {
            string? capturedShortCode = "unset";
            string? capturedIconKey = "unset";
            CaptureSave((_, _, _, code, icon, _) =>
            {
                capturedShortCode = code;
                capturedIconKey = icon;
            }, ("created", 4));
            SetupUsageRows(MakeUsageRow(4, "Blank Type", null, null));

            await _sut.SaveResourceTypeAsync(MakeRequest(shortCode: blank, iconKey: blank), CancellationToken.None);

            capturedShortCode.Should().BeNull("because the column is nullable and empty string is not the same as absent");
            capturedIconKey.Should().BeNull("because an empty IconKey must fall through to the canvas default, not match nothing");
        }

        #endregion

        #region SaveResourceTypeAsync: result mapping

        [Fact]
        public async Task SaveResourceTypeAsync_RepositoryReportsDuplicate_ReturnsAlreadyExistsNamingTheType()
        {
            SetupSaveResult("duplicate", 0);

            var response = await _sut.SaveResourceTypeAsync(MakeRequest(typeName: "Azure Redis"), CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because UK_ResourceType_TypeName forbids a second row with this name");
            HasError(response.ApiResponse.Errors, CallStatusCode.AlreadyExists).Should().BeTrue(
                "because a name collision is a conflict, not an internal error");
            response.ApiResponse.Errors!.Should().Contain(e => e.Details != null && e.Details.Contains("Azure Redis"),
                "because the message should name the type the user tried to reuse");
        }

        [Fact]
        public async Task SaveResourceTypeAsync_RepositoryReportsNotFound_ReturnsNotFound()
        {
            SetupSaveResult("notfound", 0);

            var response = await _sut.SaveResourceTypeAsync(MakeRequest(resourceTypeId: 999), CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because the row being edited no longer exists");
            HasError(response.ApiResponse.Errors, CallStatusCode.NotFound).Should().BeTrue(
                "because a concurrent delete should read as NotFound rather than a save failure");
        }

        [Fact]
        public async Task SaveResourceTypeAsync_RepositoryReportsUnknownResult_ReturnsInternalError()
        {
            SetupSaveResult("error", 0);

            var response = await _sut.SaveResourceTypeAsync(MakeRequest(), CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because an unrecognised sproc result is not a success");
            HasError(response.ApiResponse.Errors, CallStatusCode.InternalError).Should().BeTrue(
                "because an unexpected result string indicates a broken contract with the sproc");
        }

        [Fact]
        public async Task SaveResourceTypeAsync_SavedRowMissingOnReread_ReturnsInternalError()
        {
            SetupSaveResult("created", 55);
            SetupUsageRows(MakeUsageRow(1, "Some Other Type", "SOT", "web"));

            var response = await _sut.SaveResourceTypeAsync(MakeRequest(), CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because the row the sproc claims to have written could not be read back");
            HasError(response.ApiResponse.Errors, CallStatusCode.InternalError).Should().BeTrue(
                "because a write that cannot be re-read is an internal inconsistency");
        }

        [Fact]
        public async Task SaveResourceTypeAsync_HappyPath_ReturnsTheRereadRowWithItsUsageCounts()
        {
            SetupSaveResult("created", 55);
            SetupUsageRows(MakeUsageRow(55, "Azure Storage", "AST", "database", resourceCount: 0, entryPointTagCount: 2));

            var response = await _sut.SaveResourceTypeAsync(MakeRequest(typeName: "Azure Storage"), CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the repository reported a successful create");

            var model = response.ApiResponse.Data;
            model.Should().NotBeNull("because the caller needs the persisted row to add to its picklist");
            model!.ResourceTypeId.Should().Be(55, "because the inline-create caller selects the new type by id");
            model.ShortCode.Should().Be("AST", "because the persisted value is re-read rather than echoed from the request");
            model.EntryPointTagCount.Should().Be(2, "because the counts come from the re-read, not the write");
        }

        #endregion

        #region DeleteResourceTypeAsync

        [Fact]
        public async Task DeleteResourceTypeAsync_ZeroId_FailsValidationAndSkipsRepository()
        {
            var response = await _sut.DeleteResourceTypeAsync(0, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because 0 is the sentinel for 'new', never a deletable row");
            _repo.Verify(r => r.DeleteResourceTypeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never,
                "because validation must short-circuit before the repository is called");
        }

        [Fact]
        public async Task DeleteResourceTypeAsync_TypeStillInUse_ReturnsFailedPreconditionNamingTheDependentCount()
        {
            SetupDeleteResult("inuse", 12);

            var response = await _sut.DeleteResourceTypeAsync(7, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because Resource.ResourceTypeId is a NOT NULL FK");
            HasError(response.ApiResponse.Errors, CallStatusCode.FailedPrecondition).Should().BeTrue(
                "because the caller should fix the state (reassign resources) rather than retry the delete");
            response.ApiResponse.Errors!.Should().Contain(e => e.Details != null && e.Details.Contains("12 resources use"),
                "because the whole point of the guard is to name how many resources block the delete " +
                "instead of surfacing a raw FK violation");
        }

        [Fact]
        public async Task DeleteResourceTypeAsync_TypeUsedByExactlyOneResource_UsesSingularWording()
        {
            SetupDeleteResult("inuse", 1);

            var response = await _sut.DeleteResourceTypeAsync(7, CancellationToken.None);

            response.ApiResponse.Errors!.Should().Contain(e => e.Details != null && e.Details.Contains("1 resource uses"),
                "because '1 resources use this type' would read as a bug to the user");
        }

        [Fact]
        public async Task DeleteResourceTypeAsync_TypeNotFound_ReturnsNotFound()
        {
            SetupDeleteResult("notfound", 0);

            var response = await _sut.DeleteResourceTypeAsync(7, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because there was nothing to delete");
            HasError(response.ApiResponse.Errors, CallStatusCode.NotFound).Should().BeTrue(
                "because an already-deleted row should read as NotFound");
        }

        [Fact]
        public async Task DeleteResourceTypeAsync_NoDependents_ReportsDeleted()
        {
            SetupDeleteResult("deleted", 0);

            var response = await _sut.DeleteResourceTypeAsync(7, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because nothing referenced the type");
            response.ApiResponse.Data!.Deleted.Should().BeTrue("because the row was removed");
            response.ApiResponse.Data.DependentCount.Should().Be(0, "because a successful delete had no blockers");
            _repo.Verify(r => r.DeleteResourceTypeAsync(7, It.IsAny<CancellationToken>()), Times.Once,
                "because the requested id must reach the repository unchanged");
        }

        [Fact]
        public async Task DeleteResourceTypeAsync_RepositoryThrows_ReturnsInternalErrorRatherThanPropagating()
        {
            _repo.Setup(r => r.DeleteResourceTypeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var response = await _sut.DeleteResourceTypeAsync(7, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because the repository failed");
            HasError(response.ApiResponse.Errors, CallStatusCode.InternalError).Should().BeTrue(
                "because an unexpected exception is reported as an internal error, not thrown at the component");
        }

        #endregion

        #region helpers

        private static SaveResourceTypeRequest MakeRequest(int resourceTypeId = 0, string typeName = "Sample Type",
            string? shortCode = "SMP", string? iconKey = "web", bool allowCustomTags = true) => new()
        {
            ResourceTypeId = resourceTypeId,
            TypeName = typeName,
            ShortCode = shortCode,
            IconKey = iconKey,
            AllowCustomTags = allowCustomTags
        };

        private static ResourceTypeUsage MakeUsageRow(int id, string typeName, string? shortCode, string? iconKey,
            int resourceCount = 0, int entryPointTagCount = 0) => new()
        {
            ResourceTypeId = id,
            ResourceTypeUid = $"uid-{id}",
            TypeName = typeName,
            ShortCode = shortCode,
            IconKey = iconKey,
            AllowCustomTags = true,
            CreatedOn = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ResourceCount = resourceCount,
            EntryPointTagCount = entryPointTagCount
        };

        private void SetupUsageRows(params ResourceTypeUsage[] rows) =>
            _repo.Setup(r => r.GetResourceTypesWithUsageAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(rows.ToList());

        private void SetupSaveResult(string result, int resourceTypeId) =>
            _repo.Setup(r => r.SaveResourceTypeAsync(It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((result, resourceTypeId));

        private void SetupDeleteResult(string result, int dependentCount) =>
            _repo.Setup(r => r.DeleteResourceTypeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((result, dependentCount));

        /// <summary>Records the arguments the service hands the repository's save, so a test can
        /// assert on the create/update discriminator and the trimming rules.</summary>
        private void CaptureSave(Action<int?, string, string, string?, string?, bool> capture,
            (string Result, int ResourceTypeId) result) =>
            _repo.Setup(r => r.SaveResourceTypeAsync(It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback((int? id, string uid, string name, string? code, string? icon, bool custom, CancellationToken _) =>
                    capture(id, uid, name, code, icon, custom))
                .ReturnsAsync(result);

        private void VerifySaveNeverCalled() =>
            _repo.Verify(r => r.SaveResourceTypeAsync(It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never,
                "because validation must short-circuit before the repository is called");

        private static bool HasError(List<Message>? errors, CallStatusCode expected) =>
            errors?.Any(e => e.CallStatus == expected) == true;

        private static bool HasErrorForField(List<Message>? errors, string propertyName) =>
            errors?.Any(e => string.Equals(e.PropertyName, propertyName, StringComparison.Ordinal)) == true;

        #endregion
    }
}
