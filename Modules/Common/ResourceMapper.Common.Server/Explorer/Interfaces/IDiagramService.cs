using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HT.Api.Service.Contracts;
using ResourceMapper.Common.Shared.Explorer.Diagrams;
using ResourceMapper.Common.Shared.Explorer.Diagrams.Contracts;

namespace ResourceMapper.Common.Server.Explorer.Interfaces
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
