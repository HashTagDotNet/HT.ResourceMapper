namespace ResourceMapper.Common.Shared.Tags.Contracts
{
    /// <summary>
    /// Edits an existing tag definition. The KEY is the identity here and is deliberately not editable:
    /// it is the stable identifier import and export documents name, so changing it would silently stop
    /// those documents matching. Everything a user can see and reason about is editable instead.
    /// </summary>
    public class UpdateTagDefinitionRequest
    {
        /// <summary>Identifies the definition to edit. Not itself changeable.</summary>
        public string TagDefinitionKey { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        /// <summary>"Text" | "Link".</summary>
        public string ContentType { get; set; } = "Text";

        /// <summary>False = the value must come from AllowedValues; true = free text. One decision, two
        /// stored fields (PL-23) — this is the same pairing the create dialog exposes as "Values".</summary>
        public bool AllowCustomValue { get; set; }

        public bool IsMultiValued { get; set; }

        /// <summary>The controlled vocabulary, or null for free text.</summary>
        public List<string>? AllowedValues { get; set; }

        /// <summary>"Error" | "Suggested" | "Optional".</summary>
        public string RequirementLevel { get; set; } = "Optional";

        public int DisplayOrder { get; set; } = 1000;
    }

    /// <summary>
    /// Outcome of a delete attempt. A refusal is a normal result carrying the reason, not an error,
    /// because the screen turns each into a specific sentence naming what is in the way.
    /// </summary>
    public class DeleteTagDefinitionResponse
    {
        public bool Deleted { get; set; }

        /// <summary>True when refused because the tag is system-managed.</summary>
        public bool IsSystemManaged { get; set; }

        public int ResourceCount { get; set; }

        public int TypeTemplateCount { get; set; }

        public int PrimaryForCount { get; set; }

        /// <summary>Why it was refused, ready to show. Null when the delete succeeded.</summary>
        public string? Message { get; set; }
    }
}
