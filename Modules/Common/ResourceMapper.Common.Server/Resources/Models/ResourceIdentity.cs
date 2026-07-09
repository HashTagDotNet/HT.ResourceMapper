namespace ResourceMapper.Common.Server.Resources.Models
{
    /// <summary>
    /// The full (Domain + Type + Key) identity tuple for one resource, from Resource_GetAllKeys.
    /// Domain is null when no domain tag is applied (or none is defined at all).
    /// </summary>
    public class ResourceIdentity
    {
        public int ResourceId { get; set; }
        public string? Domain { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string ResourceKey { get; set; } = string.Empty;
    }
}
