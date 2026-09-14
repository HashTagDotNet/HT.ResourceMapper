using MacroPoint.Azure.ResourceExplorer.Api.Client.Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using MacroPoint.Azure.ResourceExplorer.Common.Server.Resources.Interfaces;
using MacroPoint.Azure.ResourceExplorer.Common.Shared.Import.Contracts;

namespace MacroPoint.Azure.ResourceExplorer.UI.Web.Controllers.Api;

/// <summary>
/// API controller for bulk importing resources and reference data from a JSON document.
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
