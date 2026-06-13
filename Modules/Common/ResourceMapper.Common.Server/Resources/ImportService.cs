using HT.Api.Client.Contracts.Models;
using HT.Api.Service.Contracts;
using HT.Api.Service.Contracts.BuildersOfT;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Shared.Import.Contracts;

namespace ResourceMapper.Common.Server.Resources
{
    public class ImportService : IImportService
    {
        // Phase 1 walking skeleton: returns a fixed canned response.
        // Validation and persistence are implemented in later phases
        // (see docs/ImportExportApiDesign.md).
        public Task<ApiServiceResponse<ImportResponse>> ImportAsync(
            ImportRequest request, CancellationToken cancellationToken)
        {
            var builder = new ServiceResponseBuilder<ImportResponse>();
            try
            {
                if (request == null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return Task.FromResult(builder.BuildResponse());
                }

                var response = new ImportResponse
                {
                    Success = true,
                    Summary = new ImportSummary
                    {
                        ResourceTypes = new ImportSectionSummary { Created = 1, Updated = 0, Skipped = 0 },
                        TagDefinitions = new ImportSectionSummary { Created = 2, Updated = 0, Skipped = 0 },
                        Resources = new ImportSectionSummary { Created = 8, Updated = 1, Skipped = 1 }
                    }
                };

                builder.Data.Set(response);
                return Task.FromResult(builder.BuildResponse());
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return Task.FromResult(builder.BuildResponse());
            }
        }
    }
}
