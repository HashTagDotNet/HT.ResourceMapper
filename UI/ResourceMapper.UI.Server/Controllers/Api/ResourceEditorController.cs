using HT.Api.Service.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ResourceMapper.Common.Shared.Editor.Contracts;

namespace ResourceMapper.UI.Server.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResourceEditorController : ApiControllerBase
    {

        Task<ApiServiceResponse<OpenEditorResponse>> GetResourceEditorModelAsync(OpenEditorRequest request, CancellationToken cancellationToken = default)
        {

        }
    }
}
