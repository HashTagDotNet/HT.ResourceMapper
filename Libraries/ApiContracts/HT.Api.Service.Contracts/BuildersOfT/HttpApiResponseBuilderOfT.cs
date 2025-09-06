using System.Net;
using System.Reflection.PortableExecutable;

namespace HT.Api.Service.Contracts.BuildersOfT
{
    public class HttpApiResponseBuilderOfT<TResponseData> where TResponseData : class, new()
    {
        private readonly ServiceResponseBuilder<TResponseData> _responseBuilder;

        public HttpApiResponseBuilderOfT(ServiceResponseBuilder<TResponseData> parent)
        {
            _responseBuilder = parent;
        }

      
        // Fluent methods
        public HttpApiResponseBuilderOfT<TResponseData> AddHeader(string key, string value)
        {
            _responseBuilder.BackingServiceResponse.HttpResponse.AddHeader(key, value);
            return this;
        }

        public HttpApiResponseBuilderOfT<TResponseData> SetStatusCode(HttpStatusCode statusCode)
        {
            _responseBuilder.BackingServiceResponse.HttpResponse.SetStatusCode(statusCode);
            return this;
        }

        public HttpApiResponseBuilderOfT<TResponseData> SetStatusCode(HttpStatusCode statusCode, string statusMessage)
        {
            _responseBuilder.BackingServiceResponse.HttpResponse.SetStatusCode(statusCode, statusMessage);
            return this;
        }

        public HttpApiResponseBuilderOfT<TResponseData> SetStatusMessage(string statusMessage)
        {
            _responseBuilder.BackingServiceResponse.SetStatusMessage(statusMessage);
            return this;
        }
        public ApiServiceResponse AddHeader(string key, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            Headers ??= [];
            Headers.Add(new KeyValuePair<string, string>(key, value));
            return this;
        }

        public ApiServiceResponse AppendHeader(string key, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            Headers ??= [];
            var existingHeader = Headers.FirstOrDefault(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (existingHeader.Key != null)
            {
                Headers.Remove(existingHeader);
                Headers.Add(new KeyValuePair<string, string>(key, $"{existingHeader.Value},{value}"));
            }
            else
            {
                Headers.Add(new KeyValuePair<string, string>(key, value));
            }
            return this;
        }

        /// <summary>
        /// Removes a header by key (case-insensitive)
        /// </summary>
        public ApiServiceResponse RemoveHeader(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            if (Headers != null)
            {
                var headerToRemove = Headers.FirstOrDefault(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
                if (headerToRemove.Key != null)
                {
                    Headers.Remove(headerToRemove);
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
            return Headers?.FirstOrDefault(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
        }

        /// <summary>
        /// Checks if a header exists by key (case-insensitive)
        /// </summary>
        public bool HasHeader(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return Headers?.Any(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) == true;
        }
        // Builder completion
        public ServiceResponseBuilder<TResponseData> And => _responseBuilder;
        public ApiServiceResponse<TResponseData> Build() => _responseBuilder.Build();
    }

}
