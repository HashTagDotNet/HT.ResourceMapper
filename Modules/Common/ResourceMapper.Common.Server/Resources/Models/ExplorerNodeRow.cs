namespace ResourceMapper.Common.Server.Resources.Models
{
    /// <summary>
    /// One row from Resource_GetForExplorer. Direction is 'Self' for the center resource, or
    /// 'DependsOn' / 'DependentOn' for a one-hop neighbour. Domain and PrimaryUrl may be null.
    /// </summary>
    public class ExplorerNodeRow
    {
        public string Direction { get; set; } = string.Empty;
        public string ResourceUid { get; set; } = string.Empty;
        public string ResourceKey { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string? Domain { get; set; }
        public string? PrimaryUrl { get; set; }
    }
}
