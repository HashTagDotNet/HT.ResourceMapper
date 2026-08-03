namespace ResourceMapper.Common.Shared.ResourceTypes
{
    /// <summary>
    /// A resource type as the management screen sees it: the stored row plus the two dependency
    /// counts that decide whether it can be deleted and whether it has an entry-point template.
    /// </summary>
    public class ResourceTypeModel
    {
        public int ResourceTypeId { get; set; }

        public string ResourceTypeUid { get; set; } = string.Empty;

        public string TypeName { get; set; } = string.Empty;

        /// <summary>Up to 10 chars, printed inside the explorer node. Blank falls back to the
        /// first three letters of the type name, so a bad value degrades the graph visibly.</summary>
        public string? ShortCode { get; set; }

        /// <summary>Opaque key the explorer canvas maps to a glyph + node colour. An unknown key
        /// renders a neutral dot on the default blue.</summary>
        public string? IconKey { get; set; }

        public bool AllowCustomTags { get; set; } = true;

        public DateTime CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        /// <summary>Resources currently pointing at this type. Non-zero blocks delete.</summary>
        public int ResourceCount { get; set; }

        /// <summary>ResourceTypeTag rows owned by this type (the editor's tag pre-seed template).</summary>
        public int EntryPointTagCount { get; set; }
    }
}
