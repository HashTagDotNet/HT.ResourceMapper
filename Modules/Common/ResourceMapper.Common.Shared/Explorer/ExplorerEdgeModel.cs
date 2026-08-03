namespace ResourceMapper.Common.Shared.Explorer
{
    /// <summary>
    /// A relationship between two resources that are both on the explorer canvas. Direction is
    /// implicit: FromUid depends on ToUid. Unlike <see cref="ExplorerNeighborModel"/> these are not
    /// limited to edges incident to the center resource — they include neighbour-to-neighbour
    /// relationships, which would otherwise be drawn as "unrelated" despite both nodes being visible.
    /// </summary>
    public class ExplorerEdgeModel
    {
        public string FromUid { get; set; } = string.Empty;
        public string ToUid { get; set; } = string.Empty;
    }
}
