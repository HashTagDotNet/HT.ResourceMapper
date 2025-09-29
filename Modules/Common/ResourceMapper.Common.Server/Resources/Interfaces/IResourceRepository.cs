using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ResourceMapper.Common.Server.Resources.Models;

namespace ResourceMapper.Common.Server.Resources.Interfaces
{
    public interface IResourceRepository
    {
        // Original method signature for backward compatibility
        Task<(int TotalCount, List<ResourceGridItem> GridItems)> GetResourceGridItemsAsync(
            string? requestSearchFor, 
            string? requestOrderBy, 
            string? requestOrderDirection, 
            int requestSkip, 
            int requestTake, 
            CancellationToken cancellationToken);

        // New method signature with tag limit
        Task<(int TotalCount, List<ResourceGridItem> GridItems)> GetResourceGridItemsAsync(
            string? requestSearchFor, 
            string? requestOrderBy, 
            string? requestOrderDirection, 
            int requestSkip, 
            int requestTake, 
            int tagLimit,
            CancellationToken cancellationToken);

        Task<List<ResourceType>> GetAllResourceTypesAsync(CancellationToken cancellationToken);
    }
}
