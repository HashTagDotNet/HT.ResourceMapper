using HT.Api.Client.Contracts.Models;
using System.Text.Json.Serialization;

namespace HT.Api.Client.Contracts.Interfaces
{
    public interface IApiResponse
    {
        [JsonPropertyName("errors")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyOrder(int.MaxValue-1)]
        public List<Message>? Errors { get; set; }

        [JsonPropertyName("links")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyOrder(int.MaxValue - 2)]
        public List<Link>? Links { get; set; }

        [JsonPropertyName("meta")]
        [JsonPropertyOrder(int.MaxValue)]
        public MetaData MetaData { get; set; }

        bool ShouldSerializeErrors();

        /// <summary>
        /// Determines whether to serialize the Links property
        /// </summary>
        bool ShouldSerializeLinks();

    }
}
