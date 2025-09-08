using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResourceMapper.Common.Server.Resources.Models
{
    [Table("ResourceTypeTag", Schema = "HTResourceMapper")]
    public class ResourceTypeTag
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ResourceTypeTagId { get; set; }

        [Required]
        public int ResourceTypeId { get; set; }

        [Required]
        [StringLength(40)]
        public string Tag { get; set; } = string.Empty;

        [Required]
        public int TagValueTypeId { get; set; }

        // Navigation properties
        [ForeignKey("ResourceTypeId")]
        public virtual ResourceType ResourceType { get; set; } = null!;

        [ForeignKey("TagValueTypeId")]
        public virtual TagValueType TagValueType { get; set; } = null!;
    }
}