namespace ResourceMapper.Common.Shared.Editor
{
    /// <summary>
    /// Read-only identity roll-up ("domain / type / key") plus the early-uniqueness result
    /// surfaced on the General tab.
    /// </summary>
    public class IdentityPreview
    {
        public string? Domain { get; set; }
        public string? ResourceType { get; set; }
        public string? Key { get; set; }

        public string Display => string.Join(" / ",
            new[] { Domain, ResourceType, Key }.Where(p => !string.IsNullOrWhiteSpace(p)));

        public bool? IsUnique { get; set; }
        public string? UniquenessMessage { get; set; }
    }
}
