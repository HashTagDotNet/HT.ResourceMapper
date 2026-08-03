using System.Text.Json;
using ResourceMapper.Common.Server.Resources;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Server.Resources.Models;
using ResourceMapper.Common.Shared.Export.Contracts;
using ResourceMapper.Common.Shared.Import.Contracts;

// ReSharper disable InconsistentNaming

namespace ResourceMapper.Common.Server.Tests.Resources
{
    /// <summary>
    /// Shape rules for the exported document. The overriding constraint is that the output must be
    /// something ImportService accepts unchanged, so most of these assert on details import is
    /// strict about: array-vs-string tag values, the domain tag being present per resource, and
    /// dependency keys being resolvable within the dependent's own domain.
    /// </summary>
    [Trait("Category", "Unit")]
    [Trait("Category", "ResourceMapper")]
    [Trait("Category", "ResourceMapper/Common")]
    [Trait("Category", "ResourceMapper/Common/Server")]
    [Trait("Category", "ResourceMapper/Common/Server/Resources")]
    [Trait("Category", "ResourceMapper/Common/Server/Resources/ExportService")]
    public class ExportServiceTests
    {
        private const string DomainKey = "Domain";

        private readonly Mock<IExportRepository> _repo;
        private readonly ExportService _sut;

        public ExportServiceTests()
        {
            _repo = new Mock<IExportRepository>();

            _repo.Setup(r => r.GetResourcesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ExportResourceRow>());
            _repo.Setup(r => r.GetResourceTagsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ExportTagRow>());
            _repo.Setup(r => r.GetDependenciesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ExportDependencyRow>());
            _repo.Setup(r => r.GetResourceTypesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceType>());
            _repo.Setup(r => r.GetTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition>());

            _sut = new ExportService(_repo.Object);
        }

        [Fact]
        public async Task ExportAsync_EmptyCatalog_ReturnsEmptyDocumentWithVersionAndPolicy()
        {
            // Act
            var response = await _sut.ExportAsync(new ExportOptions(), CancellationToken.None);

            // Assert
            var doc = response.ApiResponse.Data!.Document;
            doc.Version.Should().Be("1.0", "because import rejects any version other than 1.0");
            doc.Policy.OnConflict.Should().Be("upsert", "because upsert is the default round-trip policy");
            doc.Resources.Should().BeEmpty("because the catalog contains no resources");
        }

        [Fact]
        public async Task ExportAsync_Resource_CopiesIdentityFieldsIncludingUid()
        {
            // Arrange
            GivenResources(Row(1, "KEY-A", "Type A", "Name A", "Desc A", "prod", uid: "res-abc"));

            // Act
            var doc = await ExportDocumentAsync();

            // Assert
            var item = doc.Resources.Single();
            item.Uid.Should().Be("res-abc", "because decision #5 requires uid in the export output");
            item.Key.Should().Be("KEY-A", "because key is the field import matches on");
            item.Type.Should().Be("Type A", "because type is identity-bearing on import");
            item.Name.Should().Be("Name A", "because name is a required import field");
            item.Description.Should().Be("Desc A", "because description round-trips verbatim");
        }

        [Fact]
        public async Task ExportAsync_BlankDescription_OmitsDescription()
        {
            // Arrange
            GivenResources(Row(1, "KEY-A", "Type A", "Name A", description: "   "));

            // Act
            var doc = await ExportDocumentAsync();

            // Assert
            doc.Resources.Single().Description.Should()
                .BeNull("because a whitespace-only description should not be emitted as a value");
        }

        [Fact]
        public async Task ExportAsync_SingleValuedTag_EmitsBareString()
        {
            // Arrange
            GivenResources(Row(1, "KEY-A", "Type A", "Name A"));
            GivenTags(Tag(1, "owner", "team-a", isMultiValued: false));

            // Act
            var doc = await ExportDocumentAsync();

            // Assert
            doc.Resources.Single().Tags!["owner"].Should()
                .BeOfType<string>("because import rejects an array supplied for a single-valued definition")
                .And.Be("team-a", "because the stored value must survive the export unchanged");
        }

        [Fact]
        public async Task ExportAsync_MultiValuedTag_EmitsArrayOfAllValues()
        {
            // Arrange
            GivenResources(Row(1, "KEY-A", "Type A", "Name A"));
            GivenTags(
                Tag(1, "owner", "team-a", isMultiValued: true),
                Tag(1, "owner", "team-b", isMultiValued: true));

            // Act
            var doc = await ExportDocumentAsync();

            // Assert
            doc.Resources.Single().Tags!["owner"].Should()
                .BeEquivalentTo(new[] { "team-a", "team-b" },
                    "because decision #8 emits multi-valued tags as an array");
        }

