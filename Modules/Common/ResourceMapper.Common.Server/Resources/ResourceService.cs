using HT.Api.Client.Contracts.Models;
using HT.Api.Service.Contracts;
using HT.Api.Service.Contracts.BuildersOfT;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Server.Resources.Models;
using ResourceMapper.Common.Shared.HomePage.Contracts;

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
                if (builder.IsOk == false)
                {
                    return builder.BuildResponse();
                }

                var gridItems = await _repo.GetResourceGridItemsAsync(request.SearchFor,request.OrderBy,request.OrderDirection,request.Skip,request.Take, cancellationToken);
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");  
                return builder.BuildResponse();
            }
        }
    }
}
