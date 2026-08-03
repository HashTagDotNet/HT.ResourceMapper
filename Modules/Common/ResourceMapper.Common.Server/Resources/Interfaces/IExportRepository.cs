using ResourceMapper.Common.Server.Resources.Models;

namespace ResourceMapper.Common.Server.Resources.Interfaces
{
    /// <summary>
    /// Read-only bulk reads backing the catalog export. Deliberately separate from
    /// <see cref="IResourceRepository"/>: every method is a single set-based query over the whole
    /// catalog, so an export costs a constant number of round-trips regardless of catalog size
    /// (no per-resource tag/dependency fetches).
    /// </summary>
    public interface IExportRepository
    {
        Task<List<ExportResourceRow>> GetResourcesAsync(CancellationToken cancellationToken);

        Task<List<ExportTagRow>> GetResourceTagsAsync(CancellationToken cancellationToken);

        Task<List<ExportDependencyRow>> GetDependenciesAsync(CancellationToken cancellationToken);

        Task<List<ResourceType>> GetResourceTypesAsync(CancellationToken cancellationToken);

        Task<List<TagDefinition>> GetTagDefinitionsAsync(CancellationToken cancellationToken);
    }
}