        [Fact]
        public async Task ExportAsync_MultiValuedTagWithOneValue_StillEmitsArray()
        {
            // Arrange
            GivenResources(Row(1, "KEY-A", "Type A", "Name A"));
            GivenTags(Tag(1, "owner", "team-a", isMultiValued: true));

            // Act
            var doc = await ExportDocumentAsync();

            // Assert
            doc.Resources.Single().Tags!["owner"].Should()
                .BeAssignableTo<List<string>>("because the definition, not the current value count, decides the shape");
        }

        [Fact]
        public async Task ExportAsync_SingleValuedTagWithMultipleRows_EmitsFirstValueAndWarns()
        {
            // Arrange — ResourceTag has no unique constraint on (ResourceId, TagDefinitionId).
            GivenResources(Row(1, "KEY-A", "Type A", "Name A"));
            GivenTags(
                Tag(1, "owner", "team-a", isMultiValued: false),
                Tag(1, "owner", "team-b", isMultiValued: false));

            // Act
            var result = await ExportResultAsync();

            // Assert
            result.Document.Resources.Single().Tags!["owner"].Should()
                .Be("team-a", "because emitting an array for a single-valued tag would fail import validation");
            result.Warnings.Should().ContainSingle(w => w.Contains("single-valued tag"),
                "because dropping the extra value is a fidelity loss the caller must be told about");
        }

        [Fact]
        public async Task ExportAsync_DomainTag_IsIncludedInTheResourceTagMap()
        {
            // Arrange
            GivenResources(Row(1, "KEY-A", "Type A", "Name A", domain: "prod"));
            GivenTags(Tag(1, DomainKey, "prod", isMultiValued: false, isDomainTag: true));

            // Act
            var doc = await ExportDocumentAsync();

            // Assert
            doc.Resources.Single().Tags.Should().ContainKey(DomainKey,
                "because the per-resource domain tag is what re-establishes (Domain+Type+Key) identity on re-import");
            doc.Resources.Single().Tags![DomainKey].Should()
                .Be("prod", "because the domain value must survive the round-trip exactly");
        }

        [Fact]
        public async Task ExportAsync_ResourceWithNoTags_OmitsTagMap()
        {
            // Arrange
            GivenResources(Row(1, "KEY-A", "Type A", "Name A"));

            // Act
            var doc = await ExportDocumentAsync();

            // Assert
            doc.Resources.Single().Tags.Should()
                .BeNull("because an absent tags object is cleaner than an empty one and import treats both alike");
        }

        [Fact]
        public async Task ExportAsync_SameDomainDependency_IsEmitted()
        {
            // Arrange
            GivenResources(
                Row(1, "APP", "Type A", "App", domain: "prod"),
                Row(2, "DB", "Type B", "Db", domain: "prod"));
            GivenDependencies(Dep(1, "APP", "prod", "DB", "prod"));

            // Act
            var doc = await ExportDocumentAsync();

            // Assert
            doc.Resources.Single(r => r.Key == "APP").Dependencies.Should()
                .BeEquivalentTo(new[] { "DB" },
                    "because import resolves a same-domain dependency key without ambiguity");
        }

        [Fact]
        public async Task ExportAsync_CrossDomainDependency_IsOmittedAndWarned()
        {
            // Arrange
            GivenResources(
                Row(1, "APP", "Type A", "App", domain: "prod"),
                Row(2, "CACHE", "Type B", "Cache", domain: "non-prod"));
            GivenDependencies(Dep(1, "APP", "prod", "CACHE", "non-prod"));

            // Act
            var result = await ExportResultAsync();

            // Assert
            result.Document.Resources.Single(r => r.Key == "APP").Dependencies.Should()
                .BeNull("because import resolves dependency keys inside the dependent's own domain and would reject this one");
            result.Warnings.Should().ContainSingle(w => w.Contains("crosses domains"),
                "because silently dropping an edge without telling the caller would hide real data loss");
        }

        [Fact]
        public async Task ExportAsync_ResourceWithoutDependencies_OmitsDependencies()
        {
            // Arrange
            GivenResources(Row(1, "KEY-A", "Type A", "Name A"));

            // Act
            var doc = await ExportDocumentAsync();

            // Assert
            doc.Resources.Single().Dependencies.Should()
                .BeNull("because an empty dependencies array adds noise to every leaf resource");
        }

