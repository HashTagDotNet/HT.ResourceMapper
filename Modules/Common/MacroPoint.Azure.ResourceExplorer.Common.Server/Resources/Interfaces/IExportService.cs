using MacroPoint.Azure.ResourceExplorer.Api.Service.Contracts;
using MacroPoint.Azure.ResourceExplorer.Common.Shared.Export.Contracts;

namespace MacroPoint.Azure.ResourceExplorer.Common.Server.Resources.Interfaces
{
    /// <summary>
    /// Produces a full-catalog document in the import JSON format (ImportExportApiDesign #4/#5).
    /// Consumed in-process by the Blazor Import/Export page — see IImportService for the same pattern.
    /// </summary>
    public interface IExportService
    {
        Task<ApiServiceResponse<ExportResult>> ExportAsync(
            ExportOptions options, CancellationToken cancellationToken);
    }
}
