using MacroPoint.Azure.ResourceExplorer.Common.Shared.SavedViews;

namespace MacroPoint.Azure.ResourceExplorer.Common.Server.SavedViews.Interfaces
{
    public interface ISavedViewRepository
    {
        Task<List<SavedViewModel>> ListAsync(string ownerId, CancellationToken cancellationToken);

        /// <summary>
        /// Creates or updates. Result is created | updated | denied, where 'denied' means the uid
        /// exists under a different owner.
        /// </summary>
        Task<(string Result, string SavedViewUid)> UpsertAsync(
            string ownerId, string savedViewUid, string name, string queryString,
            CancellationToken cancellationToken);

        Task<int> DeleteAsync(string ownerId, string savedViewUid, CancellationToken cancellationToken);

        /// <summary>Pass a null uid to clear the owner's default entirely.</summary>
        Task SetDefaultAsync(string ownerId, string? savedViewUid, CancellationToken cancellationToken);

        Task ReorderAsync(string ownerId, IReadOnlyList<string> uidsInOrder, CancellationToken cancellationToken);
    }
}
