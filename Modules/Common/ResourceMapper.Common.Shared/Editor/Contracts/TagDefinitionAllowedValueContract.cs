namespace ResourceMapper.Common.Shared.Editor.Contracts
{
    /// <summary>
    /// Appends one value to a controlled-vocabulary tag definition's list. Every "add a new choice"
    /// affordance in the editor (Subscription, and both the single- and multi-value tag rows) routes
    /// through this one request: those pickers all read the same TagDefinition.AllowedValues list —
    /// Subscription is simply the definition flagged IsDomainTag — so a second write path would only
    /// be a second set of rules to keep in step.
    /// </summary>
    public class AddAllowedValueRequest
    {
        /// <summary>The definition whose vocabulary gains the value.</summary>
        public int TagDefinitionId { get; set; }

        /// <summary>The new choice. Trimmed; compared case-insensitively against the existing list.</summary>
        public string Value { get; set; } = string.Empty;
    }
}
