namespace ResourceMapper.Common.Shared.Editor.Contracts
{
    /// <summary>Inline tag-definition create input — no Domain/System flags (setup-only).</summary>
    public class CreateTagDefinitionRequest
    {
        public string TagDefinitionKey { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public bool AllowCustomValue { get; set; }
        public bool IsMultiValued { get; set; }
        public List<string>? AllowedValues { get; set; }
        public string RequirementLevel { get; set; } = "Optional";
        public int DisplayOrder { get; set; } = 1000;
    }
}
