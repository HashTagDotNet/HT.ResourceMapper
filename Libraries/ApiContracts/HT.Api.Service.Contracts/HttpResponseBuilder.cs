using System.Net;

namespace HT.Api.Service.Contracts
{
    public class HttpResponseBuilder<TResponseData> where TResponseData : class, new()
    {
        private readonly ServiceResponseBuilder<TResponseData> _responseBuilder;

        public HttpResponseBuilder(ServiceResponseBuilder<TResponseData> parent)
        {
            _responseBuilder = parent;
        }

      
        // Fluent methods
        public HttpResponseBuilder<TResponseData> AddHeader(string key, string value)
        {
            _responseBuilder.BackingServiceResponse.AddHeader(key, value);
            return this;
        }

        public HttpResponseBuilder<TResponseData> SetStatusCode(HttpStatusCode statusCode)
        {
            _responseBuilder.BackingServiceResponse.SetStatusCode(statusCode);
            return this;
        }

        public HttpResponseBuilder<TResponseData> SetStatusCode(HttpStatusCode statusCode, string statusMessage)
        {
            _responseBuilder.BackingServiceResponse.SetStatusCode(statusCode, statusMessage);
            return this;
        }

        public HttpResponseBuilder<TResponseData> SetStatusMessage(string statusMessage)
        {
            _responseBuilder.BackingServiceResponse.SetStatusMessage(statusMessage);
            return this;
        }

        // Builder completion
        public ServiceResponseBuilder<TResponseData> And => _responseBuilder;
        public ApiServiceResponse<TResponseData> Build() => _responseBuilder.Build();
    }

}
