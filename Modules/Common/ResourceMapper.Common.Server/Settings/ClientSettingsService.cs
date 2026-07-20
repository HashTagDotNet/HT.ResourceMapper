using System;
using System.Threading;
using System.Threading.Tasks;
using HT.Api.Client.Contracts.Models;                 // CallStatusCode
using HT.Api.Service.Contracts;                       // ApiServiceResponse<T>
using HT.Api.Service.Contracts.BuildersOfT;           // ServiceResponseBuilder<T>
using ResourceMapper.Common.Server.Settings.Interfaces;
using ResourceMapper.Common.Shared.Settings;

namespace ResourceMapper.Common.Server.Settings
{
    public class ClientSettingsService : IClientSettingsService
    {
        private readonly IClientSettingsRepository _repo;

        public ClientSettingsService(IClientSettingsRepository repo)
        {
            _repo = repo;
        }

        public async Task<ApiServiceResponse<ClientSettingModel>> GetAsync(string clientId, string settingKey, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<ClientSettingModel>();
            try
            {
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    builder.Validation.AddValidation("clientId", "Client id is required");
                    return builder.BuildResponse();
                }
                if (string.IsNullOrWhiteSpace(settingKey))
                {
                    builder.Validation.AddValidation("settingKey", "Setting key is required");
                    return builder.BuildResponse();
                }

                var value = await _repo.GetAsync(clientId, settingKey, cancellationToken);
                builder.Data.Set(new ClientSettingModel { SettingKey = settingKey, Value = value });
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        public async Task<ApiServiceResponse<object>> SetAsync(string clientId, string settingKey, string? value, CancellationToken cancellationToken = default)
        {
            var builder = new ServiceResponseBuilder<object>();
            try
            {
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    builder.Validation.AddValidation("clientId", "Client id is required");
                    return builder.BuildResponse();
                }
                if (string.IsNullOrWhiteSpace(settingKey))
                {
                    builder.Validation.AddValidation("settingKey", "Setting key is required");
                    return builder.BuildResponse();
                }

                await _repo.UpsertAsync(clientId, settingKey, value ?? string.Empty, cancellationToken);
                builder.Data.Set(new object());
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }
    }
}
