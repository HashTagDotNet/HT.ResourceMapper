using System.Collections.Generic;

namespace ResourceMapper.Common.Shared.Explorer
{
    /// <summary>
    /// The center resource for an explorer read plus its one-hop neighbours. Returned by
    /// IExplorerService.GetNodeAsync for both the initial seed load and each node expansion.
    /// <see cref="Edges"/> carries every relationship inside the on-canvas set — including
    /// neighbour-to-neighbour ones — and is the canvas's single source of truth for lines.
    /// </summary>
    public class ExplorerNodeModel
    {
        public string ResourceUid { get; set; } = string.Empty;
        public string ResourceKey { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string? ShortCode { get; set; }
        public string? IconKey { get; set; }
        public string? Domain { get; set; }
        public string? PrimaryUrl { get; set; }
        public List<ExplorerNeighborModel> Neighbors { get; set; } = new();
        public List<ExplorerEdgeModel> Edges { get; set; } = new();
    }
}
