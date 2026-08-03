namespace ResourceMapper.Common.Shared.ResourceTypes.Contracts
{
    /// <summary>
    /// Create/update input for a resource type. <see cref="ResourceTypeId"/> 0 means create.
    /// </summary>
    public class SaveResourceTypeRequest
    {
        public int ResourceTypeId { get; set; }

        public string TypeName { get; set; } = string.Empty;

        public string? ShortCode { get; set; }

        public string? IconKey { get; set; }

        public bool AllowCustomTags { get; set; } = true;
    }

    /// <summary>
    /// Result of a delete attempt. When <see cref="Deleted"/> is false the response also carries a
    /// FailedPrecondition error; <see cref="DependentCount"/> is the number of resources still
    /// using the type, so callers can name it rather than showing a raw FK violation.
    /// </summary>
    public class DeleteResourceTypeResponse
    {
        public bool Deleted { get; set; }

        public int DependentCount { get; set; }
    }
}
