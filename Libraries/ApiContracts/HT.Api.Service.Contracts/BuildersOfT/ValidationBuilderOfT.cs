using HT.Api.Client.Contracts.Models;

namespace HT.Api.Service.Contracts.BuildersOfT
{
    /// <summary>
    /// Validation is a special case of errors that are not system or application errors
    /// and pertain to returning API call validation issues back to the caller.
    /// You can use ErrorBuilder to accomplish the same task
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValidationBuilder<T> where T : class, new()
    {
        private readonly ServiceResponseBuilder<T> _responseBuilder;
        private ApiResponse<T> ApiResponse => _responseBuilder.BackingServiceResponse.ApiResponse;

        public ValidationBuilder(ServiceResponseBuilder<T> serviceResponseBuilder)
        {
            _responseBuilder = serviceResponseBuilder;
        }

        public ValidationBuilder<T> AddValidation(PropertyLocation propertyLocation, string? propertyName, string? title = null, string? detail = null, string? code = null)
        {
            ApiResponse.Errors ??= new List<Message>();
            ApiResponse.Errors.Add(new Message()
            {
                Title = title,
                Details = detail,
                MessageCode = code,
                PropertyLocation = propertyLocation,
                PropertyName = propertyName,
                CallStatus = CallStatusCode.InvalidArgument
            });
            return this;
        }
        public ValidationBuilder<T> AddValidation(CallStatusCode callStatus, PropertyLocation? propertyLocation, string? propertyName, string? title = null, string? detail = null, string? code = null)
        {
            ApiResponse.Errors ??= new List<Message>();
            ApiResponse.Errors.Add(new Message()
            {
                Title = title,
                Details = detail,
                MessageCode = code,
                PropertyLocation = propertyLocation,
                PropertyName = propertyName,
                CallStatus = CallStatusCode.InvalidArgument
            });
            return this;
        }

        public HttpApiResponseBuilder<T> Http => _responseBuilder.Http;
        public ValidationBuilder<T> Validation => _responseBuilder.Validation;
        public ErrorBuilder<T> Errors => _responseBuilder.Errors;
        public LinksBuilder<T> Links => _responseBuilder.Links;
        public DataBuilder<T> Data => _responseBuilder.Data;

        public ApiServiceResponse<T> BuildResponse(Action<ApiServiceResponse<T>> response) => _responseBuilder.BuildResponse(response);
    }
}
