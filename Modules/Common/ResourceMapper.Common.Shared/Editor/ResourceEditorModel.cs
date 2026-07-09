namespace ResourceMapper.Common.Shared.Editor
{
    public class ResourceEditorModel
    {
        public string ResourceUid { get; set; } = "";

        public SingleValueEditor ResourceType { get; set; } = new();
        public SingleValueEditor Domain { get; set; } = new();
        public SingleValueEditor Name { get; set; } = new();
        public SingleValueEditor Key { get; set; } = new();
        public SingleValueEditor Description { get; set; } = new();

        public IdentityPreview IdentityPreview { get; set; } = new();
        public string Mode { get; set; } = "Create";
        public bool IsPersisted { get; set; }
        public int? ResourceTypeId { get; set; }
        public int? PrimaryTagDefinitionId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }

        public List<TagRowEditor> Tags { get; set; } = new();
        public List<DependencyRowEditor> DependsOn { get; set; } = new();
        public List<DependencyRowEditor> DependentOn { get; set; } = new();

        public bool IsDirty =>
            ResourceType.IsChanged || Domain.IsChanged || Name.IsChanged || Key.IsChanged || Description.IsChanged
            || Tags.Any(t => t.IsRemoved || t.Values.Any(v => v.IsChanged))
            || DependsOn.Any(d => d.IsNew || d.IsRemoved)
            || DependentOn.Any(d => d.IsNew || d.IsRemoved);
    }
}
