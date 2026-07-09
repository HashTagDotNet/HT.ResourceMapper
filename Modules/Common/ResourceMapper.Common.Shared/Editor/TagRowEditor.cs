namespace ResourceMapper.Common.Shared.Editor
{
    /// <summary>
    /// One editable applied-tag row on the Tags tab. <see cref="Values"/> holds one editor for a
    /// single-valued tag or N editors (discrete chips) for a multi-valued tag — never a joined
    /// string.
    /// </summary>
    public class TagRowEditor
    {
        public TagDefinitionModel Definition { get; set; } = new();
        public int TagDefinitionId { get; set; }
        public List<SingleValueEditor> Values { get; set; } = new();
        public bool IsPrimary { get; set; }
        public bool IsPreSeeded { get; set; }
        public bool IsRemoved { get; set; }
        public List<EditorMessage> RowMessages { get; set; } = new();

        public EditorFieldLevel RowLevel
        {
            get
            {
                if (RowMessages.Any(m => m.Level == EditorFieldLevel.Error))
                    return EditorFieldLevel.Error;
                if (RowMessages.Any(m => m.Level == EditorFieldLevel.Warning))
                    return EditorFieldLevel.Warning;
                return EditorFieldLevel.Info;
            }
        }

        public bool IsLink => Definition.ContentType == "Link";
        public bool IsControlledVocab => !Definition.AllowCustomValue && Definition.AllowedValues is { Count: > 0 };
        public bool CanBePrimary => IsLink && !Definition.IsMultiValued;
        public bool IsRequired => Definition.RequirementLevel == "Error";
        public bool IsSuggested => Definition.RequirementLevel == "Suggested";
        public bool IsEditable => !Definition.IsSystemTag;
    }
}
