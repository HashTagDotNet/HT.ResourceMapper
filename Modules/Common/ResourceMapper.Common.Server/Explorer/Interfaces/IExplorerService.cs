using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HT.Api.Service.Contracts;
using ResourceMapper.Common.Shared.Explorer;

namespace ResourceMapper.Common.Server.Explorer.Interfaces
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
