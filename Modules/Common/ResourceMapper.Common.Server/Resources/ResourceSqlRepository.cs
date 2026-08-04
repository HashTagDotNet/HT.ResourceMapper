using HT.Microsoft.SqlClient.Extensions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;

using Microsoft.Data.SqlClient;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Server.Resources.Models;
using ResourceMapper.Common.Shared.Domains;
using ResourceMapper.Common.Shared.Editor.Contracts;
using ResourceMapper.Common.Shared.HomePage.Contracts;
using ResourceMapper.Common.Shared.ResourceTypes.Contracts;
using System.Data;

namespace ResourceMapper.Common.Server.Resources
{
    public class ResourceSqlRepository : IResourceRepository
    {
        private const string ResourceFilterListType = "[HTResourceMapper].[ResourceFilterList]";
        private const string TagKeyValueListType = "[HTResourceMapper].[TagKeyValueList]";
        private const string ResourceTypeTagListType = "[HTResourceMapper].[ResourceTypeTagList]";

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
                    TypeName = dr.ReadString("TypeName"),
                    ShortCode = dr.ReadString("ShortCode"),
                    IconKey = dr.ReadString("IconKey")
                };
            }, cancellationToken:cancellationToken);

        }

        public async Task<List<ResourceTypeUsage>> GetResourceTypesWithUsageAsync(CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].ResourceType_GetAllWithUsage");

            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new ResourceTypeUsage
            {
                ResourceTypeId = dr.ReadInt("ResourceTypeId"),
                ResourceTypeUid = dr.ReadString("ResourceTypeUid"),
                TypeName = dr.ReadString("TypeName"),
                ShortCode = dr.ReadString("ShortCode"),
                IconKey = dr.ReadString("IconKey"),
                AllowCustomTags = dr.ReadBoolean("AllowCustomTags"),
                CreatedOn = dr.ReadDateTime("CreatedOn"),
                UpdatedOn = dr.ReadNullableDateTime("UpdatedOn"),
                ResourceCount = dr.ReadInt("ResourceCount"),
                EntryPointTagCount = dr.ReadInt("EntryPointTagCount")
            }, cancellationToken: cancellationToken);
        }

        public async Task<(string Result, int ResourceTypeId)> SaveResourceTypeAsync(int? resourceTypeId,
            string resourceTypeUid, string typeName, string? shortCode, string? iconKey,
            bool allowCustomTags, CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].ResourceType_Save")
                .AddVarchar("@ResourceTypeUid", resourceTypeUid)
                .AddNVarchar("@TypeName", typeName)
                .AddVarchar("@ShortCode", shortCode)
                .AddVarchar("@IconKey", iconKey)
                .AddBit("@AllowCustomTags", allowCustomTags)
                // The sproc treats 0 the same as NULL ("create"), so a null id maps cleanly onto
                // the non-nullable AddInteger overload that accepts a direction.
                .AddInteger("@ResourceTypeId", resourceTypeId ?? 0, ParameterDirection.InputOutput)
                .AddVarchar("@Result", null, 10, ParameterDirection.Output);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
            return (cmd.ReadString("@Result") ?? "error", cmd.ReadInt("@ResourceTypeId"));
        }

        public async Task<(string Result, int DependentCount)> DeleteResourceTypeAsync(int resourceTypeId,
            CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].ResourceType_Delete")
                .AddInteger("@ResourceTypeId", resourceTypeId)
                .AddInteger("@DependentCount", 0, ParameterDirection.Output)
                .AddVarchar("@Result", null, 10, ParameterDirection.Output);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
            return (cmd.ReadString("@Result") ?? "error", cmd.ReadInt("@DependentCount"));
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
            // Call the filter-aware method with default tag limit of 5 and no structured filters
            return await GetResourceGridItemsAsync(requestSearchFor, requestOrderBy, requestOrderDirection,
                skipRecords, takeRecords, 5, null, cancellationToken);
        }

        // Overload with tag limit (no structured filters)
        public async Task<(int TotalCount, List<ResourceGridItem> GridItems)> GetResourceGridItemsAsync(
            string? requestSearchFor,
            string? requestOrderBy,
            string? requestOrderDirection,
            int skipRecords,
            int takeRecords,
            int tagLimit,
            CancellationToken cancellationToken)
        {
            return await GetResourceGridItemsAsync(requestSearchFor, requestOrderBy, requestOrderDirection,
                skipRecords, takeRecords, tagLimit, null, cancellationToken);
        }

        // Overload with structured filters
        public async Task<(int TotalCount, List<ResourceGridItem> GridItems)> GetResourceGridItemsAsync(
            string? requestSearchFor,
            string? requestOrderBy,
            string? requestOrderDirection,
            int skipRecords,
            int takeRecords,
            int tagLimit,
            IReadOnlyList<ResourceGridFilterDefinition>? filters,
            CancellationToken cancellationToken)
        {
            using var filterTable = BuildFilterTable(filters);

            using var cmd = _db.RO.SprocCommand("HTResourceMapper.Resource_GetItems")
                .AddNVarchar("@SearchFor", requestSearchFor)
                .AddNVarchar("@OrderBy", requestOrderBy)
                .AddVarchar("@OrderDirection", requestOrderDirection)
                .AddInteger("@Skip", skipRecords)
                .AddInteger("@Take", takeRecords)
                .AddInteger("@TagLimit", tagLimit)
                .AddTvp("@Filters", ResourceFilterListType, filterTable)
                .AddInteger("@TotalRecords", 0, ParameterDirection.Output);

            // Execute and get the denormalized result set. Tags are already capped at @TagLimit per
            // resource in SQL, so the C# side only groups the flattened rows back up.
            var rawResults = await _db.Execute.ExecuteQueryAsync(cmd, MapDenormalizedRow, cancellationToken: cancellationToken);

            var groupedResults = new Dictionary<string, ResourceGridItem>();

            foreach (var row in rawResults)
            {
                if (row.ResourceUid != null && !groupedResults.ContainsKey(row.ResourceUid))
                {
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

                if (row.ResourceUid != null && !string.IsNullOrEmpty(row.TagUid))
                {
                    groupedResults[row.ResourceUid].Tags!.Add(new ResourceGridTag
                    {
                        TagUid = row.TagUid,
                        TagKey = row.TagKey ?? string.Empty,
                        TagDisplayName = row.TagDisplayName ?? row.TagKey ?? string.Empty,
                        ContentType = row.ContentType ?? string.Empty,
                        TagValue = row.TagValue ?? string.Empty
                    });
                }
            }

            var totalCount = cmd.ReadInt("@TotalRecords");
            return (totalCount, groupedResults.Values.ToList());
        }

        public async Task<List<ResourceGridFacetItem>> GetFilterValuesAsync(
            string column,
            string? tagKey,
            string? searchFor,
            IReadOnlyList<ResourceGridFilterDefinition>? filters,
            CancellationToken cancellationToken)
        {
            using var filterTable = BuildFilterTable(filters);

            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].Resource_GetFilterValues")
                .AddNVarchar("@FacetColumn", column)
                .AddNVarchar("@FacetTagKey", tagKey)
                .AddNVarchar("@SearchFor", searchFor)
                .AddTvp("@Filters", ResourceFilterListType, filterTable);

            return await _db.Execute.ExecuteQueryAsync(cmd, reader => new ResourceGridFacetItem
            {
                IsBlank = reader.GetBoolean(reader.GetOrdinal("IsBlank")),
                Value = GetStringOrNull(reader, "Value"),
                ItemCount = reader.GetInt32(reader.GetOrdinal("ItemCount"))
            }, cancellationToken: cancellationToken);
        }

        public async Task<List<string>> GetAllTagKeysAsync(CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].TagDefinition_GetAll");
            var keys = await _db.Execute.ExecuteQueryAsync(
                cmd,
                reader => new
                {
                    Key = GetStringOrNull(reader, "TagDefinitionKey"),
                    IsSystemTag = reader.GetBoolean(reader.GetOrdinal("IsSystemTag")),
                    IsDomainTag = reader.GetBoolean(reader.GetOrdinal("IsDomainTag"))
                },
                cancellationToken: cancellationToken);

            // PL-04: these keys feed the home grid's Add-filter picker, so internal plumbing tags must
            // not appear there. Only "Domain" qualifies today, but filtering on the flags rather than
            // on a name means any future system tag is excluded the moment it is defined, instead of
            // leaking into the UI until someone notices.
            return keys
                .Where(k => !string.IsNullOrEmpty(k.Key) && !k.IsSystemTag && !k.IsDomainTag)
                .Select(k => k.Key!)
                .ToList();
        }

        public async Task<ResourceDetail?> GetResourceByUidAsync(string resourceUid, CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].Resource_GetByResourceUid")
                .AddVarchar("@ResourceUid", resourceUid);

            return await _db.Execute.ExecuteRowAsync(cmd, dr => new ResourceDetail
            {
                ResourceId = dr.ReadInt("ResourceId"),
                ResourceUid = dr.ReadString("ResourceUid"),
                ResourceKey = dr.ReadString("ResourceKey"),
                ResourceTypeId = dr.ReadInt("ResourceTypeId"),
                ResourceName = dr.ReadString("ResourceName"),
                Description = dr.ReadString("Description"),
                PrimaryTagDefinitionId = dr.ReadNullableInt("PrimaryTagDefinitionId"),
                Domain = dr.ReadString("Domain"),
                CreatedOn = dr.ReadDateTime("CreatedOn"),
                UpdatedOn = dr.ReadNullableDateTime("UpdatedOn")
            }, cancellationToken: cancellationToken);
        }

        public async Task<List<ResourceRelationshipItem>> GetRelationshipsForResourceAsync(int resourceId, CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].ResourceRelationship_GetForResource")
                .AddInteger("@ResourceId", resourceId);

            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new ResourceRelationshipItem
            {
                RelationshipId = dr.ReadInt("RelationshipId"),
                Direction = dr.ReadString("Direction"),
                OtherResourceId = dr.ReadInt("OtherResourceId"),
                OtherResourceUid = dr.ReadString("OtherResourceUid"),
                OtherResourceKey = dr.ReadString("OtherResourceKey"),
                OtherResourceName = dr.ReadString("OtherResourceName"),
                OtherResourceType = dr.ReadString("OtherResourceType"),
                OtherDomain = dr.ReadString("OtherDomain")
            }, cancellationToken: cancellationToken);
        }

        public async Task AddRelationshipAsync(int fromResourceId, int toResourceId, CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].ResourceRelationship_Add")
                .AddInteger("@FromResourceId", fromResourceId)
                .AddInteger("@ToResourceId", toResourceId);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
        }

        public async Task RemoveRelationshipAsync(int fromResourceId, int toResourceId, CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].ResourceRelationship_Remove")
                .AddInteger("@FromResourceId", fromResourceId)
                .AddInteger("@ToResourceId", toResourceId);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
        }

        public async Task DeleteResourceAsync(int resourceId, CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].Resource_Delete")
                .AddInteger("@ResourceId", resourceId);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
        }

        public async Task<(string Result, int ResourceId)> SaveResourceAsync(string resourceUid, int resourceTypeId,
            string resourceKey, string resourceName, string? description, string? domain,
            int? primaryTagDefinitionId, CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].Resource_Save")
                .AddVarchar("@ResourceUid", resourceUid)
                .AddInteger("@ResourceTypeId", resourceTypeId)
                .AddNVarchar("@ResourceKey", resourceKey)
                .AddNVarchar("@ResourceName", resourceName)
                .AddNVarchar("@Description", description)
                .AddNVarchar("@Domain", domain)
                .AddInteger("@PrimaryTagDefinitionId", primaryTagDefinitionId)
                .AddInteger("@ResourceId", 0, ParameterDirection.Output)
                .AddVarchar("@Result", null, 10, ParameterDirection.Output);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
            return (cmd.ReadString("@Result") ?? "error", cmd.ReadInt("@ResourceId"));
        }

        public async Task<List<ResourceTagRead>> GetTagsForResourceAsync(int resourceId, CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].ResourceTag_GetForResource")
                .AddInteger("@ResourceId", resourceId);

            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new ResourceTagRead
            {
                TagDefinitionId = dr.ReadInt("TagDefinitionId"),
                TagDefinitionKey = dr.ReadString("TagDefinitionKey"),
                DisplayName = dr.ReadString("DisplayName"),
                ContentType = dr.ReadString("ContentType"),
                TagValue = dr.ReadString("TagValue"),
                IsSystemTag = dr.ReadBoolean("IsSystemTag"),
                IsMultiValued = dr.ReadBoolean("IsMultiValued"),
                IsPrimary = dr.ReadBoolean("IsPrimary")
            }, cancellationToken: cancellationToken);
        }

        public async Task<bool> CheckResourceUniqueAsync(int resourceTypeId, string resourceKey, string? domain,
            string? excludeResourceUid, CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].Resource_CheckUnique")
                .AddInteger("@ResourceTypeId", resourceTypeId)
                .AddNVarchar("@ResourceKey", resourceKey)
                .AddNVarchar("@Domain", domain)
                .AddVarchar("@ExcludeResourceUid", excludeResourceUid);

            cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = "@IsUnique",
                SqlDbType = SqlDbType.Bit,
                Direction = ParameterDirection.Output
            });

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);

            var value = cmd.Parameters["@IsUnique"].Value;
            return value != DBNull.Value && (bool)value;
        }

        public async Task<List<TagDefinition>> GetAllTagDefinitionsAsync(CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].TagDefinition_GetAll");
            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new TagDefinition
            {
                TagDefinitionId = dr.ReadInt("TagDefinitionId"),
                TagDefinitionUid = dr.ReadString("TagDefinitionUid"),
                TagDefinitionKey = dr.ReadString("TagDefinitionKey"),
                DisplayName = dr.ReadString("DisplayName"),
                TagContentTypeId = dr.ReadInt("TagContentTypeId"),
                ContentType = dr.ReadString("ContentType"),
                AllowCustomValue = dr.ReadBoolean("AllowCustomValue"),
                IsMultiValued = dr.ReadBoolean("IsMultiValued"),
                AllowedValues = dr.ReadString("AllowedValues"),
                RequirementLevel = dr.ReadString("RequirementLevel") ?? "Optional",
                IsDomainTag = dr.ReadBoolean("IsDomainTag"),
                IsSystemTag = dr.ReadBoolean("IsSystemTag"),
                DisplayOrder = dr.ReadInt("DisplayOrder"),
                CreatedOn = dr.ReadDateTime("CreatedOn"),
                UpdatedOn = dr.ReadNullableDateTime("UpdatedOn")
            }, cancellationToken: cancellationToken);
        }

        public async Task<(string Result, int TagDefinitionId)> CreateTagDefinitionAsync(string tagKey,
            string tagDefinitionUid, string contentType, bool allowCustomValue, bool isMultiValued,
            string? allowedValuesJson, string? displayName, string requirementLevel, int displayOrder,
            CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].TagDefinition_Upsert")
                .AddNVarchar("@TagDefinitionKey", tagKey)
                .AddVarchar("@TagDefinitionUid", tagDefinitionUid)
                .AddVarchar("@ContentType", contentType)
                .AddBit("@AllowCustomValue", allowCustomValue)
                .AddBit("@IsMultiValued", isMultiValued)
                .AddNVarchar("@AllowedValues", allowedValuesJson)
                .AddNVarchar("@DisplayName", displayName)
                .AddVarchar("@RequirementLevel", requirementLevel)
                .AddBit("@IsDomainTag", false)
                .AddBit("@IsSystemTag", false)
                .AddInteger("@DisplayOrder", displayOrder)
                .AddVarchar("@OnConflict", "skip")
                .AddInteger("@TagDefinitionId", 0, ParameterDirection.Output)
                .AddVarchar("@Result", null, 10, ParameterDirection.Output);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
            return (cmd.ReadString("@Result") ?? "error", cmd.ReadInt("@TagDefinitionId"));
        }

        public async Task<List<DomainValueModel>> GetDomainValuesAsync(CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].DomainValue_GetAllWithUsage");
            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new DomainValueModel
            {
                Value = dr.ReadString("DomainValue") ?? string.Empty,
                ResourceCount = dr.ReadInt("ResourceCount"),
                IsUnlisted = dr.ReadBoolean("IsUnlisted")
            }, cancellationToken: cancellationToken);
        }

        public async Task<(string Result, int AffectedResources)> RenameDomainValueAsync(string oldValue, string newValue,
            CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].DomainValue_Rename")
                .AddNVarchar("@OldValue", oldValue)
                .AddNVarchar("@NewValue", newValue)
                .AddInteger("@AffectedResources", 0, ParameterDirection.Output)
                .AddVarchar("@Result", null, 10, ParameterDirection.Output);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
            return (cmd.ReadString("@Result") ?? "error", cmd.ReadInt("@AffectedResources"));
        }

        public async Task<(string Result, int ResourceCount)> DeleteDomainValueAsync(string value, CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].DomainValue_Delete")
                .AddNVarchar("@Value", value)
                .AddInteger("@ResourceCount", 0, ParameterDirection.Output)
                .AddVarchar("@Result", null, 10, ParameterDirection.Output);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
            return (cmd.ReadString("@Result") ?? "error", cmd.ReadInt("@ResourceCount"));
        }

        public async Task<string> AddDomainValueAsync(string value, CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].DomainValue_Add")
                .AddNVarchar("@Value", value)
                .AddVarchar("@Result", null, 10, ParameterDirection.Output);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
            return cmd.ReadString("@Result") ?? "error";
        }

        public async Task<string> UpdateTagDefinitionAllowedValuesAsync(string tagKey, string contentType,
            bool allowCustomValue, bool isMultiValued, string? allowedValuesJson,
            CancellationToken cancellationToken)
        {
            // @TagDefinitionUid is insert-only per the sproc's own contract, and this path only ever
            // updates, so the value never reaches the row — passing the key keeps it non-null without
            // minting a Uid that would be misleading if the contract ever changed.
            // The five presentation/flag params are deliberately omitted: NULL means "leave alone",
            // which is what protects IsDomainTag on the Subscription definition.
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].TagDefinition_Upsert")
                .AddNVarchar("@TagDefinitionKey", tagKey)
                .AddVarchar("@TagDefinitionUid", tagKey)
                .AddVarchar("@ContentType", contentType)
                .AddBit("@AllowCustomValue", allowCustomValue)
                .AddBit("@IsMultiValued", isMultiValued)
                .AddNVarchar("@AllowedValues", allowedValuesJson)
                .AddVarchar("@OnConflict", "upsert")
                .AddInteger("@TagDefinitionId", 0, ParameterDirection.Output)
                .AddVarchar("@Result", null, 10, ParameterDirection.Output);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
            return cmd.ReadString("@Result") ?? "error";
        }

        public async Task<List<ResourceTypeTag>> GetAllEntryPointTemplatesAsync(CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].ResourceTypeTag_GetAll");
            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new ResourceTypeTag
            {
                ResourceTypeId = dr.ReadInt("ResourceTypeId"),
                TagDefinitionId = dr.ReadInt("TagDefinitionId"),
                IsDefaultPrimary = dr.ReadBoolean("IsDefaultPrimary"),
                RequirementLevel = dr.ReadString("RequirementLevel")
            }, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Replaces one type's entry-point template with exactly <paramref name="tags"/>. An empty
        /// list clears it — that is a real edit (a type that prompts for nothing), so it is not
        /// short-circuited here.
        /// </summary>
        public async Task<string> SetEntryPointTemplatesAsync(int resourceTypeId,
            IReadOnlyList<EntryPointTagTemplateModel> tags, CancellationToken cancellationToken)
        {
            using var table = new DataTable();
            table.Columns.Add("TagDefinitionId", typeof(int));
            table.Columns.Add("IsDefaultPrimary", typeof(bool));
            table.Columns.Add("RequirementLevel", typeof(string));

            foreach (var t in tags)
            {
                object level = string.IsNullOrWhiteSpace(t.RequirementLevel)
                    ? DBNull.Value
                    : t.RequirementLevel!;
                table.Rows.Add(t.TagDefinitionId, t.IsDefaultPrimary, level);
            }

            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].ResourceTypeTag_SetForType")
                .AddInteger("@ResourceTypeId", resourceTypeId)
                .AddTvp("@Tags", ResourceTypeTagListType, table)
                .AddVarchar("@Result", null, 10, ParameterDirection.Output);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
            return cmd.ReadString("@Result") ?? "error";
        }

        public async Task SetResourceTagsAsync(int resourceId, IReadOnlyList<(string TagKey, string TagValue)> tags,
            CancellationToken cancellationToken)
        {
            using var table = new DataTable();
            table.Columns.Add("TagDefinitionKey", typeof(string));
            table.Columns.Add("TagValue", typeof(string));
            foreach (var (tagKey, tagValue) in tags)
                table.Rows.Add(tagKey, (object?)tagValue ?? DBNull.Value);

            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].ResourceTag_SetForResource")
                .AddInteger("@ResourceId", resourceId)
                .AddTvp("@Tags", TagKeyValueListType, table);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
        }

        public async Task<(List<ResourcePickerItem> Items, int TotalCount)> SearchResourcesForPickerAsync(
            ResourcePickerRequest request, CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].Resource_SearchForPicker")
                .AddNVarchar("@SearchFor", request.SearchFor)
                .AddNVarchar("@Domain", request.Domain)
                .AddInteger("@ResourceTypeId", request.ResourceTypeId)
                .AddVarchar("@ExcludeResourceUid", request.ExcludeResourceUid)
                .AddInteger("@Skip", request.Skip)
                .AddInteger("@Take", request.Take)
                .AddInteger("@TotalRecords", 0, ParameterDirection.Output);

            var items = await _db.Execute.ExecuteQueryAsync(cmd, dr => new ResourcePickerItem
            {
                ResourceUid = dr.ReadString("ResourceUid") ?? string.Empty,
                ResourceName = dr.ReadString("ResourceName") ?? string.Empty,
                ResourceKey = dr.ReadString("ResourceKey") ?? string.Empty,
                ResourceTypeName = dr.ReadString("ResourceTypeName") ?? string.Empty,
                Domain = dr.ReadString("Domain")
            }, cancellationToken: cancellationToken);

            var totalCount = cmd.ReadInt("@TotalRecords");
            return (items, totalCount);
        }

        // Builds the [HTResourceMapper].[ResourceFilterList] TVP rows: one per selected enumerable
        // value / tag value / text filter. Column order MUST match the TVP definition.
        private static DataTable BuildFilterTable(IReadOnlyList<ResourceGridFilterDefinition>? filters)
        {
            var table = new DataTable();
            table.Columns.Add("FilterIndex", typeof(byte));
            table.Columns.Add("FilterColumn", typeof(string));
            table.Columns.Add("TagKey", typeof(string));
            table.Columns.Add("Operator", typeof(string));
            table.Columns.Add("FilterValue", typeof(string));
            table.Columns.Add("IsBlank", typeof(bool));

            if (filters == null)
                return table;

            byte index = 0;
            foreach (var f in filters)
            {
                if (index >= byte.MaxValue) break;
                index++;

                var column = f.Column ?? string.Empty;
                object tagKey = string.IsNullOrEmpty(f.TagKey) ? DBNull.Value : f.TagKey!;

                if (f.Kind == ResourceGridFilterKind.Text)
                {
                    if (!string.IsNullOrWhiteSpace(f.Text))
                        table.Rows.Add(index, column, tagKey, "Contains", f.Text!.Trim(), false);
                    continue;
                }

                var op = f.Operator == ResourceGridFilterOperator.NotEquals ? "NotEquals" : "Equals";

                if (f.Values != null)
                {
                    foreach (var value in f.Values)
                    {
                        if (value == null) continue;
                        table.Rows.Add(index, column, tagKey, op, value, false);
                    }
                }

                if (f.IncludeBlank)
                    table.Rows.Add(index, column, tagKey, op, DBNull.Value, true);
            }

            return table;
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
                TagDisplayName = GetStringOrNull(reader, "TagDisplayName"),
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
            public string? TagDisplayName { get; set; }
            public string? ContentType { get; set; }
            public string? TagValue { get; set; }
        }
    }
}
