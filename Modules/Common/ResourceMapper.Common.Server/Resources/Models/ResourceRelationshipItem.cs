namespace ResourceMapper.Common.Server.Resources.Models
{
    /// <summary>
    /// One row of a resource's relationship edges, from either direction (from
    /// ResourceRelationship_GetForResource). "DependsOn" = an out-edge (this resource depends
    /// on the other); "DependentOn" = an in-edge (the other resource depends on this one).
    /// </summary>
    public class ResourceRelationshipItem
    {
        public int RelationshipId { get; set; }

        public string Direction { get; set; } = string.Empty;

        public int OtherResourceId { get; set; }
        public string OtherResourceUid { get; set; } = string.Empty;
        public string OtherResourceKey { get; set; } = string.Empty;
        public string OtherResourceName { get; set; } = string.Empty;
        public string OtherResourceType { get; set; } = string.Empty;
        public string? OtherDomain { get; set; }
    }
}
