using System.Threading;
using System.Threading.Tasks;
using MacroPoint.Azure.ResourceExplorer.Api.Service.Contracts;
using MacroPoint.Azure.ResourceExplorer.Common.Shared.Settings;

namespace MacroPoint.Azure.ResourceExplorer.Common.Server.Settings.Interfaces
{
    public interface IClientSettingsService
    {
        Task<ApiServiceResponse<ClientSettingModel>> GetAsync(string ownerId, string settingKey, CancellationToken cancellationToken = default);
        Task<ApiServiceResponse<object>> SetAsync(string ownerId, string settingKey, string? value, CancellationToken cancellationToken = default);
    }
}
