using HT.Api.Service.Contracts;
using ResourceMapper.Common.Shared.Import.Contracts;

namespace ResourceMapper.Common.Server.Resources.Interfaces
{
    public interface IImportService
    {
        Task<ApiServiceResponse<ImportResponse>> ImportAsync(
            ImportRequest request, CancellationToken cancellationToken);
    }
}
