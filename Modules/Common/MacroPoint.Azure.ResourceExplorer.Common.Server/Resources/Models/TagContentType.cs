using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MacroPoint.Azure.ResourceExplorer.Common.Server.Resources.Models
{
    [Table("TagContentType", Schema = "ResourceExplorer")]
    public class TagContentType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int TagContentTypeId { get; set; }

        [StringLength(50)]
        public string? TagCode { get; set; }

        // Navigation properties
        public virtual ICollection<TagDefinition> TagDefinitions { get; set; } = new List<TagDefinition>();
    }
}