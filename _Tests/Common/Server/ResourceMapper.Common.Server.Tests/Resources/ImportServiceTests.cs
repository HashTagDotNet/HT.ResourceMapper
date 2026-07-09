using System.Text.Json;
using ResourceMapper.Common.Server.Resources;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Server.Resources.Models;
using ResourceMapper.Common.Shared.Import.Contracts;

// ReSharper disable InconsistentNaming

namespace ResourceMapper.Common.Server.Tests.Resources
{
    /// <summary>
    /// Slice 1 tests: schema/reference/conflict validation and the dry-run summary.
    /// No write-phase behavior is exercised (writes are a later slice).
    /// </summary>
    [Trait("Category", "Unit")]
    [Trait("Category", "ResourceMapper")]
    [Trait("Category", "ResourceMapper/Common")]
    [Trait("Category", "ResourceMapper/Common/Server")]
    [Trait("Category", "ResourceMapper/Common/Server/Resources")]
    [Trait("Category", "ResourceMapper/Common/Server/Resources/ImportService")]
    public class ImportServiceTests
    {
        private const string DefaultType = "DefaultType";

        private readonly Mock<IResourceRepository> _resourceRepo;
        private readonly Mock<IImportRepository> _importRepo;
        private readonly ImportService _sut;

        public ImportServiceTests()
        {
            _resourceRepo = new Mock<IResourceRepository>();
            _importRepo = new Mock<IImportRepository>();

            // No domain tag defined by default (design §6 "Unused" state) — most tests exercise
            // the (Type+Key) fallback identity and never touch domain logic at all.
            _resourceRepo.Setup(r => r.GetAllResourceTypesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceType> { new() { ResourceTypeId = 1, TypeName = DefaultType } });
            _importRepo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition>());
            _importRepo.Setup(r => r.GetAllResourceIdentitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceIdentity>());

