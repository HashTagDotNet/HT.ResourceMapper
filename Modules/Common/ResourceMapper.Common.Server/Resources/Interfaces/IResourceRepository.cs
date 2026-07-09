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

        // Single-resource read by external uid; null if not found.
        Task<ResourceDetail?> GetResourceByUidAsync(string resourceUid, CancellationToken cancellationToken);

        // Every relationship edge touching this resource, from both directions.
        Task<List<ResourceRelationshipItem>> GetRelationshipsForResourceAsync(int resourceId, CancellationToken cancellationToken);

        // Idempotent: adding an edge that already exists is a no-op.
        Task AddRelationshipAsync(int fromResourceId, int toResourceId, CancellationToken cancellationToken);

        Task RemoveRelationshipAsync(int fromResourceId, int toResourceId, CancellationToken cancellationToken);

        // Deletes the resource and cascades its relationship edges (both directions) and tags.
        Task DeleteResourceAsync(int resourceId, CancellationToken cancellationToken);

        // Editor create/update, keyed by the immutable ResourceUid (not (type+key) — the editor
        // allows Key rename). Result is 'created' | 'updated' | 'error' (Type or Domain changed
        // post-save).
        Task<(string Result, int ResourceId)> SaveResourceAsync(string resourceUid, int resourceTypeId,
            string resourceKey, string resourceName, string? description, string? domain,
            int? primaryTagDefinitionId, CancellationToken cancellationToken);

        // Every applied tag value for the resource, joined to its definition.
        Task<List<ResourceTagRead>> GetTagsForResourceAsync(int resourceId, CancellationToken cancellationToken);

        // True iff (domain, resourceTypeId, resourceKey) is not already used by another resource
        // (excluding the one identified by excludeResourceUid, for edit-mode self-exclusion).
        Task<bool> CheckResourceUniqueAsync(int resourceTypeId, string resourceKey, string? domain,
            string? excludeResourceUid, CancellationToken cancellationToken);

        // Full tag dictionary (all TagDefinition rows), for the editor's "Add tag" picker.
        Task<List<TagDefinition>> GetAllTagDefinitionsAsync(CancellationToken cancellationToken);

        // Inline tag-definition create/upsert (Domain/System flags forced off by the caller).
        // Result is 'created' | 'updated' | 'skipped' | 'error' (bad content type).
        Task<(string Result, int TagDefinitionId)> CreateTagDefinitionAsync(string tagKey, string tagDefinitionUid,
            string contentType, bool allowCustomValue, bool isMultiValued, string? allowedValuesJson,
            string? displayName, string requirementLevel, int displayOrder, CancellationToken cancellationToken);
    }
}
