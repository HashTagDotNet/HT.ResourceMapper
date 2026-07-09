using ResourceMapper.Common.Server.Resources;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Server.Resources.Models;
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

        #region helpers

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
