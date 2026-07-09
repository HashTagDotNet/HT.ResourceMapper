using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResourceMapper.Common.Server.Resources.Models
{
    [Table("TagDefinition", Schema = "HTResourceMapper")]
    public class TagDefinition
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TagDefinitionId { get; set; }

        [Required]
        [StringLength(40)]
        public string TagDefinitionUid { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string TagDefinitionKey { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DisplayName { get; set; }

        [Required]
        public int TagContentTypeId { get; set; }

        [Required]
        public bool AllowCustomValue { get; set; } = true;

        [Required]
        public bool IsMultiValued { get; set; } = true;

        [StringLength(2000)]
        public string? AllowedValues { get; set; }

        [Required]
        [StringLength(20)]
        public string RequirementLevel { get; set; } = "Optional";

        [Required]
        public bool IsDomainTag { get; set; } = false;

        [Required]
        public bool IsSystemTag { get; set; } = false;

        [Required]
        public int DisplayOrder { get; set; } = 1000;

        [Required]
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

        // Navigation properties
        [ForeignKey("TagContentTypeId")]
        public virtual TagContentType TagContentType { get; set; } = null!;

        public virtual ICollection<ResourceTag> ResourceTags { get; set; } = new List<ResourceTag>();
    }
}
