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

        /// <summary>
        /// The type's entry-point template, REPLACED wholesale — an absent row means removed, and an
        /// empty list clears the template. Null means "leave the template alone", which is how a
        /// caller that does not edit templates (e.g. the editor's inline create-type) opts out.
        /// </summary>
        public List<EntryPointTagTemplateModel>? EntryPointTags { get; set; }
    }

    /// <summary>
    /// One row of a resource type's entry-point template: a tag the type prompts for, whether it is
    /// the type's default primary link, and an optional per-type override of the definition's own
    /// requirement level.
    /// </summary>
    public class EntryPointTagTemplateModel
    {
        public int TagDefinitionId { get; set; }

        /// <summary>At most one per type — seeds a new resource's PrimaryTagDefinitionId.</summary>
        public bool IsDefaultPrimary { get; set; }

        /// <summary>"Error" | "Suggested" | "Optional", or null to inherit the definition's.</summary>
        public string? RequirementLevel { get; set; }
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
