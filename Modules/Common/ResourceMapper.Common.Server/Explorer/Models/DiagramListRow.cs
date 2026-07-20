namespace ResourceMapper.Common.Server.Explorer.Models
{
    /// <summary>Metadata row for Open-Recent (Diagram_ListForClient); no DiagramJson.</summary>
    public class DiagramListRow
    {
        public string DiagramUid { get; set; } = string.Empty;
        public string ShareId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SeedResourceUid { get; set; } = string.Empty;
        public string? UpdatedOnUtc { get; set; }
    }
}
