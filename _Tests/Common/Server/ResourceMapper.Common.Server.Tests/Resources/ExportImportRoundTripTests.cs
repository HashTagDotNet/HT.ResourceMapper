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
    /// The acceptance test for PL-09: a document produced by <see cref="ExportService"/> must be
    /// consumable by the real <see cref="ImportService"/> without creating anything.
    ///
    /// Both services are the real implementations. Only the repositories are doubles, and the import
    /// repository is not a stub that returns canned results — it is a small in-memory simulation of
    /// the upsert sprocs' matching semantics, keyed on the same (Domain + Type + Key) triple
    /// Resource_Upsert uses. That is what makes "no duplicates" a real assertion: a mismatch in the
    /// exported shape shows up as a "created" result and a grown store, not as a passing stub.
    /// </summary>
    [Trait("Category", "Unit")]
    [Trait("Category", "ResourceMapper")]
    [Trait("Category", "ResourceMapper/Common")]
    [Trait("Category", "ResourceMapper/Common/Server")]
    [Trait("Category", "ResourceMapper/Common/Server/Resources")]
    [Trait("Category", "ResourceMapper/Common/Server/Resources/ExportImportRoundTrip")]
    public class ExportImportRoundTripTests
    {
        private const string DomainKey = "Domain";

        private readonly FakeCatalog _catalog;
        private readonly ExportService _exportService;
        private readonly ImportService _importService;

        public ExportImportRoundTripTests()
        {
            _catalog = FakeCatalog.Seeded();
            _exportService = new ExportService(_catalog.BuildExportRepository());
            _importService = new ImportService(
                _catalog.BuildResourceRepository(), _catalog.BuildImportRepository());
        }

        [Fact]
        public async Task RoundTrip_UpsertPolicy_UpdatesEveryResourceAndCreatesNothing()
        {
            // Arrange
            var request = await ExportThenParseAsync();
            request.Policy.OnConflict = "upsert";
            var countBefore = _catalog.ResourceCount;

            // Act
            var response = await _importService.ImportAsync(request, CancellationToken.None);

            // Assert
            var summary = SuccessfulSummary(response);
            summary.Resources.Created.Should()
                .Be(0, "because every exported resource already exists and must match on re-import");
            summary.Resources.Updated.Should()
                .Be(countBefore, "because an upsert of an unchanged export updates each existing row in place");
            _catalog.ResourceCount.Should()
                .Be(countBefore, "because a round-trip must never grow the catalog");
        }

        [Fact]
        public async Task RoundTrip_SkipPolicy_SkipsEveryResourceAndCreatesNothing()
        {
            // Arrange
            var request = await ExportThenParseAsync();
            request.Policy.OnConflict = "skip";
            var countBefore = _catalog.ResourceCount;

            // Act
            var response = await _importService.ImportAsync(request, CancellationToken.None);

            // Assert
            var summary = SuccessfulSummary(response);
            summary.Resources.Created.Should()
                .Be(0, "because skip must recognise every exported resource as already present");
            summary.Resources.Skipped.Should()
                .Be(countBefore, "because skip-means-skip applies to every matched resource");
            _catalog.ResourceCount.Should()
                .Be(countBefore, "because a skip round-trip is a no-op on the catalog");
        }

        [Fact]
        public async Task RoundTrip_ExportedDocument_PassesImportValidation()
        {
            // Arrange
            var request = await ExportThenParseAsync();

            // Act
            var response = await _importService.ImportAsync(request, CancellationToken.None);

            // Assert
            response.ApiResponse.Data!.Errors.Should()
                .BeNullOrEmpty("because an exported document must satisfy every import validation rule");
            response.IsSuccess().Should()
                .BeTrue("because a format that fails its own import is a failed round-trip");
        }

        [Fact]
        public async Task RoundTrip_ResourcesInDifferentDomainsSharingAKey_StayDistinct()
        {
            // Arrange — the seed has SHARED-KEY in both prod and non-prod.
            var request = await ExportThenParseAsync();
            request.Policy.OnConflict = "upsert";

            // Act
            await _importService.ImportAsync(request, CancellationToken.None);

            // Assert
            _catalog.CountByKey("SHARED-KEY").Should()
                .Be(2, "because the domain tag travels with each resource and keeps the two identities apart");
        }

        [Fact]
        public async Task RoundTrip_SameDomainDependencies_AreReEstablished()
        {
            // Arrange
            var request = await ExportThenParseAsync();
            request.Policy.OnConflict = "upsert";

            // Act
            var response = await _importService.ImportAsync(request, CancellationToken.None);

            // Assert
            SuccessfulSummary(response).ResourceRelationships.Created.Should()
                .Be(2, "because the two same-domain edges in the seed are re-asserted from the exported keys");
        }

        [Fact]
        public async Task RoundTrip_MultiValuedTag_SurvivesWithAllValues()
        {
            // Arrange
            var request = await ExportThenParseAsync();

            // Act
            var owner = request.Resources.Single(r => r.Key == "APP-1").Tags!["owner"];

            // Assert
            owner.ValueKind.Should().Be(JsonValueKind.Array, "because owner is a multi-valued definition");
            owner.EnumerateArray().Select(v => v.GetString()).Should()
                .BeEquivalentTo(new[] { "team-a", "team-b" }, "because every stored value must round-trip");
        }

        [Fact]
        public async Task RoundTrip_SecondConsecutiveImport_StillCreatesNothing()
        {
            // Arrange — idempotency: re-importing twice must be as safe as once.
            var request = await ExportThenParseAsync();
            request.Policy.OnConflict = "upsert";
            await _importService.ImportAsync(request, CancellationToken.None);
            var countAfterFirst = _catalog.ResourceCount;

            // Act
            var second = await _importService.ImportAsync(await ExportThenParseAsync(), CancellationToken.None);

            // Assert
            SuccessfulSummary(second).Resources.Created.Should()
                .Be(0, "because the catalog is unchanged and every resource must still match");
            _catalog.ResourceCount.Should()
                .Be(countAfterFirst, "because repeated imports of the same export must be idempotent");
        }

        [Fact]
        public async Task RoundTrip_CrossDomainEdge_IsOmittedFromTheDocument()
        {
            // Arrange / Act — the seed has APP-1 (prod) -> LEGACY (non-prod).
            var result = await ExportAsync();

            // Assert
            result.Document.Resources.Single(r => r.Key == "APP-1").Dependencies.Should()
                .NotContain("LEGACY", "because import cannot resolve a dependency key outside the dependent's domain");
            result.Warnings.Should().ContainSingle(w => w.Contains("crosses domains"),
                "because the omitted edge is a known fidelity gap that must be reported, not hidden");
        }

        #region helpers

        private async Task<ExportResult> ExportAsync()
        {
            var response = await _exportService.ExportAsync(new ExportOptions(), CancellationToken.None);
            response.IsSuccess().Should().BeTrue("because the export step must succeed before it can be re-imported");
            return response.ApiResponse.Data!;
        }

        /// <summary>Export, serialize, and parse back exactly the way the UI download and import page do.</summary>
        private async Task<ImportRequest> ExportThenParseAsync()
        {
            var json = ExportJson.Serialize((await ExportAsync()).Document);
            var request = JsonSerializer.Deserialize<ImportRequest>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            request.Should().NotBeNull("because the exported JSON must parse as an import document");
            return request!;
        }

        private static ImportSummary SuccessfulSummary(
            HT.Api.Service.Contracts.ApiServiceResponse<ImportResponse> response)
        {
            var data = response.ApiResponse.Data;
            data.Should().NotBeNull("because the import must return a payload");
            data!.Errors.Should().BeNullOrEmpty("because the re-imported export must not raise validation errors");
            data.Summary.Should().NotBeNull("because a successful import reports a summary");
            return data.Summary!;
        }

        /// <summary>
        /// In-memory stand-in for the catalog tables, shared by the export and import repository
        /// doubles so both sides see one consistent world. Matching mirrors Resource_Upsert:
        /// (Domain + Type + Key), case-insensitive.
        /// </summary>
        private sealed class FakeCatalog
        {
            private readonly List<ExportResourceRow> _resources = [];
            private readonly List<ExportTagRow> _tags = [];
            private readonly List<ExportDependencyRow> _dependencies = [];
            private readonly List<ResourceType> _types = [];
            private readonly List<TagDefinition> _tagDefinitions = [];
            private readonly HashSet<(int From, int To)> _edges = [];
            private int _nextId = 100;

            public int ResourceCount => _resources.Count;

            public int CountByKey(string key)
                => _resources.Count(r => string.Equals(r.ResourceKey, key, StringComparison.OrdinalIgnoreCase));

            public static FakeCatalog Seeded()
            {
                var c = new FakeCatalog();

                c._types.Add(new ResourceType { ResourceTypeId = 1, TypeName = "Application", AllowCustomTags = true });
                c._types.Add(new ResourceType { ResourceTypeId = 2, TypeName = "Database", AllowCustomTags = false });

                c._tagDefinitions.Add(new TagDefinition
                {
                    TagDefinitionId = 1,
                    TagDefinitionKey = DomainKey,
                    ContentType = "Text",
                    IsDomainTag = true,
                    RequirementLevel = "Error",
                    AllowCustomValue = false,
                    IsMultiValued = false,
                    AllowedValues = "[\"prod\",\"non-prod\"]"
                });
                c._tagDefinitions.Add(new TagDefinition
                {
                    TagDefinitionId = 2,
                    TagDefinitionKey = "owner",
                    ContentType = "Text",
                    AllowCustomValue = true,
                    IsMultiValued = true
                });

                // prod: APP-1 -> DB-1, APP-1 -> SHARED-KEY. Plus a cross-domain APP-1 -> LEGACY.
                var app1 = c.AddResource("APP-1", "Application", "App One", "prod");
                var db1 = c.AddResource("DB-1", "Database", "Db One", "prod");
                var sharedProd = c.AddResource("SHARED-KEY", "Application", "Shared (prod)", "prod");
                var legacy = c.AddResource("LEGACY", "Application", "Legacy", "non-prod");
                c.AddResource("SHARED-KEY", "Application", "Shared (non-prod)", "non-prod");

                c.AddTag(app1, "owner", "team-a", isMultiValued: true);
                c.AddTag(app1, "owner", "team-b", isMultiValued: true);

                c.AddEdge(app1, "APP-1", "prod", db1, "DB-1", "prod");
                c.AddEdge(app1, "APP-1", "prod", sharedProd, "SHARED-KEY", "prod");
                c.AddEdge(app1, "APP-1", "prod", legacy, "LEGACY", "non-prod");

                return c;
            }

            private int AddResource(string key, string type, string name, string domain)
            {
                var id = _nextId++;
                _resources.Add(new ExportResourceRow
                {
                    ResourceId = id,
                    ResourceUid = $"res-{id}",
                    ResourceKey = key,
                    TypeName = type,
                    ResourceName = name,
                    Description = $"{name} description",
                    Domain = domain
                });
                // The domain lives as a ResourceTag row, exactly as it does in the database.
                _tags.Add(new ExportTagRow
                {
                    ResourceId = id,
                    TagDefinitionKey = DomainKey,
                    TagValue = domain,
                    IsMultiValued = false,
                    IsDomainTag = true
                });
                return id;
            }

            private void AddTag(int resourceId, string key, string value, bool isMultiValued)
                => _tags.Add(new ExportTagRow
                {
                    ResourceId = resourceId,
                    TagDefinitionKey = key,
                    TagValue = value,
                    IsMultiValued = isMultiValued
                });

            private void AddEdge(int fromId, string fromKey, string fromDomain,
                int toId, string toKey, string toDomain)
            {
                _edges.Add((fromId, toId));
                _dependencies.Add(new ExportDependencyRow
                {
                    FromResourceId = fromId,
                    FromResourceKey = fromKey,
                    FromDomain = fromDomain,
                    ToResourceKey = toKey,
                    ToDomain = toDomain
                });
            }

            private static string Norm(string? s) => (s ?? string.Empty).Trim().ToLowerInvariant();

            public IExportRepository BuildExportRepository()
            {
                var repo = new Mock<IExportRepository>();
                repo.Setup(r => r.GetResourcesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => _resources.ToList());
                repo.Setup(r => r.GetResourceTagsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => _tags.ToList());
                repo.Setup(r => r.GetDependenciesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => _dependencies.ToList());
                repo.Setup(r => r.GetResourceTypesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => _types.ToList());
                repo.Setup(r => r.GetTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => _tagDefinitions.ToList());
                return repo.Object;
            }

            public IResourceRepository BuildResourceRepository()
            {
                var repo = new Mock<IResourceRepository>();
                repo.Setup(r => r.GetAllResourceTypesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => _types.ToList());
                repo.Setup(r => r.AddRelationshipAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Callback<int, int, CancellationToken>((from, to, _) => _edges.Add((from, to)))
                    .Returns(Task.CompletedTask);
                return repo.Object;
            }

            public IImportRepository BuildImportRepository()
            {
                var repo = new Mock<IImportRepository>();

                repo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => _tagDefinitions.ToList());

                repo.Setup(r => r.GetAllResourceIdentitiesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => _resources.Select(x => new ResourceIdentity
                    {
                        ResourceId = x.ResourceId,
                        ResourceKey = x.ResourceKey,
                        TypeName = x.TypeName,
                        Domain = x.Domain
                    }).ToList());

                repo.Setup(r => r.UpsertResourceTypeAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((string typeName, string _, bool allowCustomTags, string onConflict, CancellationToken _) =>
                    {
                        var existing = _types.FirstOrDefault(t =>
                            string.Equals(t.TypeName, typeName, StringComparison.OrdinalIgnoreCase));
                        if (existing is null)
                        {
                            _types.Add(new ResourceType { TypeName = typeName, AllowCustomTags = allowCustomTags });
                            return "created";
                        }
                        if (onConflict != "upsert") return "skipped";
                        existing.AllowCustomTags = allowCustomTags;
                        return "updated";
                    });

                repo.Setup(r => r.UpsertTagDefinitionAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                        It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync("skipped");

                // The heart of the simulation: Resource_Upsert's (Domain + Type + Key) match.
                repo.Setup(r => r.UpsertResourceAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((string key, string uid, string? type, string name, string? description,
                        string? domain, string onConflict, CancellationToken _) =>
                    {
                        var match = _resources.FirstOrDefault(x =>
                            Norm(x.ResourceKey) == Norm(key)
                            && Norm(x.TypeName) == Norm(type)
                            && Norm(x.Domain) == Norm(domain));

                        if (match is null)
                        {
                            var id = _nextId++;
                            _resources.Add(new ExportResourceRow
                            {
                                ResourceId = id,
                                ResourceUid = uid,
                                ResourceKey = key,
                                TypeName = type ?? string.Empty,
                                ResourceName = name,
                                Description = description,
                                Domain = domain
                            });
                            return ("created", id);
                        }

                        if (onConflict != "upsert") return ("skipped", match.ResourceId);

                        match.ResourceName = name;
                        match.Description = description;
                        return ("updated", match.ResourceId);
                    });

                repo.Setup(r => r.SetResourceTagsAsync(
                        It.IsAny<int>(), It.IsAny<IReadOnlyList<(string, string)>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                return repo.Object;
            }
        }

        #endregion
    }
}
