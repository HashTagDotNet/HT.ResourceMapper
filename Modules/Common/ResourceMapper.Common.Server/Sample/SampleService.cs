using System.Net;
using HT.Api.Service.Contracts;
using HT.Microsoft.ILogger.Extensions;
using Microsoft.Extensions.Logging;
using ResourceMapper.Common.Server.Sample.Interfaces;
using ResourceMapper.Common.Shared.Contracts;

namespace ResourceMapper.Common.Server.Sample
{
    public class SampleService : ISampleService
    {
        private readonly ILogger _logger;
        private readonly ISampleRepository _repo;

        public SampleService(ILogger<SampleService> logger,
            ISampleRepository repo)
        {
            _logger = logger;
            _repo = repo;
        }



        public async Task<ApiServiceResponse<SampleGetDateTimeResponse>> GetDaysAgoAsync(SampleGetDateTimeRequest request, CancellationToken cancellationToken)
        {
            var response = new ApiServiceResponse<SampleGetDateTimeResponse>();
            try
            {
                if (ValidateRequest(request, response))
                {
                    return response;
                }


                response.Data ??= new SampleGetDateTimeResponse();
                response.Data.FoundDate = await _repo.GetDaysAgoAysnc(request.DateOffsetToGet, cancellationToken);
                response.SetStatusCode(HttpStatusCode.OK);
                return response;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Unexpected error retrieving date span. {ex.ToString()}");
                response.SetStatusMessage("Unexpected error retrieving date span");
                return response;
            }
        }

        private bool ValidateRequest(SampleGetDateTimeRequest request, ApiServiceResponse<SampleGetDateTimeResponse> response)
        {

            if (request.DateOffsetToGet <= -10)
            {
                response.SetStatusMessage("Date offset must be greater than -10");
                response.SetStatusCode(HttpStatusCode.BadRequest);
                return false;
            }

            if (request.DateOffsetToGet >= 10)
            {
                response.SetStatusMessage("Date offset must be less than 10");
                response.SetStatusCode(HttpStatusCode.BadRequest);
                return false;
            }
            return true;
        }
    }
}