            // Write-method defaults: everything "created" unless a test overrides.
            _importRepo.Setup(r => r.UpsertResourceTypeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("created");
            _importRepo.Setup(r => r.UpsertTagDefinitionAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("created");
            _importRepo.Setup(r => r.UpsertResourceAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("created", 1));
            _importRepo.Setup(r => r.SetResourceTagsAsync(
                    It.IsAny<int>(), It.IsAny<IReadOnlyList<(string, string)>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _sut = new ImportService(_resourceRepo.Object, _importRepo.Object);
        }

        #region schema validation

        [Fact]
        public async Task ImportAsync_NullRequest_ReturnsValidationError()
        {
            var response = await _sut.ImportAsync(null!, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a null request cannot be imported");
        }

        [Fact]
        public async Task ImportAsync_InvalidVersion_ReturnsValidationError()
        {
            var request = ValidRequest();
            request.Version = "2.0";

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because only version 1.0 is supported");
            ErrorsOf(response).Should().Contain(e => e.Field == "version",
                "because the unsupported version should be reported on the version field");
        }

        [Theory]
        [InlineData("merge")]
        [InlineData("")]
        [InlineData("UPSERTX")]
        public async Task ImportAsync_InvalidOnConflict_ReturnsValidationError(string policy)
        {
            var request = ValidRequest();
            request.Policy.OnConflict = policy;

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because onConflict must be upsert|skip|fail");
            ErrorsOf(response).Should().Contain(e => e.Field == "policy.onConflict",
                "because the invalid policy should be reported");
        }

        [Fact]
        public async Task ImportAsync_DuplicateKeysInPayload_ReturnsValidationError()
        {
            var request = ValidRequest(
                Resource("dup", "First"),
                Resource("dup", "Second"));

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because resource keys must be unique");
            ErrorsOf(response).Should().Contain(e => e.Section == "resources" && e.Field == "key",
                "because the duplicate key should be reported");
        }

        [Fact]
        public async Task ImportAsync_DuplicateKeysDifferingByCase_ReturnsValidationError()
        {
            var request = ValidRequest(
                Resource("App-A", "First"),
                Resource("app-a", "Second"));

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because keys are compared case-insensitively");
            ErrorsOf(response).Should().Contain(e => e.Section == "resources" && e.Field == "key",
                "because case-only-different keys are duplicates");
        }

        [Fact]
        public async Task ImportAsync_MissingResourceName_ReturnsValidationError()
        {
            var request = ValidRequest(Resource("res-1", ""));

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a resource name is required");
            ErrorsOf(response).Should().Contain(e => e.Field == "name",
                "because the missing name should be reported");
        }

        #endregion

        #region reference validation

        [Fact]
        public async Task ImportAsync_UnknownResourceType_ReturnsReferenceError()
        {
            var resource = Resource("res-1", "Resource One");
            resource.Type = "Ghost";
            var request = ValidRequest(resource);

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because the type resolves to nothing in payload or DB");
            ErrorsOf(response).Should().Contain(e => e.Field == "type",
                "because the unknown type should be reported");
        }

        [Fact]
        public async Task ImportAsync_UnknownTagKey_ReturnsReferenceError()
        {
            var resource = Resource("res-1", "Resource One");
            resource.Tags = new Dictionary<string, JsonElement> { ["mystery"] = Str("x") };
            var request = ValidRequest(resource);

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because every tag key needs a definition");
            ErrorsOf(response).Should().Contain(e => e.Field == "tags.mystery",
                "because the undefined tag key should be reported");
        }

        [Fact]
        public async Task ImportAsync_ArrayValueForSingleValuedTag_ReturnsValidationError()
        {
            var resource = Resource("res-1", "Resource One");
            resource.Tags = new Dictionary<string, JsonElement> { ["env"] = Arr("dev", "prod") };
            var request = ValidRequest(resource);
            request.TagDefinitions = new()
            {
                ["env"] = new ImportTagDefinitionModel { ContentType = "Text", IsMultiValued = false, AllowCustomValue = true }
            };

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because an array value is illegal on a single-valued tag");
            ErrorsOf(response).Should().Contain(e => e.Field == "tags.env",
                "because the array-on-single-valued violation should be reported");
        }

        [Fact]
        public async Task ImportAsync_ValueNotInAllowedValues_ReturnsValidationError()
        {
            var resource = Resource("res-1", "Resource One");
            resource.Tags = new Dictionary<string, JsonElement> { ["env"] = Str("staging") };
            var request = ValidRequest(resource);
            request.TagDefinitions = new()
            {
                ["env"] = new ImportTagDefinitionModel
                {
                    ContentType = "Text",
                    IsMultiValued = false,
                    AllowCustomValue = false,
                    AllowedValues = new List<string> { "dev", "prod" }
                }
            };

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because 'staging' is not an allowed value");
            ErrorsOf(response).Should().Contain(e => e.Field == "tags.env",
                "because the disallowed value should be reported");
        }

        [Fact]
        public async Task ImportAsync_TagKeyDefinedInDatabaseOnly_Succeeds()
        {
            _importRepo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition>
                {
                    new() { TagDefinitionKey = "env", IsMultiValued = true, AllowCustomValue = true }
                });

            var resource = Resource("res-1", "Resource One");
            resource.Tags = new Dictionary<string, JsonElement> { ["env"] = Str("dev") };
            var request = ValidRequest(resource);

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the tag definition exists in the database");
        }

        #endregion

        #region conflict validation (fail policy)

        [Fact]
        public async Task ImportAsync_ConflictWithFailPolicy_ReturnsConflictError()
        {
            _importRepo.Setup(r => r.GetAllResourceIdentitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceIdentity>
                {
                    new() { ResourceId = 1, ResourceKey = "res-1", TypeName = DefaultType, Domain = null }
                });

            var request = ValidRequest(Resource("res-1", "Resource One"));
            request.Policy.OnConflict = "fail";

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because the resource already exists and policy is fail");
            ErrorsOf(response).Should().Contain(e => e.Section == "resources" && e.Key == "res-1",
                "because the conflicting resource should be reported");
        }

        #endregion

        #region domain resolution

        [Fact]
        public async Task ImportAsync_DomainOverride_TakesPrecedenceOverBatchDefault()
        {
            _importRepo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition> { DomainTagDef(allowedValues: new[] { "prod", "non-prod" }) });

            string? capturedDomain = null;
            _importRepo.Setup(r => r.UpsertResourceAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback((string _, string _, string _, string _, string _, string? d, string _, CancellationToken _) => capturedDomain = d)
                .ReturnsAsync(("created", 1));

            var resource = Resource("orders-queue", "Orders Queue");
            resource.Tags = new Dictionary<string, JsonElement> { ["Domain"] = Str("prod") };
            var request = ValidRequest(resource);
            request.Defaults.Domain = "non-prod";

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because 'prod' is a valid domain value");
            capturedDomain.Should().Be("prod", "because a per-resource override wins over the batch default");
        }

        [Fact]
        public async Task ImportAsync_NoOverride_FallsBackToBatchDefaultDomain()
        {
            _importRepo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition> { DomainTagDef(allowedValues: new[] { "prod", "non-prod" }) });

            string? capturedDomain = null;
            _importRepo.Setup(r => r.UpsertResourceAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback((string _, string _, string _, string _, string _, string? d, string _, CancellationToken _) => capturedDomain = d)
                .ReturnsAsync(("created", 1));

            var request = ValidRequest(Resource("orders-queue", "Orders Queue"));
            request.Defaults.Domain = "non-prod";

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because a batch default should apply when no override is given");
            capturedDomain.Should().Be("non-prod", "because the batch default applies when no per-resource override is given");
        }

        [Fact]
        public async Task ImportAsync_DomainRequiredAndMissing_ReturnsValidationErrorAndNoWrites()
        {
            _importRepo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition> { DomainTagDef(required: true, allowedValues: new[] { "prod", "non-prod" }) });

            var request = ValidRequest(Resource("orders-queue", "Orders Queue")); // no default, no override

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because Domain is RequirementLevel=Error and nothing supplies it");
            ErrorsOf(response).Should().Contain(e => e.Field == "domain", "because the missing domain should be reported");
            _importRepo.Verify(r => r.UpsertResourceAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never, "because validation must fail before any write");
        }

        [Fact]
        public async Task ImportAsync_DomainNotInAllowedValues_ReturnsValidationError()
        {
            _importRepo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition> { DomainTagDef(allowedValues: new[] { "prod", "non-prod" }) });

            var request = ValidRequest(Resource("orders-queue", "Orders Queue"));
            request.Defaults.Domain = "staging";

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because 'staging' is not in the domain's allowed values");
            ErrorsOf(response).Should().Contain(e => e.Field == "domain", "because the invalid domain value should be reported");
        }

        [Fact]
        public async Task ImportAsync_SameKeyDifferentDomain_IsNotADuplicate()
        {
            _importRepo.Setup(r => r.GetAllTagDefinitionsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TagDefinition> { DomainTagDef(allowedValues: new[] { "prod", "non-prod" }) });

            var nonProd = Resource("orders-queue", "Orders Queue");
            nonProd.Tags = new Dictionary<string, JsonElement> { ["Domain"] = Str("non-prod") };
            var prod = Resource("orders-queue", "Orders Queue");
            prod.Tags = new Dictionary<string, JsonElement> { ["Domain"] = Str("prod") };
            var request = ValidRequest(nonProd, prod);

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the two resources are distinct under (domain+type+key)");
            response.ApiResponse.Data!.Summary!.Resources.Created.Should().Be(2, "because both are new, distinct resources");
        }

        [Fact]
        public async Task ImportAsync_LegacyStringContentType_MapsToText()
        {
            var request = ValidRequest(Resource("res-1", "One"));
            request.TagDefinitions = new()
            {
                ["env"] = new ImportTagDefinitionModel { ContentType = "string", IsMultiValued = true, AllowCustomValue = true }
            };

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the legacy 'string' content type is mapped to 'Text'");
            _importRepo.Verify(r => r.UpsertTagDefinitionAsync("env", It.IsAny<string>(), "Text", true, true, It.IsAny<string>(), "upsert", It.IsAny<CancellationToken>()),
                Times.Once, "because 'string' must be normalized to 'Text' before writing");
        }

        [Fact]
        public async Task ImportAsync_InvalidContentType_ReturnsValidationError()
        {
            var request = ValidRequest(Resource("res-1", "One"));
            request.TagDefinitions = new()
            {
                ["env"] = new ImportTagDefinitionModel { ContentType = "boolean", IsMultiValued = true, AllowCustomValue = true }
            };

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because contentType must be 'Text' or 'Link'");
            ErrorsOf(response).Should().Contain(e => e.Field == "contentType",
                "because an unrecognized content type (other than the legacy 'string') must be rejected, not auto-registered");
        }

        #endregion

        #region dependency resolution

        [Fact]
        public async Task ImportAsync_ResolvableDependency_WritesRelationshipEdge()
        {
            _importRepo.Setup(r => r.UpsertResourceAsync("orders-queue", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("created", 100));
            _importRepo.Setup(r => r.UpsertResourceAsync("orders-db", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("created", 200));

            var queue = Resource("orders-queue", "Orders Queue");
            queue.Dependencies = new List<string> { "orders-db" };
            var db = Resource("orders-db", "Orders DB");
            var request = ValidRequest(queue, db);

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the dependency resolves to exactly one target");
            _resourceRepo.Verify(r => r.AddRelationshipAsync(100, 200, It.IsAny<CancellationToken>()), Times.Once,
                "because the queue depends on the db, resolved to their upserted ids");
            response.ApiResponse.Data!.Summary!.ResourceRelationships.Created.Should().Be(1);
        }

        [Fact]
        public async Task ImportAsync_SelfReferenceDependency_ReturnsValidationErrorAndNoWrites()
        {
            var resource = Resource("orders-queue", "Orders Queue");
            resource.Dependencies = new List<string> { "orders-queue" };
            var request = ValidRequest(resource);

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because a resource cannot depend on itself");
            ErrorsOf(response).Should().Contain(e => e.Field == "dependencies", "because the self-reference should be reported");
            _importRepo.Verify(r => r.UpsertResourceAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never, "because dependency validation runs before any write");
        }

        [Fact]
        public async Task ImportAsync_UnresolvedDependency_ReturnsValidationErrorAndNoWrites()
        {
            var resource = Resource("orders-queue", "Orders Queue");
            resource.Dependencies = new List<string> { "ghost-target" };
            var request = ValidRequest(resource);

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because 'ghost-target' matches nothing");
            ErrorsOf(response).Should().Contain(e => e.Field == "dependencies", "because the unresolved dependency should be reported");
            _importRepo.Verify(r => r.UpsertResourceAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never, "because dependency validation runs before any write");
        }

        [Fact]
        public async Task ImportAsync_AmbiguousDependency_ReturnsValidationErrorAndNoWrites()
        {
            var queueA = Resource("orders", "Orders Queue");
            queueA.Type = "Queue";
            var dbA = Resource("orders", "Orders DB");
            dbA.Type = "Database";
            var dependent = Resource("billing", "Billing");
            dependent.Dependencies = new List<string> { "orders" };

            var request = ValidRequest(queueA, dbA, dependent);
            request.ResourceTypes = new()
            {
                ["Queue"] = new ImportResourceTypeDefinition(),
                ["Database"] = new ImportResourceTypeDefinition()
            };

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeFalse("because 'orders' matches both Queue and Database");
            ErrorsOf(response).Should().Contain(e => e.Field == "dependencies" && e.Key == "billing",
                "because the ambiguous dependency should be reported against the dependent resource");
            _importRepo.Verify(r => r.UpsertResourceAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never, "because ambiguity must fail the whole import before any write");
        }

        [Fact]
        public async Task ImportAsync_DependencyToPreExistingResourceNotInPayload_ResolvesViaExistingIdentities()
        {
            _importRepo.Setup(r => r.GetAllResourceIdentitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResourceIdentity>
                {
                    new() { ResourceId = 999, ResourceKey = "orders-db", TypeName = DefaultType, Domain = null }
                });
            _importRepo.Setup(r => r.UpsertResourceAsync("orders-queue", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("created", 100));

            var queue = Resource("orders-queue", "Orders Queue");
            queue.Dependencies = new List<string> { "orders-db" };
            var request = ValidRequest(queue); // orders-db is NOT redeclared in this payload

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because 'orders-db' resolves against the pre-existing identity set");
            _resourceRepo.Verify(r => r.AddRelationshipAsync(100, 999, It.IsAny<CancellationToken>()), Times.Once,
                "because the pre-existing target's id must be used");
        }

        [Fact]
        public async Task ImportAsync_SkippedSourceResource_DoesNotWriteItsOutEdges()
        {
            _importRepo.Setup(r => r.UpsertResourceAsync("orders-queue", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("skipped", 100));
            _importRepo.Setup(r => r.UpsertResourceAsync("orders-db", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("created", 200));

            var queue = Resource("orders-queue", "Orders Queue");
            queue.Dependencies = new List<string> { "orders-db" };
            var db = Resource("orders-db", "Orders DB");
            var request = ValidRequest(queue, db);
            request.Policy.OnConflict = "skip";

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue();
            _resourceRepo.Verify(r => r.AddRelationshipAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never,
                "because skip-means-skip extends to a skipped resource's out-edges");
        }

        #endregion

        #region write phase

        [Fact]
        public async Task ImportAsync_EmptyResources_ReturnsSuccessWithZerosAndNoWrites()
        {
            var response = await _sut.ImportAsync(ValidRequest(), CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because an empty resources array is a valid no-op");
            response.ApiResponse.Data!.Summary!.Resources.Created.Should().Be(0, "because nothing was supplied");
            _importRepo.Verify(r => r.UpsertResourceAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never, "because there are no resources to write");
        }

        [Fact]
        public async Task ImportAsync_NewResource_WritesAndCountsCreated()
        {
            var response = await _sut.ImportAsync(ValidRequest(Resource("res-new", "New")), CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the request is valid");
            response.ApiResponse.Data!.Summary!.Resources.Created.Should().Be(1, "because the repo reported 'created'");
            _importRepo.Verify(r => r.UpsertResourceAsync(
                    "res-new", It.IsAny<string>(), It.IsAny<string>(), "New", It.IsAny<string>(), It.IsAny<string?>(), "upsert", It.IsAny<CancellationToken>()),
                Times.Once, "because the resource should be upserted exactly once");
        }

        [Fact]
        public async Task ImportAsync_ExistingResourceUpsert_CountsUpdated()
        {
            _importRepo.Setup(r => r.UpsertResourceAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("updated", 1));

            var response = await _sut.ImportAsync(ValidRequest(Resource("res-1", "One")), CancellationToken.None);

            response.ApiResponse.Data!.Summary!.Resources.Updated.Should().Be(1, "because the repo reported 'updated'");
        }

        [Fact]
        public async Task ImportAsync_SkippedResource_NotCountedCreatedAndTagsNotTouched()
        {
            _importRepo.Setup(r => r.UpsertResourceAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("skipped", 9));

            var resource = Resource("res-1", "One");
            resource.Tags = new Dictionary<string, JsonElement> { ["env"] = Str("dev") };
            var request = ValidRequest(resource);
            request.TagDefinitions = new()
            {
                ["env"] = new ImportTagDefinitionModel { ContentType = "Text", IsMultiValued = true, AllowCustomValue = true }
            };
            request.Policy.OnConflict = "skip";

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.ApiResponse.Data!.Summary!.Resources.Skipped.Should().Be(1, "because the repo reported 'skipped'");
            _importRepo.Verify(r => r.SetResourceTagsAsync(
                    It.IsAny<int>(), It.IsAny<IReadOnlyList<(string, string)>>(), It.IsAny<CancellationToken>()),
                Times.Never, "because a skipped resource must not have its tags replaced");
        }

        [Fact]
        public async Task ImportAsync_MultiValuedTag_ExpandsToOneRowPerValueAgainstResourceId()
        {
            _importRepo.Setup(r => r.UpsertResourceAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(("created", 42));

            var resource = Resource("res-1", "One");
            resource.Tags = new Dictionary<string, JsonElement> { ["env"] = Arr("dev", "prod") };
            var request = ValidRequest(resource);
            request.TagDefinitions = new()
            {
                ["env"] = new ImportTagDefinitionModel { ContentType = "Text", IsMultiValued = true, AllowCustomValue = true }
            };

            await _sut.ImportAsync(request, CancellationToken.None);

            _importRepo.Verify(r => r.SetResourceTagsAsync(
                    42,
                    It.Is<IReadOnlyList<(string TagKey, string TagValue)>>(t =>
                        t.Count == 2
                        && t.Any(x => x.TagKey == "env" && x.TagValue == "dev")
                        && t.Any(x => x.TagKey == "env" && x.TagValue == "prod")),
                    It.IsAny<CancellationToken>()),
                Times.Once, "because a multi-valued tag expands to one row per value, written against the returned resource id");
        }

        [Fact]
        public async Task ImportAsync_ReferenceData_TypesAndTagDefinitionsAreUpserted()
        {
            var resource = Resource("res-1", "One");
            resource.Type = "AppService";
            var request = ValidRequest(resource);
            request.ResourceTypes = new() { ["AppService"] = new ImportResourceTypeDefinition { AllowCustomTags = true } };
            request.TagDefinitions = new()
            {
                ["env"] = new ImportTagDefinitionModel { ContentType = "Text", IsMultiValued = true, AllowCustomValue = true }
            };

            var response = await _sut.ImportAsync(request, CancellationToken.None);

            response.IsSuccess().Should().BeTrue("because the document is valid");
            _importRepo.Verify(r => r.UpsertResourceTypeAsync("AppService", It.IsAny<string>(), true, "upsert", It.IsAny<CancellationToken>()),
                Times.Once, "because the declared resource type should be upserted");
            _importRepo.Verify(r => r.UpsertTagDefinitionAsync("env", It.IsAny<string>(), "Text", true, true, It.IsAny<string>(), "upsert", It.IsAny<CancellationToken>()),
                Times.Once, "because the declared tag definition should be upserted");
        }

        #endregion

        #region helpers

        private static ImportRequest ValidRequest(params ImportResourceItem[] resources) => new()
        {
            Version = "1.0",
            Policy = new ImportPolicy { OnConflict = "upsert" },
            Resources = resources.ToList()
        };

        private static ImportResourceItem Resource(string key, string name) => new() { Key = key, Name = name, Type = DefaultType };

        private static TagDefinition DomainTagDef(bool required = true, bool allowCustom = false, string[]? allowedValues = null) => new()
        {
            TagDefinitionKey = "Domain",
            IsDomainTag = true,
            RequirementLevel = required ? "Error" : "Optional",
            AllowCustomValue = allowCustom,
            AllowedValues = allowedValues is { Length: > 0 } ? JsonSerializer.Serialize(allowedValues) : null,
            ContentType = "Text"
        };

        private static JsonElement Str(string value) => JsonSerializer.SerializeToElement(value);

        private static JsonElement Arr(params string[] values) => JsonSerializer.SerializeToElement(values);

        private static List<ImportError> ErrorsOf(HT.Api.Service.Contracts.ApiServiceResponse<ImportResponse> response)
            => response.ApiResponse.Data?.Errors ?? new List<ImportError>();

        #endregion
    }
}
