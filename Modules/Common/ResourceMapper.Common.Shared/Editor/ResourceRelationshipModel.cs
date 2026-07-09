namespace ResourceMapper.Common.Shared.Editor
{
    /// <summary>
    /// One relationship edge touching a resource, from either direction. "DependsOn" = an
    /// out-edge (this resource depends on the other); "DependentOn" = an in-edge (the other
    /// resource depends on this one).
    /// </summary>
    public class ResourceRelationshipModel
    {
        public int RelationshipId { get; set; }
        public string Direction { get; set; } = string.Empty;
        public string OtherResourceUid { get; set; } = string.Empty;
        public string OtherResourceKey { get; set; } = string.Empty;
        public string OtherResourceName { get; set; } = string.Empty;
        public string OtherResourceType { get; set; } = string.Empty;
        public string? OtherDomain { get; set; }
    }
}
