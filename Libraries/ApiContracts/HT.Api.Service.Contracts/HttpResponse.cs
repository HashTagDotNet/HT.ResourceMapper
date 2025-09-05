using System.Net;
using System.Text.Json.Serialization;

namespace HT.Api.Service.Contracts
{
    public class HttpResponse
    {
        /// <summary>
        /// Headers the controller will add to the HTTP response
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<KeyValuePair<string, string>>? Headers { get; set; }

        /// <summary>
        /// Explicit HTTP status code to return.  If null, the controller will determine the status code based on the presence of the call status ApiResponse.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public HttpStatusCode? HttpStatusCode { get; set; }

        /// <summary>
        /// Message text to return with the HTTP status code (e.g. 200 OK, might become 200 Resource processed)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? HttpStatusMessage { get; set; }

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
    }
}
