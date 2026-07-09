namespace ResourceMapper.Common.Shared.Editor.Contracts
{
    /// <summary>Early uniqueness check backing the General tab's identity preview.</summary>
    public class ResourceUniquenessRequest
    {
        public string? Domain { get; set; }
        public int ResourceTypeId { get; set; }
        public string ResourceKey { get; set; } = string.Empty;

        /// <summary>Excludes the resource being edited from the collision check.</summary>
        public string? ExcludeResourceUid { get; set; }
    }

    public class ResourceUniquenessResponse
    {
        public bool IsUnique { get; set; }
        public string? Message { get; set; }
        public string? IdentityDisplay { get; set; }
    }
}
