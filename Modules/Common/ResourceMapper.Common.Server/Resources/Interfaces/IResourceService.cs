using HT.Api.Service.Contracts;
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
    }
}
