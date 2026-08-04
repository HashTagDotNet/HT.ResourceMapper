using System.Text.Json;
using HT.Api.Client.Contracts.Models;
using HT.Api.Service.Contracts;
using HT.Api.Service.Contracts.BuildersOfT;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Server.Resources.Models;
using ResourceMapper.Common.Shared.Cascade;
using ResourceMapper.Common.Shared.Domains;
using ResourceMapper.Common.Shared.Domains.Contracts;
using ResourceMapper.Common.Shared.HomePage.Contracts;
using ResourceMapper.Common.Shared.HomePage;
using ResourceMapper.Common.Shared.Editor;
using ResourceMapper.Common.Shared.Editor.Contracts;
using ResourceMapper.Common.Shared.ResourceTypes;
using ResourceMapper.Common.Shared.ResourceTypes.Contracts;
using ResourceMapper.Common.Shared.Tags;
using ResourceMapper.Common.Shared.Tags.Contracts;

namespace ResourceMapper.Common.Server.Resources
{
    public class ResourceService : IResourceService
    {
        private readonly IResourceRepository _repo;

        public ResourceService(IResourceRepository repository)
        {
            _repo = repository;
        }
        
        public async Task<ApiServiceResponse<ResourceGridResponse>> GetResourceGridItems(ResourceGridRequest? request, CancellationToken cancellationToken)
        {
            var builder = new ServiceResponseBuilder<ResourceGridResponse>();
            try
            {
                if (request == null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return builder.BuildResponse();
                }
                if (request.Skip < 0)
                {
                    builder.Validation.AddValidation("request.Skip", "Must be greater than or equal to 0");
                }
                if (request.Take <= 0)
                {
                    builder.Validation.AddValidation("request.Take", "Must be greater than 0");
                }
                ValidateSearchFor(builder, request.SearchFor);
                ValidateOrderBy(builder, request.OrderBy);
                var filters = ValidateAndSanitizeFilters(builder, request.Filters);
                if (builder.IsOk == false)
                {
                    return builder.BuildResponse();
                }

                var repositoryResult = await _repo.GetResourceGridItemsAsync(
                    request.SearchFor,
                    NormalizeOrderBy(request.OrderBy),
                    request.OrderDirection,
                    request.Skip,
                    ClampTake(request.Take),
                    DefaultTagLimit,
                    filters,
                    cancellationToken);

                var response = new ResourceGridResponse
                {
                    TotalItems = repositoryResult.TotalCount,
                    Items = repositoryResult.GridItems?.Select(MapToResourceGridItemModel).ToList()
                };

                builder.Data.Set(response);
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");  
                return builder.BuildResponse();
            }
        }
        public async Task<ApiServiceResponse<ResourceGridFacetResponse>> GetResourceGridFacet(
            ResourceGridFacetRequest? request, CancellationToken cancellationToken)
        {
            var builder = new ServiceResponseBuilder<ResourceGridFacetResponse>();
            try
            {
                if (request == null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return builder.BuildResponse();
                }

                if (!FacetColumns.Contains(request.Column))
                {
                    builder.Validation.AddValidation("request.Column", "Must be 'ResourceType' or 'Tag'");
                }
                var isTag = string.Equals(request.Column, ColumnTag, StringComparison.Ordinal);
                if (isTag && string.IsNullOrWhiteSpace(request.TagKey))
                {
                    builder.Validation.AddValidation("request.TagKey", "Is required for a Tag facet");
                }
                if (isTag && request.TagKey is { Length: > MaxTagKeyLength })
                {
                    builder.Validation.AddValidation("request.TagKey", $"Must be {MaxTagKeyLength} characters or fewer");
                }
                ValidateSearchFor(builder, request.SearchFor);
                var filters = ValidateAndSanitizeFilters(builder, request.Filters);
                if (builder.IsOk == false)
                {
                    return builder.BuildResponse();
                }

                // Strip the facet's own dimension so it never constrains itself (the sproc also strips).
                var otherFilters = filters
                    .Where(f => !(string.Equals(f.Column, request.Column, StringComparison.Ordinal)
                                  && (!isTag || string.Equals(f.TagKey, request.TagKey, StringComparison.Ordinal))))
                    .ToList();

                var rows = await _repo.GetFilterValuesAsync(
                    request.Column, isTag ? request.TagKey : null, request.SearchFor, otherFilters, cancellationToken);

                var response = new ResourceGridFacetResponse
                {
                    Column = request.Column,
                    TagKey = isTag ? request.TagKey : null,
                    Values = rows.Select(r => new ResourceGridFacetValue
                    {
                        Value = r.Value,
                        IsBlank = r.IsBlank,
                        Count = r.ItemCount,
                        Display = r.IsBlank ? "(blank)" : (r.Value ?? string.Empty)
                    }).ToList()
                };

                builder.Data.Set(response);
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<List<string>>> GetTagFilterKeys(CancellationToken cancellationToken)
        {
            var builder = new ServiceResponseBuilder<List<string>>();
            try
            {
                var keys = await _repo.GetAllTagKeysAsync(cancellationToken);
                builder.Data.Set(keys);
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<OpenEditorResponse>> GetResourceEditorModelAsync(OpenEditorRequest request, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<OpenEditorResponse>();
            try
            {
                var allResourceTypes = await _repo.GetAllResourceTypesAsync(cancellationToken);
                var tagDictionary = await _repo.GetAllTagDefinitionsAsync(cancellationToken);
                var tagDictionaryModels = tagDictionary.Select(MapToTagDefinitionModel).ToList();
                var domainDef = tagDictionary.FirstOrDefault(t => t.IsDomainTag);
                var domainAllowedValues = ParseAllowedValues(domainDef?.AllowedValues) ?? new List<string>();
                var entryPointTemplates = await _repo.GetAllEntryPointTemplatesAsync(cancellationToken);

                var isEditOrView = string.Equals(request.Mode, "Edit", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(request.Mode, "View", StringComparison.OrdinalIgnoreCase);

                ResourceEditorModel editorModel;

                if (isEditOrView && !string.IsNullOrWhiteSpace(request.ResourceUid))
                {
                    var detail = await _repo.GetResourceByUidAsync(request.ResourceUid, cancellationToken);
                    if (detail == null)
                    {
                        builder.Errors.AddError(CallStatusCode.NotFound, "Resource Not Found",
                            $"Resource with UID '{request.ResourceUid}' was not found.", "resourceUid");
                        return builder.BuildResponse();
                    }

                    var typeName = allResourceTypes.FirstOrDefault(t => t.ResourceTypeId == detail.ResourceTypeId)?.TypeName ?? string.Empty;
                    var tagReads = await _repo.GetTagsForResourceAsync(detail.ResourceId, cancellationToken);
                    var relationshipItems = await _repo.GetRelationshipsForResourceAsync(detail.ResourceId, cancellationToken);

                    editorModel = BuildEditEditorModel(detail, typeName, tagReads, relationshipItems, tagDictionaryModels, request.Mode, domainDef?.TagDefinitionId);
                }
                else
                {
                    editorModel = BuildCreateEditorModel();
                }

                builder.Data.Set(new OpenEditorResponse
                {
                    EditorModel = editorModel,
                    ResourceTypes = allResourceTypes.Select(r => new ResourceTypeOption
                    {
                        ResourceTypeId = r.ResourceTypeId,
                        TypeName = r.TypeName,
                        ResourceTypeUid = r.ResourceTypeUid
                    }).ToList(),
                    TagDictionary = tagDictionaryModels,
                    EntryPointTemplates = entryPointTemplates.Select(t => new ResourceTypeEntryPointModel
                    {
                        ResourceTypeId = t.ResourceTypeId,
                        TagDefinitionId = t.TagDefinitionId,
                        IsDefaultPrimary = t.IsDefaultPrimary,
                        RequirementLevel = t.RequirementLevel
                    }).ToList(),
                    DomainAllowedValues = domainAllowedValues
                });
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<ResourceDetailModel>> GetResourceDetailAsync(string resourceUid, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<ResourceDetailModel>();
            try
            {
                if (string.IsNullOrWhiteSpace(resourceUid))
                {
                    builder.Validation.AddValidation("resourceUid", "Resource UID is required");
                    return builder.BuildResponse();
                }

                var detail = await _repo.GetResourceByUidAsync(resourceUid, cancellationToken);
                if (detail == null)
                {
                    builder.Errors.AddError(CallStatusCode.NotFound, "Resource Not Found",
                        $"Resource with UID '{resourceUid}' was not found.", "resourceUid");
                    return builder.BuildResponse();
                }

                builder.Data.Set(await ProjectResourceDetailAsync(detail, cancellationToken));
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<SaveResourceResponse>> SaveResourceAsync(SaveResourceRequest request, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<SaveResourceResponse>();
            try
            {
                if (request == null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return builder.BuildResponse();
                }

                if (request.ResourceTypeId <= 0)
                    builder.Validation.AddValidation("request.ResourceTypeId", "Resource type is required");

                if (string.IsNullOrWhiteSpace(request.ResourceUid))
                    builder.Validation.AddValidation("request.ResourceUid", "Resource UID is required");

                if (string.IsNullOrWhiteSpace(request.ResourceName))
                    builder.Validation.AddValidation("request.ResourceName", "Name is required");
                else if (request.ResourceName.Length > MaxResourceNameLength)
                    builder.Validation.AddValidation("request.ResourceName", $"Must be {MaxResourceNameLength} characters or fewer");

                if (string.IsNullOrWhiteSpace(request.ResourceKey))
                    builder.Validation.AddValidation("request.ResourceKey", "Key is required");
                else if (request.ResourceKey.Length > MaxResourceKeyLength)
                    builder.Validation.AddValidation("request.ResourceKey", $"Must be {MaxResourceKeyLength} characters or fewer");

                if (!builder.IsOk)
                    return builder.BuildResponse();

                var isEdit = string.Equals(request.Mode, "Edit", StringComparison.OrdinalIgnoreCase);
                ResourceDetail? existing = null;
                if (isEdit)
                {
                    existing = await _repo.GetResourceByUidAsync(request.ResourceUid, cancellationToken);
                    if (existing == null)
                    {
                        builder.Errors.AddError(CallStatusCode.NotFound, "Resource Not Found",
                            $"Resource with UID '{request.ResourceUid}' was not found.", "resourceUid");
                        return builder.BuildResponse();
                    }

                    if (existing.ResourceTypeId != request.ResourceTypeId)
                    {
                        builder.Validation.AddValidation("request.ResourceTypeId", "Resource type cannot be changed after save");
                        return builder.BuildResponse();
                    }

                    if (!string.IsNullOrEmpty(existing.Domain) && !string.IsNullOrEmpty(request.Domain)
                        && !string.Equals(existing.Domain, request.Domain, StringComparison.Ordinal))
                    {
                        builder.Validation.AddValidation("request.Domain", "Domain cannot be changed after save");
                        return builder.BuildResponse();
                    }
                }

                var isUnique = await _repo.CheckResourceUniqueAsync(request.ResourceTypeId, request.ResourceKey, request.Domain, request.ResourceUid, cancellationToken);
                if (!isUnique)
                {
                    builder.Validation.AddValidation("request.ResourceKey", "Another resource of this type already uses this key");
                    return builder.BuildResponse();
                }

                var tagDictionary = await _repo.GetAllTagDefinitionsAsync(cancellationToken);
                // Domain is dropped here at the source (not just at the sproc) — a domain-keyed
                // entry in request.Tags, if the client ever sent one, is never validated, written,
                // or eligible as primary.
                var domainDefId = tagDictionary.FirstOrDefault(d => d.IsDomainTag)?.TagDefinitionId;
                var nonEmptyTags = (request.Tags ?? new List<SaveTagValue>())
                    .Where(t => !string.IsNullOrWhiteSpace(t.Value))
                    .Where(t => domainDefId is null || t.TagDefinitionId != domainDefId)
                    .ToList();

                if (!ValidateTagsForSave(builder, nonEmptyTags, tagDictionary))
                    return builder.BuildResponse();

                // RD2: only pass a primary through when it's a Link tag with a value that
                // actually survives this save (the client already filters this; double-safe).
                var effectivePrimaryId = ResolveEffectivePrimary(request.PrimaryTagDefinitionId, nonEmptyTags, tagDictionary);

                // Relationships: validate + resolve BEFORE any write (self-loop / not-found /
                // same-domain — RD2/RD4/RD5/RD14). Create has no resourceId yet, so the actual
                // edge writes are deferred to after Resource_Save below.
                var effectiveDomain = isEdit ? existing!.Domain : request.Domain;
                var (relationshipsValid, resolvedDependsOn, resolvedDependentOn) =
                    await ValidateAndResolveRelationshipsAsync(builder, request, effectiveDomain, cancellationToken);
                if (!relationshipsValid)
                    return builder.BuildResponse();

                var (result, resourceId) = await _repo.SaveResourceAsync(
                    request.ResourceUid, request.ResourceTypeId, request.ResourceKey, request.ResourceName,
                    request.Description, request.Domain, effectivePrimaryId, cancellationToken);

                if (string.Equals(result, "error", StringComparison.Ordinal))
                {
                    builder.Validation.AddValidation("request.ResourceTypeId", "Resource type or Domain cannot be changed after save");
                    return builder.BuildResponse();
                }

                // RD4 (non-atomic): Resource_Save then SetResourceTagsAsync are two sprocs, no
                // shared transaction — matches the import path. Domain is excluded here (the
                // client already excludes it; ResourceTag_SetForResource also skips it —
                // double-safe).
                await _repo.SetResourceTagsAsync(resourceId,
                    nonEmptyTags.Select(t => (t.TagDefinitionKey, t.Value!)).ToList(), cancellationToken);

                // RD8 (non-atomic): reconcile relationship edges after the resource + tags are
                // written, using the resourceId that (in Create mode) only exists post-save.
                await ReconcileRelationshipsAsync(resourceId, resolvedDependsOn, resolvedDependentOn, cancellationToken);

                var saved = await _repo.GetResourceByUidAsync(request.ResourceUid, cancellationToken);
                builder.Data.Set(new SaveResourceResponse
                {
                    ResourceUid = request.ResourceUid,
                    Message = $"Saved {request.ResourceName}",
                    Saved = saved != null ? await ProjectResourceDetailAsync(saved, cancellationToken) : null
                });
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<ResourceUniquenessResponse>> CheckUniquenessAsync(ResourceUniquenessRequest request, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<ResourceUniquenessResponse>();
            try
            {
                if (request == null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return builder.BuildResponse();
                }

                if (request.ResourceTypeId <= 0)
                    builder.Validation.AddValidation("request.ResourceTypeId", "Resource type is required");
                if (string.IsNullOrWhiteSpace(request.ResourceKey))
                    builder.Validation.AddValidation("request.ResourceKey", "Key is required");

                if (!builder.IsOk)
                    return builder.BuildResponse();

                var isUnique = await _repo.CheckResourceUniqueAsync(request.ResourceTypeId, request.ResourceKey, request.Domain, request.ExcludeResourceUid, cancellationToken);

                var typeName = (await _repo.GetAllResourceTypesAsync(cancellationToken))
                    .FirstOrDefault(t => t.ResourceTypeId == request.ResourceTypeId)?.TypeName ?? string.Empty;

                builder.Data.Set(new ResourceUniquenessResponse
                {
                    IsUnique = isUnique,
                    Message = isUnique ? null : "Another resource of this type already uses this key",
                    IdentityDisplay = BuildIdentityDisplay(request.Domain, typeName, request.ResourceKey)
                });
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<object>> DeleteResourceAsync(string resourceUid, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<object>();
            try
            {
                if (string.IsNullOrWhiteSpace(resourceUid))
                {
                    builder.Validation.AddValidation("resourceUid", "Resource UID is required");
                    return builder.BuildResponse();
                }

                var detail = await _repo.GetResourceByUidAsync(resourceUid, cancellationToken);
                if (detail == null)
                {
                    builder.Errors.AddError(CallStatusCode.NotFound, "Resource Not Found",
                        $"Resource with UID '{resourceUid}' was not found.", "resourceUid");
                    return builder.BuildResponse();
                }

                await _repo.DeleteResourceAsync(detail.ResourceId, cancellationToken);
                builder.Data.Set(new object());
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<TagDefinitionModel>> CreateTagDefinitionAsync(CreateTagDefinitionRequest request, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<TagDefinitionModel>();
            try
            {
                if (request == null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return builder.BuildResponse();
                }

                if (string.IsNullOrWhiteSpace(request.TagDefinitionKey))
                    builder.Validation.AddValidation("request.TagDefinitionKey", "Key is required");
                else if (request.TagDefinitionKey.Length > MaxTagDefinitionKeyLength)
                    builder.Validation.AddValidation("request.TagDefinitionKey", $"Must be {MaxTagDefinitionKeyLength} characters or fewer");

                if (!AllowedContentTypes.Contains(request.ContentType))
                    builder.Validation.AddValidation("request.ContentType", "Must be 'Text' or 'Link'");

                string? allowedValuesJson = null;
                if (request.AllowedValues is { Count: > 0 })
                {
                    allowedValuesJson = JsonSerializer.Serialize(request.AllowedValues);
                    if (allowedValuesJson.Length > MaxAllowedValuesLength)
                        builder.Validation.AddValidation("request.AllowedValues", $"Must be {MaxAllowedValuesLength} characters or fewer when serialized");
                }

                if (!builder.IsOk)
                    return builder.BuildResponse();

                var uid = Guid.NewGuid().ToString();
                var (result, tagDefinitionId) = await _repo.CreateTagDefinitionAsync(
                    request.TagDefinitionKey, uid, request.ContentType, request.AllowCustomValue, request.IsMultiValued,
                    allowedValuesJson, request.DisplayName, request.RequirementLevel, request.DisplayOrder, cancellationToken);

                if (string.Equals(result, "error", StringComparison.Ordinal))
                {
                    builder.Validation.AddValidation("request.ContentType", "Must be 'Text' or 'Link'");
                    return builder.BuildResponse();
                }

                // 'skipped' = an existing definition with this key (search-existing-first duplicate);
                // 'created' = a new definition. Either way, return the resulting definition.
                var dictionary = await _repo.GetAllTagDefinitionsAsync(cancellationToken);
                var created = dictionary.FirstOrDefault(d => d.TagDefinitionId == tagDefinitionId);
                if (created == null)
                {
                    builder.Errors.AddError(CallStatusCode.InternalError, "Tag definition could not be loaded after create.");
                    return builder.BuildResponse();
                }

                builder.Data.Set(MapToTagDefinitionModel(created));
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<TagDefinitionModel>> AddAllowedValueAsync(AddAllowedValueRequest request, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<TagDefinitionModel>();
            try
            {
                if (request == null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return builder.BuildResponse();
                }

                var value = request.Value?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(value))
                    builder.Validation.AddValidation("request.Value", "Value is required");

                if (!builder.IsOk)
                    return builder.BuildResponse();

                var dictionary = await _repo.GetAllTagDefinitionsAsync(cancellationToken);
                var definition = dictionary.FirstOrDefault(d => d.TagDefinitionId == request.TagDefinitionId);
                if (definition == null)
                {
                    builder.Errors.AddError(CallStatusCode.NotFound, "Tag definition not found.", null, "TagDefinitionId");
                    return builder.BuildResponse();
                }

                // A free-text definition has no list to extend, and the editor would never render the
                // list even if one were stored (TagRowEditor.IsControlledVocab tests AllowCustomValue).
                // Refusing here keeps a caller from writing a vocabulary that can never be seen.
                if (definition.AllowCustomValue)
                {
                    builder.Validation.AddValidation("request.TagDefinitionId",
                        "This tag takes free text, so it has no list of choices to add to");
                    return builder.BuildResponse();
                }

                // A system-managed definition's shape is owned by deployment, and TagDefinition_Upsert
                // (which this path uses) refuses to write one — it would answer 'skipped' and the
                // caller would be told "could not add" with nothing actionable in it. Say why instead.
                // The domain/Subscription vocabulary is reachable through CreateDomainValueAsync,
                // which appends without being able to alter the definition.
                if (definition.IsSystemTag)
                {
                    builder.Validation.AddValidation("request.TagDefinitionId",
                        "This tag is system-managed, so its list of choices cannot be edited here");
                    return builder.BuildResponse();
                }

                var existing = ParseAllowedValues(definition.AllowedValues) ?? new List<string>();

                // Idempotent by design: two people adding "prod" from two tabs should converge on one
                // entry, not two that differ only by case. The existing spelling wins.
                if (existing.Any(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase)))
                {
                    builder.Data.Set(MapToTagDefinitionModel(definition));
                    return builder.BuildResponse();
                }

                var updated = new List<string>(existing) { value };
                var allowedValuesJson = JsonSerializer.Serialize(updated);
                if (allowedValuesJson.Length > MaxAllowedValuesLength)
                {
                    builder.Validation.AddValidation("request.Value",
                        $"The list would exceed {MaxAllowedValuesLength} characters once serialized");
                    return builder.BuildResponse();
                }

                // ContentType is denormalized onto the row by TagDefinition_GetAll and the sproc
                // resolves it back to an id, so an empty one would come back as a bare 'error' with
                // nothing to explain it. Say so here instead.
                if (string.IsNullOrWhiteSpace(definition.ContentType))
                {
                    builder.Errors.AddError(CallStatusCode.InternalError,
                        "Tag definition has no content type.", null, "ContentType");
                    return builder.BuildResponse();
                }

                var result = await _repo.UpdateTagDefinitionAllowedValuesAsync(
                    definition.TagDefinitionKey, definition.ContentType!, definition.AllowCustomValue,
                    definition.IsMultiValued, allowedValuesJson, cancellationToken);

                if (!string.Equals(result, "updated", StringComparison.Ordinal))
                {
                    builder.Errors.AddError(CallStatusCode.InternalError, "Could not add the value.", result, "InternalError");
                    return builder.BuildResponse();
                }

                // Re-read rather than patching the in-memory copy: the stored list is the authority,
                // and a concurrent add would otherwise be dropped from the caller's picker.
                var refreshed = await _repo.GetAllTagDefinitionsAsync(cancellationToken);
                var saved = refreshed.FirstOrDefault(d => d.TagDefinitionId == definition.TagDefinitionId);
                if (saved == null)
                {
                    builder.Errors.AddError(CallStatusCode.InternalError, "Tag definition could not be loaded after update.");
                    return builder.BuildResponse();
                }

                builder.Data.Set(MapToTagDefinitionModel(saved));
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        /// <summary>Max length of a single domain value, matching DomainValue_Add's @Value parameter.</summary>
        private const int MaxDomainValueLength = 200;

        public async Task<ApiServiceResponse<List<string>>> CreateDomainValueAsync(string value, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<List<string>>();
            try
            {
                var trimmed = value?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(trimmed))
                    builder.Validation.AddValidation("value", "Is required");
                else if (trimmed.Length > MaxDomainValueLength)
                    builder.Validation.AddValidation("value", $"Must be {MaxDomainValueLength} characters or fewer");

                if (!builder.IsOk)
                    return builder.BuildResponse();

                var result = await _repo.AddDomainValueAsync(trimmed, cancellationToken);

                // 'exists' is a success: two people adding the same subscription should converge, and
                // the caller's next step (select it) is valid either way. Only 'error' — no domain tag
                // designated, or the list would overflow the column — is a failure.
                if (!string.Equals(result, "added", StringComparison.Ordinal)
                    && !string.Equals(result, "exists", StringComparison.Ordinal))
                {
                    builder.Errors.AddError(CallStatusCode.InternalError,
                        "Could not create the value.", result, "InternalError");
                    return builder.BuildResponse();
                }

                // Return the stored list rather than the caller's plus one, so a value added elsewhere
                // in the meantime shows up in the picker too.
                var dictionary = await _repo.GetAllTagDefinitionsAsync(cancellationToken);
                var domainDef = dictionary.FirstOrDefault(d => d.IsDomainTag);
                builder.Data.Set(ParseAllowedValues(domainDef?.AllowedValues) ?? new List<string>());
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<ForceDeleteTagResponse>> ForceDeleteTagDefinitionAsync(int tagDefinitionId,
            CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<ForceDeleteTagResponse>();
            try
            {
                if (tagDefinitionId <= 0)
                {
                    builder.Validation.AddValidation("tagDefinitionId", "Is required");
                    return builder.BuildResponse();
                }

                var (result, values, templates, primaries) =
                    await _repo.ForceDeleteTagDefinitionAsync(tagDefinitionId, cancellationToken);

                switch (result)
                {
                    case "deleted":
                        builder.Data.Set(new ForceDeleteTagResponse
                        {
                            Deleted = true,
                            ValuesRemoved = values,
                            TemplatesRemoved = templates,
                            PrimaryLinksCleared = primaries
                        });
                        return builder.BuildResponse();

                    case "system":
                        builder.Data.Set(new ForceDeleteTagResponse
                        {
                            Deleted = false,
                            IsSystemManaged = true,
                            Message = "This tag is system-managed, so it cannot be removed."
                        });
                        return builder.BuildResponse();

                    case "notfound":
                        builder.Errors.AddError(CallStatusCode.NotFound, "Tag definition not found.", null, "TagDefinitionId");
                        return builder.BuildResponse();

                    default:
                        builder.Errors.AddError(CallStatusCode.InternalError, "Could not remove the tag.", result, "InternalError");
                        return builder.BuildResponse();
                }
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<ReassignAndDeleteResponse>> ReassignAndDeleteDomainValueAsync(
            ReassignAndDeleteRequest request, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<ReassignAndDeleteResponse>();
            try
            {
                var from = request?.From?.Trim() ?? string.Empty;
                var to = request?.To?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(from))
                    builder.Validation.AddValidation("request.From", "Is required");
                if (string.IsNullOrWhiteSpace(to))
                    builder.Validation.AddValidation("request.To", "A replacement is required");

                if (!builder.IsOk)
                    return builder.BuildResponse();

                var (result, affected, conflicts) =
                    await _repo.ReassignAndDeleteDomainValueAsync(from, to, cancellationToken);

                builder.Data.Set(MapReassignResult(result, affected, conflicts, from, to,
                    notFoundMessage: $"'{from}' was not found.",
                    missingTargetMessage: $"'{to}' is not one of the available values."));
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<ReassignAndDeleteResponse>> ReassignAndDeleteResourceTypeAsync(
            ReassignAndDeleteRequest request, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<ReassignAndDeleteResponse>();
            try
            {
                if (!int.TryParse(request?.From, out var fromId) || fromId <= 0)
                    builder.Validation.AddValidation("request.From", "Is required");
                if (!int.TryParse(request?.To, out var toId) || toId <= 0)
                    builder.Validation.AddValidation("request.To", "A replacement is required");

                if (!builder.IsOk)
                    return builder.BuildResponse();

                var (result, affected, conflicts) =
                    await _repo.ReassignAndDeleteResourceTypeAsync(fromId, toId, cancellationToken);

                builder.Data.Set(MapReassignResult(result, affected, conflicts, request!.From, request.To,
                    notFoundMessage: "That resource type was not found.",
                    missingTargetMessage: "The replacement resource type was not found."));
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        /// <summary>
        /// One place to turn a reassign procedure's result code into a response, because Subscription and
        /// Resource Type answer with the same vocabulary and must explain themselves identically. Every
        /// refusal is a normal response with a reason — only a genuinely unexpected code is an error.
        /// </summary>
        private static ReassignAndDeleteResponse MapReassignResult(string result, int affected, int conflicts,
            string from, string to, string notFoundMessage, string missingTargetMessage) => result switch
        {
            "reassigned" => new ReassignAndDeleteResponse { Succeeded = true, AffectedResources = affected },

            // Nothing moved: the whole operation is refused rather than partially applied, so the count is
            // what the user has to resolve, not a progress report.
            "conflict" => new ReassignAndDeleteResponse
            {
                Succeeded = false,
                Conflicts = conflicts,
                Message = $"{conflicts} {(conflicts == 1 ? "resource" : "resources")} would end up with a duplicate " +
                          $"identity in '{to}' (same type and key already there). Nothing was changed."
            },

            "same" => new ReassignAndDeleteResponse
            {
                Succeeded = false,
                Message = "The replacement is the same as what is being removed."
            },

            "notfound" => new ReassignAndDeleteResponse { Succeeded = false, Message = notFoundMessage },

            "nonewvalue" or "nonewtype" => new ReassignAndDeleteResponse { Succeeded = false, Message = missingTargetMessage },

            _ => new ReassignAndDeleteResponse { Succeeded = false, Message = "Could not complete the change." }
        };

        public async Task<ApiServiceResponse<List<TagDefinitionUsageModel>>> GetTagDefinitionsWithUsageAsync(CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<List<TagDefinitionUsageModel>>();
            try
            {
                var rows = await _repo.GetTagDefinitionsWithUsageAsync(cancellationToken);
                builder.Data.Set(rows.Select(r => new TagDefinitionUsageModel
                {
                    Definition = MapToTagDefinitionModel(r.Definition),
                    ResourceCount = r.ResourceCount,
                    TypeTemplateCount = r.TypeTemplateCount,
                    PrimaryForCount = r.PrimaryForCount
                }).ToList());
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<TagDefinitionModel>> UpdateTagDefinitionAsync(UpdateTagDefinitionRequest request,
            CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<TagDefinitionModel>();
            try
            {
                if (request == null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return builder.BuildResponse();
                }

                if (string.IsNullOrWhiteSpace(request.TagDefinitionKey))
                    builder.Validation.AddValidation("request.TagDefinitionKey", "Key is required");

                if (!AllowedContentTypes.Contains(request.ContentType))
                    builder.Validation.AddValidation("request.ContentType", "Must be 'Text' or 'Link'");

                // Same rule the create path enforces: "choose from a list" with no list is a tag nobody
                // can fill in. Enforced here too, or an edit could produce what create refuses.
                if (!request.AllowCustomValue && (request.AllowedValues is null || request.AllowedValues.Count == 0))
                    builder.Validation.AddValidation("request.AllowedValues",
                        "Add at least one value to the list, or allow free text");

                string? allowedValuesJson = null;
                if (request.AllowedValues is { Count: > 0 })
                {
                    allowedValuesJson = JsonSerializer.Serialize(request.AllowedValues);
                    if (allowedValuesJson.Length > MaxAllowedValuesLength)
                        builder.Validation.AddValidation("request.AllowedValues", $"Must be {MaxAllowedValuesLength} characters or fewer when serialized");
                }

                if (!builder.IsOk)
                    return builder.BuildResponse();

                var dictionary = await _repo.GetAllTagDefinitionsAsync(cancellationToken);
                var existing = dictionary.FirstOrDefault(d =>
                    string.Equals(d.TagDefinitionKey, request.TagDefinitionKey, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    builder.Errors.AddError(CallStatusCode.NotFound, "Tag definition not found.", null, "TagDefinitionKey");
                    return builder.BuildResponse();
                }

                // Checked before the write as well as inside TagDefinition_Upsert: the procedure answers
                // a bare 'skipped' for a system tag, which on its own is indistinguishable from a
                // duplicate-key skip. Refusing here is what makes the reason sayable.
                if (existing.IsSystemTag || existing.IsDomainTag)
                {
                    builder.Validation.AddValidation("request.TagDefinitionKey",
                        "This tag is system-managed, so it cannot be edited here");
                    return builder.BuildResponse();
                }

                var result = await _repo.UpdateTagDefinitionAsync(
                    existing.TagDefinitionKey, request.ContentType, request.AllowCustomValue, request.IsMultiValued,
                    allowedValuesJson, request.DisplayName, request.RequirementLevel, request.DisplayOrder,
                    cancellationToken);

                if (!string.Equals(result, "updated", StringComparison.Ordinal))
                {
                    builder.Errors.AddError(CallStatusCode.InternalError, "Could not save the tag.", result, "InternalError");
                    return builder.BuildResponse();
                }

                var refreshed = await _repo.GetAllTagDefinitionsAsync(cancellationToken);
                var saved = refreshed.FirstOrDefault(d => d.TagDefinitionId == existing.TagDefinitionId);
                if (saved == null)
                {
                    builder.Errors.AddError(CallStatusCode.InternalError, "Tag definition could not be loaded after save.");
                    return builder.BuildResponse();
                }

                builder.Data.Set(MapToTagDefinitionModel(saved));
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<DeleteTagDefinitionResponse>> DeleteTagDefinitionAsync(int tagDefinitionId,
            CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<DeleteTagDefinitionResponse>();
            try
            {
                if (tagDefinitionId <= 0)
                {
                    builder.Validation.AddValidation("tagDefinitionId", "Is required");
                    return builder.BuildResponse();
                }

                var (result, resourceCount, templateCount, primaryCount) =
                    await _repo.DeleteTagDefinitionAsync(tagDefinitionId, cancellationToken);

                switch (result)
                {
                    case "deleted":
                        builder.Data.Set(new DeleteTagDefinitionResponse { Deleted = true });
                        return builder.BuildResponse();

                    case "system":
                        builder.Data.Set(new DeleteTagDefinitionResponse
                        {
                            Deleted = false,
                            IsSystemManaged = true,
                            Message = "This tag is system-managed, so it cannot be deleted."
                        });
                        return builder.BuildResponse();

                    case "inuse":
                        // Every blocker, not just the first: one trip should tell the user everything they
                        // have to clear, or they discover the next one only after fixing this one.
                        var blockers = new List<string>();
                        if (resourceCount > 0)
                            blockers.Add($"{resourceCount} {(resourceCount == 1 ? "resource carries" : "resources carry")} it");
                        if (templateCount > 0)
                            blockers.Add($"{templateCount} resource {(templateCount == 1 ? "type lists" : "types list")} it in a tag template");
                        if (primaryCount > 0)
                            blockers.Add($"{primaryCount} {(primaryCount == 1 ? "resource uses" : "resources use")} it as the primary link");

                        builder.Data.Set(new DeleteTagDefinitionResponse
                        {
                            Deleted = false,
                            ResourceCount = resourceCount,
                            TypeTemplateCount = templateCount,
                            PrimaryForCount = primaryCount,
                            Message = "Still in use — " + string.Join("; ", blockers) + "."
                        });
                        return builder.BuildResponse();

                    case "notfound":
                        builder.Errors.AddError(CallStatusCode.NotFound, "Tag definition not found.", null, "TagDefinitionId");
                        return builder.BuildResponse();

                    default:
                        builder.Errors.AddError(CallStatusCode.InternalError, "Could not delete the tag.", result, "InternalError");
                        return builder.BuildResponse();
                }
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<List<DomainValueModel>>> GetDomainValuesAsync(CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<List<DomainValueModel>>();
            try
            {
                builder.Data.Set(await _repo.GetDomainValuesAsync(cancellationToken));
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<RenameDomainValueResponse>> RenameDomainValueAsync(RenameDomainValueRequest request,
            CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<RenameDomainValueResponse>();
            try
            {
                if (request == null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return builder.BuildResponse();
                }

                var oldValue = request.OldValue?.Trim() ?? string.Empty;
                var newValue = request.NewValue?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(oldValue))
                    builder.Validation.AddValidation("request.OldValue", "Is required");
                if (string.IsNullOrWhiteSpace(newValue))
                    builder.Validation.AddValidation("request.NewValue", "Is required");
                else if (newValue.Length > MaxDomainValueLength)
                    builder.Validation.AddValidation("request.NewValue", $"Must be {MaxDomainValueLength} characters or fewer");

                // A rename to the identical string is a no-op that would still rewrite every resource
                // row for nothing. A case-only change is NOT caught here: that is a real rename.
                if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
                    builder.Validation.AddValidation("request.NewValue", "Is the same as the current name");

                if (!builder.IsOk)
                    return builder.BuildResponse();

                var (result, affected) = await _repo.RenameDomainValueAsync(oldValue, newValue, cancellationToken);

                switch (result)
                {
                    case "exists":
                        builder.Validation.AddValidation("request.NewValue", $"'{newValue}' already exists");
                        return builder.BuildResponse();
                    case "notfound":
                        builder.Errors.AddError(CallStatusCode.NotFound, $"'{oldValue}' was not found.", null, "OldValue");
                        return builder.BuildResponse();
                    case "renamed":
                        break;
                    default:
                        builder.Errors.AddError(CallStatusCode.InternalError, "Could not rename the value.", result, "InternalError");
                        return builder.BuildResponse();
                }

                var dictionary = await _repo.GetAllTagDefinitionsAsync(cancellationToken);
                var domainDef = dictionary.FirstOrDefault(d => d.IsDomainTag);

                builder.Data.Set(new RenameDomainValueResponse
                {
                    Value = newValue,
                    AffectedResources = affected,
                    AllowedValues = ParseAllowedValues(domainDef?.AllowedValues) ?? new List<string>()
                });
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<DeleteDomainValueResponse>> DeleteDomainValueAsync(string value,
            CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<DeleteDomainValueResponse>();
            try
            {
                var trimmed = value?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    builder.Validation.AddValidation("value", "Is required");
                    return builder.BuildResponse();
                }

                var (result, resourceCount) = await _repo.DeleteDomainValueAsync(trimmed, cancellationToken);

                // The two refusals are normal outcomes the screen turns into a sentence, so they come
                // back as a successful response carrying Deleted = false — mirroring how
                // DeleteResourceTypeAsync reports its own in-use refusal.
                switch (result)
                {
                    case "deleted":
                        builder.Data.Set(new DeleteDomainValueResponse { Deleted = true });
                        return builder.BuildResponse();

                    case "inuse":
                        builder.Data.Set(new DeleteDomainValueResponse
                        {
                            Deleted = false,
                            ResourceCount = resourceCount,
                            Message = $"{resourceCount} {(resourceCount == 1 ? "resource still uses" : "resources still use")} " +
                                      "this value — reassign them first."
                        });
                        return builder.BuildResponse();

                    case "last":
                        builder.Data.Set(new DeleteDomainValueResponse
                        {
                            Deleted = false,
                            WasLastValue = true,
                            Message = "This is the only value left, and a resource cannot be saved without one."
                        });
                        return builder.BuildResponse();

                    case "notfound":
                        builder.Errors.AddError(CallStatusCode.NotFound, $"'{trimmed}' was not found.", null, "Value");
                        return builder.BuildResponse();

                    default:
                        builder.Errors.AddError(CallStatusCode.InternalError, "Could not delete the value.", result, "InternalError");
                        return builder.BuildResponse();
                }
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<List<TagDefinitionModel>>> GetTagDictionaryAsync(CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<List<TagDefinitionModel>>();
            try
            {
                var dictionary = await _repo.GetAllTagDefinitionsAsync(cancellationToken);
                builder.Data.Set(dictionary.Select(MapToTagDefinitionModel).ToList());
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<object>> AddRelationshipAsync(string fromResourceUid, string toResourceUid, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<object>();
            try
            {
                var (isValid, fromId, toId) = await ValidateRelationshipEndpointsAsync(fromResourceUid, toResourceUid, builder, cancellationToken);
                if (!isValid)
                    return builder.BuildResponse();

                await _repo.AddRelationshipAsync(fromId, toId, cancellationToken);
                builder.Data.Set(new object());
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<object>> RemoveRelationshipAsync(string fromResourceUid, string toResourceUid, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<object>();
            try
            {
                var (isValid, fromId, toId) = await ValidateRelationshipEndpointsAsync(fromResourceUid, toResourceUid, builder, cancellationToken);
                if (!isValid)
                    return builder.BuildResponse();

                await _repo.RemoveRelationshipAsync(fromId, toId, cancellationToken);
                builder.Data.Set(new object());
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<ResourcePickerResponse>> SearchResourcesForPickerAsync(ResourcePickerRequest request, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<ResourcePickerResponse>();
            try
            {
                if (request == null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return builder.BuildResponse();
                }

                var skip = request.Skip < 0 ? 0 : request.Skip;
                var take = request.Take is > 0 and <= MaxPickerTake ? request.Take : DefaultPickerTake;

                var (items, totalCount) = await _repo.SearchResourcesForPickerAsync(new ResourcePickerRequest
                {
                    SearchFor = request.SearchFor,
                    Domain = request.Domain,
                    ResourceTypeId = request.ResourceTypeId,
                    ExcludeResourceUid = request.ExcludeResourceUid,
                    Skip = skip,
                    Take = take
                }, cancellationToken);

                builder.Data.Set(new ResourcePickerResponse { Items = items, TotalItems = totalCount });
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        // ---------------- resource type CRUD (PL-28) ----------------

        public async Task<ApiServiceResponse<List<ResourceTypeModel>>> GetResourceTypesAsync(CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<List<ResourceTypeModel>>();
            try
            {
                var rows = await _repo.GetResourceTypesWithUsageAsync(cancellationToken);
                builder.Data.Set(rows.Select(MapToResourceTypeModel).ToList());
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<Dictionary<int, List<EntryPointTagTemplateModel>>>> GetEntryPointTemplatesAsync(
            CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<Dictionary<int, List<EntryPointTagTemplateModel>>>();
            try
            {
                var rows = await _repo.GetAllEntryPointTemplatesAsync(cancellationToken);

                var byType = rows
                    .GroupBy(r => r.ResourceTypeId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(r => new EntryPointTagTemplateModel
                        {
                            TagDefinitionId = r.TagDefinitionId,
                            IsDefaultPrimary = r.IsDefaultPrimary,
                            RequirementLevel = r.RequirementLevel
                        }).ToList());

                builder.Data.Set(byType);
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<ResourceTypeModel>> SaveResourceTypeAsync(SaveResourceTypeRequest request,
            CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<ResourceTypeModel>();
            try
            {
                if (request == null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return builder.BuildResponse();
                }

                var typeName = request.TypeName?.Trim() ?? string.Empty;
                var shortCode = NullIfBlank(request.ShortCode);
                var iconKey = NullIfBlank(request.IconKey);

                if (string.IsNullOrWhiteSpace(typeName))
                    builder.Validation.AddValidation("request.TypeName", "Type name is required");
                else if (typeName.Length > MaxResourceTypeNameLength)
                    builder.Validation.AddValidation("request.TypeName", $"Must be {MaxResourceTypeNameLength} characters or fewer");

                if (shortCode is { Length: > MaxShortCodeLength })
                    builder.Validation.AddValidation("request.ShortCode", $"Must be {MaxShortCodeLength} characters or fewer");

                if (iconKey is { Length: > MaxIconKeyLength })
                    builder.Validation.AddValidation("request.IconKey", $"Must be {MaxIconKeyLength} characters or fewer");

                if (!builder.IsOk)
                    return builder.BuildResponse();

                var isCreate = request.ResourceTypeId == 0;
                var uid = isCreate ? Guid.NewGuid().ToString() : string.Empty;

                var (result, resourceTypeId) = await _repo.SaveResourceTypeAsync(
                    isCreate ? null : request.ResourceTypeId, uid, typeName, shortCode, iconKey,
                    request.AllowCustomTags, cancellationToken);

                switch (result)
                {
                    case "duplicate":
                        builder.Errors.AddError(CallStatusCode.AlreadyExists, "Duplicate Resource Type",
                            $"A resource type named '{typeName}' already exists.", "TypeName");
                        return builder.BuildResponse();
                    case "notfound":
                        builder.Errors.AddError(CallStatusCode.NotFound, "Resource Type Not Found",
                            $"Resource type {request.ResourceTypeId} was not found.", "ResourceTypeId");
                        return builder.BuildResponse();
                    case "created":
                    case "updated":
                        break;
                    default:
                        builder.Errors.AddError(CallStatusCode.InternalError, "Resource type could not be saved.");
                        return builder.BuildResponse();
                }

                // Entry-point template, when the caller edits it. Null means "leave it alone", which
                // is how the editor's inline create-type opts out — it has no template UI, and
                // treating null as "clear" would silently wipe a template on every such save.
                // Runs after the row is saved so a create has an id to attach rows to.
                if (request.EntryPointTags is not null)
                {
                    var templateResult = await _repo.SetEntryPointTemplatesAsync(
                        resourceTypeId, request.EntryPointTags, cancellationToken);

                    if (templateResult != "ok")
                    {
                        // The type itself saved; only the template failed. Say exactly that rather
                        // than implying nothing was written.
                        builder.Errors.AddError(CallStatusCode.InternalError,
                            "Tag template could not be saved",
                            "The resource type was saved, but its tag template was not.", "EntryPointTags");
                        return builder.BuildResponse();
                    }
                }

                // Re-read so the caller gets the persisted row plus its dependency counts (the
                // create path needs the new id and uid; the update path needs the new UpdatedOn).
                var saved = (await _repo.GetResourceTypesWithUsageAsync(cancellationToken))
                    .FirstOrDefault(t => t.ResourceTypeId == resourceTypeId);

                if (saved == null)
                {
                    builder.Errors.AddError(CallStatusCode.InternalError, "Resource type could not be loaded after save.");
                    return builder.BuildResponse();
                }

                builder.Data.Set(MapToResourceTypeModel(saved));
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<DeleteResourceTypeResponse>> DeleteResourceTypeAsync(int resourceTypeId,
            CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<DeleteResourceTypeResponse>();
            try
            {
                if (resourceTypeId == 0)
                {
                    builder.Validation.AddValidation("resourceTypeId", "Resource type id is required");
                    return builder.BuildResponse();
                }

                var (result, dependentCount) = await _repo.DeleteResourceTypeAsync(resourceTypeId, cancellationToken);

                switch (result)
                {
                    case "inuse":
                        // Resource.ResourceTypeId is a NOT NULL FK — name the blocker rather than
                        // letting a raw constraint violation reach the user.
                        builder.Errors.AddError(CallStatusCode.FailedPrecondition, "Resource Type In Use",
                            $"{dependentCount} {(dependentCount == 1 ? "resource uses" : "resources use")} this type. " +
                            "Reassign or delete them before deleting the type.", "ResourceTypeId");
                        return builder.BuildResponse();
                    case "notfound":
                        builder.Errors.AddError(CallStatusCode.NotFound, "Resource Type Not Found",
                            $"Resource type {resourceTypeId} was not found.", "ResourceTypeId");
                        return builder.BuildResponse();
                    case "deleted":
                        builder.Data.Set(new DeleteResourceTypeResponse { Deleted = true, DependentCount = 0 });
                        return builder.BuildResponse();
                    default:
                        builder.Errors.AddError(CallStatusCode.InternalError, "Resource type could not be deleted.");
                        return builder.BuildResponse();
                }
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        private static ResourceTypeModel MapToResourceTypeModel(ResourceTypeUsage row) => new()
        {
            ResourceTypeId = row.ResourceTypeId,
            ResourceTypeUid = row.ResourceTypeUid,
            TypeName = row.TypeName,
            ShortCode = row.ShortCode,
            IconKey = row.IconKey,
            AllowCustomTags = row.AllowCustomTags,
            CreatedOn = row.CreatedOn,
            UpdatedOn = row.UpdatedOn,
            ResourceCount = row.ResourceCount,
            EntryPointTagCount = row.EntryPointTagCount
        };

        private static string? NullIfBlank(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        // ---------------- editor / detail helpers ----------------

        private async Task<ResourceDetailModel> ProjectResourceDetailAsync(ResourceDetail detail, CancellationToken cancellationToken)
        {
            var tags = await _repo.GetTagsForResourceAsync(detail.ResourceId, cancellationToken);
            var relationships = await _repo.GetRelationshipsForResourceAsync(detail.ResourceId, cancellationToken);
            var types = await _repo.GetAllResourceTypesAsync(cancellationToken);

            var typeName = types.FirstOrDefault(t => t.ResourceTypeId == detail.ResourceTypeId)?.TypeName ?? string.Empty;

            var tagModels = tags.Select(t => new ResourceTagModel
            {
                TagDefinitionId = t.TagDefinitionId,
                TagDefinitionKey = t.TagDefinitionKey,
                DisplayName = t.DisplayName,
                ContentType = t.ContentType,
                Value = t.TagValue,
                IsPrimary = t.IsPrimary
            }).ToList();

            var relationshipModels = relationships.Select(r => new ResourceRelationshipModel
            {
                RelationshipId = r.RelationshipId,
                Direction = r.Direction,
                OtherResourceUid = r.OtherResourceUid,
                OtherResourceKey = r.OtherResourceKey,
                OtherResourceName = r.OtherResourceName,
                OtherResourceType = r.OtherResourceType,
                OtherDomain = r.OtherDomain
            }).ToList();

            var primaryLinkUrl = tagModels.FirstOrDefault(t => t.IsPrimary)?.Value;

            return new ResourceDetailModel
            {
                ResourceId = detail.ResourceId,
                ResourceUid = detail.ResourceUid,
                ResourceKey = detail.ResourceKey,
                ResourceName = detail.ResourceName,
                Description = detail.Description,
                ResourceTypeId = detail.ResourceTypeId,
                ResourceTypeName = typeName,
                Domain = detail.Domain,
                PrimaryTagDefinitionId = detail.PrimaryTagDefinitionId,
                PrimaryLinkUrl = primaryLinkUrl,
                IdentityDisplay = BuildIdentityDisplay(detail.Domain, typeName, detail.ResourceKey),
                Tags = tagModels,
                Relationships = relationshipModels,
                CreatedOn = detail.CreatedOn,
                UpdatedOn = detail.UpdatedOn
            };
        }

        private static bool ValidateTagsForSave(ServiceResponseBuilder<SaveResourceResponse> builder,
            List<SaveTagValue> nonEmptyTags, List<TagDefinition> tagDictionary)
        {
            var isValid = true;

            foreach (var tag in nonEmptyTags)
            {
                var def = tagDictionary.FirstOrDefault(d => d.TagDefinitionId == tag.TagDefinitionId);
                if (def is { ContentType: "Link" } && !IsValidLinkValue(tag.Value))
                {
                    builder.Validation.AddValidation("request.Tags", $"'{def.DisplayName ?? def.TagDefinitionKey}' must be a valid http/https URL");
                    isValid = false;
                }
            }

            // Required (Error) tags must have a surviving non-empty value. Domain is excluded —
            // its requiredness is enforced via request.Domain, not the tag list.
            foreach (var def in tagDictionary.Where(d => !d.IsDomainTag && string.Equals(d.RequirementLevel, "Error", StringComparison.Ordinal)))
            {
                if (!nonEmptyTags.Any(t => t.TagDefinitionId == def.TagDefinitionId))
                {
                    builder.Validation.AddValidation("request.Tags", $"'{def.DisplayName ?? def.TagDefinitionKey}' is required");
                    isValid = false;
                }
            }

            return isValid;
        }

        private static bool IsValidLinkValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static int? ResolveEffectivePrimary(int? requestedPrimaryId, List<SaveTagValue> nonEmptyTags, List<TagDefinition> tagDictionary)
        {
            if (requestedPrimaryId is null) return null;

            var def = tagDictionary.FirstOrDefault(d => d.TagDefinitionId == requestedPrimaryId);
            if (def is not { ContentType: "Link" }) return null;

            var survives = nonEmptyTags.Any(t => t.TagDefinitionId == requestedPrimaryId);
            return survives ? requestedPrimaryId : null;
        }

        private static string BuildIdentityDisplay(string? domain, string? typeName, string? key)
        {
            var parts = new[] { domain, typeName, key }.Where(p => !string.IsNullOrWhiteSpace(p));
            return string.Join(" / ", parts);
        }

        private static TagDefinitionModel MapToTagDefinitionModel(TagDefinition def)
        {
            return new TagDefinitionModel
            {
                TagDefinitionId = def.TagDefinitionId,
                TagDefinitionUid = def.TagDefinitionUid,
                TagDefinitionKey = def.TagDefinitionKey,
                DisplayName = def.DisplayName,
                TagContentTypeId = def.TagContentTypeId,
                ContentType = def.ContentType ?? string.Empty,
                RequirementLevel = def.RequirementLevel,
                IsMultiValued = def.IsMultiValued,
                AllowCustomValue = def.AllowCustomValue,
                AllowedValues = ParseAllowedValues(def.AllowedValues),
                IsDomainTag = def.IsDomainTag,
                IsSystemTag = def.IsSystemTag,
                DisplayOrder = def.DisplayOrder
            };
        }

        private static List<string>? ParseAllowedValues(string? allowedValuesJson)
        {
            if (string.IsNullOrWhiteSpace(allowedValuesJson)) return null;
            try
            {
                return JsonSerializer.Deserialize<List<string>>(allowedValuesJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static ResourceEditorModel BuildCreateEditorModel()
        {
            return new ResourceEditorModel
            {
                ResourceUid = Guid.NewGuid().ToString(),
                ResourceType = new SingleValueEditor(),
                Domain = new SingleValueEditor(),
                Name = new SingleValueEditor(),
                Key = new SingleValueEditor(),
                Description = new SingleValueEditor(),
                Mode = "Create",
                IsPersisted = false
            };
        }

        private static ResourceEditorModel BuildEditEditorModel(
            ResourceDetail detail,
            string typeName,
            List<ResourceTagRead> tagReads,
            List<ResourceRelationshipItem> relationshipItems,
            List<TagDefinitionModel> tagDictionary,
            string mode,
            int? domainTagDefinitionId)
        {
            var model = new ResourceEditorModel
            {
                ResourceUid = detail.ResourceUid,
                ResourceType = MakeUnchangedEditor(typeName),
                Domain = MakeUnchangedEditor(detail.Domain),
                Name = MakeUnchangedEditor(detail.ResourceName),
                Key = MakeUnchangedEditor(detail.ResourceKey),
                Description = MakeUnchangedEditor(detail.Description),
                Mode = string.Equals(mode, "View", StringComparison.OrdinalIgnoreCase) ? "View" : "Edit",
                IsPersisted = true,
                ResourceTypeId = detail.ResourceTypeId,
                PrimaryTagDefinitionId = detail.PrimaryTagDefinitionId,
                CreatedOn = detail.CreatedOn,
                UpdatedOn = detail.UpdatedOn,
                IdentityPreview = new IdentityPreview
                {
                    Domain = detail.Domain,
                    ResourceType = typeName,
                    Key = detail.ResourceKey
                }
            };

            // RD1: the domain tag is a ResourceTag row like any other applied tag — exclude it
            // here so it never surfaces as an editable row on the Tags tab.
            model.Tags = tagReads
                .Where(t => domainTagDefinitionId is null || t.TagDefinitionId != domainTagDefinitionId)
                .GroupBy(t => t.TagDefinitionId)
                .Select(g => BuildTagRowEditor(g, tagDictionary))
                .ToList();

            model.DependsOn = relationshipItems
                .Where(r => string.Equals(r.Direction, "DependsOn", StringComparison.Ordinal))
                .Select(BuildDependencyRowEditor)
                .ToList();

            model.DependentOn = relationshipItems
                .Where(r => string.Equals(r.Direction, "DependentOn", StringComparison.Ordinal))
                .Select(BuildDependencyRowEditor)
                .ToList();

            return model;
        }

        private static SingleValueEditor MakeUnchangedEditor(string? value)
        {
            return new SingleValueEditor { OriginalValue = value ?? string.Empty, EditedValue = value ?? string.Empty };
        }

        private static TagRowEditor BuildTagRowEditor(IGrouping<int, ResourceTagRead> group, List<TagDefinitionModel> dictionary)
        {
            var first = group.First();
            var definition = dictionary.FirstOrDefault(d => d.TagDefinitionId == first.TagDefinitionId) ?? new TagDefinitionModel
            {
                TagDefinitionId = first.TagDefinitionId,
                TagDefinitionKey = first.TagDefinitionKey,
                DisplayName = first.DisplayName,
                ContentType = first.ContentType,
                IsSystemTag = first.IsSystemTag,
                IsMultiValued = first.IsMultiValued
            };

            return new TagRowEditor
            {
                Definition = definition,
                TagDefinitionId = first.TagDefinitionId,
                Values = group.Select(t => MakeUnchangedEditor(t.TagValue)).ToList(),
                IsPrimary = group.Any(t => t.IsPrimary),
                IsPreSeeded = false
            };
        }

        private static DependencyRowEditor BuildDependencyRowEditor(ResourceRelationshipItem item)
        {
            return new DependencyRowEditor
            {
                RelationshipId = item.RelationshipId,
                Direction = item.Direction,
                OtherResourceUid = item.OtherResourceUid,
                OtherResourceName = item.OtherResourceName,
                OtherResourceType = item.OtherResourceType,
                OtherDomain = item.OtherDomain
            };
        }

        // ---------------- relationship reconciliation (slice #8) ----------------

        /// <summary>
        /// Validates + resolves every desired relationship uid BEFORE any write: rejects a
        /// self-loop, an unknown uid, or a cross-domain target. Returns the resolved (uid, id)
        /// pairs per direction so ReconcileRelationshipsAsync doesn't need to re-resolve them.
        /// </summary>
        private async Task<(bool IsValid, List<(string Uid, int Id)> DependsOn, List<(string Uid, int Id)> DependentOn)>
            ValidateAndResolveRelationshipsAsync(
                ServiceResponseBuilder<SaveResourceResponse> builder,
                SaveResourceRequest request,
                string? effectiveDomain,
                CancellationToken cancellationToken)
        {
            var isValid = true;
            var dependsOnResolved = new List<(string Uid, int Id)>();
            var dependentOnResolved = new List<(string Uid, int Id)>();

            var directions = new (List<string> Uids, string PropertyKey, List<(string Uid, int Id)> Resolved)[]
            {
                (request.DependsOnUids ?? new List<string>(), "request.DependsOnUids", dependsOnResolved),
                (request.DependentOnUids ?? new List<string>(), "request.DependentOnUids", dependentOnResolved)
            };

            foreach (var (uids, propertyKey, resolved) in directions)
            {
                foreach (var uid in uids.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct(StringComparer.Ordinal))
                {
                    if (string.Equals(uid, request.ResourceUid, StringComparison.Ordinal))
                    {
                        builder.Validation.AddValidation(propertyKey, "A resource cannot depend on itself");
                        isValid = false;
                        continue;
                    }

                    var other = await _repo.GetResourceByUidAsync(uid, cancellationToken);
                    if (other is null)
                    {
                        builder.Validation.AddValidation(propertyKey, $"Resource '{uid}' was not found");
                        isValid = false;
                        continue;
                    }

                    // Both-null domains are equal (the "Unused" state, design §6); a
                    // domain-having side never matches a domain-less side, and vice-versa.
                    var sameDomain = string.IsNullOrEmpty(effectiveDomain)
                        ? string.IsNullOrEmpty(other.Domain)
                        : string.Equals(effectiveDomain, other.Domain, StringComparison.Ordinal);
                    if (!sameDomain)
                    {
                        builder.Validation.AddValidation(propertyKey, $"'{other.ResourceName}' is not in the same domain");
                        isValid = false;
                        continue;
                    }

                    resolved.Add((uid, other.ResourceId));
                }
            }

            return (isValid, dependsOnResolved, dependentOnResolved);
        }

        /// <summary>
        /// Full-set-replace per direction (RD3), scoped so each direction only touches its own
        /// edges (RD1). DependentOn adds/removes write the FAR side — editing "what depends on
        /// me" mutates the other resource's out-edge (RD7).
        /// </summary>
        private async Task ReconcileRelationshipsAsync(
            int resourceId,
            List<(string Uid, int Id)> desiredDependsOn,
            List<(string Uid, int Id)> desiredDependentOn,
            CancellationToken cancellationToken)
        {
            var currentEdges = await _repo.GetRelationshipsForResourceAsync(resourceId, cancellationToken);

            var currentDependsOnIds = currentEdges
                .Where(e => string.Equals(e.Direction, "DependsOn", StringComparison.Ordinal))
                .Select(e => e.OtherResourceId)
                .ToHashSet();
            var currentDependentOnIds = currentEdges
                .Where(e => string.Equals(e.Direction, "DependentOn", StringComparison.Ordinal))
                .Select(e => e.OtherResourceId)
                .ToHashSet();

            var desiredDependsOnIds = desiredDependsOn.Select(d => d.Id).ToHashSet();
            var desiredDependentOnIds = desiredDependentOn.Select(d => d.Id).ToHashSet();

            // DependsOn: out-edges (from=this, to=target).
            foreach (var targetId in desiredDependsOnIds.Except(currentDependsOnIds))
                await _repo.AddRelationshipAsync(resourceId, targetId, cancellationToken);
            foreach (var targetId in currentDependsOnIds.Except(desiredDependsOnIds))
                await _repo.RemoveRelationshipAsync(resourceId, targetId, cancellationToken);

            // DependentOn: in-edges (from=target, to=this) — the system writes the far side.
            foreach (var targetId in desiredDependentOnIds.Except(currentDependentOnIds))
                await _repo.AddRelationshipAsync(targetId, resourceId, cancellationToken);
            foreach (var targetId in currentDependentOnIds.Except(desiredDependentOnIds))
                await _repo.RemoveRelationshipAsync(targetId, resourceId, cancellationToken);
        }

        private async Task<(bool IsValid, int FromId, int ToId)> ValidateRelationshipEndpointsAsync(
            string fromResourceUid, string toResourceUid, ServiceResponseBuilder<object> builder, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fromResourceUid) || string.IsNullOrWhiteSpace(toResourceUid))
            {
                builder.Validation.AddValidation("resourceUid", "Both a from and to resource UID are required");
                return (false, 0, 0);
            }

            if (string.Equals(fromResourceUid, toResourceUid, StringComparison.Ordinal))
            {
                builder.Validation.AddValidation("toResourceUid", "A resource cannot depend on itself");
                return (false, 0, 0);
            }

            var from = await _repo.GetResourceByUidAsync(fromResourceUid, cancellationToken);
            if (from == null)
            {
                builder.Errors.AddError(CallStatusCode.NotFound, "Resource Not Found",
                    $"Resource with UID '{fromResourceUid}' was not found.", "fromResourceUid");
                return (false, 0, 0);
            }

            var to = await _repo.GetResourceByUidAsync(toResourceUid, cancellationToken);
            if (to == null)
            {
                builder.Errors.AddError(CallStatusCode.NotFound, "Resource Not Found",
                    $"Resource with UID '{toResourceUid}' was not found.", "toResourceUid");
                return (false, 0, 0);
            }

            return (true, from.ResourceId, to.ResourceId);
        }

        // ---------------- filter validation / sanitization (§5b) ----------------

        private const string ColumnResourceType = "ResourceType";
        private const string ColumnResourceName = "ResourceName";
        private const string ColumnDescription = "Description";
        private const string ColumnTag = "Tag";
        private const int DefaultTagLimit = 5;
        private const int MaxFilters = 4;
        private const int MaxValuesPerFilter = 500;
        private const int MaxTagKeyLength = 50;
        private const int MaxValueLength = 2000;
        private const int MaxTextLength = 255;
        private const int MaxSearchLength = 255;
        private const int MaxResourceNameLength = 250;
        private const int MaxResourceKeyLength = 250;
        private const int MaxTagDefinitionKeyLength = 50;
        private const int MaxAllowedValuesLength = 2000;
        // Mirror ResourceType's column widths so an over-long value is a validation message
        // rather than a truncation or a SQL error.
        private const int MaxResourceTypeNameLength = 250;
        private const int MaxShortCodeLength = 10;
        private const int MaxIconKeyLength = 40;
        private const int DefaultPickerTake = 20;
        private const int MaxPickerTake = 100;

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.Ordinal) { "Text", "Link" };

        private static readonly HashSet<string> FilterColumns = new(StringComparer.Ordinal)
            { ColumnResourceType, ColumnResourceName, ColumnDescription, ColumnTag };
        private static readonly HashSet<string> FacetColumns = new(StringComparer.Ordinal)
            { ColumnResourceType, ColumnTag };
        private static readonly HashSet<string> OrderByWhitelist = new(StringComparer.Ordinal)
            { "ResourceId", "ResourceUid", "ResourceKey", "ResourceName", "Description", "CreatedOn", "UpdatedOn", "TypeName" };
        private static readonly int[] AllowedTakes = { 50, 100, 500 };

        private static void ValidateSearchFor<T>(ServiceResponseBuilder<T> builder, string? searchFor) where T : class, new()
        {
            if (searchFor is { Length: > MaxSearchLength })
                builder.Validation.AddValidation("request.SearchFor", $"Must be {MaxSearchLength} characters or fewer");
        }

        private static void ValidateOrderBy<T>(ServiceResponseBuilder<T> builder, string? orderBy) where T : class, new()
        {
            if (!string.IsNullOrWhiteSpace(orderBy) && !OrderByWhitelist.Contains(orderBy))
                builder.Validation.AddValidation("request.OrderBy", "Is not a sortable column");
        }

        private static string NormalizeOrderBy(string? orderBy)
            => !string.IsNullOrWhiteSpace(orderBy) && OrderByWhitelist.Contains(orderBy) ? orderBy! : "ResourceName";

        private static int ClampTake(int take)
            => Array.IndexOf(AllowedTakes, take) >= 0 ? take : 100;

        /// <summary>
        /// Validates each filter and returns only the CONSTRAINING ones (empty enumerable selections
        /// and blank text filters are dropped — the "All = no constraint" rule). Adds validation
        /// messages to <paramref name="builder"/> on any hard failure; callers must check IsOk.
        /// </summary>
        private static List<ResourceGridFilterDefinition> ValidateAndSanitizeFilters<T>(
            ServiceResponseBuilder<T> builder, List<ResourceGridFilterDefinition>? filters) where T : class, new()
        {
            var result = new List<ResourceGridFilterDefinition>();
            if (filters == null || filters.Count == 0)
                return result;

            if (filters.Count > MaxFilters)
                builder.Validation.AddValidation("request.Filters", $"A maximum of {MaxFilters} filters is supported");

            foreach (var f in filters)
            {
                var col = f.Column ?? string.Empty;
                if (!FilterColumns.Contains(col))
                {
                    builder.Validation.AddValidation("request.Filters", $"Unknown filter column '{col}'");
                    continue;
                }

                var isTag = string.Equals(col, ColumnTag, StringComparison.Ordinal);
                var isText = string.Equals(col, ColumnResourceName, StringComparison.Ordinal)
                             || string.Equals(col, ColumnDescription, StringComparison.Ordinal);

                if (isTag)
                {
                    if (string.IsNullOrWhiteSpace(f.TagKey))
                    {
                        builder.Validation.AddValidation("request.Filters", "A Tag filter requires a TagKey");
                        continue;
                    }
                    if (f.TagKey!.Length > MaxTagKeyLength)
                    {
                        builder.Validation.AddValidation("request.Filters", $"TagKey must be {MaxTagKeyLength} characters or fewer");
                        continue;
                    }
                }

                if (isText)
                {
                    if (f.Kind != ResourceGridFilterKind.Text || f.Operator != ResourceGridFilterOperator.Contains)
                    {
                        builder.Validation.AddValidation("request.Filters", $"Column '{col}' supports only a 'contains' text filter");
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(f.Text))
                        continue; // non-constraining — drop
                    if (f.Text!.Length > MaxTextLength)
                    {
                        builder.Validation.AddValidation("request.Filters", $"Filter text must be {MaxTextLength} characters or fewer");
                        continue;
                    }
                    result.Add(f);
                    continue;
                }

                // enumerable (ResourceType or Tag)
                if (f.Kind != ResourceGridFilterKind.Enumerable
                    || (f.Operator != ResourceGridFilterOperator.Equals && f.Operator != ResourceGridFilterOperator.NotEquals))
                {
                    builder.Validation.AddValidation("request.Filters", $"Column '{col}' supports only Equals/NotEquals value filters");
                    continue;
                }

                var values = f.Values?.Where(v => v != null).ToList() ?? new List<string>();
                if (values.Count == 0 && !f.IncludeBlank)
                    continue; // non-constraining — drop
                if (values.Count > MaxValuesPerFilter)
                {
                    builder.Validation.AddValidation("request.Filters", $"A filter supports at most {MaxValuesPerFilter} values");
                    continue;
                }
                if (values.Any(v => v.Length > MaxValueLength))
                {
                    builder.Validation.AddValidation("request.Filters", $"A filter value must be {MaxValueLength} characters or fewer");
                    continue;
                }

                result.Add(new ResourceGridFilterDefinition
                {
                    Column = col,
                    TagKey = isTag ? f.TagKey : null,
                    Kind = ResourceGridFilterKind.Enumerable,
                    Operator = f.Operator,
                    Values = values,
                    IncludeBlank = f.IncludeBlank
                });
            }

            return result;
        }

        private static ResourceGridItemModel MapToResourceGridItemModel(ResourceGridItem item)
        {
            return new ResourceGridItemModel
            {
                ResourceUid = item.ResourceUid ?? string.Empty,
                ResourceName = item.ResourceName ?? string.Empty,
                ResourceType = item.ResourceType,
                Description = item.Description ?? string.Empty,
                LastUpdatedOn = item.LastUpdatedOn,
                RelativeLastUpdatedOn = GetRelativeTimeString(item.LastUpdatedOn),
                Tags = item.Tags?.Select(MapToResourceGridTagModel).ToList() ?? new List<ResourceGridTagModel>(),
                TotalTags = item.Tags?.Count
            };
        }

        private static ResourceGridTagModel MapToResourceGridTagModel(ResourceGridTag tag)
        {
            return new ResourceGridTagModel
            {
                TagUid = tag.TagUid ?? string.Empty,
                TagKey = tag.TagKey ?? string.Empty,
                TagDisplayName = tag.TagDisplayName ?? tag.TagKey ?? string.Empty,
                ContentType = tag.ContentType ?? string.Empty,
                TagValue = tag.TagValue ?? string.Empty
            };
        }

        private static string GetRelativeTimeString(DateTime dateTime)
        {
            var now = DateTime.UtcNow;
            var timeSpan = now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{(timeSpan.TotalMinutes > 1 ? "s" : "")} ago";
            
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours > 1 ? "s" : "")} ago";
            
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} day{(timeSpan.TotalDays > 1 ? "s" : "")} ago";
            
            if (timeSpan.TotalDays < 365)
            {
                var months = (int)(timeSpan.TotalDays / 30);
                return $"{months} month{(months > 1 ? "s" : "")} ago";
            }
            
            var years = (int)(timeSpan.TotalDays / 365);
            return $"{years} year{(years > 1 ? "s" : "")} ago";
        }
    }
}
