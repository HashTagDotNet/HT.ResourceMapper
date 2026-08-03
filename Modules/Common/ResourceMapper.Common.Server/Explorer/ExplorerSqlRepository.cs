using System.Collections.Generic;
using System.Linq;
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

        public async Task<List<ExplorerNodeRow>> GetForExplorerAsync(string resourceUid,
            IReadOnlyCollection<string>? knownResourceUids, CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].Resource_GetForExplorer")
                .AddVarchar("@ResourceUid", resourceUid)
                .AddNVarchar("@KnownResourceUids", JoinKnownUids(knownResourceUids));

            return await _db.Execute.ExecuteQueryAsync(cmd, dr => new ExplorerNodeRow
            {
                Direction = dr.ReadString("Direction"),
                ResourceUid = dr.ReadString("ResourceUid"),
                ResourceKey = dr.ReadString("ResourceKey"),
                ResourceName = dr.ReadString("ResourceName"),
                ResourceType = dr.ReadString("ResourceType"),
                ShortCode = dr.ReadString("ShortCode"),
                IconKey = dr.ReadString("IconKey"),
                Domain = dr.ReadString("Domain"),
                PrimaryUrl = dr.ReadString("PrimaryUrl"),
                FromResourceUid = dr.ReadString("FromResourceUid"),
                ToResourceUid = dr.ReadString("ToResourceUid")
            }, cancellationToken: cancellationToken);
        }

        // The sproc splits this on ',' -- uids are guids/slugs, but drop anything containing the
        // delimiter rather than silently sending a value that could not round-trip. Null (not "")
        // when there is nothing to send, so STRING_SPLIT sees NULL and yields no rows.
        private static string? JoinKnownUids(IReadOnlyCollection<string>? knownResourceUids)
        {
            if (knownResourceUids is null || knownResourceUids.Count == 0) return null;
            var clean = knownResourceUids
                .Where(u => !string.IsNullOrWhiteSpace(u) && !u.Contains(','))
                .Select(u => u.Trim())
                .Distinct()
                .ToList();
            return clean.Count == 0 ? null : string.Join(",", clean);
        }
    }
}
