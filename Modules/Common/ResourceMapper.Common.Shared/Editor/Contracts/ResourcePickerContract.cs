namespace ResourceMapper.Common.Shared.Editor.Contracts
{
    /// <summary>Same-domain resource search backing the Dependencies / Dependent On tabs' picker.</summary>
    public class ResourcePickerRequest
    {
        public string? SearchFor { get; set; }
        public string? Domain { get; set; }
        public int? ResourceTypeId { get; set; }

        /// <summary>Excludes the resource being edited (no self-loops).</summary>
        public string? ExcludeResourceUid { get; set; }

        public int Skip { get; set; }
        public int Take { get; set; }
    }

    public class ResourcePickerItem
    {
        public string ResourceUid { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceKey { get; set; } = string.Empty;
        public string ResourceTypeName { get; set; } = string.Empty;
        public string? Domain { get; set; }
    }

    public class ResourcePickerResponse
    {
        public List<ResourcePickerItem> Items { get; set; } = new();
        public int TotalItems { get; set; }
    }
}
