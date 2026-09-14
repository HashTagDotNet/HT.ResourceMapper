namespace ResourceMapper.Common.Shared.SavedViews
{
    /// <summary>One saved grid query, as the UI sees it.</summary>
    public class SavedViewModel
    {
        public string SavedViewUid { get; set; } = string.Empty;

        /// <summary>User-facing name. Unique per owner.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The grid's own '?n=...&amp;s=...&amp;f=...' payload, produced by
        /// <c>ResourceFilterState.ToQueryString()</c>. Opaque to the server: nothing parses it there,
        /// which is what lets the filter format change without a migration.
        /// </summary>
        public string QueryString { get; set; } = string.Empty;

        /// <summary>Manual position in the owner's list. Lower sorts first.</summary>
        public int SortOrder { get; set; }

        /// <summary>At most one per owner. Opens on a bare URL instead of the resume setting.</summary>
        public bool IsDefault { get; set; }
    }
}
