using HT.Api.Client.Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Shared.HomePage.Contracts;
using System.Net;
using HT.Api.Service.Contracts;
using System.ComponentModel.DataAnnotations;
using ResourceMapper.Common.Shared.Editor.Contracts;

namespace ResourceMapper.UI.Server.Controllers.Api;

/// <summary>
/// API controller for managing resource grid operations including pagination, search, and filtering
/// </summary>

[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Resource Grid")]
public class ResourceGridController : ApiControllerBase
{
    private readonly IResourceService _svc;

    public ResourceGridController(IResourceService service)
    {
        _svc = service;
    }

    /// <summary>
    /// Retrieves a paginated list of resources with their associated tags
    /// </summary>
    /// <param name="request">The grid request parameters including pagination, search, and sorting options</param>
    /// <param name="cancellationToken">Cancellation token for the request</param>
    /// <returns>A paginated response containing resource items and total count</returns>
    /// <remarks>
    /// This endpoint supports:
    /// - Pagination via Skip and Take parameters
    /// - Free-text search across resource names, descriptions, and tags
    /// - Sorting by various resource properties (ResourceName, ResourceType, Description, CreatedOn, UpdatedOn)
    /// - Filtering capabilities through the Filters collection
    /// 
    /// Example requests:
    /// 
    /// Basic pagination:
    /// GET /api/ResourceGrid?skip=0&amp;take=10
    /// 
    /// Search with sorting:
    /// GET /api/ResourceGrid?skip=0&amp;take=10&amp;searchFor=database&amp;orderBy=ResourceName&amp;orderDirection=Asc
    /// 
    /// Advanced filtering:
    /// GET /api/ResourceGrid?skip=0&amp;take=10&amp;filters[0].column=ResourceType&amp;filters[0].operator=Contains&amp;filters[0].value=API
    /// </remarks>
    /// <response code="200">Successfully retrieved resource grid items</response>
    /// <response code="400">Invalid request parameters - check validation errors in the response</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ResourceGridResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<ResourceGridResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<ResourceGridResponse>), 500)]
    public async Task<ActionResult<ApiResponse<ResourceGridResponse>>> GetResourceGridItem(
        [FromQuery] ResourceGridRequest request, 
        CancellationToken cancellationToken = default)
    {
        var svcResponse = await _svc.GetResourceGridItems(request, cancellationToken);
        return MapServiceResponseToActionResult(svcResponse);
    }

    [HttpGet("editor")]
    public Task<ActionResult<ApiResponse<OpenEditorResponse>>> GetResourceEditorForm()
    {
        throw new NotImplementedException();
    }
}