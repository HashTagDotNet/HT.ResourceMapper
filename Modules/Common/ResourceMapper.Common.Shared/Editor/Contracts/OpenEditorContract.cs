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
        public List<KeyValuePair<string,string>> ResourceTypes { get; set; }
    }
}
