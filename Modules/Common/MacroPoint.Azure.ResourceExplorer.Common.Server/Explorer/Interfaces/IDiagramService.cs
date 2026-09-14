using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MacroPoint.Azure.ResourceExplorer.Api.Service.Contracts;
using MacroPoint.Azure.ResourceExplorer.Common.Shared.Explorer.Diagrams;
using MacroPoint.Azure.ResourceExplorer.Common.Shared.Explorer.Diagrams.Contracts;

namespace MacroPoint.Azure.ResourceExplorer.Common.Server.Explorer.Interfaces
{
    public interface IDiagramService
    {
        Task<ApiServiceResponse<SaveDiagramResponse>> SaveAsync(string ownerId, SaveDiagramRequest request, CancellationToken cancellationToken = default);
        Task<ApiServiceResponse<List<DiagramListItem>>> ListForClientAsync(string ownerId, CancellationToken cancellationToken = default);
        Task<ApiServiceResponse<DiagramModel>> GetByShareIdAsync(string shareId, string? callerOwnerId, CancellationToken cancellationToken = default);
        Task<ApiServiceResponse<SaveDiagramResponse>> SaveCopyAsync(string ownerId, string shareId, string? newName, CancellationToken cancellationToken = default);
        Task<ApiServiceResponse<object>> DeleteAsync(string ownerId, string diagramUid, CancellationToken cancellationToken = default);
    }
}
