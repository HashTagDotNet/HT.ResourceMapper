using System.Text.Json.Serialization;
using HT.Api.Client.Contracts.Models;

namespace HT.Api.Service.Contracts
{
    public class ApiServiceResponse<TApiPayload> : ApiServiceResponse where TApiPayload : class, new()
    {
        private ApiResponse<TApiPayload>? _apiResponse;

        public new ApiResponse<TApiPayload>? ApiResponse
        {
            get
            {
                if (_apiResponse != null) return _apiResponse;
                _apiResponse = new ApiResponse<TApiPayload>();
                return _apiResponse;
            }
            set => _apiResponse = value;
        }

        [JsonIgnore]
        public TApiPayload? Data
        {
            get => ApiResponse?.Data;
            set
            {
                if (ApiResponse != null)
                {
                    ApiResponse.Data = value;
                }
            }
        }

    }
}
