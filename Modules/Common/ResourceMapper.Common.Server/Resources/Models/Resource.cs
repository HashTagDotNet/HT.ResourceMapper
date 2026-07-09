using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMapper.Common.Server.Resources.Models
{
    [Table("Resource", Schema = "HTResourceMapper")]
    public class Resource
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ResourceId { get; set; }

        [Required]
        [StringLength(40)]
        public string ResourceUid { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string ResourceKey { get; set; } = string.Empty;

        [Required]
        public int ResourceTypeId { get; set; }

        [Required]
        [StringLength(250)]
        public string ResourceName { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        public int? PrimaryTagDefinitionId { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

        // Navigation properties
        [ForeignKey("ResourceTypeId")]
        public virtual ResourceType? ResourceType { get; set; }

        [ForeignKey("PrimaryTagDefinitionId")]
        public virtual TagDefinition? PrimaryTagDefinition { get; set; }

        public virtual ICollection<ResourceTag> ResourceTags { get; set; } = new List<ResourceTag>();

        public virtual ICollection<ResourceRelationship> Dependencies { get; set; } = new List<ResourceRelationship>();

        public virtual ICollection<ResourceRelationship> Dependents { get; set; } = new List<ResourceRelationship>();
    }
}
