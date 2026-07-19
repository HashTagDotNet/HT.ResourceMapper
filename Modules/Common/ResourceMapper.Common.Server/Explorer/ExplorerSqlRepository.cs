using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HT.Microsoft.SqlClient.Extensions;                       // SprocCommand / AddVarchar / ExecuteQueryAsync
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces; // IDbConnector
using ResourceMapper.Common.Server.Explorer.Interfaces;
using ResourceMapper.Common.Server.Resources.Models;

namespace ResourceMapper.Common.Server.Explorer
{
    public class ExplorerSqlRepository : IExplorerRepository
    {
        private readonly IDbConnector _db;

        public ExplorerSqlRepository(IDbConnector db)
        {
            _db = db;
        }

        public async Task<List<ExplorerNodeRow>> GetForExplorerAsync(string resourceUid, CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].Resource_GetForExplorer")
                .AddVarchar("@ResourceUid", resourceUid);

            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new ExplorerNodeRow
            {
                Direction = dr.ReadString("Direction"),
                ResourceUid = dr.ReadString("ResourceUid"),
                ResourceKey = dr.ReadString("ResourceKey"),
                ResourceName = dr.ReadString("ResourceName"),
                ResourceType = dr.ReadString("ResourceType"),
                Domain = dr.ReadString("Domain"),
                PrimaryUrl = dr.ReadString("PrimaryUrl")
            }, cancellationToken: cancellationToken);
        }
    }
}
