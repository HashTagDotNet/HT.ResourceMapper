using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResourceMapper.Common.Server.Resources.Models
{
    [Table("ResourceDependency", Schema = "HTResourceMapper")]
    public class ResourceDependency
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ResourceDependencyId { get; set; }

        [Required]
        public int ResourceId { get; set; }

        [Required]
        public int DependencyResourceId { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

        // Navigation properties
        [ForeignKey("ResourceId")]
        public virtual Resource Resource { get; set; } = null!;

        [ForeignKey("DependencyResourceId")]
        public virtual Resource DependencyResource { get; set; } = null!;
    }
}