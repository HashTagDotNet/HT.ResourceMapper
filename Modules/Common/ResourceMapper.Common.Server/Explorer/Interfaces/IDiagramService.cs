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
        Task<ApiServiceResponse<SaveDiagramResponse>> SaveAsync(string clientId, SaveDiagramRequest request, CancellationToken cancellationToken = default);
        Task<ApiServiceResponse<List<DiagramListItem>>> ListForClientAsync(string clientId, CancellationToken cancellationToken = default);
        Task<ApiServiceResponse<DiagramModel>> GetByShareIdAsync(string shareId, CancellationToken cancellationToken = default);
        Task<ApiServiceResponse<SaveDiagramResponse>> SaveCopyAsync(string clientId, string shareId, string? newName, CancellationToken cancellationToken = default);
        Task<ApiServiceResponse<object>> DeleteAsync(string clientId, string diagramUid, CancellationToken cancellationToken = default);
    }
}
