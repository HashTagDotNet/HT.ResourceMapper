using HT.Api.Service.Contracts;
using ResourceMapper.Common.Shared.SavedViews;
using ResourceMapper.Common.Shared.SavedViews.Contracts;

namespace ResourceMapper.Common.Server.SavedViews.Interfaces
{
    /// <summary>
    /// CRUD for saved grid views. Every method takes the owner explicitly rather than resolving it
    /// internally, so the service stays testable and the caller (component or controller) is the
    /// one place that touches ICurrentIdentity.
    /// </summary>
    public interface ISavedViewService
    {
        Task<ApiServiceResponse<List<SavedViewModel>>> ListAsync(string ownerId, CancellationToken cancellationToken);

        Task<ApiServiceResponse<SavedViewModel>> SaveAsync(
            string ownerId, SaveViewRequest request, CancellationToken cancellationToken);

        Task<ApiServiceResponse<object>> DeleteAsync(string ownerId, string savedViewUid, CancellationToken cancellationToken);

        Task<ApiServiceResponse<object>> SetDefaultAsync(string ownerId, string? savedViewUid, CancellationToken cancellationToken);

        Task<ApiServiceResponse<object>> ReorderAsync(
            string ownerId, IReadOnlyList<string> uidsInOrder, CancellationToken cancellationToken);
    }
}
