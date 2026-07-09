using ResourceMapper.Common.Server.Resources;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Server.Resources.Models;
using ResourceMapper.Common.Shared.Editor;
using ResourceMapper.Common.Shared.Editor.Contracts;
using ResourceMapper.Common.Shared.HomePage.Contracts;

// ReSharper disable InconsistentNaming

namespace ResourceMapper.Common.Server.Tests.Resources
{
    /// <summary>
    /// Filtering behaviour of ResourceService: filters are forwarded (not dropped), the §5b validation
    /// guards, and facet mapping / same-dimension stripping.
    /// </summary>
    [Trait("Category", "Unit")]
    [Trait("Category", "ResourceMapper")]
    [Trait("Category", "ResourceMapper/Common")]
    [Trait("Category", "ResourceMapper/Common/Server")]
    [Trait("Category", "ResourceMapper/Common/Server/Resources")]
    [Trait("Category", "ResourceMapper/Common/Server/Resources/ResourceService")]
    public class ResourceServiceTests
    {
        private readonly Mock<IResourceRepository> _repo;
        private readonly ResourceService _sut;

        public ResourceServiceTests()
        {
            _repo = new Mock<IResourceRepository>();

            _repo.Setup(r => r.GetResourceGridItemsAsync(
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<IReadOnlyList<ResourceGridFilterDefinition>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((0, new List<ResourceGridItem>()));

            _repo.Setup(r => r.GetFilterValuesAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<IReadOnlyList<ResourceGridFilterDefinition>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceGridFacetItem>());

            _sut = new ResourceService(_repo.Object);
        }

        #region grid: filter forwarding

        [Fact]
        public async Task GetResourceGridItems_WithFilters_ForwardsFiltersToRepository()
        {
            IReadOnlyList<ResourceGridFilterDefinition>? captured = null;
            _repo.Setup(r => r.GetResourceGridItemsAsync(
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<IReadOnlyList<ResourceGridFilterDefinition>?>(), It.IsAny<CancellationToken>()))
                .Callback((string? _, string? _, string? _, int _, int _, int _, IReadOnlyList<ResourceGridFilterDefinition>? f, CancellationToken _) => captured = f)
                .ReturnsAsync((0, new List<ResourceGridItem>()));

            var request = GridRequest(TypeEquals("Storage", "Compute"));

            var response = await _sut.GetResourceGridItems(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a valid request should succeed");
            captured.Should().ContainSingle("because the one constraining filter must reach the repository (regression: filters used to be dropped)");
            captured![0].Values.Should().BeEquivalentTo(new[] { "Compute", "Storage" }, "because the selected values must be forwarded");
        }

        [Fact]
        public async Task GetResourceGridItems_EmptyValuesFilter_IsDroppedBeforeRepository()
        {
            IReadOnlyList<ResourceGridFilterDefinition>? captured = null;
            SetupCapture(f => captured = f);

            var emptyEnum = new ResourceGridFilterDefinition
            {
                Column = "ResourceType", Kind = ResourceGridFilterKind.Enumerable,
                Operator = ResourceGridFilterOperator.Equals, Values = new List<string>()
            };
            var blankText = new ResourceGridFilterDefinition
            {
                Column = "ResourceName", Kind = ResourceGridFilterKind.Text,
                Operator = ResourceGridFilterOperator.Contains, Text = "   "
            };

            await _sut.GetResourceGridItems(GridRequest(emptyEnum, blankText), CancellationToken.None);

            captured.Should().BeEmpty("because non-constraining filters (empty selection / blank text) are dropped");
        }

        [Fact]
        public async Task GetResourceGridItems_IncludeBlankOnly_IsForwardedAsConstraining()
        {
            IReadOnlyList<ResourceGridFilterDefinition>? captured = null;
            SetupCapture(f => captured = f);

            var blankBucket = new ResourceGridFilterDefinition
            {
                Column = "ResourceType", Kind = ResourceGridFilterKind.Enumerable,
                Operator = ResourceGridFilterOperator.Equals, Values = new List<string>(), IncludeBlank = true
            };

            await _sut.GetResourceGridItems(GridRequest(blankBucket), CancellationToken.None);

            captured.Should().ContainSingle("because selecting the blank bucket is a real constraint");
            captured![0].IncludeBlank.Should().BeTrue("because IncludeBlank must survive sanitization");
        }

        #endregion

        #region grid: validation guards (§5b)

        [Fact]
        public async Task GetResourceGridItems_MoreThanFourFilters_ReturnsValidationErrorAndDoesNotCallRepo()
        {
            var filters = new[]
            {
                TypeEquals("a"), TagEquals("Environment", "dev"), TagEquals("Owner", "me"),
                new ResourceGridFilterDefinition { Column = "ResourceName", Kind = ResourceGridFilterKind.Text, Operator = ResourceGridFilterOperator.Contains, Text = "x" },
                new ResourceGridFilterDefinition { Column = "Description", Kind = ResourceGridFilterKind.Text, Operator = ResourceGridFilterOperator.Contains, Text = "y" }
            };

            var response = await _sut.GetResourceGridItems(GridRequest(filters), CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because more than four filters is not allowed");
            VerifyGridRepoNeverCalled();
        }

        [Fact]
        public async Task GetResourceGridItems_UnknownColumn_ReturnsValidationError()
        {
            var bad = new ResourceGridFilterDefinition
            {
                Column = "Nonsense", Kind = ResourceGridFilterKind.Enumerable,
                Operator = ResourceGridFilterOperator.Equals, Values = new List<string> { "x" }
            };

            var response = await _sut.GetResourceGridItems(GridRequest(bad), CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because an unknown filter column must be rejected");
            VerifyGridRepoNeverCalled();
        }

        [Fact]
        public async Task GetResourceGridItems_TagFilterMissingTagKey_ReturnsValidationError()
        {
            var bad = new ResourceGridFilterDefinition
            {
                Column = "Tag", TagKey = null, Kind = ResourceGridFilterKind.Enumerable,
                Operator = ResourceGridFilterOperator.Equals, Values = new List<string> { "dev" }
            };

            var response = await _sut.GetResourceGridItems(GridRequest(bad), CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a Tag filter without a TagKey is invalid");
            VerifyGridRepoNeverCalled();
        }

        [Fact]
        public async Task GetResourceGridItems_OverLongFilterValue_ReturnsValidationError()
        {
            var bad = TypeEquals(new string('x', 2001));

            var response = await _sut.GetResourceGridItems(GridRequest(bad), CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a filter value longer than the column width must be rejected");
            VerifyGridRepoNeverCalled();
        }

        [Fact]
        public async Task GetResourceGridItems_InvalidOrderBy_ReturnsValidationError()
        {
            var request = GridRequest();
            request.OrderBy = "DROP TABLE";

            var response = await _sut.GetResourceGridItems(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because OrderBy must be a whitelisted sortable column");
            VerifyGridRepoNeverCalled();
        }

        [Theory]
        [InlineData(25, 100)]
        [InlineData(50, 50)]
        [InlineData(100, 100)]
        [InlineData(500, 500)]
        [InlineData(9999, 100)]
        public async Task GetResourceGridItems_Take_IsClampedToAllowedValues(int requestedTake, int expectedTake)
        {
            int captured = -1;
            _repo.Setup(r => r.GetResourceGridItemsAsync(
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<IReadOnlyList<ResourceGridFilterDefinition>?>(), It.IsAny<CancellationToken>()))
                .Callback((string? _, string? _, string? _, int _, int take, int _, IReadOnlyList<ResourceGridFilterDefinition>? _, CancellationToken _) => captured = take)
                .ReturnsAsync((0, new List<ResourceGridItem>()));

            var request = GridRequest();
            request.Take = requestedTake;

            await _sut.GetResourceGridItems(request, CancellationToken.None);

            captured.Should().Be(expectedTake, "because Take must be clamped to one of the allowed display counts");
        }

        #endregion

        #region facet

        [Fact]
        public async Task GetResourceGridFacet_MapsBlankBucketToDisplayAndFlag()
        {
            _repo.Setup(r => r.GetFilterValuesAsync("ResourceType", null, It.IsAny<string?>(),
                    It.IsAny<IReadOnlyList<ResourceGridFilterDefinition>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceGridFacetItem>
                {
                    new() { IsBlank = true, Value = null, ItemCount = 3 },
                    new() { IsBlank = false, Value = "Storage", ItemCount = 5 }
                });

            var response = await _sut.GetResourceGridFacet(
                new ResourceGridFacetRequest { Column = "ResourceType" }, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a ResourceType facet request is valid");
            var values = response.ApiResponse.Data!.Values;
            values.Should().HaveCount(2, "because both rows must be mapped");
            values.Single(v => v.IsBlank).Display.Should().Be("(blank)", "because the blank bucket renders as (blank)");
            values.Single(v => !v.IsBlank).Display.Should().Be("Storage", "because ordinary values display their value");
        }

        [Fact]
        public async Task GetResourceGridFacet_StripsSameDimensionFilter()
        {
            IReadOnlyList<ResourceGridFilterDefinition>? captured = null;
            _repo.Setup(r => r.GetFilterValuesAsync("ResourceType", null, It.IsAny<string?>(),
                    It.IsAny<IReadOnlyList<ResourceGridFilterDefinition>?>(), It.IsAny<CancellationToken>()))
                .Callback((string _, string? _, string? _, IReadOnlyList<ResourceGridFilterDefinition>? f, CancellationToken _) => captured = f)
                .ReturnsAsync(new List<ResourceGridFacetItem>());

            var request = new ResourceGridFacetRequest
            {
                Column = "ResourceType",
                Filters = new List<ResourceGridFilterDefinition> { TypeEquals("Storage"), TagEquals("Environment", "dev") }
            };

            await _sut.GetResourceGridFacet(request, CancellationToken.None);

            captured.Should().ContainSingle("because the facet's own ResourceType filter must be stripped, leaving the Tag filter");
            captured![0].Column.Should().Be("Tag", "because only the other-dimension filter should reach the repository");
        }

        [Fact]
        public async Task GetResourceGridFacet_TagFacetMissingTagKey_ReturnsValidationError()
        {
            var response = await _sut.GetResourceGridFacet(
                new ResourceGridFacetRequest { Column = "Tag", TagKey = null }, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a Tag facet requires a TagKey");
        }

        [Fact]
        public async Task GetResourceGridFacet_UnknownColumn_ReturnsValidationError()
        {
            var response = await _sut.GetResourceGridFacet(
                new ResourceGridFacetRequest { Column = "ResourceName" }, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because only ResourceType and Tag are facetable");
        }

        #endregion

        #region GetResourceDetailAsync

        [Fact]
        public async Task GetResourceDetailAsync_ResourceNotFound_ReturnsNotFound()
        {
            _repo.Setup(r => r.GetResourceByUidAsync("missing-uid", It.IsAny<CancellationToken>()))
                .ReturnsAsync((ResourceDetail?)null);

            var response = await _sut.GetResourceDetailAsync("missing-uid", CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a resource that doesn't exist cannot be retrieved");
        }

        [Fact]
        public async Task GetResourceDetailAsync_HappyPath_ProjectsTagsRelationshipsAndComputesPrimaryLinkAndIdentity()
        {
            var detail = new ResourceDetail
            {
                ResourceId = 10,
                ResourceUid = "uid-10",
                ResourceKey = "orders-api",
                ResourceTypeId = 5,
                ResourceName = "Orders API",
                Description = "desc",
                PrimaryTagDefinitionId = 100,
                CreatedOn = new DateTime(2026, 1, 1)
            };

            _repo.Setup(r => r.GetResourceByUidAsync("uid-10", It.IsAny<CancellationToken>())).ReturnsAsync(detail);
            _repo.Setup(r => r.GetAllResourceTypesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceType> { new() { ResourceTypeId = 5, TypeName = "AppService" } });
            _repo.Setup(r => r.GetTagsForResourceAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceTagRead>
                {
                    new() { TagDefinitionId = 100, TagDefinitionKey = "Overview", ContentType = "Link", TagValue = "https://example.com", IsPrimary = true },
                    new() { TagDefinitionId = 101, TagDefinitionKey = "Environment", ContentType = "Text", TagValue = "dev", IsPrimary = false }
                });
            _repo.Setup(r => r.GetRelationshipsForResourceAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceRelationshipItem>
                {
                    new() { RelationshipId = 1, Direction = "DependsOn", OtherResourceUid = "uid-20", OtherResourceName = "Redis", OtherResourceType = "Cache" }
                });

            var response = await _sut.GetResourceDetailAsync("uid-10", CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the resource exists");
            var model = response.ApiResponse.Data!;
            model.Tags.Should().HaveCount(2, "because both applied tags must be projected");
            model.PrimaryLinkUrl.Should().Be("https://example.com", "because the primary tag's value is the resource's primary link");
            model.IdentityDisplay.Should().Be("AppService / orders-api", "because Domain is not yet resolved (deferred to slice #5)");
            model.Relationships.Should().ContainSingle("because the one relationship edge must be projected");
        }

        #endregion

        #region SaveResourceAsync

        [Fact]
        public async Task SaveResourceAsync_MissingRequiredFields_ReturnsValidationErrorAndDoesNotCallRepo()
        {
            var request = new SaveResourceRequest { Mode = "Create", ResourceUid = "", ResourceTypeId = 0, ResourceName = "", ResourceKey = "" };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because type/name/key/uid are all missing");
            VerifySaveRepoNeverCalled();
        }

        [Fact]
        public async Task SaveResourceAsync_KeyCollision_ReturnsValidationErrorAndDoesNotCallRepo()
        {
            _repo.Setup(r => r.CheckResourceUniqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var request = new SaveResourceRequest { Mode = "Create", ResourceUid = Guid.NewGuid().ToString(), ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api" };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because the (type, key) pair collides with another resource");
            VerifySaveRepoNeverCalled();
        }

        [Fact]
        public async Task SaveResourceAsync_HappyPath_CallsRepoAndReturnsSavedToastMessage()
        {
            var uid = Guid.NewGuid().ToString();
            _repo.Setup(r => r.CheckResourceUniqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _repo.Setup(r => r.SaveResourceAsync(uid, 5, "orders-api", "Orders API", It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("created", 10));
            _repo.Setup(r => r.GetResourceByUidAsync(uid, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = 10, ResourceUid = uid, ResourceKey = "orders-api", ResourceTypeId = 5, ResourceName = "Orders API" });
            _repo.Setup(r => r.GetAllResourceTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceType>());
            _repo.Setup(r => r.GetTagsForResourceAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceTagRead>());
            _repo.Setup(r => r.GetRelationshipsForResourceAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceRelationshipItem>());

            var request = new SaveResourceRequest { Mode = "Create", ResourceUid = uid, ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api" };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a valid, non-colliding save should succeed");
            response.ApiResponse.Data!.Message.Should().Be("Saved Orders API", "because the toast message names the saved resource");
            response.ApiResponse.Data!.ResourceUid.Should().Be(uid, "because the response echoes the resource's uid");
        }

        [Fact]
        public async Task SaveResourceAsync_EditWithChangedType_ReturnsValidationErrorAndDoesNotCallRepo()
        {
            var uid = Guid.NewGuid().ToString();
            _repo.Setup(r => r.GetResourceByUidAsync(uid, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = 10, ResourceUid = uid, ResourceKey = "orders-api", ResourceTypeId = 5, ResourceName = "Orders API" });

            var request = new SaveResourceRequest { Mode = "Edit", ResourceUid = uid, ResourceTypeId = 6, ResourceName = "Orders API", ResourceKey = "orders-api" };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because Type is read-only after save");
            VerifySaveRepoNeverCalled();
        }

        #endregion

        #region CheckUniquenessAsync

        [Fact]
        public async Task CheckUniquenessAsync_Unique_ReturnsIsUniqueTrue()
        {
            _repo.Setup(r => r.CheckResourceUniqueAsync(5, "orders-api", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _repo.Setup(r => r.GetAllResourceTypesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceType> { new() { ResourceTypeId = 5, TypeName = "AppService" } });

            var response = await _sut.CheckUniquenessAsync(new ResourceUniquenessRequest { ResourceTypeId = 5, ResourceKey = "orders-api" }, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a valid uniqueness check always succeeds, unique or not");
            response.ApiResponse.Data!.IsUnique.Should().BeTrue("because no other resource uses this (type, key)");
        }

        [Fact]
        public async Task CheckUniquenessAsync_Collision_ReturnsIsUniqueFalseWithMessage()
        {
            _repo.Setup(r => r.CheckResourceUniqueAsync(5, "orders-api", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _repo.Setup(r => r.GetAllResourceTypesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceType> { new() { ResourceTypeId = 5, TypeName = "AppService" } });

            var response = await _sut.CheckUniquenessAsync(new ResourceUniquenessRequest { ResourceTypeId = 5, ResourceKey = "orders-api" }, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a collision is a normal (not erroneous) uniqueness result");
            response.ApiResponse.Data!.IsUnique.Should().BeFalse("because another resource of this type already uses this key");
            response.ApiResponse.Data!.Message.Should().NotBeNullOrEmpty("because a collision must surface an inline message");
        }

        #endregion

        #region CreateTagDefinitionAsync

        [Fact]
        public async Task CreateTagDefinitionAsync_BadContentType_ReturnsValidationErrorAndDoesNotCallRepo()
        {
            var request = new CreateTagDefinitionRequest { TagDefinitionKey = "Foo", ContentType = "Bogus" };

            var response = await _sut.CreateTagDefinitionAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because ContentType must be 'Text' or 'Link'");
            _repo.Verify(r => r.CreateTagDefinitionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never, "because validation must short-circuit before the repository is called");
        }

        [Fact]
        public async Task CreateTagDefinitionAsync_ExistingKey_ReturnsExistingDefinitionWithDomainAndSystemOff()
        {
            _repo.Setup(r => r.CreateTagDefinitionAsync("Overview", It.IsAny<string>(), "Link", It.IsAny<bool>(), It.IsAny<bool>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("skipped", 42));
            _repo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition> { new() { TagDefinitionId = 42, TagDefinitionKey = "Overview", ContentType = "Link", IsDomainTag = false, IsSystemTag = false } });

            var request = new CreateTagDefinitionRequest { TagDefinitionKey = "Overview", ContentType = "Link" };

            var response = await _sut.CreateTagDefinitionAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a duplicate-key create resolves to the existing definition, not an error");
            response.ApiResponse.Data!.TagDefinitionId.Should().Be(42, "because the existing definition's id must be returned, search-existing-first");
            response.ApiResponse.Data!.IsDomainTag.Should().BeFalse("because inline create can never produce a domain tag");
            response.ApiResponse.Data!.IsSystemTag.Should().BeFalse("because inline create can never produce a system tag");
        }

        [Fact]
        public async Task CreateTagDefinitionAsync_NewKey_ReturnsCreatedDefinition()
        {
            _repo.Setup(r => r.CreateTagDefinitionAsync("Kusto", It.IsAny<string>(), "Link", It.IsAny<bool>(), It.IsAny<bool>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("created", 43));
            _repo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition> { new() { TagDefinitionId = 43, TagDefinitionKey = "Kusto", ContentType = "Link" } });

            var request = new CreateTagDefinitionRequest { TagDefinitionKey = "Kusto", ContentType = "Link" };

            var response = await _sut.CreateTagDefinitionAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a new key creates a new definition");
            response.ApiResponse.Data!.TagDefinitionId.Should().Be(43, "because the newly created definition's id must be returned");
        }

        #endregion

        #region AddRelationshipAsync

        [Fact]
        public async Task AddRelationshipAsync_MissingEndpoint_ReturnsValidationErrorAndDoesNotCallRepo()
        {
            var response = await _sut.AddRelationshipAsync("", "uid-2", CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because both endpoints are required");
            _repo.Verify(r => r.AddRelationshipAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never,
                "because validation must short-circuit before the repository is called");
        }

        [Fact]
        public async Task AddRelationshipAsync_SelfLoop_ReturnsValidationErrorAndDoesNotCallRepo()
        {
            var response = await _sut.AddRelationshipAsync("uid-1", "uid-1", CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a resource cannot depend on itself");
            _repo.Verify(r => r.AddRelationshipAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never,
                "because a self-loop must be rejected before the repository is called");
        }

        [Fact]
        public async Task AddRelationshipAsync_HappyPath_ResolvesUidsAndCallsRepo()
        {
            _repo.Setup(r => r.GetResourceByUidAsync("uid-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = 1, ResourceUid = "uid-1" });
            _repo.Setup(r => r.GetResourceByUidAsync("uid-2", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = 2, ResourceUid = "uid-2" });

            var response = await _sut.AddRelationshipAsync("uid-1", "uid-2", CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because both endpoints exist and are distinct");
            _repo.Verify(r => r.AddRelationshipAsync(1, 2, It.IsAny<CancellationToken>()), Times.Once,
                "because the resolved resource ids must reach the repository");
        }

        #endregion

        #region helpers

        private void VerifySaveRepoNeverCalled()
        {
            _repo.Verify(r => r.SaveResourceAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never,
                "because validation must short-circuit before the repository is called");
        }

        private void SetupCapture(Action<IReadOnlyList<ResourceGridFilterDefinition>?> capture)
        {
            _repo.Setup(r => r.GetResourceGridItemsAsync(
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<IReadOnlyList<ResourceGridFilterDefinition>?>(), It.IsAny<CancellationToken>()))
                .Callback((string? _, string? _, string? _, int _, int _, int _, IReadOnlyList<ResourceGridFilterDefinition>? f, CancellationToken _) => capture(f))
                .ReturnsAsync((0, new List<ResourceGridItem>()));
        }

        private void VerifyGridRepoNeverCalled()
        {
            _repo.Verify(r => r.GetResourceGridItemsAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<IReadOnlyList<ResourceGridFilterDefinition>?>(), It.IsAny<CancellationToken>()), Times.Never,
                "because validation must short-circuit before the repository is called");
        }

        private static ResourceGridRequest GridRequest(params ResourceGridFilterDefinition[] filters) => new()
        {
            Skip = 0,
            Take = 100,
            OrderBy = "ResourceName",
            OrderDirection = "Asc",
            Filters = filters.ToList()
        };

        private static ResourceGridFilterDefinition TypeEquals(params string[] values) => new()
        {
            Column = "ResourceType",
            Kind = ResourceGridFilterKind.Enumerable,
            Operator = ResourceGridFilterOperator.Equals,
            Values = values.ToList()
        };

        private static ResourceGridFilterDefinition TagEquals(string key, params string[] values) => new()
        {
            Column = "Tag",
            TagKey = key,
            Kind = ResourceGridFilterKind.Enumerable,
            Operator = ResourceGridFilterOperator.Equals,
            Values = values.ToList()
        };

        #endregion
    }
}
