using ResourceMapper.Common.Server.Resources.Models;

namespace ResourceMapper.Common.Server.Resources.Interfaces
{
    /// <summary>
    /// Data access for the resource import pipeline: read-only pre-validation lookups
    /// plus the per-item write operations. Writes are called only after validation passes.
    /// The <c>onConflict</c> passed to writes is 'upsert' | 'skip' ('fail' is mapped to 'skip'
    /// by the service after conflict validation).
    /// </summary>
    public interface IImportRepository
    {
        // ---- read-only lookups ----

        /// <summary>All tag definitions, for reference/value validation against the import payload.</summary>
        Task<List<TagDefinition>> GetAllTagDefinitionsAsync(CancellationToken cancellationToken);

        /// <summary>Every existing resource's full (Domain + Type + Key) identity, for triple
        /// conflict/dedup checks and dependency-target resolution.</summary>
        Task<List<ResourceIdentity>> GetAllResourceIdentitiesAsync(CancellationToken cancellationToken);

        // ---- writes (after validation) ----

        /// <returns>'created' | 'updated' | 'skipped'</returns>
        Task<string> UpsertResourceTypeAsync(string typeName, string resourceTypeUid, bool allowCustomTags,
            string onConflict, CancellationToken cancellationToken);

        /// <returns>'created' | 'updated' | 'skipped'</returns>
        Task<string> UpsertTagDefinitionAsync(string tagKey, string tagDefinitionUid, string contentType,
            bool allowCustomValue, bool isMultiValued, string? allowedValuesJson,
            string onConflict, CancellationToken cancellationToken);

        /// <returns>(result: 'created'|'updated'|'skipped', resourceId — returned even on skip)</returns>
        Task<(string Result, int ResourceId)> UpsertResourceAsync(string resourceKey, string resourceUid,
            string? typeName, string resourceName, string? description, string? domain,
            string onConflict, CancellationToken cancellationToken);

        /// <summary>Replaces all tags for the resource with the supplied (key, value) pairs.</summary>
        Task SetResourceTagsAsync(int resourceId, IReadOnlyList<(string TagKey, string TagValue)> tags,
            CancellationToken cancellationToken);
    }
}
