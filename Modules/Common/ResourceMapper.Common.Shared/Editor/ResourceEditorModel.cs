namespace ResourceMapper.Common.Shared.Editor
{
    public class ResourceEditorModel
    {
        public string ResourceUid { get; set; }
        public EditorFieldModel Code { get; set; } = new EditorFieldModel { FieldName = "Code" };


    }
}
