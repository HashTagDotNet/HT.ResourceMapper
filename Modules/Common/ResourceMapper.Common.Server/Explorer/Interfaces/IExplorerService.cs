using System.Threading;
using System.Threading.Tasks;
using HT.Api.Service.Contracts;
using ResourceMapper.Common.Shared.Explorer;

namespace ResourceMapper.Common.Server.Explorer.Interfaces
{
    public interface IExplorerService
    {
        Task<ApiServiceResponse<ExplorerNodeModel>> GetNodeAsync(string resourceUid, CancellationToken cancellationToken = default);
    }
}
