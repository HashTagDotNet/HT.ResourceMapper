namespace ResourceMapper.Common.Shared.Editor.Contracts
{
    public class OpenEditorRequest
    {
        public string Mode { get; set; } = string.Empty;
        public string? ResourceUid { get; set; }
    }
    public class OpenEditorResponse
    {
        public ResourceEditorModel EditorModel { get; set; } = new();
        public List<ResourceTypeOption> ResourceTypes { get; set; } = new();

        /// <summary>Full tag dictionary — drives "Add tag" search + render rules.</summary>
        public List<TagDefinitionModel> TagDictionary { get; set; } = new();

        /// <summary>Per-type entry-point templates — drives Tags-tab pre-seeding and client-side re-seed on type change.</summary>
        public List<ResourceTypeEntryPointModel> EntryPointTemplates { get; set; } = new();

        /// <summary>Allowed Domain values (e.g. prod/non-prod) for the Domain select.</summary>
        public List<string> DomainAllowedValues { get; set; } = new();
    }
}
