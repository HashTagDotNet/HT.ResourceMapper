using HT.Api.Client.Contracts.Models;

namespace HT.Api.Service.Contracts.BuildersOfT
{
    public class DataBuilder<T> where T : class, new()
    {
        private readonly ServiceResponseBuilder<T> _responseBuilder;
        private ApiResponse<T> ApiResponse => _responseBuilder.BackingServiceResponse.ApiResponse;

        public DataBuilder(ServiceResponseBuilder<T> parent)
        {
            _responseBuilder = parent;
        }
        
        public DataBuilder<T> Set(T data)
        {
            if (ApiResponse.Errors is { Count: > 0 })
            {
                throw new InvalidOperationException(
                    "Cannot set data on API response when there are errors already registered.");
            }
            ApiResponse.Data = data;
            return this;
        }

        public DataBuilder<T> Clear()
        {
            ApiResponse.Data = null;
            return this;
        }

        public HttpApiResponseBuilder<T> Http => _responseBuilder.Http;
        public ValidationBuilder<T> Validation => _responseBuilder.Validation;
        public ErrorBuilder<T> Errors => _responseBuilder.Errors;
        public LinksBuilder<T> Links => _responseBuilder.Links;
        public MetaBuilder<T> Meta => _responseBuilder.Meta;
        public ApiServiceResponse<T> BuildResponse(Action<ApiServiceResponse<T>>? response=null) => _responseBuilder.BuildResponse(response);
    }

}
