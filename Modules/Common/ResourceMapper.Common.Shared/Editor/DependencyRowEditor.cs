namespace ResourceMapper.Common.Shared.Editor
{
    /// <summary>
    /// One editable relationship row on either the Dependencies (out-edge) or Dependent On
    /// (in-edge) tab.
    /// </summary>
    public class DependencyRowEditor
    {
        public int? RelationshipId { get; set; }
        public string Direction { get; set; } = string.Empty;
        public string OtherResourceUid { get; set; } = string.Empty;
        public string OtherResourceName { get; set; } = string.Empty;
        public string OtherResourceType { get; set; } = string.Empty;
        public string? OtherDomain { get; set; }
        public bool IsNew { get; set; }
        public bool IsRemoved { get; set; }
        public List<EditorMessage> RowMessages { get; set; } = new();
    }
}
