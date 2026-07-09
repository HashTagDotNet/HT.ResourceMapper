namespace ResourceMapper.Common.Shared.Editor
{
    /// <summary>
    /// The tag "render rulebook" — shared metadata driving how a tag is displayed, validated,
    /// and edited, independent of any single resource's applied value.
    /// </summary>
    public class TagDefinitionModel
    {
        public int TagDefinitionId { get; set; }
        public string TagDefinitionUid { get; set; } = string.Empty;
        public string TagDefinitionKey { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public int TagContentTypeId { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string RequirementLevel { get; set; } = "Optional";
        public bool IsMultiValued { get; set; }
        public bool AllowCustomValue { get; set; }
        public List<string>? AllowedValues { get; set; }
        public bool IsDomainTag { get; set; }
        public bool IsSystemTag { get; set; }
        public int DisplayOrder { get; set; }
    }
}
