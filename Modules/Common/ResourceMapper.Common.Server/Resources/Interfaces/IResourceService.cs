using HT.Api.Service.Contracts;
using ResourceMapper.Common.Shared.HomePage.Contracts;

namespace ResourceMapper.Common.Server.Resources.Interfaces
{
    public interface IResourceService
    {
        Task<ApiServiceResponse<ResourceGridResponse>> GetResourceGridItems(ResourceGridRequest? request,
            CancellationToken cancellationToken);
    }
}
