using System.Text.Json.Serialization;
using HT.Api.Contracts.Client.Interfaces;

namespace HT.Api.Contracts.Client.Models
{
    public partial class ApiResponse:IApiResponse 
    {
        /// <summary>
        /// List of errors on this message.  Not included on successful response. https://jsonapi.org/format/#errors
        /// </summary>
        [JsonPropertyOrder(int.MinValue)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ErrorMessage>? Errors { get; set; }
        public bool ShouldSerializeErrors() => Errors is { Count: > 0 };

        /// <summary>
        /// List of links (e.g. to object, log entries, etc.) https://jsonapi.org/format/#document-top-level
        /// </summary>
        [JsonPropertyOrder(int.MaxValue-1)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Link>? Links { get; set; }
        public bool ShouldSerializeLinks() => Links is { Count: > 0 };

        /// <summary>
        /// Included in each response. https://jsonapi.org/format/#document-meta
        /// </summary>
        [JsonPropertyName("meta")]
        [JsonPropertyOrder(int.MaxValue)]
        public MetaData MetaData { get; set; } = new MetaData();

        public void AddValidation(string propertyName, string message)
        {
            Errors ??= new List<ErrorMessage>();
            var errorMessage = new ErrorMessage
            {
                Property = propertyName,
                Detail = message,
                StatusCode = ErrorCodes.InvalidArgument
            };
            Errors.Add(errorMessage);
        }
    }
}

