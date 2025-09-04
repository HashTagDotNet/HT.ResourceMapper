using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global

namespace HT.Api.Client.Contracts.Models
{
    /// <summary>
    /// https://jsonapi.org/format/#errors
    /// </summary>
    public class ErrorMessage:MessageBase
    {
        /// <summary>
        /// <inheritdoc cref="ErrorCodes"/>
        /// </summary>
        public int StatusId => (int)StatusCode;

        /// <summary>
        /// <inheritdoc cref="ErrorCodes"/>
        /// </summary>
        public ErrorCodes StatusCode { get; set; } = ErrorCodes.Ok;
        
        /// <summary>
        /// Practical actions that the developer of application consuming the API could take in order to resolve the error condition.  May be localized to callers language, resource code, or other content. 
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SuggestedApplicationActions { get; set; }

        /// <summary>
        /// Practical actions that a user of the application consuming the API could take in order to resolve the error condition.  May be localized to callers language, resource code, or other content.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SuggestedUserActions { get; set; }
    }
}
