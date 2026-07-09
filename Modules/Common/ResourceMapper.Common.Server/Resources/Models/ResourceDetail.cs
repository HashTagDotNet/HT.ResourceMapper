namespace ResourceMapper.Common.Server.Resources.Models
{
    /// <summary>
    /// Single-resource read shape, from Resource_GetByResourceUid.
    /// </summary>
    public class ResourceDetail
    {
        public int ResourceId { get; set; }
        public string ResourceUid { get; set; } = string.Empty;
        public string ResourceKey { get; set; } = string.Empty;
        public int ResourceTypeId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? PrimaryTagDefinitionId { get; set; }
        public string? Domain { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
