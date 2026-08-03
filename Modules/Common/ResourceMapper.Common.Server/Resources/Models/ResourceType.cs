using System.ComponentModel.DataAnnotations;

namespace ResourceMapper.Common.Server.Resources.Models
{
    public class ResourceType
    {
    
        public int ResourceTypeId { get; set; }

        [Required]
        [StringLength(40)]
        public string ResourceTypeUid { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string TypeName { get; set; } = string.Empty;

        [Required]
        public bool AllowCustomTags { get; set; } = true;

        /// <summary>Badge text printed inside an explorer node (falls back to the type name's
        /// first three letters when empty).</summary>
        [StringLength(10)]
        public string? ShortCode { get; set; }

        /// <summary>Explorer glyph + node-colour key. See ResourceTypeIcons.KnownKeys.</summary>
        [StringLength(40)]
        public string? IconKey { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

    }
}