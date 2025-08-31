using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global

namespace HT.Api.Contracts.Client.Models
{
    public class MessageBase
    {
        /// <summary>
        /// A unique identifier for this particular occurrence of the problem. Might be used for correlation scenarios.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MessageUid { get; set; }

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
        /// A human-readable explanation specific to this occurrence of the problem. Like title, this field’s value can be localized
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Detail { get; set; }
        
        /// <summary>
        /// Non-standard meta-data about this particular message.  The API author should create documentation for the expected values.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("tags")]
        public Dictionary<string, string?>? Tags { get; set; }

        /// <summary>
        /// <inheritdoc cref="RequestLocation"/>
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? RequestLocationId => RequestLocation != null ? (int)RequestLocation : null;

        /// <summary>
        /// Which part of request this message was detected on (e.g. Header, Body, Query, Path).
        /// May be null when message is not related to a specific content of the request (e.g. authorization message, unhandled exception)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RequestLocation? RequestLocation { get; set; }

        /// <summary>
        /// Name of body property/field/attribute, header key, path segment, or query parameter that this message is referencing
        /// Callers might use this value to bind a message to an input field
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Property { get; set; }

        /// <summary>
        /// List of link(s) that describe more information about this message (e.g. error resolution)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Link>? Links { get; set; }
    }
}
