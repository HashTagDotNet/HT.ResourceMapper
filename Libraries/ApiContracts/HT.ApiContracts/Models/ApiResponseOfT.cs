using System.Text.Json.Serialization;
using HT.Api.Contracts.Client.Interfaces;

// ReSharper disable InconsistentNaming

namespace HT.Api.Contracts.Client.Models
{

    /// <summary>
    /// https://jsonapi.org/format/#document-top-level
    /// </summary>
    public class ApiResponse<TData> : ApiResponse, IApiResponse<TData>, IApiResponse where TData : class, new()
    {
        /// <summary>
        /// The payload of the response. Data and Errors cannot be included in same document https://jsonapi.org/format/#document-top-level
        /// </summary>
        [JsonPropertyName("data")]
        [JsonPropertyOrder(0)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TData? Data { get; set; }
       
        /// <summary>
        /// Only serialize data when there are no errors.  Data and Errors cannot be included in same document https://jsonapi.org/format/#document-top-level
        /// </summary>
        /// <returns></returns>
        public bool ShouldSerializeData() => !ShouldSerializeErrors();



    }
}
