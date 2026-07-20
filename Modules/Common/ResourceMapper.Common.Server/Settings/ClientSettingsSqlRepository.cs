using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HT.Microsoft.SqlClient.Extensions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;
using ResourceMapper.Common.Server.Settings.Interfaces;
using ResourceMapper.Common.Server.Settings.Models;

namespace ResourceMapper.Common.Server.Settings
{
    public class ClientSettingsSqlRepository : IClientSettingsRepository
    {
        private readonly IDbConnector _db;

        public ClientSettingsSqlRepository(IDbConnector db)
        {
            _db = db;
        }

        public async Task<string?> GetAsync(string clientId, string settingKey, CancellationToken cancellationToken)
        {
            using var cmd = _db.RO.SprocCommand("[HTResourceMapper].ClientSetting_Get")
                .AddVarchar("@ClientId", clientId)
                .AddVarchar("@SettingKey", settingKey);

            var rows = await _db.Execute.ExecuteQueryAsync(cmd, dr => new ClientSettingRow
            {
                Value = dr.ReadString("Value")
            }, cancellationToken: cancellationToken);

            return rows.FirstOrDefault()?.Value;
        }

        public async Task UpsertAsync(string clientId, string settingKey, string settingJson, CancellationToken cancellationToken)
        {
            using var cmd = _db.RW.SprocCommand("[HTResourceMapper].ClientSetting_Upsert")
                .AddVarchar("@ClientId", clientId)
                .AddVarchar("@SettingKey", settingKey)
                .AddNVarchar("@SettingJson", settingJson);

            await _db.Execute.ExecuteNonQueryAsync(cmd, cancellationToken: cancellationToken);
        }
    }
}
