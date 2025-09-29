using HT.Microsoft.SqlClient.Extensions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;

using Microsoft.Data.SqlClient;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Server.Resources.Models;
using System.Data;

namespace ResourceMapper.Common.Server.Resources
{
    public class ResourceSqlRepository : IResourceRepository
    {
        private readonly IDbConnector _db;

        public ResourceSqlRepository(IDbConnector db)
        {
            _db = db;
        }

        public async Task<List<ResourceType>> GetAllResourceTypesAsync(CancellationToken cancellationToken)
        {
            const string sql = "[HTResourceMapper].ResourceType_GetAll";

            using var _cmd = _db.RO.SprocCommand(sql);
            return await _db.Execute.ExecuteQueryAsync(_cmd, dr =>
            {
                return new ResourceType()
                {
                    AllowCustomTags = dr.ReadBoolean("AllowCustomTags"),
                    CreatedOn = dr.ReadDateTime("CreatedOn"),
                    UpdatedOn = dr.ReadDateTime("UpdatedOn"),
                    ResourceTypeId = dr.ReadInt("ResourceTypeId"),
                    ResourceTypeUid = dr.ReadString("ResourceTypeUid"),
                    TypeName = dr.ReadString("TypeName")
                };
            }, cancellationToken:cancellationToken);

        }

        // Original method signature for backward compatibility
        public async Task<(int TotalCount, List<ResourceGridItem> GridItems)> GetResourceGridItemsAsync(
            string? requestSearchFor,
            string? requestOrderBy,
            string? requestOrderDirection,
            int skipRecords,
            int takeRecords,
            CancellationToken cancellationToken)
        {
            // Call the new method with default tag limit of 5
            return await GetResourceGridItemsAsync(requestSearchFor, requestOrderBy, requestOrderDirection,
                skipRecords, takeRecords, 5, cancellationToken);
        }

        // New method signature with tag limit
        public async Task<(int TotalCount, List<ResourceGridItem> GridItems)> GetResourceGridItemsAsync(
            string? requestSearchFor,
            string? requestOrderBy,
            string? requestOrderDirection,
            int skipRecords,
            int takeRecords,
            int tagLimit,
            CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("HTResourceMapper.Resource_GetItems")
                .AddNVarchar("@SearchFor", requestSearchFor)
                .AddNVarchar("@OrderBy", requestOrderBy)
                .AddVarchar("@OrderDirection", requestOrderDirection)
                .AddInteger("@Skip", skipRecords)
                .AddInteger("@Take", takeRecords)
                .AddInteger("@TotalRecords", 0, ParameterDirection.Output);

            // Execute and get the denormalized result set
            var rawResults = await _db.Execute.ExecuteQueryAsync(cmd, MapDenormalizedRow, cancellationToken: cancellationToken);

            // Group by ResourceUid and apply tag limit
            var groupedResults = new Dictionary<string, ResourceGridItem>();

            foreach (var row in rawResults)
            {
                if (row.ResourceUid != null && !groupedResults.ContainsKey(row.ResourceUid))
                {
                    // First occurrence of this resource
                    groupedResults[row.ResourceUid] = new ResourceGridItem
                    {
                        ResourceUid = row.ResourceUid,
                        ResourceName = row.ResourceName,
                        ResourceType = row.ResourceType,
                        Description = row.Description,
                        LastUpdatedOn = row.LastUpdatedOn,
                        Tags = new List<ResourceGridTag>()
                    };
                }

                // Add tag if we have tag data and haven't exceeded limit
                if (row.ResourceUid != null &&
                    !string.IsNullOrEmpty(row.TagUid) &&
                    groupedResults[row.ResourceUid].Tags!.Count < tagLimit)
                {
                    groupedResults[row.ResourceUid].Tags!.Add(new ResourceGridTag
                    {
                        TagUid = row.TagUid,
                        TagKey = row.TagKey ?? string.Empty,
                        ContentType = row.ContentType ?? string.Empty,
                        TagValue = row.TagValue ?? string.Empty
                    });
                }
            }

            var totalCount = cmd.ReadInt("@TotalRecords");
            return (totalCount, groupedResults.Values.ToList());
        }

        private DenormalizedRow MapDenormalizedRow(SqlDataReader reader)
        {
            return new DenormalizedRow
            {
                ResourceUid = GetStringOrNull(reader, "ResourceUid"),
                ResourceName = GetStringOrNull(reader, "ResourceName"),
                ResourceType = GetStringOrNull(reader, "ResourceType"),
                Description = GetStringOrNull(reader, "Description"),
                LastUpdatedOn = reader.GetDateTime("LastUpdatedOn"),
                TagUid = GetStringOrNull(reader, "TagUid"),
                TagKey = GetStringOrNull(reader, "TagKey"),
                ContentType = GetStringOrNull(reader, "ContentType"),
                TagValue = GetStringOrNull(reader, "TagValue")
            };
        }

        private string? GetStringOrNull(SqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        // Internal helper class for denormalized data
        private class DenormalizedRow
        {
            public string? ResourceUid { get; set; }
            public string? ResourceName { get; set; }
            public string? ResourceType { get; set; }
            public string? Description { get; set; }
            public DateTime LastUpdatedOn { get; set; }
            public string? TagUid { get; set; }
            public string? TagKey { get; set; }
            public string? ContentType { get; set; }
            public string? TagValue { get; set; }
        }
    }
}
