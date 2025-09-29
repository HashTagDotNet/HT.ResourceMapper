using System.ComponentModel.DataAnnotations;

namespace ResourceMapper.Common.Server.Resources.Models
{
    public class ResourceType
    {
    
        public int ResourceTypeId { get; set; }

        [Required]
        [StringLength(40)]
        public string ResourceTypeUid { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string TypeName { get; set; } = string.Empty;

        [Required]
        public bool AllowCustomTags { get; set; } = true;

        [Required]
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

    }
}