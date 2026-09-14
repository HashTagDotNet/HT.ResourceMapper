using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HT.Microsoft.SqlClient.Extensions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;
using ResourceMapper.Common.Server.Explorer.Interfaces;
using ResourceMapper.Common.Server.Explorer.Models;

namespace ResourceMapper.Common.Server.Explorer
{
    public class DiagramSqlRepository : IDiagramRepository
    {
        private readonly IDbConnector _db;

        public DiagramSqlRepository(IDbConnector db)
        {
            _db = db;
        }

        public async Task<DiagramUpsertResult> UpsertAsync(string diagramUid, string shareId, string ownerId,
            string name, string seedResourceUid, string displayPreset, string diagramJson,
            CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].Diagram_Upsert")
                .AddVarchar("@DiagramUid", diagramUid)
                .AddVarchar("@ShareId", shareId)
                .AddVarchar("@OwnerId", ownerId)
                .AddNVarchar("@Name", name)
                .AddVarchar("@SeedResourceUid", seedResourceUid)
                .AddVarchar("@DisplayPreset", displayPreset)
                .AddNVarchar("@DiagramJson", diagramJson);

            var rows = await _db.Execute.ExecuteQueryAsync(cmd, dr => new DiagramUpsertResult
            {
                Result = dr.ReadString("Result"),
                DiagramUid = dr.ReadString("DiagramUid"),
                ShareId = dr.ReadString("ShareId")
            }, cancellationToken: cancellationToken);

            return rows.First();
        }

        public async Task<DiagramRow?> GetByShareIdAsync(string shareId, CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].Diagram_GetByShareId")
                .AddVarchar("@ShareId", shareId);

            var rows = await _db.Execute.ExecuteQueryAsync(cmd, dr => new DiagramRow
            {
                DiagramUid = dr.ReadString("DiagramUid"),
                ShareId = dr.ReadString("ShareId"),
                OwnerId = dr.ReadString("OwnerId"),
                Name = dr.ReadString("Name"),
                SeedResourceUid = dr.ReadString("SeedResourceUid"),
                DisplayPreset = dr.ReadString("DisplayPreset"),
                DiagramJson = dr.ReadString("DiagramJson"),
                UpdatedOnUtc = dr.ReadString("UpdatedOnUtc")
            }, cancellationToken: cancellationToken);

            return rows.FirstOrDefault();
        }

        public async Task<List<DiagramListRow>> ListForClientAsync(string ownerId, CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].Diagram_ListForOwner")
                .AddVarchar("@OwnerId", ownerId);

            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new DiagramListRow
            {
                DiagramUid = dr.ReadString("DiagramUid"),
                ShareId = dr.ReadString("ShareId"),
                Name = dr.ReadString("Name"),
                SeedResourceUid = dr.ReadString("SeedResourceUid"),
                UpdatedOnUtc = dr.ReadString("UpdatedOnUtc")
            }, cancellationToken: cancellationToken);
        }

        public async Task<bool> DeleteAsync(string ownerId, string diagramUid, CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].Diagram_Delete")
                .AddVarchar("@OwnerId", ownerId)
                .AddVarchar("@DiagramUid", diagramUid);

            var rows = await _db.Execute.ExecuteQueryAsync(cmd, dr => dr.ReadInt("Deleted"),
                cancellationToken: cancellationToken);

            return rows.FirstOrDefault() > 0;
        }
    }
}
