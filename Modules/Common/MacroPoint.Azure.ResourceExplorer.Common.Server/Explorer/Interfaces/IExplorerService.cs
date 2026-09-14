using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MacroPoint.Azure.ResourceExplorer.Api.Service.Contracts;
using MacroPoint.Azure.ResourceExplorer.Common.Shared.Explorer;

namespace MacroPoint.Azure.ResourceExplorer.Common.Server.Explorer.Interfaces
{
    public interface IExplorerService
    {
        /// <param name="resourceUid">The center resource of this read.</param>
        /// <param name="knownResourceUids">
        /// Uids already on the caller's canvas, so edges between the newly returned nodes and nodes
        /// already drawn are included in <see cref="ExplorerNodeModel.Edges"/>.
        /// </param>
        /// <param name="cancellationToken"></param>
        Task<ApiServiceResponse<ExplorerNodeModel>> GetNodeAsync(string resourceUid,
            IReadOnlyCollection<string>? knownResourceUids = null,
            CancellationToken cancellationToken = default);
    }
}
