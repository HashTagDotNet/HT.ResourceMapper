namespace ResourceMapper.Common.Server.Resources.Models
{
    /// <summary>
    /// A ResourceType row plus its dependency counts, as returned by
    /// <c>ResourceType_GetAllWithUsage</c>. Backs the management screen; the counts are what make
    /// "delete is blocked because 12 resources use this" possible without a round trip per row.
    /// </summary>
    public class ResourceTypeUsage
    {
        public int ResourceTypeId { get; set; }

        public string ResourceTypeUid { get; set; } = string.Empty;

        public string TypeName { get; set; } = string.Empty;

        public string? ShortCode { get; set; }

        public string? IconKey { get; set; }

        public bool AllowCustomTags { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int ResourceCount { get; set; }

        public int EntryPointTagCount { get; set; }
    }
}
