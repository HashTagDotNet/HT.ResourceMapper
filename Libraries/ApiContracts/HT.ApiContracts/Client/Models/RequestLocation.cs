using System.Text.Json.Serialization;

namespace HT.Api.Contracts.Client.Models
{
    /// <summary>
    /// Component in HTTP request that is being referenced
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RequestLocation
    {
        Other = 0,
        /// <summary>
        /// Any part of request or not location specific (e.g. permission)
        /// </summary>
        Request = 1,

        /// <summary>
        /// Query string / filter
        /// </summary>
        Query = 2,

        /// <summary>
        /// Anywhere starting from protocol ending before start of query
        /// </summary>
        Path = 3,

        /// <summary>
        /// Body of message (if any)
        /// </summary>
        Body = 4,

        /// <summary>
        /// HTTP header
        /// </summary>
        Header = 5

    }
}
