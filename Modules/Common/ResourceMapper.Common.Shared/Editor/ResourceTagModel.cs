namespace ResourceMapper.Common.Shared.Editor
{
    /// <summary>
    /// An applied tag value on a resource. Carries the definition's key/contentType/displayName
    /// inline so the client can render without a separate dictionary lookup.
    /// </summary>
    public class ResourceTagModel
    {
        public int TagDefinitionId { get; set; }
        public string TagDefinitionKey { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string? Value { get; set; }
        public bool IsPrimary { get; set; }
    }
}
