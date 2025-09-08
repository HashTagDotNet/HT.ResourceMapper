using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResourceMapper.Common.Server.Resources.Models
{
    [Table("TagValueType", Schema = "HTResourceMapper")]
    public class TagValueType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int TagValueTypeId { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public bool IsRequired { get; set; } = false;

        // Navigation properties
        public virtual ICollection<ResourceTypeTag> ResourceTypeTags { get; set; } = new List<ResourceTypeTag>();
    }
}