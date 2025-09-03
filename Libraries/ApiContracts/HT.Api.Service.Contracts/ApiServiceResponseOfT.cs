using HT.ApiContracts.Client.Models;
using System.Text.Json.Serialization;

namespace HT.Api.Service.Contracts
{
    public class ApiServiceResponse<TApiPayload>:ApiServiceResponse where TApiPayload : class,new()
    {
        private ApiResponse<TApiPayload>? _apiResponse;

        public new  ApiResponse<TApiPayload>? ApiResponse
        {
            get
            {
                if (_apiResponse != null) return _apiResponse;
                _apiResponse = new ApiResponse<TApiPayload>();
                return ApiResponse;
            }
            set => _apiResponse = value;
        }

        [JsonIgnore]
        public TApiPayload? Data
        {
            get => ApiResponse?.Data;

            set
            {
                if (ApiResponse != null) ApiResponse.Data = value;
            }
        }

    }
}
