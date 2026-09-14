using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ResourceMapper.Common.Server.Explorer.Models;

namespace ResourceMapper.Common.Server.Explorer.Interfaces
{
    public interface IDiagramRepository
    {
        Task<DiagramUpsertResult> UpsertAsync(string diagramUid, string shareId, string ownerId,
            string name, string seedResourceUid, string displayPreset, string diagramJson,
            CancellationToken cancellationToken);

        Task<DiagramRow?> GetByShareIdAsync(string shareId, CancellationToken cancellationToken);

        Task<List<DiagramListRow>> ListForClientAsync(string ownerId, CancellationToken cancellationToken);

        Task<bool> DeleteAsync(string ownerId, string diagramUid, CancellationToken cancellationToken);
    }
}
