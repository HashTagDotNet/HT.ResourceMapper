namespace ResourceMapper.Common.Server.Resources.Models
{
    /// <summary>
    /// Row shape of TagDefinition_GetAllWithUsage: the stored definition plus the three references that
    /// exist to it. Server-side model, mapped to the shared TagDefinitionUsageModel by the service —
    /// the same split TagDefinition / TagDefinitionModel already uses.
    /// </summary>
    public class TagDefinitionUsage
    {
        public TagDefinition Definition { get; set; } = new();

        /// <summary>ResourceTag rows: resources carrying a value for this tag.</summary>
        public int ResourceCount { get; set; }

        /// <summary>ResourceTypeTag rows: resource types whose entry-point template lists it.</summary>
        public int TypeTemplateCount { get; set; }

        /// <summary>Resource.PrimaryTagDefinitionId: resources whose click-through link IS this tag.
        /// That FK is NO ACTION, so this count is what stops a delete becoming a constraint error.</summary>
        public int PrimaryForCount { get; set; }
    }
}
