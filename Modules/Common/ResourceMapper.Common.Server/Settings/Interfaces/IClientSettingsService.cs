using System.Threading;
using System.Threading.Tasks;
using HT.Api.Service.Contracts;
using ResourceMapper.Common.Shared.Settings;

namespace ResourceMapper.Common.Server.Settings.Interfaces
{
    public interface IClientSettingsService
    {
        Task<ApiServiceResponse<ClientSettingModel>> GetAsync(string ownerId, string settingKey, CancellationToken cancellationToken = default);
        Task<ApiServiceResponse<object>> SetAsync(string ownerId, string settingKey, string? value, CancellationToken cancellationToken = default);
    }
}
