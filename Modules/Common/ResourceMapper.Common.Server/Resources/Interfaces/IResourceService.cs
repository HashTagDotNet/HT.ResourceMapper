using HT.Api.Service.Contracts;
using ResourceMapper.Common.Shared.Editor;
using ResourceMapper.Common.Shared.Editor.Contracts;
using ResourceMapper.Common.Shared.HomePage.Contracts;
using ResourceMapper.Common.Shared.ResourceTypes;
using ResourceMapper.Common.Shared.ResourceTypes.Contracts;

namespace ResourceMapper.Common.Server.Resources.Interfaces
{
    public interface IResourceService
    {
        Task<ApiServiceResponse<ResourceGridResponse>> GetResourceGridItems(ResourceGridRequest? request,
            CancellationToken cancellationToken);

        /// <summary>Distinct value+count list for a filterable column (ResourceType or a tag key).</summary>
        Task<ApiServiceResponse<ResourceGridFacetResponse>> GetResourceGridFacet(ResourceGridFacetRequest? request,
            CancellationToken cancellationToken);

        /// <summary>Distinct tag keys for the "Add filter" column picker.</summary>
        Task<ApiServiceResponse<List<string>>> GetTagFilterKeys(CancellationToken cancellationToken);

        Task<ApiServiceResponse<OpenEditorResponse>> GetResourceEditorModelAsync(OpenEditorRequest request, CancellationToken cancellationToken = default);

        /// <summary>Details/view read shape for a single resource by its immutable uid.</summary>
        Task<ApiServiceResponse<ResourceDetailModel>> GetResourceDetailAsync(string resourceUid, CancellationToken cancellationToken = default);

        /// <summary>Creates or updates a resource's General-tab fields + primary link (uid-keyed).</summary>
        Task<ApiServiceResponse<SaveResourceResponse>> SaveResourceAsync(SaveResourceRequest request, CancellationToken cancellationToken = default);

        /// <summary>Early uniqueness check for the General tab's identity preview.</summary>
        Task<ApiServiceResponse<ResourceUniquenessResponse>> CheckUniquenessAsync(ResourceUniquenessRequest request, CancellationToken cancellationToken = default);

        /// <summary>Deletes a resource; cascades its relationship edges and tags.</summary>
        Task<ApiServiceResponse<object>> DeleteResourceAsync(string resourceUid, CancellationToken cancellationToken = default);

        /// <summary>Inline tag-definition create (search-existing-first; Domain/System flags forced off).</summary>
        Task<ApiServiceResponse<TagDefinitionModel>> CreateTagDefinitionAsync(CreateTagDefinitionRequest request, CancellationToken cancellationToken = default);

        /// <summary>Full tag dictionary for the "Add tag" picker / refresh after inline create.</summary>
        /// <summary>Appends one choice to a controlled-vocabulary definition's list and returns the
        /// updated definition. Backs the tag rows' "Add {tag}…" affordance. Refuses system-managed
        /// definitions — the domain/Subscription vocabulary has its own narrow path below.</summary>
        Task<ApiServiceResponse<TagDefinitionModel>> AddAllowedValueAsync(AddAllowedValueRequest request, CancellationToken cancellationToken = default);

        /// <summary>Creates a new domain value (a Subscription here) and returns the full refreshed
        /// list, so the caller's picker can offer it and select it. Separate from AddAllowedValueAsync
        /// because the domain tag is system-managed: this goes through DomainValue_Add, which can
        /// append to the vocabulary but cannot touch the tag's shape or flags.</summary>
        Task<ApiServiceResponse<List<string>>> CreateDomainValueAsync(string value, CancellationToken cancellationToken = default);

        Task<ApiServiceResponse<List<TagDefinitionModel>>> GetTagDictionaryAsync(CancellationToken cancellationToken = default);

        /// <summary>Adds a DependsOn edge between two resources (idempotent; rejects self-loops).</summary>
        Task<ApiServiceResponse<object>> AddRelationshipAsync(string fromResourceUid, string toResourceUid, CancellationToken cancellationToken = default);

        Task<ApiServiceResponse<object>> RemoveRelationshipAsync(string fromResourceUid, string toResourceUid, CancellationToken cancellationToken = default);

        /// <summary>Same-domain resource search backing the Dependencies / Dependent On tabs' picker.</summary>
        Task<ApiServiceResponse<ResourcePickerResponse>> SearchResourcesForPickerAsync(ResourcePickerRequest request, CancellationToken cancellationToken = default);

        /// <summary>All resource types with their dependency counts, for the /resource-types management screen.</summary>
        Task<ApiServiceResponse<List<ResourceTypeModel>>> GetResourceTypesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Every type's entry-point template, keyed by ResourceTypeId. Returns all types in one call
        /// (as ResourceTypeTag_GetAll does) so the management screen can edit any row without a
        /// per-type round trip.
        /// </summary>
        Task<ApiServiceResponse<Dictionary<int, List<EntryPointTagTemplateModel>>>> GetEntryPointTemplatesAsync(
            CancellationToken cancellationToken = default);

        /// <summary>Creates (ResourceTypeId 0) or updates a resource type, including the
        /// ShortCode/IconKey pair the explorer renders from.</summary>
        Task<ApiServiceResponse<ResourceTypeModel>> SaveResourceTypeAsync(SaveResourceTypeRequest request, CancellationToken cancellationToken = default);

        /// <summary>Deletes a resource type. Refused (FailedPrecondition) while any resource still
        /// uses it — the response names the dependent count instead of surfacing an FK error.</summary>
        Task<ApiServiceResponse<DeleteResourceTypeResponse>> DeleteResourceTypeAsync(int resourceTypeId, CancellationToken cancellationToken = default);
    }
}
