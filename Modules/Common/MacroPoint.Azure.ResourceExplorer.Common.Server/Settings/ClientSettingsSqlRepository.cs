using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MacroPoint.Azure.ResourceExplorer.SqlClient.Extensions;
using MacroPoint.Azure.ResourceExplorer.SqlClient.Extensions.Abstractions.Interfaces;
using MacroPoint.Azure.ResourceExplorer.Common.Server.Settings.Interfaces;
using MacroPoint.Azure.ResourceExplorer.Common.Server.Settings.Models;

namespace MacroPoint.Azure.ResourceExplorer.Common.Server.Settings
{
    public class ClientSettingsSqlRepository : IClientSettingsRepository
    {
        private readonly IDbConnector _db;

        public ClientSettingsSqlRepository(IDbConnector db)
        {
            _db = db;
        }

        public async Task<string?> GetAsync(string ownerId, string settingKey, CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[ResourceExplorer].ClientSetting_Get")
                .AddVarchar("@OwnerId", ownerId)
                .AddVarchar("@SettingKey", settingKey);

            var rows = await _db.Execute.ExecuteQueryAsync(cmd, dr => new ClientSettingRow
            {
                Value = dr.ReadString("Value")
            }, cancellationToken: cancellationToken);

            return rows.FirstOrDefault()?.Value;
        }

        public async Task UpsertAsync(string ownerId, string settingKey, string settingJson, CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[ResourceExplorer].ClientSetting_Upsert")
                .AddVarchar("@OwnerId", ownerId)
                .AddVarchar("@SettingKey", settingKey)
                .AddNVarchar("@SettingJson", settingJson);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
        }
    }
}
