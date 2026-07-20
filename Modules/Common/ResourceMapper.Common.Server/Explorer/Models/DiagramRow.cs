namespace ResourceMapper.Common.Server.Explorer.Models
{
    /// <summary>Full diagram row (Diagram_GetByShareId).</summary>
    public class DiagramRow
    {
        public string DiagramUid { get; set; } = string.Empty;
        public string ShareId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SeedResourceUid { get; set; } = string.Empty;
        public string DisplayPreset { get; set; } = string.Empty;
        public string DiagramJson { get; set; } = string.Empty;
        public string? UpdatedOnUtc { get; set; }
    }
}
