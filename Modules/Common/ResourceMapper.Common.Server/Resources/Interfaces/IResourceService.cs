using HT.Api.Service.Contracts;
using ResourceMapper.Common.Shared.Cascade;
using ResourceMapper.Common.Shared.Domains;
using ResourceMapper.Common.Shared.Domains.Contracts;
using ResourceMapper.Common.Shared.Editor;
using ResourceMapper.Common.Shared.Editor.Contracts;
using ResourceMapper.Common.Shared.HomePage.Contracts;
using ResourceMapper.Common.Shared.ResourceTypes;
using ResourceMapper.Common.Shared.ResourceTypes.Contracts;
using ResourceMapper.Common.Shared.Tags;
using ResourceMapper.Common.Shared.Tags.Contracts;

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

        /// <summary>Appends one choice to a controlled-vocabulary definition's list and returns the
        /// updated definition. Backs the tag rows' "Add {tag}…" affordance. Refuses system-managed
        /// definitions — the domain/Subscription vocabulary has its own narrow path below.</summary>
        Task<ApiServiceResponse<TagDefinitionModel>> AddAllowedValueAsync(AddAllowedValueRequest request, CancellationToken cancellationToken = default);

        /// <summary>Creates a new domain value (a Subscription here) and returns the full refreshed
        /// list, so the caller's picker can offer it and select it. Separate from AddAllowedValueAsync
        /// because the domain tag is system-managed: this goes through DomainValue_Add, which can
        /// append to the vocabulary but cannot touch the tag's shape or flags.</summary>
        Task<ApiServiceResponse<List<string>>> CreateDomainValueAsync(string value, CancellationToken cancellationToken = default);

        /// <summary>Every domain value with its resource count, for the management screen. Includes
        /// values carried by resources but missing from the vocabulary.</summary>
        Task<ApiServiceResponse<List<DomainValueModel>>> GetDomainValuesAsync(CancellationToken cancellationToken = default);

        /// <summary>Renames a domain value in the vocabulary and on every resource carrying it. The
        /// response reports how many resources were rewritten.</summary>
        Task<ApiServiceResponse<RenameDomainValueResponse>> RenameDomainValueAsync(RenameDomainValueRequest request, CancellationToken cancellationToken = default);

        /// <summary>Removes a domain value. Refuses while resources carry it, and refuses to leave the
        /// vocabulary empty — both come back as a normal response with a reason, not an error.</summary>
        Task<ApiServiceResponse<DeleteDomainValueResponse>> DeleteDomainValueAsync(string value, CancellationToken cancellationToken = default);

        /// <summary>Full tag dictionary for the "Add tag" picker / refresh after inline create.</summary>
        Task<ApiServiceResponse<List<TagDefinitionModel>>> GetTagDictionaryAsync(CancellationToken cancellationToken = default);

        /// <summary>"Remove everywhere" for a tag: deletes it and every reference — recorded values,
        /// template entries, primary-link choices — reporting what was destroyed. Never deletes resources,
        /// and still refuses system-managed tags.</summary>
        Task<ApiServiceResponse<ForceDeleteTagResponse>> ForceDeleteTagDefinitionAsync(int tagDefinitionId, CancellationToken cancellationToken = default);

        /// <summary>"Remove everywhere" for a domain value: moves its resources onto another value, then
        /// deletes it. Refuses when the move would duplicate an identity.</summary>
        Task<ApiServiceResponse<ReassignAndDeleteResponse>> ReassignAndDeleteDomainValueAsync(ReassignAndDeleteRequest request, CancellationToken cancellationToken = default);

        /// <summary>"Remove everywhere" for a resource type: moves its resources onto another type, then
        /// deletes it and its entry-point template. Refuses on identity collisions.</summary>
        Task<ApiServiceResponse<ReassignAndDeleteResponse>> ReassignAndDeleteResourceTypeAsync(ReassignAndDeleteRequest request, CancellationToken cancellationToken = default);

        /// <summary>Every tag definition with the three counts that reference it, for the tag manager.</summary>
        Task<ApiServiceResponse<List<TagDefinitionUsageModel>>> GetTagDefinitionsWithUsageAsync(CancellationToken cancellationToken = default);

        /// <summary>Edits an existing tag definition. The key identifies it and is not itself editable —
        /// import and export documents name it. Refuses system-managed definitions.</summary>
        Task<ApiServiceResponse<TagDefinitionModel>> UpdateTagDefinitionAsync(UpdateTagDefinitionRequest request, CancellationToken cancellationToken = default);

        /// <summary>Deletes a tag definition only when nothing references it. Refusals come back as a
        /// normal response naming every reference in the way, not as an error.</summary>
        Task<ApiServiceResponse<DeleteTagDefinitionResponse>> DeleteTagDefinitionAsync(int tagDefinitionId, CancellationToken cancellationToken = default);

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
