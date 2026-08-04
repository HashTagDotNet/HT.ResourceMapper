using ResourceMapper.Common.Shared.Editor;

namespace ResourceMapper.Common.Shared.Tags
{
    /// <summary>
    /// A tag definition as the management screen sees it: the definition itself plus the three
    /// references that exist to it. Those three are not decoration — they are exactly what blocks a
    /// delete, and exactly what a shape edit puts at risk, so the screen needs all three to explain
    /// itself rather than just saying "cannot delete".
    /// </summary>
    public class TagDefinitionUsageModel
    {
        /// <summary>The stored definition. Composed rather than flattened so the edit dialog can be
        /// handed the same TagDefinitionModel it already knows how to render.</summary>
        public TagDefinitionModel Definition { get; set; } = new();

        /// <summary>Resources carrying a value for this tag (ResourceTag rows).</summary>
        public int ResourceCount { get; set; }

        /// <summary>Resource types whose entry-point template lists this tag (ResourceTypeTag rows).</summary>
        public int TypeTemplateCount { get; set; }

        /// <summary>
        /// Resources whose click-through link IS this tag (Resource.PrimaryTagDefinitionId). That FK is
        /// NO ACTION, so this is the count that would turn a delete into a raw constraint violation.
        /// </summary>
        public int PrimaryForCount { get; set; }

        /// <summary>True when nothing references the tag, i.e. it is safe to delete.</summary>
        public bool IsUnused => ResourceCount == 0 && TypeTemplateCount == 0 && PrimaryForCount == 0;

        /// <summary>System-managed (the domain tag is one): shape and existence are owned by deployment,
        /// so the screen offers neither edit nor delete.</summary>
        public bool IsSystemManaged => Definition.IsSystemTag || Definition.IsDomainTag;
    }
}
