using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMapper.Common.Server.Resources.Models
{
    public class ResourceGridItem
    {
        public string? ResourceUid { get; set; } 
        public string? ResourceName { get; set; }
        public string? ResourceType { get; set; }
        public string? Description { get; set; }
        
        public DateTime LastUpdatedOn { get; set; }

        public List<ResourceGridTag>? Tags { get; set; }
    }
}
