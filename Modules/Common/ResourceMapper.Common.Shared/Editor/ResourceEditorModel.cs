namespace ResourceMapper.Common.Shared.Editor
{
    public class ResourceEditorModel
    {
        public string ResourceUid { get; set; }

        public string ResourceType { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Notes { get; set; }

        public List<KeyValuePair<string,string>> PossibleTags { get; set; }
        
        // Properties for new tag fields
        public string NewTagName { get; set; } = string.Empty;
        public string NewTagValue { get; set; } = string.Empty;
    }
}
