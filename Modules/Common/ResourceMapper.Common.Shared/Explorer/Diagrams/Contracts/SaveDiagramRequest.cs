namespace ResourceMapper.Common.Shared.Explorer.Diagrams.Contracts
{
    /// <summary>Save (create when DiagramUid is null/empty, else update) a diagram.</summary>
    public class SaveDiagramRequest
    {
        public string? DiagramUid { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SeedResourceUid { get; set; } = string.Empty;
        public string DisplayPreset { get; set; } = "nameType";
        public string DiagramJson { get; set; } = string.Empty;
    }
}
