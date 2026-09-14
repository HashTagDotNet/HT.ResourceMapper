using HT.Api.Client.Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using ResourceMapper.Common.Server.Identity;
using ResourceMapper.Common.Server.SavedViews.Interfaces;
using ResourceMapper.Common.Shared.SavedViews;
using ResourceMapper.Common.Shared.SavedViews.Contracts;

namespace ResourceMapper.UI.Web.Controllers.Api;

/// <summary>
/// CRUD for saved grid views.
/// <para>
/// The owner is resolved server-side from <see cref="ICurrentIdentity"/> and is never taken from
/// the request, so a caller cannot ask for someone else's views by naming them.
/// </para>
/// <para>
/// The Blazor components call <see cref="ISavedViewService"/> in-process rather than through these
/// endpoints — this is server-side Blazor, so a loopback HTTP hop would be pure overhead. This
/// controller is a second caller of the same service, not a layer the UI routes through.
/// </para>
/// </summary>
[Route("api/saved-views")]
[Produces("application/json")]
[Tags("Saved Views")]
public class SavedViewsController : ApiControllerBase
{
    private readonly ISavedViewService _svc;
    private readonly ICurrentIdentity _identity;

    public SavedViewsController(ISavedViewService service, ICurrentIdentity identity)
    {
        _svc = service;
        _identity = identity;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SavedViewModel>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<List<SavedViewModel>>), 400)]
    [ProducesResponseType(typeof(ApiResponse<List<SavedViewModel>>), 500)]
    public async Task<ActionResult<ApiResponse<List<SavedViewModel>>>> List(
        CancellationToken cancellationToken = default)
        => MapServiceResponseToActionResult(await _svc.ListAsync(_identity.OwnerId, cancellationToken));

    /// <summary>
    /// Create or update. An empty savedViewUid creates; a populated one updates, which is also how
    /// rename and overwrite-an-existing-name are expressed.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SavedViewModel>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SavedViewModel>), 400)]
    [ProducesResponseType(typeof(ApiResponse<SavedViewModel>), 500)]
    public async Task<ActionResult<ApiResponse<SavedViewModel>>> Save(
        [FromBody] SaveViewRequest request,
        CancellationToken cancellationToken = default)
        => MapServiceResponseToActionResult(await _svc.SaveAsync(_identity.OwnerId, request, cancellationToken));

    /// <remarks>
    /// Deleting a view that is already gone is reported as <c>CallStatusCode.NotFound</c>, but the
    /// shared response builder currently maps that to HTTP 422 rather than 404 — the pre-existing
    /// PL-01 defect, which has two failing tests of its own in HT.Api.Service.Contracts. The 404
    /// annotation below states the intent; fixing the mapping fixes every controller at once.
    /// </remarks>
    [HttpDelete("{savedViewUid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        string savedViewUid,
        CancellationToken cancellationToken = default)
        => MapServiceResponseToActionResult(
            await _svc.DeleteAsync(_identity.OwnerId, savedViewUid, cancellationToken));

    [HttpPut("{savedViewUid}/default")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<object>>> SetDefault(
        string savedViewUid,
        CancellationToken cancellationToken = default)
        => MapServiceResponseToActionResult(
            await _svc.SetDefaultAsync(_identity.OwnerId, savedViewUid, cancellationToken));

    /// <summary>Clears the default entirely, leaving the grid to fall back to the resume setting.</summary>
    [HttpPut("default")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<object>>> ClearDefault(
        CancellationToken cancellationToken = default)
        => MapServiceResponseToActionResult(
            await _svc.SetDefaultAsync(_identity.OwnerId, null, cancellationToken));

    /// <summary>Applies a whole new ordering. Position in the list becomes the sort order.</summary>
    [HttpPut("order")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<object>>> Reorder(
        [FromBody] List<string> uidsInOrder,
        CancellationToken cancellationToken = default)
        => MapServiceResponseToActionResult(
            await _svc.ReorderAsync(_identity.OwnerId, uidsInOrder, cancellationToken));
}
