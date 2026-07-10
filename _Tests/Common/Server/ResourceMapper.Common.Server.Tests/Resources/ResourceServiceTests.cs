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

            _repo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition>());
            _repo.Setup(r => r.GetAllEntryPointTemplatesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceTypeTag>());

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

        #region GetResourceEditorModelAsync

        [Fact]
        public async Task GetResourceEditorModelAsync_Create_ResourceTypesCarryTheIntId()
        {
            _repo.Setup(r => r.GetAllResourceTypesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceType>
                {
                    new() { ResourceTypeId = 5, TypeName = "AppService", ResourceTypeUid = "rt-uid-5" }
                });
            _repo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition>());

            var response = await _sut.GetResourceEditorModelAsync(new OpenEditorRequest { Mode = "Create" }, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a Create request always succeeds");
            var option = response.ApiResponse.Data!.ResourceTypes.Should().ContainSingle().Subject;
            option.ResourceTypeId.Should().Be(5, "because SaveResourceRequest/ResourceUniquenessRequest need the int id, not just the uid");
            option.TypeName.Should().Be("AppService");
            option.ResourceTypeUid.Should().Be("rt-uid-5");
        }

        [Fact]
        public async Task GetResourceEditorModelAsync_Create_PopulatesEntryPointTemplatesFromRepo()
        {
            _repo.Setup(r => r.GetAllResourceTypesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceType> { new() { ResourceTypeId = 5, TypeName = "AppService" } });
            _repo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition>());
            _repo.Setup(r => r.GetAllEntryPointTemplatesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceTypeTag>
                {
                    new() { ResourceTypeId = 5, TagDefinitionId = 100, IsDefaultPrimary = true, RequirementLevel = "Suggested" }
                });

            var response = await _sut.GetResourceEditorModelAsync(new OpenEditorRequest { Mode = "Create" }, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a Create request always succeeds");
            var template = response.ApiResponse.Data!.EntryPointTemplates.Should().ContainSingle().Subject;
            template.ResourceTypeId.Should().Be(5);
            template.TagDefinitionId.Should().Be(100);
            template.IsDefaultPrimary.Should().BeTrue("because the template row's default-primary flag must survive the projection");
        }

        [Fact]
        public async Task GetResourceEditorModelAsync_EditLoad_ExcludesDomainTagFromEditorModelTags()
        {
            _repo.Setup(r => r.GetAllResourceTypesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceType> { new() { ResourceTypeId = 5, TypeName = "AppService" } });
            _repo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition>
                {
                    new() { TagDefinitionId = 1, TagDefinitionKey = "Domain", ContentType = "Text", IsDomainTag = true },
                    new() { TagDefinitionId = 2, TagDefinitionKey = "Overview", ContentType = "Link", IsDomainTag = false }
                });
            _repo.Setup(r => r.GetResourceByUidAsync("uid-10", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = 10, ResourceUid = "uid-10", ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api", Domain = "non-prod" });
            _repo.Setup(r => r.GetTagsForResourceAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceTagRead>
                {
                    new() { TagDefinitionId = 1, TagDefinitionKey = "Domain", ContentType = "Text", TagValue = "non-prod" },
                    new() { TagDefinitionId = 2, TagDefinitionKey = "Overview", ContentType = "Link", TagValue = "https://example.com" }
                });
            _repo.Setup(r => r.GetRelationshipsForResourceAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceRelationshipItem>());

            var response = await _sut.GetResourceEditorModelAsync(new OpenEditorRequest { Mode = "Edit", ResourceUid = "uid-10" }, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the resource exists");
            var tags = response.ApiResponse.Data!.EditorModel.Tags;
            tags.Should().ContainSingle("because the domain tag row must be excluded from the editable Tags tab (RD1)");
            tags.Single().TagDefinitionId.Should().Be(2, "because only the non-domain Overview tag should remain");
        }

        [Fact]
        public async Task GetResourceEditorModelAsync_EditLoad_PopulatesOtherDomainOnDependencyRows()
        {
            _repo.Setup(r => r.GetAllResourceTypesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceType> { new() { ResourceTypeId = 5, TypeName = "AppService" } });
            _repo.Setup(r => r.GetResourceByUidAsync("uid-10", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = 10, ResourceUid = "uid-10", ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api", Domain = "non-prod" });
            _repo.Setup(r => r.GetTagsForResourceAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceTagRead>());
            _repo.Setup(r => r.GetRelationshipsForResourceAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceRelationshipItem>
                {
                    new() { RelationshipId = 1, Direction = "DependsOn", OtherResourceUid = "uid-20", OtherResourceName = "Redis", OtherResourceType = "Cache", OtherDomain = "non-prod" }
                });

            var response = await _sut.GetResourceEditorModelAsync(new OpenEditorRequest { Mode = "Edit", ResourceUid = "uid-10" }, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the resource exists");
            var row = response.ApiResponse.Data!.EditorModel.DependsOn.Should().ContainSingle().Subject;
            row.OtherDomain.Should().Be("non-prod", "because OtherDomain must be populated from the relationship read, not hard-null (closes the #5 TODO)");
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
            _repo.Setup(r => r.CheckResourceUniqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
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
            _repo.Setup(r => r.CheckResourceUniqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _repo.Setup(r => r.SaveResourceAsync(uid, 5, "orders-api", "Orders API", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
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
        public async Task SaveResourceAsync_WithDomain_PassesDomainThroughToRepositoryAndProjection()
        {
            var uid = Guid.NewGuid().ToString();
            string? capturedDomain = null;
            _repo.Setup(r => r.CheckResourceUniqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _repo.Setup(r => r.SaveResourceAsync(uid, 5, "orders-api", "Orders API", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Callback((string _, int _, string _, string _, string? _, string? domain, int? _, CancellationToken _) => capturedDomain = domain)
                .ReturnsAsync(("created", 10));
            _repo.Setup(r => r.GetResourceByUidAsync(uid, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = 10, ResourceUid = uid, ResourceKey = "orders-api", ResourceTypeId = 5, ResourceName = "Orders API", Domain = "non-prod" });
            _repo.Setup(r => r.GetAllResourceTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceType>());
            _repo.Setup(r => r.GetTagsForResourceAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceTagRead>());
            _repo.Setup(r => r.GetRelationshipsForResourceAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceRelationshipItem>());

            var request = new SaveResourceRequest { Mode = "Create", ResourceUid = uid, ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api", Domain = "non-prod" };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a valid, non-colliding save should succeed");
            capturedDomain.Should().Be("non-prod", "because the request's Domain must reach the repository");
            response.ApiResponse.Data!.Saved!.Domain.Should().Be("non-prod", "because the saved projection reflects the persisted domain");
        }

        [Fact]
        public async Task SaveResourceAsync_WithTags_WritesNonEmptyTagsExcludingDomain()
        {
            var uid = Guid.NewGuid().ToString();
            IReadOnlyList<(string TagKey, string TagValue)>? capturedTags = null;
            _repo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition>
                {
                    new() { TagDefinitionId = 1, TagDefinitionKey = "Domain", ContentType = "Text", IsDomainTag = true },
                    new() { TagDefinitionId = 2, TagDefinitionKey = "Overview", ContentType = "Link" },
                    new() { TagDefinitionId = 3, TagDefinitionKey = "Environment", ContentType = "Text" }
                });
            _repo.Setup(r => r.CheckResourceUniqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _repo.Setup(r => r.SaveResourceAsync(uid, 5, "orders-api", "Orders API", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("created", 10));
            _repo.Setup(r => r.SetResourceTagsAsync(10, It.IsAny<IReadOnlyList<(string, string)>>(), It.IsAny<CancellationToken>()))
                .Callback((int _, IReadOnlyList<(string TagKey, string TagValue)> tags, CancellationToken _) => capturedTags = tags)
                .Returns(Task.CompletedTask);
            _repo.Setup(r => r.GetResourceByUidAsync(uid, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = 10, ResourceUid = uid, ResourceKey = "orders-api", ResourceTypeId = 5, ResourceName = "Orders API" });
            _repo.Setup(r => r.GetAllResourceTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceType>());
            _repo.Setup(r => r.GetTagsForResourceAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceTagRead>());
            _repo.Setup(r => r.GetRelationshipsForResourceAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceRelationshipItem>());

            var request = new SaveResourceRequest
            {
                Mode = "Create", ResourceUid = uid, ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api",
                Tags = new List<SaveTagValue>
                {
                    new() { TagDefinitionId = 1, TagDefinitionKey = "Domain", Value = "non-prod" }, // dropped even if present
                    new() { TagDefinitionId = 2, TagDefinitionKey = "Overview", Value = "https://example.com" },
                    new() { TagDefinitionId = 3, TagDefinitionKey = "Environment", Value = "dev" },
                    new() { TagDefinitionId = 3, TagDefinitionKey = "Environment", Value = "" } // empty — dropped
                }
            };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the tags are all valid");
            capturedTags.Should().NotBeNull("because SetResourceTagsAsync must be called");
            capturedTags!.Select(t => t.TagKey).Should().BeEquivalentTo(new[] { "Overview", "Environment" },
                "because domain is excluded and the empty Environment row is dropped");
        }

        [Fact]
        public async Task SaveResourceAsync_RequiredTagMissing_ReturnsValidationErrorAndDoesNotCallRepo()
        {
            _repo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition>
                {
                    new() { TagDefinitionId = 2, TagDefinitionKey = "Overview", ContentType = "Link", RequirementLevel = "Error" }
                });
            _repo.Setup(r => r.CheckResourceUniqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var request = new SaveResourceRequest
            {
                Mode = "Create", ResourceUid = Guid.NewGuid().ToString(), ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api",
                Tags = new List<SaveTagValue>()
            };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a required (Error) tag has no value");
            VerifySaveRepoNeverCalled();
        }

        [Fact]
        public async Task SaveResourceAsync_LinkTagNotHttpOrHttps_ReturnsValidationErrorAndDoesNotCallRepo()
        {
            _repo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition>
                {
                    new() { TagDefinitionId = 2, TagDefinitionKey = "Overview", ContentType = "Link" }
                });
            _repo.Setup(r => r.CheckResourceUniqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var request = new SaveResourceRequest
            {
                Mode = "Create", ResourceUid = Guid.NewGuid().ToString(), ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api",
                Tags = new List<SaveTagValue> { new() { TagDefinitionId = 2, TagDefinitionKey = "Overview", Value = "javascript:alert(1)" } }
            };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a Link value must be a well-formed http/https URL");
            VerifySaveRepoNeverCalled();
        }

        [Fact]
        public async Task SaveResourceAsync_PrimaryDoesNotSurviveSave_PassesNullPrimaryToRepository()
        {
            var uid = Guid.NewGuid().ToString();
            int? capturedPrimary = -1;
            _repo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition>
                {
                    new() { TagDefinitionId = 2, TagDefinitionKey = "Overview", ContentType = "Link" }
                });
            _repo.Setup(r => r.CheckResourceUniqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _repo.Setup(r => r.SaveResourceAsync(uid, 5, "orders-api", "Orders API", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Callback((string _, int _, string _, string _, string? _, string? _, int? primary, CancellationToken _) => capturedPrimary = primary)
                .ReturnsAsync(("created", 10));
            _repo.Setup(r => r.GetResourceByUidAsync(uid, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = 10, ResourceUid = uid, ResourceKey = "orders-api", ResourceTypeId = 5, ResourceName = "Orders API" });
            _repo.Setup(r => r.GetAllResourceTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceType>());
            _repo.Setup(r => r.GetTagsForResourceAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceTagRead>());
            _repo.Setup(r => r.GetRelationshipsForResourceAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceRelationshipItem>());

            var request = new SaveResourceRequest
            {
                Mode = "Create", ResourceUid = uid, ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api",
                PrimaryTagDefinitionId = 2, // primary row was emptied/removed client-side — no matching Tags entry
                Tags = new List<SaveTagValue>()
            };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because an unset primary is not itself invalid");
            capturedPrimary.Should().BeNull("because a primary whose value didn't survive the save must be cleared (RD2)");
        }

        [Fact]
        public async Task SaveResourceAsync_PrimarySurvives_PassesPrimaryThroughToRepository()
        {
            var uid = Guid.NewGuid().ToString();
            int? capturedPrimary = null;
            _repo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition>
                {
                    new() { TagDefinitionId = 2, TagDefinitionKey = "Overview", ContentType = "Link" }
                });
            _repo.Setup(r => r.CheckResourceUniqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _repo.Setup(r => r.SaveResourceAsync(uid, 5, "orders-api", "Orders API", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Callback((string _, int _, string _, string _, string? _, string? _, int? primary, CancellationToken _) => capturedPrimary = primary)
                .ReturnsAsync(("created", 10));
            _repo.Setup(r => r.GetResourceByUidAsync(uid, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = 10, ResourceUid = uid, ResourceKey = "orders-api", ResourceTypeId = 5, ResourceName = "Orders API" });
            _repo.Setup(r => r.GetAllResourceTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceType>());
            _repo.Setup(r => r.GetTagsForResourceAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceTagRead>());
            _repo.Setup(r => r.GetRelationshipsForResourceAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceRelationshipItem>());

            var request = new SaveResourceRequest
            {
                Mode = "Create", ResourceUid = uid, ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api",
                PrimaryTagDefinitionId = 2,
                Tags = new List<SaveTagValue> { new() { TagDefinitionId = 2, TagDefinitionKey = "Overview", Value = "https://example.com" } }
            };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the primary tag has a valid surviving value");
            capturedPrimary.Should().Be(2, "because a Link primary with a surviving non-empty value passes through");
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

        #region SaveResourceAsync — relationship reconciliation (#8)

        [Fact]
        public async Task SaveResourceAsync_DependsOn_AddsMissingAndRemovesExtra()
        {
            var uid = Guid.NewGuid().ToString();
            SetupHappySaveStubs(uid, resourceId: 10, domain: "non-prod");

            _repo.Setup(r => r.GetResourceByUidAsync("uid-keep", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = 101, ResourceUid = "uid-keep", ResourceName = "Keep", Domain = "non-prod" });
            _repo.Setup(r => r.GetResourceByUidAsync("uid-add", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = 102, ResourceUid = "uid-add", ResourceName = "Add", Domain = "non-prod" });
            _repo.Setup(r => r.GetRelationshipsForResourceAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceRelationshipItem>
                {
                    new() { RelationshipId = 1, Direction = "DependsOn", OtherResourceId = 101 }, // stays
                    new() { RelationshipId = 2, Direction = "DependsOn", OtherResourceId = 999 }  // stale — removed
                });

            var request = new SaveResourceRequest
            {
                Mode = "Create", ResourceUid = uid, ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api",
                Domain = "non-prod",
                DependsOnUids = new List<string> { "uid-keep", "uid-add" }
            };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because both dependency targets are valid and same-domain");
            _repo.Verify(r => r.AddRelationshipAsync(10, 102, It.IsAny<CancellationToken>()), Times.Once,
                "because uid-add is newly desired and must be added");
            _repo.Verify(r => r.RemoveRelationshipAsync(10, 999, It.IsAny<CancellationToken>()), Times.Once,
                "because the stale edge to 999 is no longer desired and must be removed");
            _repo.Verify(r => r.AddRelationshipAsync(10, 101, It.IsAny<CancellationToken>()), Times.Never,
                "because uid-keep's edge already exists and must not be re-added");
            _repo.Verify(r => r.RemoveRelationshipAsync(10, 101, It.IsAny<CancellationToken>()), Times.Never,
                "because uid-keep is still desired and must not be removed");
        }

        [Fact]
        public async Task SaveResourceAsync_DependentOn_WritesTheFarSideEdge()
        {
            var uid = Guid.NewGuid().ToString();
            SetupHappySaveStubs(uid, resourceId: 10, domain: "non-prod");

            _repo.Setup(r => r.GetResourceByUidAsync("uid-x", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = 201, ResourceUid = "uid-x", ResourceName = "X", Domain = "non-prod" });
            _repo.Setup(r => r.GetRelationshipsForResourceAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceRelationshipItem>());

            var request = new SaveResourceRequest
            {
                Mode = "Create", ResourceUid = uid, ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api",
                Domain = "non-prod",
                DependentOnUids = new List<string> { "uid-x" }
            };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the dependent target is valid and same-domain");
            _repo.Verify(r => r.AddRelationshipAsync(201, 10, It.IsAny<CancellationToken>()), Times.Once,
                "because editing DependentOn writes the edge on the FAR side (from=target, to=this) — RD7");
            _repo.Verify(r => r.AddRelationshipAsync(10, 201, It.IsAny<CancellationToken>()), Times.Never,
                "because the edge direction must not be reversed");
        }

        [Fact]
        public async Task SaveResourceAsync_EmptyDependsOnList_RemovesAllDependsOnEdgesButLeavesDependentOnUntouched()
        {
            var uid = Guid.NewGuid().ToString();
            SetupHappySaveStubs(uid, resourceId: 10, domain: "non-prod");

            _repo.Setup(r => r.GetResourceByUidAsync("uid-y", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = 301, ResourceUid = "uid-y", ResourceName = "Y", Domain = "non-prod" });
            _repo.Setup(r => r.GetRelationshipsForResourceAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceRelationshipItem>
                {
                    new() { RelationshipId = 1, Direction = "DependsOn", OtherResourceId = 401 },
                    new() { RelationshipId = 2, Direction = "DependentOn", OtherResourceId = 301 }
                });

            var request = new SaveResourceRequest
            {
                Mode = "Create", ResourceUid = uid, ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api",
                Domain = "non-prod",
                DependsOnUids = new List<string>(), // empty — full-set-replace wipes this direction (RD3)
                DependentOnUids = new List<string> { "uid-y" } // unchanged from current
            };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue();
            _repo.Verify(r => r.RemoveRelationshipAsync(10, 401, It.IsAny<CancellationToken>()), Times.Once,
                "because an empty DependsOnUids removes every existing DependsOn edge");
            _repo.Verify(r => r.RemoveRelationshipAsync(301, 10, It.IsAny<CancellationToken>()), Times.Never,
                "because DependentOn's own edge is still desired and reconciling DependsOn must not touch it (RD1)");
            _repo.Verify(r => r.AddRelationshipAsync(301, 10, It.IsAny<CancellationToken>()), Times.Never,
                "because the DependentOn edge already exists and must not be re-added");
        }

        [Fact]
        public async Task SaveResourceAsync_CrossDomainDependsOnTarget_ReturnsValidationErrorAndDoesNotCallRepo()
        {
            var uid = Guid.NewGuid().ToString();
            _repo.Setup(r => r.CheckResourceUniqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _repo.Setup(r => r.GetResourceByUidAsync("uid-cross", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = 501, ResourceUid = "uid-cross", ResourceName = "Cross", Domain = "prod" });

            var request = new SaveResourceRequest
            {
                Mode = "Create", ResourceUid = uid, ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api",
                Domain = "non-prod",
                DependsOnUids = new List<string> { "uid-cross" }
            };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because the target is in a different domain (RD4)");
            VerifySaveRepoNeverCalled();
        }

        [Fact]
        public async Task SaveResourceAsync_SelfReferentialDependsOnUid_ReturnsValidationErrorAndDoesNotCallRepo()
        {
            var uid = Guid.NewGuid().ToString();
            _repo.Setup(r => r.CheckResourceUniqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var request = new SaveResourceRequest
            {
                Mode = "Create", ResourceUid = uid, ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api",
                Domain = "non-prod",
                DependsOnUids = new List<string> { uid } // self
            };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a resource cannot depend on itself (RD5)");
            VerifySaveRepoNeverCalled();
        }

        [Fact]
        public async Task SaveResourceAsync_UnknownDependsOnUid_ReturnsValidationErrorAndDoesNotCallRepo()
        {
            var uid = Guid.NewGuid().ToString();
            _repo.Setup(r => r.CheckResourceUniqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _repo.Setup(r => r.GetResourceByUidAsync("uid-missing", It.IsAny<CancellationToken>()))
                .ReturnsAsync((ResourceDetail?)null);

            var request = new SaveResourceRequest
            {
                Mode = "Create", ResourceUid = uid, ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api",
                Domain = "non-prod",
                DependsOnUids = new List<string> { "uid-missing" }
            };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because the target resource does not exist");
            VerifySaveRepoNeverCalled();
        }

        [Fact]
        public async Task SaveResourceAsync_DuplicateDependsOnUids_ResolvesOnceAndAddsOnce()
        {
            var uid = Guid.NewGuid().ToString();
            SetupHappySaveStubs(uid, resourceId: 10, domain: "non-prod");

            _repo.Setup(r => r.GetResourceByUidAsync("uid-dup", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = 601, ResourceUid = "uid-dup", ResourceName = "Dup", Domain = "non-prod" });
            _repo.Setup(r => r.GetRelationshipsForResourceAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceRelationshipItem>());

            var request = new SaveResourceRequest
            {
                Mode = "Create", ResourceUid = uid, ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api",
                Domain = "non-prod",
                DependsOnUids = new List<string> { "uid-dup", "uid-dup" } // duplicate — must collapse
            };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue();
            _repo.Verify(r => r.AddRelationshipAsync(10, 601, It.IsAny<CancellationToken>()), Times.Once,
                "because duplicate uids in one direction must collapse to a single edge write");
        }

        [Fact]
        public async Task SaveResourceAsync_EditMode_UsesPersistedDomainNotRequestDomainForRelationshipValidation()
        {
            var uid = Guid.NewGuid().ToString();
            var existing = new ResourceDetail { ResourceId = 10, ResourceUid = uid, ResourceKey = "orders-api", ResourceTypeId = 5, ResourceName = "Orders API", Domain = "non-prod" };
            _repo.Setup(r => r.GetResourceByUidAsync(uid, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
            _repo.Setup(r => r.CheckResourceUniqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _repo.Setup(r => r.SaveResourceAsync(uid, 5, "orders-api", "Orders API", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("updated", 10));
            _repo.Setup(r => r.GetAllResourceTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceType>());
            _repo.Setup(r => r.GetTagsForResourceAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceTagRead>());

            _repo.Setup(r => r.GetResourceByUidAsync("uid-same-domain", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = 701, ResourceUid = "uid-same-domain", ResourceName = "Same", Domain = "non-prod" });
            _repo.Setup(r => r.GetRelationshipsForResourceAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceRelationshipItem>());

            // Request omits Domain entirely (simulating a stale/partial client payload in Edit
            // mode) — the frozen-domain guard only fires when BOTH sides are non-empty, so this
            // slips past it; relationship validation must still use the PERSISTED domain
            // ("non-prod"), not the empty request.Domain (RD14).
            var request = new SaveResourceRequest
            {
                Mode = "Edit", ResourceUid = uid, ResourceTypeId = 5, ResourceName = "Orders API", ResourceKey = "orders-api",
                Domain = null,
                DependsOnUids = new List<string> { "uid-same-domain" }
            };

            var response = await _sut.SaveResourceAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the target shares the persisted domain, even though request.Domain was empty");
            _repo.Verify(r => r.AddRelationshipAsync(10, 701, It.IsAny<CancellationToken>()), Times.Once,
                "because same-domain validation must use existing.Domain, not the empty request.Domain");
        }

        #endregion

        #region SearchResourcesForPickerAsync

        [Fact]
        public async Task SearchResourcesForPickerAsync_ForwardsDomainAndExcludeToRepository()
        {
            ResourcePickerRequest? captured = null;
            _repo.Setup(r => r.SearchResourcesForPickerAsync(It.IsAny<ResourcePickerRequest>(), It.IsAny<CancellationToken>()))
                .Callback((ResourcePickerRequest req, CancellationToken _) => captured = req)
                .ReturnsAsync((new List<ResourcePickerItem>(), 0));

            var request = new ResourcePickerRequest { SearchFor = "orders", Domain = "non-prod", ExcludeResourceUid = "self-uid", Skip = 0, Take = 20 };

            var response = await _sut.SearchResourcesForPickerAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue();
            captured.Should().NotBeNull();
            captured!.Domain.Should().Be("non-prod");
            captured!.ExcludeResourceUid.Should().Be("self-uid");
            captured!.SearchFor.Should().Be("orders");
        }

        [Fact]
        public async Task SearchResourcesForPickerAsync_OutOfRangeTake_ClampsToDefault()
        {
            ResourcePickerRequest? captured = null;
            _repo.Setup(r => r.SearchResourcesForPickerAsync(It.IsAny<ResourcePickerRequest>(), It.IsAny<CancellationToken>()))
                .Callback((ResourcePickerRequest req, CancellationToken _) => captured = req)
                .ReturnsAsync((new List<ResourcePickerItem>(), 0));

            var request = new ResourcePickerRequest { Take = 99999 };

            await _sut.SearchResourcesForPickerAsync(request, CancellationToken.None);

            captured!.Take.Should().Be(20, "because an out-of-range Take falls back to the default rather than being forwarded as-is");
        }

        #endregion

        #region CheckUniquenessAsync

        [Fact]
        public async Task CheckUniquenessAsync_Unique_ReturnsIsUniqueTrue()
        {
            _repo.Setup(r => r.CheckResourceUniqueAsync(5, "orders-api", null, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _repo.Setup(r => r.GetAllResourceTypesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceType> { new() { ResourceTypeId = 5, TypeName = "AppService" } });

            var response = await _sut.CheckUniquenessAsync(new ResourceUniquenessRequest { ResourceTypeId = 5, ResourceKey = "orders-api" }, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a valid uniqueness check always succeeds, unique or not");
            response.ApiResponse.Data!.IsUnique.Should().BeTrue("because no other resource uses this (type, key)");
        }

        [Fact]
        public async Task CheckUniquenessAsync_Collision_ReturnsIsUniqueFalseWithMessage()
        {
            _repo.Setup(r => r.CheckResourceUniqueAsync(5, "orders-api", null, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
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
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never,
                "because validation must short-circuit before the repository is called");
        }

        /// <summary>Stubs the common Create-mode SaveResourceAsync happy-path chain (uniqueness,
        /// the repo save call, and the post-save projection reads) so a relationship-focused test
        /// only has to add its own dependency-specific stubs.</summary>
        private void SetupHappySaveStubs(string uid, int resourceId, string? domain)
        {
            _repo.Setup(r => r.CheckResourceUniqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _repo.Setup(r => r.SaveResourceAsync(uid, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("created", resourceId));
            _repo.Setup(r => r.GetResourceByUidAsync(uid, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceDetail { ResourceId = resourceId, ResourceUid = uid, ResourceKey = "orders-api", ResourceTypeId = 5, ResourceName = "Orders API", Domain = domain });
            _repo.Setup(r => r.GetAllResourceTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceType>());
            _repo.Setup(r => r.GetTagsForResourceAsync(resourceId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ResourceTagRead>());
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
