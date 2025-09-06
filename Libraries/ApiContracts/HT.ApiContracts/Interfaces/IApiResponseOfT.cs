using System.Text.Json.Serialization;

namespace HT.Api.Client.Contracts.Interfaces
{
    public interface IApiResponse<TApiData>:IApiResponse where TApiData : class,new()
    {
        /// <summary>
        /// The data returned by the API
        /// </summary>
        [JsonPropertyName("data")]
        [JsonPropertyOrder(int.MaxValue)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TApiData? Data { get; set; }

        bool ShouldSerializeData();
    }

}
