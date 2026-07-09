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
        public int TagDefinitionId { get; set; }

        [Required]
        public bool IsDefaultPrimary { get; set; } = false;

        [StringLength(20)]
        public string? RequirementLevel { get; set; }

        // Navigation properties
        [ForeignKey("ResourceTypeId")]
        public virtual ResourceType ResourceType { get; set; } = null!;

        [ForeignKey("TagDefinitionId")]
        public virtual TagDefinition TagDefinition { get; set; } = null!;
    }
}
