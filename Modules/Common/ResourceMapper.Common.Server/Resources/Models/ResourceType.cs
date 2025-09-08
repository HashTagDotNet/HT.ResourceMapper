using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResourceMapper.Common.Server.Resources.Models
{
    [Table("ResourceType", Schema = "HTResourceMapper")]
    public class ResourceType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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

        // Navigation properties
        public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();

        public virtual ICollection<ResourceTypeTag> ResourceTypeTags { get; set; } = new List<ResourceTypeTag>();
    }
}