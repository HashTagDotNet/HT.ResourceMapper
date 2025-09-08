namespace ResourceMapper.Common.Shared.HomePage
{
    public class ResourceGridItemModel
    {
        public string ResourceUid { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string? ResourceType { get; set; }
        public string Description { get; set; } = string.Empty;
        public string RelativeLastUpdatedOn { get; set; } = string.Empty;
        public DateTime LastUpdatedOn { get; set; }
        
        public List<ResourceGridTagModel> Tags { get; set; } = new List<ResourceGridTagModel>();
        public int? TotalTags { get; set; }
    }
}
