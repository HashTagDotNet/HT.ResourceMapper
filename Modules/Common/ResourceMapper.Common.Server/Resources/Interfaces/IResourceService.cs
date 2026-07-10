using HT.Api.Service.Contracts;
using ResourceMapper.Common.Shared.Editor;
using ResourceMapper.Common.Shared.Editor.Contracts;
using ResourceMapper.Common.Shared.HomePage.Contracts;

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
        Task<ApiServiceResponse<List<TagDefinitionModel>>> GetTagDictionaryAsync(CancellationToken cancellationToken = default);

        /// <summary>Adds a DependsOn edge between two resources (idempotent; rejects self-loops).</summary>
        Task<ApiServiceResponse<object>> AddRelationshipAsync(string fromResourceUid, string toResourceUid, CancellationToken cancellationToken = default);

        Task<ApiServiceResponse<object>> RemoveRelationshipAsync(string fromResourceUid, string toResourceUid, CancellationToken cancellationToken = default);

        /// <summary>Same-domain resource search backing the Dependencies / Dependent On tabs' picker.</summary>
        Task<ApiServiceResponse<ResourcePickerResponse>> SearchResourcesForPickerAsync(ResourcePickerRequest request, CancellationToken cancellationToken = default);
    }
}
