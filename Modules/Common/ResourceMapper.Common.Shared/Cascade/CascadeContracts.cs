namespace ResourceMapper.Common.Shared.Cascade
{
    /// <summary>
    /// Moves everything off one identity-bearing value onto another, then deletes the original. Shared by
    /// Subscription and Resource Type because the operation is the same shape: both are REQUIRED on a
    /// resource, so there is no empty state to leave behind — "remove everywhere" has to mean "move, then
    /// remove", not "destroy the resources".
    /// </summary>
    public class ReassignAndDeleteRequest
    {
        /// <summary>What is being removed. A domain value's own text, or a resource type's id as a string.</summary>
        public string From { get; set; } = string.Empty;

        /// <summary>What its resources move onto. Must already exist — this operation never invents one.</summary>
        public string To { get; set; } = string.Empty;
    }

    /// <summary>
    /// Outcome of a reassign-and-delete. Refusals are normal results carrying a reason, not errors: the
    /// screen turns each into a sentence, and the conflict case in particular needs its count.
    /// </summary>
    public class ReassignAndDeleteResponse
    {
        public bool Succeeded { get; set; }

        /// <summary>Resources moved onto the replacement.</summary>
        public int AffectedResources { get; set; }

        /// <summary>
        /// Resources that could not move because the target already holds the same identity. Identity is
        /// (Domain + Type + Key) and is enforced in code, not by a constraint, so a move could otherwise
        /// manufacture duplicates the app believes cannot exist. Non-zero means nothing was moved at all.
        /// </summary>
        public int Conflicts { get; set; }

        /// <summary>Why it was refused, ready to show. Null on success.</summary>
        public string? Message { get; set; }
    }

    /// <summary>
    /// Outcome of a tag's "remove everywhere". Unlike the reassign pair this genuinely destroys data —
    /// recorded values, template entries and primary-link choices — so each count is reported to say
    /// exactly what was lost. Resources themselves are never deleted.
    /// </summary>
    public class ForceDeleteTagResponse
    {
        public bool Deleted { get; set; }

        /// <summary>True when refused because the tag is system-managed.</summary>
        public bool IsSystemManaged { get; set; }

        /// <summary>Recorded tag values deleted from resources.</summary>
        public int ValuesRemoved { get; set; }

        /// <summary>Entry-point template rows removed from resource types.</summary>
        public int TemplatesRemoved { get; set; }

        /// <summary>Resources whose click-through link was reset to none.</summary>
        public int PrimaryLinksCleared { get; set; }

        /// <summary>Why it was refused, ready to show. Null on success.</summary>
        public string? Message { get; set; }
    }
}
