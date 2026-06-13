using HT.Api.Client.Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Shared.Import.Contracts;

namespace ResourceMapper.UI.Server.Controllers.Api;

/// <summary>
/// API controller for bulk importing resources and reference data from a JSON document
/// </summary>
[Route("api/resources")]
[Produces("application/json")]
[Tags("Resource Import")]
public class ResourceImportController : ApiControllerBase
{
    private readonly IImportService _svc;

    public ResourceImportController(IImportService service)
    {
        _svc = service;
    }

    /// <summary>
    /// Imports 0..N resources plus optional reference data (resource types, tag definitions)
    /// </summary>
    /// <param name="request">The import document including conflict policy, reference data, and resources</param>
    /// <param name="cancellationToken">Cancellation token for the request</param>
    /// <returns>A summary of created/updated/skipped counts per section, or a flat error list</returns>
    /// <response code="200">Import succeeded; summary contains per-section counts</response>
    /// <response code="400">Validation failed; nothing was written - see data.errors for detail</response>
    /// <response code="500">Internal server error occurred; partial writes may have occurred</response>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ApiResponse<ImportResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<ImportResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<ImportResponse>), 500)]
    public async Task<ActionResult<ApiResponse<ImportResponse>>> Import(
        [FromBody] ImportRequest request,
        CancellationToken cancellationToken = default)
    {
        var svcResponse = await _svc.ImportAsync(request, cancellationToken);
        return MapServiceResponseToActionResult(svcResponse);
    }
}
