namespace ResourceMapper.Common.Shared.Explorer
{
    /// <summary>
    /// A one-hop neighbour of an explorer node. Direction is 'DependsOn' (this node depends on
    /// the neighbour) or 'DependentOn' (the neighbour depends on this node).
    /// </summary>
    public class ExplorerNeighborModel
    {
        public string Direction { get; set; } = string.Empty;
        public string ResourceUid { get; set; } = string.Empty;
        public string ResourceKey { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string? ShortCode { get; set; }
        public string? IconKey { get; set; }
        public string? Domain { get; set; }
        public string? PrimaryUrl { get; set; }
    }
}
