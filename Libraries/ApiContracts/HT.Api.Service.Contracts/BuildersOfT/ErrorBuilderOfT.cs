using HT.Api.Client.Contracts.Models;

namespace HT.Api.Service.Contracts.BuildersOfT
{
    public class ErrorBuilder<T> where T: class, new()
    {
        private readonly ServiceResponseBuilder<T> _responseBuilder;
        private ApiResponse<T> ApiResponse => _responseBuilder.BackingServiceResponse.ApiResponse;


        public ErrorBuilder(ServiceResponseBuilder<T> parent)
        {
            _responseBuilder = parent;
        }


        public ErrorBuilder<T> AddError(CallStatusCode callStatus, string? title=null, string? detail=null, string? code = null)
        {
            ApiResponse.Errors ??= [];

            ApiResponse.Errors.Add(new Message()
            {
                Title = title,
                Details = detail,
                MessageCode = code,
                CallStatus = callStatus,
            });
            return this;
        }

        public ErrorBuilder<T> AddError(Message error)
        {
            ArgumentNullException.ThrowIfNull(error);
            ApiResponse.Errors ??= [];
            ApiResponse.Errors.Add(error);
            return this;
        }
        public ErrorBuilder<T> ClearErrors()
        {
            ApiResponse.Errors?.Clear();
            ApiResponse.Errors = null;
            return this;
        }

        public ErrorBuilder<T> AddError(Action<Message> message)
        {
            ArgumentNullException.ThrowIfNull(message);
            ApiResponse.Errors ??= [];
            var msg = new Message();
            message(msg);
            ApiResponse.Errors.Add(msg);
            return this;
        }
        public HttpApiResponseBuilder<T> Http => _responseBuilder.Http;
        public ValidationBuilder<T> Validation => _responseBuilder.Validation;
        public DataBuilder<T> Data => _responseBuilder.Data;
        public LinksBuilder<T> Links => _responseBuilder.Links;
        public MetaBuilder<T> Meta => _responseBuilder.Meta;

        public ApiServiceResponse<T> BuildResponse(Action<ApiServiceResponse<T>>? response=null) => _responseBuilder.BuildResponse(response);
    }
}
   
    