        [Fact]
        public async Task ExportAsync_IncludeResourceTypesDefault_EmitsResourceTypesSection()
        {
            // Arrange
            GivenResourceTypes(new ResourceType { TypeName = "Type A", AllowCustomTags = false });

            // Act
            var doc = await ExportDocumentAsync();

            // Assert
            doc.ResourceTypes.Should().ContainKey("Type A",
                "because resourceTypes is safe to re-import - its upsert only touches AllowCustomTags");
            doc.ResourceTypes!["Type A"].AllowCustomTags.Should()
                .BeFalse("because the flag must round-trip at its stored value");
        }

        [Fact]
        public async Task ExportAsync_IncludeTagDefinitionsFalse_OmitsTagDefinitionsSection()
        {
            // Arrange
            GivenTagDefinitions(TagDef("owner"));

            // Act
            var result = await ExportResultAsync(new ExportOptions { IncludeTagDefinitions = false });

            // Assert
            result.Document.TagDefinitions.Should()
                .BeNull("because re-importing tag definitions would reset IsDomainTag and RequirementLevel to defaults");
            result.Warnings.Should().BeEmpty("because omitting the lossy section is the safe default, not a warning");
        }

        [Fact]
        public async Task ExportAsync_IncludeTagDefinitionsTrue_EmitsSectionAndWarnsItIsLossy()
        {
            // Arrange
            GivenTagDefinitions(TagDef("environment", contentType: "Link", isMultiValued: true,
                allowCustomValue: false, allowedValues: "[\"dev\",\"prod\"]"));

            // Act
            var result = await ExportResultAsync(new ExportOptions { IncludeTagDefinitions = true });

            // Assert
            var def = result.Document.TagDefinitions!["environment"];
            def.ContentType.Should().Be("Link", "because import only accepts the seeded Text|Link codes");
            def.IsMultiValued.Should().BeTrue("because the definition's multi-valuedness must round-trip");
            def.AllowCustomValue.Should().BeFalse("because the constraint must round-trip");
            def.AllowedValues.Should().BeEquivalentTo(new[] { "dev", "prod" },
                "because the stored JSON array is re-expanded into the contract's list form");
            result.Warnings.Should().ContainSingle(w => w.Contains("tagDefinitions"),
                "because the caller must know this section resets unmapped columns on re-import");
        }

        [Fact]
        public async Task ExportAsync_TagDefinitionWithMissingContentType_FallsBackToText()
        {
            // Arrange
            GivenTagDefinitions(TagDef("owner", contentType: null));

            // Act
            var result = await ExportResultAsync(new ExportOptions { IncludeTagDefinitions = true });

            // Assert
            result.Document.TagDefinitions!["owner"].ContentType.Should()
                .Be("Text", "because emitting a null contentType would fail import validation");
        }

        [Fact]
        public async Task ExportAsync_SerializedDocument_DeserializesIntoImportRequest()
        {
            // Arrange
            GivenResources(Row(1, "APP", "Type A", "App", "Desc", "prod", "res-1"));
            GivenTags(
                Tag(1, DomainKey, "prod", isMultiValued: false, isDomainTag: true),
                Tag(1, "owner", "team-a", isMultiValued: true));
            GivenResourceTypes(new ResourceType { TypeName = "Type A", AllowCustomTags = true });

            // Act
            var doc = await ExportDocumentAsync();
            var json = ExportJson.Serialize(doc);
            var request = JsonSerializer.Deserialize<ImportRequest>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Assert
            request.Should().NotBeNull("because the export document must be a valid import document");
            request!.Version.Should().Be("1.0", "because the version field must survive serialization");
            request.Resources.Should().ContainSingle("because the single exported resource must be parsed back");
            var parsed = request.Resources[0];
            parsed.Key.Should().Be("APP", "because key is the import match field");
            parsed.Type.Should().Be("Type A", "because type is identity-bearing");
            parsed.Tags.Should().ContainKey(DomainKey, "because the domain tag must survive serialization");
            parsed.Tags![DomainKey].GetString().Should()
                .Be("prod", "because a single-valued tag deserializes as a JSON string");
            parsed.Tags["owner"].ValueKind.Should()
                .Be(JsonValueKind.Array, "because a multi-valued tag deserializes as a JSON array");
        }

