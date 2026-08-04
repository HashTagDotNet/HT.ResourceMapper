namespace ResourceMapper.Common.Shared.Domains.Contracts
{
    /// <summary>Renames a domain value everywhere: the vocabulary and every resource carrying it.</summary>
    public class RenameDomainValueRequest
    {
        public string OldValue { get; set; } = string.Empty;

        public string NewValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// Outcome of a rename. The affected count is the point of the response, not decoration: the
    /// caller warned the user that N resources would be rewritten, and this is what actually was.
    /// </summary>
    public class RenameDomainValueResponse
    {
        public string Value { get; set; } = string.Empty;

        public int AffectedResources { get; set; }

        /// <summary>The full refreshed vocabulary, so a caller's picker does not need a second call.</summary>
        public List<string> AllowedValues { get; set; } = new();
    }

    /// <summary>
    /// Outcome of a delete attempt. Refusals are normal results rather than errors, because the screen
    /// turns each into a specific sentence: still in use (with the count), or the last value standing.
    /// </summary>
    public class DeleteDomainValueResponse
    {
        public bool Deleted { get; set; }

        /// <summary>Resources still carrying the value when the delete was refused as in-use.</summary>
        public int ResourceCount { get; set; }

        /// <summary>True when the delete was refused because it was the only value left — Subscription
        /// is required to save a resource, so an empty vocabulary would block creating any.</summary>
        public bool WasLastValue { get; set; }

        /// <summary>Why it was refused, ready to show. Null when the delete succeeded.</summary>
        public string? Message { get; set; }
    }
}
