namespace ResourceMapper.Common.Shared.Editor
{
    /// <summary>
    /// A resource-type picklist entry for the editor's Type select. Carries the int id that
    /// SaveResourceRequest/ResourceUniquenessRequest require (the uid alone is not enough).
    /// </summary>
    public class ResourceTypeOption
    {
        public int ResourceTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string ResourceTypeUid { get; set; } = string.Empty;
    }
}
