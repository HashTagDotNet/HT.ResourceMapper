namespace ResourceMapper.Common.Server.Explorer.Models
{
    /// <summary>Result row from Diagram_Upsert.</summary>
    public class DiagramUpsertResult
    {
        public string Result { get; set; } = string.Empty;   // 'created' | 'updated'
        public string DiagramUid { get; set; } = string.Empty;
        public string ShareId { get; set; } = string.Empty;
    }
}
