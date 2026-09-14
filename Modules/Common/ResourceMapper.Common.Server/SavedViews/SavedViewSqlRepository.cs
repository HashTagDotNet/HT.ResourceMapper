using System.Data;
using HT.Microsoft.SqlClient.Extensions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;
using ResourceMapper.Common.Server.SavedViews.Interfaces;
using ResourceMapper.Common.Shared.SavedViews;

namespace ResourceMapper.Common.Server.SavedViews
{
    public class SavedViewSqlRepository : ISavedViewRepository
    {
        private const string SavedViewOrderListType = "[HTResourceMapper].[SavedViewOrderList]";

        private readonly IDbConnector _db;

        public SavedViewSqlRepository(IDbConnector db)
        {
            _db = db;
        }

        public async Task<List<SavedViewModel>> ListAsync(string ownerId, CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].SavedView_ListForOwner")
                .AddVarchar("@OwnerId", ownerId);

            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new SavedViewModel
            {
                SavedViewUid = dr.ReadString("SavedViewUid"),
                Name         = dr.ReadString("Name"),
                QueryString  = dr.ReadString("QueryString"),
                SortOrder    = dr.ReadInt("SortOrder"),
                IsDefault    = dr.ReadBoolean("IsDefault")
            }, cancellationToken: cancellationToken);
        }

        public async Task<(string Result, string SavedViewUid)> UpsertAsync(
            string ownerId, string savedViewUid, string name, string queryString,
            CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].SavedView_Upsert")
                .AddVarchar("@SavedViewUid", savedViewUid)
                .AddVarchar("@OwnerId", ownerId)
                .AddNVarchar("@Name", name)
                .AddNVarchar("@QueryString", queryString);

            var rows = await _db.Execute.ExecuteQueryAsync(cmd, dr => new UpsertRow
            {
                Result = dr.ReadString("Result"),
                Uid    = dr.ReadString("SavedViewUid")
            }, cancellationToken: cancellationToken);

            var row = rows.FirstOrDefault();
            return (row?.Result ?? "denied", row?.Uid ?? string.Empty);
        }

        public async Task<int> DeleteAsync(string ownerId, string savedViewUid, CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].SavedView_Delete")
                .AddVarchar("@OwnerId", ownerId)
                .AddVarchar("@SavedViewUid", savedViewUid);

            var rows = await _db.Execute.ExecuteQueryAsync(
                cmd, dr => dr.ReadInt("DeletedCount"), cancellationToken: cancellationToken);

            return rows.FirstOrDefault();
        }

        public async Task SetDefaultAsync(string ownerId, string? savedViewUid, CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].SavedView_SetDefault")
                .AddVarchar("@OwnerId", ownerId)
                .AddVarchar("@SavedViewUid", savedViewUid);   // null clears the owner's default

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
        }

        public async Task ReorderAsync(
            string ownerId, IReadOnlyList<string> uidsInOrder, CancellationToken cancellationToken)
        {
            using var table = new DataTable();
            table.Columns.Add("SavedViewUid", typeof(string));
            table.Columns.Add("SortOrder", typeof(int));

            // Position in the list IS the sort order, so the caller never computes one.
            for (var i = 0; i < uidsInOrder.Count; i++)
                table.Rows.Add(uidsInOrder[i], i);

            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].SavedView_Reorder")
                .AddVarchar("@OwnerId", ownerId)
                .AddTvp("@Order", SavedViewOrderListType, table);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
        }

        /// <summary>Shape of the single row SavedView_Upsert selects back.</summary>
        private sealed class UpsertRow
        {
            public string Result { get; set; } = string.Empty;
            public string Uid { get; set; } = string.Empty;
        }
    }
}
