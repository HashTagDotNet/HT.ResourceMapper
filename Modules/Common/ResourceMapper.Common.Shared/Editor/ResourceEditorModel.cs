namespace ResourceMapper.Common.Shared.Editor
{
    public class ResourceEditorModel
    {
        public string ResourceUid { get; set; }

        public SingleValueEditor ResourceType { get; set; }

        public SingleValueEditor Code { get; set; }
        public SingleValueEditor Name { get; set; }
        public SingleValueEditor Notes { get; set; }

}
