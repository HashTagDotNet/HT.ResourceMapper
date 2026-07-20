using System.Threading;
using System.Threading.Tasks;

namespace ResourceMapper.Common.Server.Settings.Interfaces
{
    public interface IClientSettingsRepository
    {
        Task<string?> GetAsync(string clientId, string settingKey, CancellationToken cancellationToken);
        Task UpsertAsync(string clientId, string settingKey, string settingJson, CancellationToken cancellationToken);
    }
}
