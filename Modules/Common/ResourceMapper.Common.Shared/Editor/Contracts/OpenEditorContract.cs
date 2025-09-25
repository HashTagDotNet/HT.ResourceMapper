namespace ResourceMapper.Common.Shared.Editor.Contracts
{
    public class OpenEditorRequest
    {
        public string Mode { get; set; }
        public string ResourceUid { get; set; }
    }
    public class OpenEditorResponse
    {
        public ResourceEditorModel EditorModel { get; set; }
    }
}
