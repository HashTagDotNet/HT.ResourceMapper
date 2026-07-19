using System.Collections.Generic;

namespace ResourceMapper.Common.Shared.Explorer
{
    /// <summary>
    /// The center resource for an explorer read plus its one-hop neighbours. Returned by
    /// IExplorerService.GetNodeAsync for both the initial seed load and each node expansion.
    /// </summary>
    public class ExplorerNodeModel
    {
        public string ResourceUid { get; set; } = string.Empty;
        public string ResourceKey { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string? Domain { get; set; }
        public string? PrimaryUrl { get; set; }
        public List<ExplorerNeighborModel> Neighbors { get; set; } = new();
    }
}
