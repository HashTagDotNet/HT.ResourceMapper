namespace ResourceMapper.Common.Shared.Explorer.Diagrams
{
    /// <summary>A full saved diagram returned to the client (by share token).</summary>
    public class DiagramModel
    {
        public string DiagramUid { get; set; } = string.Empty;
        public string ShareId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SeedResourceUid { get; set; } = string.Empty;
        public string DisplayPreset { get; set; } = string.Empty;
        public string DiagramJson { get; set; } = string.Empty;
        public string? UpdatedOnUtc { get; set; }
        public bool IsOwner { get; set; }
    }
}
