namespace ResourceMapper.Common.Shared.Domains
{
    /// <summary>
    /// One domain value (a Subscription in this deployment) as the management screen sees it.
    ///
    /// There is no Domain table: the vocabulary lives in the domain TagDefinition's AllowedValues, and
    /// each resource stores its own value as text on ResourceTag. That is why this model carries a
    /// usage count and an "unlisted" flag rather than an id — the two sides can disagree, and the
    /// screen has to be able to show and fix that.
    /// </summary>
    public class DomainValueModel
    {
        /// <summary>The value itself. It IS the key — there is no surrogate id to fall back on, which
        /// is also why a rename has to cascade to every resource carrying it.</summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>Resources currently carrying this value. Non-zero blocks delete.</summary>
        public int ResourceCount { get; set; }

        /// <summary>True when resources carry the value but the vocabulary does not offer it — an
        /// import writes a resource's domain without touching AllowedValues. Renaming such a value
        /// also adopts it into the list.</summary>
        public bool IsUnlisted { get; set; }
    }
}
