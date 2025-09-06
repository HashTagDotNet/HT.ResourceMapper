using System.Net;
using System.Text.Json.Serialization;

namespace HT.Api.Service.Contracts
{
    public class HttpApiResponse
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

       
    }
}
