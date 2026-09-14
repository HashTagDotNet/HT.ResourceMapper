using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MacroPoint.Azure.ResourceExplorer.Common.Server.Resources.Models;

namespace MacroPoint.Azure.ResourceExplorer.Common.Server.Explorer.Interfaces
{
    public interface IExplorerRepository
    {
        /// <param name="resourceUid">The center resource of this read.</param>
        /// <param name="knownResourceUids">
        /// Uids already drawn on the caller's canvas. Widens the scope of the returned 'Edge' rows
        /// so relationships between the new nodes and nodes already present are backfilled. Null or
        /// empty means "scope the edges to this hop only".
        /// </param>
        /// <param name="cancellationToken"></param>
        Task<List<ExplorerNodeRow>> GetForExplorerAsync(string resourceUid,
            IReadOnlyCollection<string>? knownResourceUids, CancellationToken cancellationToken);
    }
}
