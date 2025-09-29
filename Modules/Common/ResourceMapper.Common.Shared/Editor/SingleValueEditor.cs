namespace ResourceMapper.Common.Shared.Editor
{
    public class SingleValueEditor
    {
        public string FieldName { get; set; }
        public string OriginalValue { get; set; }
        public string EditedValue { get; set; }
        public bool IsChanged => !string.Equals(OriginalValue, EditedValue, StringComparison.Ordinal);

        public List<EditorMessage> Messages { get; set; } = new List<EditorMessage>();
        public EditorFieldLevel Level
        {
            get
            {
                if (Messages.Any(m => m.Level == EditorFieldLevel.Error))
                {
                    return EditorFieldLevel.Error;
                }
                else if (Messages.Any(m => m.Level == EditorFieldLevel.Warning))
                {
                    return EditorFieldLevel.Warning;
                }
                else
                {
                    return EditorFieldLevel.Info;
                }
            }
        }

    }
}
