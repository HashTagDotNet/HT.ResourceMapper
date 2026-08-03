namespace ResourceMapper.Common.Server.Resources.Models
{
    /// <summary>One resource row from Export_GetResources (Domain is the IsDomainTag=1 tag value).</summary>
    public class ExportResourceRow
    {
        public int ResourceId { get; set; }
        public string ResourceUid { get; set; } = string.Empty;
        public string ResourceKey { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Domain { get; set; }
    }

    /// <summary>One ResourceTag row from Export_GetResourceTags, joined to its definition.</summary>
    public class ExportTagRow
    {
        public int ResourceId { get; set; }
        public string TagDefinitionKey { get; set; } = string.Empty;
        public string? TagValue { get; set; }
        public bool IsMultiValued { get; set; }
        public bool IsDomainTag { get; set; }
    }

    /// <summary>
    /// One dependency edge from Export_GetDependencies (From = dependent, To = dependency),
    /// carrying both endpoints' domains so the service can drop edges import cannot express.
    /// </summary>
    public class ExportDependencyRow
    {
        public int FromResourceId { get; set; }
        public string FromResourceKey { get; set; } = string.Empty;
        public string? FromDomain { get; set; }
        public string ToResourceKey { get; set; } = string.Empty;
        public string? ToDomain { get; set; }
    }
}
