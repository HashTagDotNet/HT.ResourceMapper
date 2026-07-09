using System.Text.Json.Serialization;

namespace ResourceMapper.Common.Shared.HomePage.Contracts
{

    public class ResourceGridRequest
    {

        public int Skip { get; set; }
        public int Take { get; set; }

        /// <summary>
        /// Free text search across multiple columns looking for 'contains' matches.
        /// Applied in addition to (AND) any structured <see cref="Filters"/>.
        /// </summary>
        public string? SearchFor { get; set; }

        /// <summary>
        /// Order result by a single column. Alternate to SortBy collection
        /// </summary>
        public string? OrderBy { get; set; }
        public string? OrderDirection { get; set; } = "Asc"; // or "Desc"

        public List<ResourceGridSortDefinition>? SortBy { get; set; }

        public List<ResourceGridFilterDefinition>? Filters { get; set; }
    }

    public class ResourceGridSortDefinition
    {
        public string? Column { get; set; }        // Maps to PropertyFunc
        public string Direction { get; set; } = "Asc"; // or "Desc" or "" for natural
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ResourceGridFilterKind
    {
        Enumerable,
        Text
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ResourceGridFilterOperator
    {
        Equals,
        NotEquals,
        Contains
    }

    /// <summary>
    /// A single structured filter. Filters AND together; selected <see cref="Values"/> OR within one filter.
    /// </summary>
    public class ResourceGridFilterDefinition
    {
        /// <summary>"ResourceType" | "ResourceName" | "Description" | "Tag".</summary>
        public string Column { get; set; } = "";

        /// <summary>The tag key (e.g. "Environment") — set iff <see cref="Column"/> == "Tag".</summary>
        public string? TagKey { get; set; }

        public ResourceGridFilterKind Kind { get; set; }

        /// <summary>Equals / NotEquals for enumerable filters; Contains for text filters.</summary>
        public ResourceGridFilterOperator Operator { get; set; } = ResourceGridFilterOperator.Equals;

        /// <summary>Enumerable OR-set (Type values, or a tag key's values). Null/empty for text filters.</summary>
        public List<string>? Values { get; set; }

        /// <summary>True when the "(blank)"/NULL bucket is part of the selection.</summary>
        public bool IncludeBlank { get; set; }

        /// <summary>Contains text for text filters (ResourceName / Description).</summary>
        public string? Text { get; set; }
    }

    public class ResourceGridResponse
    {
        public List<ResourceGridItemModel>? Items { get; set; }

        public int TotalItems { get; set; }
    }

    /// <summary>
    /// Request for a column's distinct value+count list. Counts reflect the search box AND every active
    /// filter EXCEPT the facet's own dimension (Column, and TagKey when Column == "Tag").
    /// </summary>
    public class ResourceGridFacetRequest
    {
        /// <summary>"ResourceType" | "Tag".</summary>
        public string Column { get; set; } = "";

        /// <summary>Required when <see cref="Column"/> == "Tag".</summary>
        public string? TagKey { get; set; }

        public string? SearchFor { get; set; }

        /// <summary>All active filters; the server ignores the one matching this facet's dimension.</summary>
        public List<ResourceGridFilterDefinition>? Filters { get; set; }
    }

    public class ResourceGridFacetResponse
    {
        public string Column { get; set; } = "";
        public string? TagKey { get; set; }
        public List<ResourceGridFacetValue> Values { get; set; } = new();
    }

    public class ResourceGridFacetValue
    {
        /// <summary>The distinct value; null for the blank bucket.</summary>
        public string? Value { get; set; }

        /// <summary>Display label ("(blank)" for the blank bucket, otherwise the value).</summary>
        public string Display { get; set; } = "";

        public int Count { get; set; }

        public bool IsBlank { get; set; }
    }
}
