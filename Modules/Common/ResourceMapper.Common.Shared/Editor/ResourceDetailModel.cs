namespace ResourceMapper.Common.Shared.Editor
{
    /// <summary>
    /// Details/view read shape for a single resource: identity, applied tags, and relationships.
    /// Domain/PrimaryLinkUrl/IdentityDisplay are computed during projection; Domain stays null
    /// until the domain-tag join lands.
    /// </summary>
    public class ResourceDetailModel
    {
        public int ResourceId { get; set; }
        public string ResourceUid { get; set; } = string.Empty;
        public string ResourceKey { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ResourceTypeId { get; set; }
        public string ResourceTypeName { get; set; } = string.Empty;
        public string? Domain { get; set; }
        public int? PrimaryTagDefinitionId { get; set; }
        public string? PrimaryLinkUrl { get; set; }
        public string? IdentityDisplay { get; set; }
        public List<ResourceTagModel> Tags { get; set; } = new();
        public List<ResourceRelationshipModel> Relationships { get; set; } = new();
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
