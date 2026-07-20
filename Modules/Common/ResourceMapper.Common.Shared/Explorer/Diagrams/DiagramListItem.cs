namespace ResourceMapper.Common.Shared.Explorer.Diagrams
{
    /// <summary>Open-Recent entry (metadata only).</summary>
    public class DiagramListItem
    {
        public string DiagramUid { get; set; } = string.Empty;
        public string ShareId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SeedResourceUid { get; set; } = string.Empty;
        public string? UpdatedOnUtc { get; set; }
    }
}
