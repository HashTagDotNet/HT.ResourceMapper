using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ResourceMapper.Common.Server.Resources.Models;

namespace ResourceMapper.Common.Server.Explorer.Interfaces
{
    public interface IExplorerRepository
    {
        Task<List<ExplorerNodeRow>> GetForExplorerAsync(string resourceUid, CancellationToken cancellationToken);
    }
}