        [Fact]
        public async Task ExportAsync_SerializedDocument_UsesCamelCasePropertyNames()
        {
            // Arrange
            GivenResources(Row(1, "APP", "Type A", "App"));

            // Act
            var json = ExportJson.Serialize(await ExportDocumentAsync());

            // Assert
            json.Should().Contain("\"resources\"", "because the import schema uses camelCase property names");
            json.Should().Contain("\"onConflict\"", "because the policy property is camelCase in the schema");
        }

        [Fact]
        public async Task ExportAsync_RepositoryThrows_ReturnsInternalError()
        {
            // Arrange
            _repo.Setup(r => r.GetResourcesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            // Act
            var response = await _sut.ExportAsync(new ExportOptions(), CancellationToken.None);

            // Assert
            response.IsSuccess().Should()
                .BeFalse("because an unexpected repository failure must surface as an error, not an empty export");
        }

        [Fact]
        public async Task ExportAsync_LargeCatalog_ReadsEachTableExactlyOnce()
        {
            // Arrange — the guard against N+1: cost must not scale with resource count.
            var rows = Enumerable.Range(1, 200)
                .Select(i => Row(i, $"KEY-{i}", "Type A", $"Name {i}", domain: "prod"))
                .ToArray();
            GivenResources(rows);

            // Act
            await _sut.ExportAsync(new ExportOptions(), CancellationToken.None);

            // Assert
            _repo.Verify(r => r.GetResourcesAsync(It.IsAny<CancellationToken>()), Times.Once,
                "because the whole catalog is read in one query");
            _repo.Verify(r => r.GetResourceTagsAsync(It.IsAny<CancellationToken>()), Times.Once,
                "because tags are fetched in bulk, not per resource");
            _repo.Verify(r => r.GetDependenciesAsync(It.IsAny<CancellationToken>()), Times.Once,
                "because dependency edges are fetched in bulk, not per resource");
        }

        #region helpers

        private async Task<ExportDocument> ExportDocumentAsync(ExportOptions? options = null)
            => (await ExportResultAsync(options)).Document;

        private async Task<ExportResult> ExportResultAsync(ExportOptions? options = null)
        {
            var response = await _sut.ExportAsync(options ?? new ExportOptions(), CancellationToken.None);
            response.IsSuccess().Should().BeTrue("because the export was expected to succeed");
            return response.ApiResponse.Data!;
        }

        private void GivenResources(params ExportResourceRow[] rows)
            => _repo.Setup(r => r.GetResourcesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(rows.ToList());

        private void GivenTags(params ExportTagRow[] rows)
            => _repo.Setup(r => r.GetResourceTagsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(rows.ToList());

        private void GivenDependencies(params ExportDependencyRow[] rows)
            => _repo.Setup(r => r.GetDependenciesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(rows.ToList());

        private void GivenResourceTypes(params ResourceType[] types)
            => _repo.Setup(r => r.GetResourceTypesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(types.ToList());

        private void GivenTagDefinitions(params TagDefinition[] defs)
            => _repo.Setup(r => r.GetTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(defs.ToList());

        private static ExportResourceRow Row(int id, string key, string type, string name,
            string? description = null, string? domain = null, string uid = "res-uid")
            => new()
            {
                ResourceId = id,
                ResourceUid = uid,
                ResourceKey = key,
                TypeName = type,
                ResourceName = name,
                Description = description,
                Domain = domain
            };

        private static ExportTagRow Tag(int resourceId, string key, string? value,
            bool isMultiValued, bool isDomainTag = false)
            => new()
            {
                ResourceId = resourceId,
                TagDefinitionKey = key,
                TagValue = value,
                IsMultiValued = isMultiValued,
                IsDomainTag = isDomainTag
            };

        private static ExportDependencyRow Dep(int fromId, string fromKey, string? fromDomain,
            string toKey, string? toDomain)
            => new()
            {
                FromResourceId = fromId,
                FromResourceKey = fromKey,
                FromDomain = fromDomain,
                ToResourceKey = toKey,
                ToDomain = toDomain
            };

        private static TagDefinition TagDef(string key, string? contentType = "Text",
            bool isMultiValued = false, bool allowCustomValue = true, string? allowedValues = null)
            => new()
            {
                TagDefinitionKey = key,
                ContentType = contentType,
                IsMultiValued = isMultiValued,
                AllowCustomValue = allowCustomValue,
                AllowedValues = allowedValues
            };

        #endregion
    }
}
