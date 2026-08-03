using System.Data;
using HT.Microsoft.SqlClient.Extensions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Server.Resources.Models;

namespace ResourceMapper.Common.Server.Resources
{
    public class ImportSqlRepository : IImportRepository
    {
        private const string TagKeyValueListType = "[HTResourceMapper].[TagKeyValueList]";

        private readonly IDbConnector _db;

        public ImportSqlRepository(IDbConnector db)
        {
            _db = db;
        }

        // ---------------- reads ----------------

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

        public async Task<List<ResourceIdentity>> GetAllResourceIdentitiesAsync(CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].Resource_GetAllKeys");
            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new ResourceIdentity
            {
                ResourceId = dr.ReadInt("ResourceId"),
                ResourceKey = dr.ReadString("ResourceKey"),
                TypeName = dr.ReadString("TypeName"),
                Domain = dr.ReadString("Domain")
            }, cancellationToken: cancellationToken);
        }

        // ---------------- writes ----------------

        public async Task<string> UpsertResourceTypeAsync(string typeName, string resourceTypeUid, bool allowCustomTags,
            string onConflict, CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].ResourceType_Upsert")
                .AddNVarchar("@TypeName", typeName)
                .AddVarchar("@ResourceTypeUid", resourceTypeUid)
                .AddBit("@AllowCustomTags", allowCustomTags)
                .AddVarchar("@OnConflict", onConflict)
                .AddVarchar("@Result", null, 10, ParameterDirection.Output);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
            return cmd.ReadString("@Result") ?? "skipped";
        }

        public async Task<string> UpsertTagDefinitionAsync(string tagKey, string tagDefinitionUid, string contentType,
            bool allowCustomValue, bool isMultiValued, string? allowedValuesJson,
            string onConflict, CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].TagDefinition_Upsert")
                .AddNVarchar("@TagDefinitionKey", tagKey)
                .AddVarchar("@TagDefinitionUid", tagDefinitionUid)
                .AddVarchar("@ContentType", contentType)
                .AddBit("@AllowCustomValue", allowCustomValue)
                .AddBit("@IsMultiValued", isMultiValued)
                .AddNVarchar("@AllowedValues", allowedValuesJson)
                // @DisplayName / @RequirementLevel / @IsDomainTag / @IsSystemTag / @DisplayOrder are
                // deliberately NOT passed: the import document has no syntax for them, so import has
                // no opinion to express. The sproc reads their NULL defaults as "preserve on update"
                // (PL-49) - previously it overwrote them, clearing IsDomainTag and breaking the
                // (Domain + Type + Key) identity model on any import that named an existing tag.
                .AddVarchar("@OnConflict", onConflict)
                .AddVarchar("@Result", null, 10, ParameterDirection.Output);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
            return cmd.ReadString("@Result") ?? "skipped";
        }

        public async Task<(string Result, int ResourceId)> UpsertResourceAsync(string resourceKey, string resourceUid,
            string? typeName, string resourceName, string? description, string? domain,
            string onConflict, CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].Resource_Upsert")
                .AddNVarchar("@ResourceKey", resourceKey)
                .AddVarchar("@ResourceUid", resourceUid)
                .AddNVarchar("@TypeName", typeName)
                .AddNVarchar("@ResourceName", resourceName)
                .AddNVarchar("@Description", description)
                .AddNVarchar("@Domain", domain)
                .AddVarchar("@OnConflict", onConflict)
                .AddInteger("@ResourceId", 0, ParameterDirection.Output)
                .AddVarchar("@Result", null, 10, ParameterDirection.Output);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
            return (cmd.ReadString("@Result") ?? "skipped", cmd.ReadInt("@ResourceId"));
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
    }
}
