namespace ResourceMapper.Common.Shared.Editor
{
    /// <summary>
    /// A resource type's per-type entry-point tag template — drives the Tags tab's pre-seeded
    /// rows and default primary on a new resource of that type.
    /// </summary>
    public class ResourceTypeEntryPointModel
    {
        public int ResourceTypeId { get; set; }
        public int TagDefinitionId { get; set; }
        public bool IsDefaultPrimary { get; set; }
        public string? RequirementLevel { get; set; }
    }
}
