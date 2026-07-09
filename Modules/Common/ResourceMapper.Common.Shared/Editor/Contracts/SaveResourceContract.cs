namespace ResourceMapper.Common.Shared.Editor.Contracts
{
    /// <summary>
    /// Tag/edge lists are forward-looking: slice #4 persists General-tab fields + primary link
    /// only. Applied-tag write-on-save lands in #7; relationship save-reconciliation lands in #8.
    /// </summary>
    public class SaveResourceRequest
    {
        public string Mode { get; set; } = string.Empty;
        public string ResourceUid { get; set; } = string.Empty;
        public int ResourceTypeId { get; set; }
        public string? Domain { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceKey { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? PrimaryTagDefinitionId { get; set; }

        public List<SaveTagValue> Tags { get; set; } = new();
        public List<string> DependsOnUids { get; set; } = new();
        public List<string> DependentOnUids { get; set; } = new();
    }

    public class SaveTagValue
    {
        public int TagDefinitionId { get; set; }
        public string TagDefinitionKey { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    public class SaveResourceResponse
    {
        public string ResourceUid { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ResourceDetailModel? Saved { get; set; }
    }
}
