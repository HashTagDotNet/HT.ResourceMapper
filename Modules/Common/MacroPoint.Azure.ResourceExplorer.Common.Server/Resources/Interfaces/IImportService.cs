using MacroPoint.Azure.ResourceExplorer.Api.Service.Contracts;
using MacroPoint.Azure.ResourceExplorer.Common.Shared.Import.Contracts;

namespace MacroPoint.Azure.ResourceExplorer.Common.Server.Resources.Interfaces
{
    public interface IImportService
    {
        Task<ApiServiceResponse<ImportResponse>> ImportAsync(
            ImportRequest request, CancellationToken cancellationToken);
    }
}
