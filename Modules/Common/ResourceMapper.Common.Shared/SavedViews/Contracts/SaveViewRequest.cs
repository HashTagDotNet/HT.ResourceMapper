namespace ResourceMapper.Common.Shared.SavedViews.Contracts
{
    /// <summary>
    /// Save or Save As. An empty <see cref="SavedViewUid"/> means "create"; a populated one means
    /// "update that view" — which is also how rename and overwrite-an-existing-name are expressed,
    /// since the procedure keys on the uid rather than on the name.
    /// </summary>
    public class SaveViewRequest
    {
        public string SavedViewUid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string QueryString { get; set; } = string.Empty;
    }
}
