namespace MacroPoint.Azure.ResourceExplorer.Common.Server.Resources.Models
{
    /// <summary>Outcome of the transactional resource save: 'created' | 'updated' | 'error', and the resource's id.</summary>
    public sealed record ResourceSaveResult(string Result, int ResourceId);
}
