using System.Net;

namespace HT.Api.Service.Contracts.BuildersOfT
{
    public class HttpApiResponseBuilder<T> where T : class, new()
    {
        private readonly ServiceResponseBuilder<T> _responseBuilder;
        private ApiServiceResponse<T> ServiceResponse => _responseBuilder.BackingServiceResponse;
 
        private HttpApiResponse HttpResponse
        {
            get
            {
                _responseBuilder.BackingServiceResponse.HttpResponse ??= new HttpApiResponse();
                return _responseBuilder.BackingServiceResponse.HttpResponse;
            }
        }

        public HttpApiResponseBuilder(ServiceResponseBuilder<T> parent)
        {
            _responseBuilder = parent;
        }


        // Fluent methods
        public HttpApiResponseBuilder<T> AddHeader(string key, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            HttpResponse.Headers ??= [];
            HttpResponse.Headers.Add(new KeyValuePair<string, string>(key, value));
            return this;
        }

        public HttpApiResponseBuilder<T> SetStatusCode(HttpStatusCode statusCode)
        {
            HttpResponse.HttpStatusCode = statusCode;
            return this;
        }

        public HttpApiResponseBuilder<T> SetStatusCode(HttpStatusCode statusCode, string statusMessage)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(statusMessage);
            HttpResponse.HttpStatusCode = statusCode;
            HttpResponse.HttpStatusMessage = statusMessage;
            return this;
        }

        public HttpApiResponseBuilder<T> SetStatusMessage(string statusMessage)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(statusMessage);
            HttpResponse.HttpStatusMessage = statusMessage;
            return this;
        }


        public HttpApiResponseBuilder<T> AppendHeader(string key, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            HttpResponse.Headers ??= [];
            var existingHeader = HttpResponse.Headers.FirstOrDefault(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (existingHeader.Key != null)
            {
                HttpResponse.Headers.Remove(existingHeader);
                HttpResponse.Headers.Add(new KeyValuePair<string, string>(key, $"{existingHeader.Value},{value}"));
            }
            else
            {
                HttpResponse.Headers.Add(new KeyValuePair<string, string>(key, value));
            }
            return this;
        }

        /// <summary>
        /// Removes a header by key (case-insensitive)
        /// </summary>
        public HttpApiResponseBuilder<T> RemoveHeader(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            if (ServiceResponse?.HttpResponse?.Headers != null)
            {
                var headerToRemove = HttpResponse.Headers.FirstOrDefault(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
                if (headerToRemove.Key != null)
                {
                    HttpResponse.Headers.Remove(headerToRemove);
                }
                if (ServiceResponse.HttpResponse.Headers is { Count: 0 })
                {
                    ServiceResponse.HttpResponse.Headers = null;
                }

                if (ServiceResponse?.HttpResponse?.Headers == null &&
                    ServiceResponse?.HttpResponse?.HttpStatusCode == null &&
                    string.IsNullOrWhiteSpace(ServiceResponse.HttpResponse.HttpStatusMessage))
                {
                    ServiceResponse.HttpResponse = null;
                }
            }
        
            return this;
        }

        /// <summary>
        /// Gets a header value by key (case-insensitive)
        /// </summary>
        public string? GetHeaderValue(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (ServiceResponse?.HttpResponse?.Headers == null)
            {
                return null;
            }
            return HttpResponse.Headers?.FirstOrDefault(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
        }

        /// <summary>
        /// Checks if a header exists by key (case-insensitive)
        /// </summary>
        public bool HasHeader(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (ServiceResponse?.HttpResponse?.Headers == null)
            {
                return false;
            }
            return HttpResponse.Headers?.Any(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) == true;
        }
        public HttpApiResponseBuilder<T> Http => _responseBuilder.Http;
        public ValidationBuilder<T> Validation => _responseBuilder.Validation;
        public ErrorBuilder<T> Errors => _responseBuilder.Errors;
        public LinksBuilder<T> Links => _responseBuilder.Links;
        public MetaBuilder<T> Meta => _responseBuilder.Meta;
        public DataBuilder<T> Data => _responseBuilder.Data;
        public ApiServiceResponse<T> BuildResponse(Action<ApiServiceResponse<T>>? response=null) => _responseBuilder.BuildResponse(response);
    }

}
