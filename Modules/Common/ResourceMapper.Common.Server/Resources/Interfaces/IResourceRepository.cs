using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ResourceMapper.Common.Server.Resources.Models;
using ResourceMapper.Common.Shared.HomePage.Contracts;

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

        // Overload with tag limit
        Task<(int TotalCount, List<ResourceGridItem> GridItems)> GetResourceGridItemsAsync(
            string? requestSearchFor,
            string? requestOrderBy,
            string? requestOrderDirection,
            int requestSkip,
            int requestTake,
            int tagLimit,
            CancellationToken cancellationToken);

        // Overload with structured filters (AND across filters; OR within an enumerable filter)
        Task<(int TotalCount, List<ResourceGridItem> GridItems)> GetResourceGridItemsAsync(
            string? requestSearchFor,
            string? requestOrderBy,
            string? requestOrderDirection,
            int requestSkip,
            int requestTake,
            int tagLimit,
            IReadOnlyList<ResourceGridFilterDefinition>? filters,
            CancellationToken cancellationToken);

        // Distinct value+count facet for a column (ResourceType or a tag key), respecting the
        // search text and all OTHER active filters.
        Task<List<ResourceGridFacetItem>> GetFilterValuesAsync(
            string column,
            string? tagKey,
            string? searchFor,
            IReadOnlyList<ResourceGridFilterDefinition>? filters,
            CancellationToken cancellationToken);

        Task<List<ResourceType>> GetAllResourceTypesAsync(CancellationToken cancellationToken);

        // Distinct tag keys (for the "Add filter" column picker).
        Task<List<string>> GetAllTagKeysAsync(CancellationToken cancellationToken);
    }
}
