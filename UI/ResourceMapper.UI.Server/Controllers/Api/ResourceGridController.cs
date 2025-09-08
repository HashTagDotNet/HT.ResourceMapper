using HT.Api.Client.Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Shared.HomePage.Contracts;
using System.Net;

namespace ResourceMapper.UI.Server.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ResourceGridController : ControllerBase
{
    private readonly IResourceService _svc;

    public ResourceGridController(IResourceService service)
    {
        _svc = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ResourceGridResponse>>> GetResourceGridItem([FromQuery] ResourceGridRequest request, CancellationToken cancellationToken = default)
    {
        var svcResponse = await _svc.GetResourceGridItems(request, cancellationToken);
        return StatusCode((int)( HttpStatusCode.OK), svcResponse.ApiResponse);
    }

}