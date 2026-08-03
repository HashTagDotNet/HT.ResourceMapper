using HT.Microsoft.SqlClient.Extensions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Server.Resources.Models;

namespace ResourceMapper.Common.Server.Resources
{
    /// <summary>
    /// Bulk read-only queries backing the catalog export. Five queries total for the whole catalog —
    /// resources, their tags, their dependency edges, plus the two reference sections — so export
    /// cost does not grow with resource count. Reuses the existing ResourceType_GetAll /
    /// TagDefinition_GetAll sprocs for reference data rather than duplicating them.
    /// </summary>
    public class ExportSqlRepository : IExportRepository
    {
        private readonly IDbConnector _db;

        public ExportSqlRepository(IDbConnector db)
        {
            _db = db;
        }

        public async Task<List<ExportResourceRow>> GetResourcesAsync(CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].Export_GetResources");
            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new ExportResourceRow
            {
                ResourceId = dr.ReadInt("ResourceId"),
                ResourceUid = dr.ReadString("ResourceUid") ?? string.Empty,
                ResourceKey = dr.ReadString("ResourceKey") ?? string.Empty,
                TypeName = dr.ReadString("TypeName") ?? string.Empty,
                ResourceName = dr.ReadString("ResourceName") ?? string.Empty,
                Description = dr.ReadString("Description"),
                Domain = dr.ReadString("Domain")
            }, cancellationToken: cancellationToken);
        }

        public async Task<List<ExportTagRow>> GetResourceTagsAsync(CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].Export_GetResourceTags");
            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new ExportTagRow
            {
                ResourceId = dr.ReadInt("ResourceId"),
                TagDefinitionKey = dr.ReadString("TagDefinitionKey") ?? string.Empty,
                TagValue = dr.ReadString("TagValue"),
                IsMultiValued = dr.ReadBoolean("IsMultiValued"),
                IsDomainTag = dr.ReadBoolean("IsDomainTag")
            }, cancellationToken: cancellationToken);
        }

        public async Task<List<ExportDependencyRow>> GetDependenciesAsync(CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].Export_GetDependencies");
            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new ExportDependencyRow
            {
                FromResourceId = dr.ReadInt("FromResourceId"),
                FromResourceKey = dr.ReadString("FromResourceKey") ?? string.Empty,
                FromDomain = dr.ReadString("FromDomain"),
                ToResourceKey = dr.ReadString("ToResourceKey") ?? string.Empty,
                ToDomain = dr.ReadString("ToDomain")
            }, cancellationToken: cancellationToken);
        }

        public async Task<List<ResourceType>> GetResourceTypesAsync(CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].ResourceType_GetAll");
            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new ResourceType
            {
                ResourceTypeId = dr.ReadInt("ResourceTypeId"),
                ResourceTypeUid = dr.ReadString("ResourceTypeUid") ?? string.Empty,
                TypeName = dr.ReadString("TypeName") ?? string.Empty,
                AllowCustomTags = dr.ReadBoolean("AllowCustomTags")
            }, cancellationToken: cancellationToken);
        }

        public async Task<List<TagDefinition>> GetTagDefinitionsAsync(CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].TagDefinition_GetAll");
            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new TagDefinition
            {
                TagDefinitionId = dr.ReadInt("TagDefinitionId"),
                TagDefinitionUid = dr.ReadString("TagDefinitionUid") ?? string.Empty,
                TagDefinitionKey = dr.ReadString("TagDefinitionKey") ?? string.Empty,
                DisplayName = dr.ReadString("DisplayName"),
                TagContentTypeId = dr.ReadInt("TagContentTypeId"),
                ContentType = dr.ReadString("ContentType"),
                AllowCustomValue = dr.ReadBoolean("AllowCustomValue"),
                IsMultiValued = dr.ReadBoolean("IsMultiValued"),
                AllowedValues = dr.ReadString("AllowedValues"),
                RequirementLevel = dr.ReadString("RequirementLevel") ?? "Optional",
                IsDomainTag = dr.ReadBoolean("IsDomainTag"),
                IsSystemTag = dr.ReadBoolean("IsSystemTag"),
                DisplayOrder = dr.ReadInt("DisplayOrder")
            }, cancellationToken: cancellationToken);
        }
    }
}
