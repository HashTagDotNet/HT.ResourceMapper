namespace ResourceMapper.Common.Shared.HomePage.Contracts
{
 
    public class ResourceGridRequest
    {
   
        public int Skip { get; set; }
        public int Take { get; set; }

        /// <summary>
        /// Free text search across multiple columns looking for 'contains' matches
        /// Alternate to using Filters
        /// </summary>
        public string? SearchFor { get; set; }

        /// <summary>
        /// Order result by a single column. Alternate to SortBy collection
        /// </summary>
        public string? OrderBy { get; set; }
        public string? OrderDirection { get; set; } = "Asc"; // or "Desc"

        public List<ResourceGridSortDefinition>? SortBy { get; set; }
    
        public List<ResourceGridFilterDefinition>? Filters { get; set; }
    }
    public class ResourceGridSortDefinition
    {
        public string? Column { get; set; }        // Maps to PropertyFunc
        public string Direction { get; set; } = "Asc"; // or "Desc" or "" for natural
    }

    public class ResourceGridFilterDefinition
    {
        public string? Column { get; set; }       
        public string Operator { get; set; } = "Contains";
        public string? Value { get; set; }
        public string? FieldType { get; set; } = "String";
    }

    public class ResourceGridResponse
    {
        public List<ResourceGridItemModel>? Items { get; set; }

        public int TotalItems { get; set; }
    }
}
