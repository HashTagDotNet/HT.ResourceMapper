using System.Threading;
using System.Threading.Tasks;

namespace MacroPoint.Azure.ResourceExplorer.Common.Server.Settings.Interfaces
{
    public interface IClientSettingsRepository
    {
        Task<string?> GetAsync(string ownerId, string settingKey, CancellationToken cancellationToken);
        Task UpsertAsync(string ownerId, string settingKey, string settingJson, CancellationToken cancellationToken);
    }
}
