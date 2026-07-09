using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResourceMapper.Common.Server.Resources.Models
{
    [Table("ResourceRelationship", Schema = "HTResourceMapper")]
    public class ResourceRelationship
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RelationshipId { get; set; }

        [Required]
        public int FromResourceId { get; set; }

        [Required]
        public int ToResourceId { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

        // Navigation properties
        [ForeignKey("FromResourceId")]
        public virtual Resource FromResource { get; set; } = null!;

        [ForeignKey("ToResourceId")]
        public virtual Resource ToResource { get; set; } = null!;
    }
}
