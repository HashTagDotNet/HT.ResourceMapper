namespace ResourceMapper.Common.Server.Identity
{
    /// <summary>
    /// Who owns the personal artefacts: saved views, explorer diagrams, grid settings.
    /// <para>
    /// There is no identity provider yet, so the only implementation returns a single configured
    /// owner. This exists so that ownership has the right <i>shape</i> now — every repository and
    /// stored procedure already filters by owner — and swapping in a real provider later is one
    /// implementation rather than a schema change across three tables.
    /// </para>
    /// <para>
    /// It is not authentication and not authorisation. Anyone reaching the app is the owner.
    /// </para>
    /// </summary>
    public interface ICurrentIdentity
    {
        /// <summary>The current owner. Never null or empty.</summary>
        string OwnerId { get; }
    }
}
