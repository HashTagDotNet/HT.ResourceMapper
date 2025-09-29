using HT.Api.Service.Contracts;
using ResourceMapper.Common.Shared.Editor.Contracts;
using ResourceMapper.Common.Shared.HomePage.Contracts;

namespace ResourceMapper.Common.Server.Resources.Interfaces
{
    public interface IResourceService
    {
        Task<ApiServiceResponse<ResourceGridResponse>> GetResourceGridItems(ResourceGridRequest? request,
            CancellationToken cancellationToken);

        Task<ApiServiceResponse<OpenEditorResponse>> GetResourceEditorModelAsync(OpenEditorRequest request, CancellationToken cancellationToken = default);
    }
}
