using System.Text.Json.Serialization;

namespace HT.Api.Client.Contracts.Models
{
    ///<summary>
    /// System information about the API call and not specifically bound to the response payload (e.g. timings, correlation ids, timestamps, validation errors)
    /// </summary>
    /// <remarks>Inspired by OData https://www.odata.org/documentation/odata-version-3-0/json-verbose-format/</remarks>
    public class MetaData
    {
        /// <summary>
        /// Unique identifier of this response.  Can be used for a correlation id
        /// </summary>
        public string ResponseId { get; set; } = Guid.NewGuid().ToString("N")[..8];

        /// <summary>
        /// Timestamp when this response was returned to caller. Based on server's time.  yyyy-MM-ddTHH:mm:ss.ffZ format
        /// </summary>
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffZ");

        /// <summary>
        /// Random key-value pair of data.  Sender and caller must agree on semantics.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SortedDictionary<string, string>? Tags { get; set; }

        /// <summary>
        /// List of message(s) the API wants to send back to the caller.
        /// For example, it may be informational, warning (quota about to expire).
        /// This will often be the &#x27;error response&#x27; found in other API vendors.
        /// NOTE: These messages do not contribute to the determination of the success or failure of the API call.
        /// Use the presence of ErrorCodes in the Errors collection to determine success or failure.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Message>? Messages { get; set; }

        /// <summary>
        /// Defines the overall status of the API call.  This is the primary status code for the response.
        /// The server MUST always ensure this value is set before the response is returned to the caller.
        /// The server can set this value based on the presence of CallStatusCodes in the Errors collection.
        /// </summary>
        public CallStatusCode? CallStatus { get; set; } = CallStatusCode.Ok;
    }
}
