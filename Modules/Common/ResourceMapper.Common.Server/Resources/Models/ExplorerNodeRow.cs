namespace ResourceMapper.Common.Server.Resources.Models
{
    /// <summary>
    /// One row from Resource_GetForExplorer. Direction is 'Self' for the center resource,
    /// 'DependsOn' / 'DependentOn' for a one-hop neighbour, or 'Edge' for a relationship whose
    /// both endpoints are on the canvas. On 'Edge' rows the node columns are blank and
    /// FromResourceUid / ToResourceUid carry the endpoints; on node rows they are null.
    /// Domain and PrimaryUrl may be null.
    /// </summary>
    public class ExplorerNodeRow
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
        public string? FromResourceUid { get; set; }
        public string? ToResourceUid { get; set; }
    }
}
