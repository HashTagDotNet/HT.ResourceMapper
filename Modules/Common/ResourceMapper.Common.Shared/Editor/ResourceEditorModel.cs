namespace ResourceMapper.Common.Shared.Editor
{
    public class ResourceEditorModel
    {
        public string ResourceUid { get; set; } = "";

        public SingleValueEditor ResourceType { get; set; } = new();
        public SingleValueEditor Code { get; set; } = new();
        public SingleValueEditor Name { get; set; } = new();
        public SingleValueEditor Notes { get; set; } = new();

        public string NewTagName { get; set; } = "";
        public string NewTagValue { get; set; } = "";
    }
}
