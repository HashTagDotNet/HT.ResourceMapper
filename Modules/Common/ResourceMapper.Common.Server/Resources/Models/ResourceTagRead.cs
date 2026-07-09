namespace ResourceMapper.Common.Server.Resources.Models
{
    /// <summary>
    /// Read shape for an applied tag value, from ResourceTag_GetForResource. Distinct from the
    /// EF ResourceTag entity; projected to ResourceTagModel in the service.
    /// </summary>
    public class ResourceTagRead
    {
        public int TagDefinitionId { get; set; }
        public string TagDefinitionKey { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string? TagValue { get; set; }
        public bool IsSystemTag { get; set; }
        public bool IsMultiValued { get; set; }
        public bool IsPrimary { get; set; }
    }
}
