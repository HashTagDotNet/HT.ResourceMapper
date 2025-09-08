using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResourceMapper.Common.Server.Resources.Models
{
    [Table("ResourceTag", Schema = "HTResourceMapper")]
    public class ResourceTag
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ResourceTagId { get; set; }

        [Required]
        public int ResourceId { get; set; }

        [Required]
        public int TagDefinitionId { get; set; }

        [Required]
        [StringLength(2000)]
        public string TagValue { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

        // Navigation properties
        [ForeignKey("ResourceId")]
        public virtual Resource Resource { get; set; } = null!;

        [ForeignKey("TagDefinitionId")]
        public virtual TagDefinition TagDefinition { get; set; } = null!;
    }
}