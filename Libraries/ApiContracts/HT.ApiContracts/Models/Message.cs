using System.Text.Json.Serialization;

namespace HT.Api.Client.Contracts.Models
{
    /// <summary>
    /// A message the API wishes to send back to the caller about this request.  Often validation results.
    /// NOTE: This is the primary way for an API to communicate out-of-band information back to caller.
    /// NOTE: Not all implementations will use all these fields. Consult your API's documentation for details.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// A unique identifier for this particular occurrence of the problem. Might be used for correlation scenarios.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MessageUid { get; set; }

        /// <summary>
        /// Determines which specific type of message is (if message is an error). This value
        /// applies *only* to this particular message instance. Use MetaData call status for overall status of the API call.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CallStatusCode? CallStatus { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? CallStatusId { get; set; } // set during message build process

        /// <summary>
        /// An application-specific message identifier expressed as a string value. This is not a correlation id, but a code that can be used to uniquely identify message. 
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MessageCode { get; set; }

        /// <summary>
        /// A short, human-readable summary of the problem that SHOULD NOT change from occurrence to occurrence of the problem, except for purposes of localization.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Title { get; set; }

        /// <summary>
        /// A human-readable explanation specific to this occurrence of the problem. Like title, this field’s value can be localized by the caller
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Details { get; set; }

        /// <summary>
        /// Internal technical details the API author wants to share with the caller but the caller should not
        /// normally display to the user.  Often used during development or troubleshooting. Many APIs ignore this field.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SystemDetails { get; set; }

        /// <summary>
        /// Non-standard meta-data about this particular message.  The API author should create documentation for the expected values.
        /// An implementation might use tags to group like messages together for filtering or other purposes.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("tags")]
        public Dictionary<string, string?>? Tags { get; set; }


        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? PropertyLocationId { get; set; }

        /// <summary>
        /// Which part of request this message was detected on (e.g. Header, Body, Query, Path). Often used in validation responses.
        /// May be null when message is not related to a specific content of the request (e.g. authorization message, unhandled exception)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PropertyLocation? PropertyLocation{ get; set; }

        /// <summary>
        /// Name of body property/field/attribute, header key, path segment, or query parameter that this message is referencing
        /// Often used in validation responses. Callers might use this value to bind a message to an input field. When lacking other context, may be
        /// a pointer in  request document that caused the error [e.g. "/data" for a primary data object, or "/data/attributes/title"]
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PropertyName { get; set; }

        /// <summary>
        /// List of link(s) that describe more information about this message (e.g. error resolution), help pages, error dictionaries,
        /// See <a href="https://jsonapi.org/format/#error-objects"></a>
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Link>? Links { get; set; }

        /// <summary>
        /// Tells caller how important this message might be. (e.g. a UI might use this level to display colored toast messages) <inheritdoc cref="MessageSeverity"/>
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MessageSeverity? SeverityCode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SeverityId { get; set; } // set during message build process
    }
}
