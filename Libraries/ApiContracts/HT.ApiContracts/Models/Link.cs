using System.Text.Json.Serialization;

namespace HT.Api.Client.Contracts.Models
{
    /// <summary>
    /// A standard way to describing a link in the API response structure.  It is based on the informational RFC 8631
    /// </summary>
    public class Link
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Link(){}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="href">A URL to the referenced object</param>
        /// <param name="rel">Describes the relationship of this object to the object referenced in <paramref name="href"/> </param>
        /// <param name="title">Provides some human-readable name or short description about what this link does</param>
        /// <param name="method">The HTTP verb that to be used to make a request to the target of the link. (default: GET)</param>
        public Link(string href, string? rel = null, string? title=null, string? method=null)
        {
            Href = href;
            Rel = rel;
            Title = title;
            Method = method??="GET";
        }

        /// <summary>
        /// A URL reference to the referenced object
        /// </summary>
        [JsonPropertyName("href")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Href { get; set; }

        /// <summary>
        ///  Describes the relationship of this object to the object referenced in <see cref="Href"></see>. (e.g. 'self', 'firstChild')/>
        /// </summary>
        [JsonPropertyName("rel")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Rel { get; set; }

        /// <summary>
        /// The HTTP verb that to be used to make a request to the target of the link. (default: GET)
        /// </summary>
        [JsonPropertyName("method")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Method { get; set; }

        /// <summary>
        /// Provides some human-readable name or short description about what this link does. Callers might
        /// use this value when building &lt;a&gt; links in the UI.
        /// </summary>
        [JsonPropertyName("title")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Title { get; set; }
    }
}
