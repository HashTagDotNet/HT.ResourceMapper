using System.Text.Json.Serialization;

namespace HT.Api.Client.Contracts.Models
{
    /// <summary>
    /// A message the API wishes to send back to the caller about this request.  Often validation results
    /// </summary>
    public class Message:MessageBase
    {
        /// <summary>
        /// Defines a group of like messages (e.g. internal process, quota, accounting, etc.)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MessageType { get; set; }

        /// <summary>
        /// Tells caller how important this message might be. (e.g. toast messages) <inheritdoc cref="MessageLevel"/>
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MessageLevel? SeverityCode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SeverityId => SeverityCode != null ? (int)SeverityCode : null;
    }
}
