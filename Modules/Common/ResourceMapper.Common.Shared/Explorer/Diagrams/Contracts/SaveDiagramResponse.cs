namespace ResourceMapper.Common.Shared.Explorer.Diagrams.Contracts
{
    public class SaveDiagramResponse
    {
        public string DiagramUid { get; set; } = string.Empty;
        public string ShareId { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;   // 'created' | 'updated'
    }
}
