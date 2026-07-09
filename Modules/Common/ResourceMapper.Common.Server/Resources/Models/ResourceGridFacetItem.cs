namespace ResourceMapper.Common.Server.Resources.Models
{
    /// <summary>
    /// One row of a column's distinct value+count facet (from Resource_GetFilterValues).
    /// </summary>
    public class ResourceGridFacetItem
    {
        /// <summary>The distinct value; null for the blank/NULL bucket.</summary>
        public string? Value { get; set; }

        /// <summary>True when this row represents the NULL/blank bucket.</summary>
        public bool IsBlank { get; set; }

        public int ItemCount { get; set; }
    }
}
