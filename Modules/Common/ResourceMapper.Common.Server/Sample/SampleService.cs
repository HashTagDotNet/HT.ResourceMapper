using System.Net;
using HT.Api.Service.Contracts;
using HT.Microsoft.ILogger.Extensions;
using Microsoft.Extensions.Logging;
using ResourceMapper.Common.Server.Sample.Interfaces;
using ResourceMapper.Common.Shared.Contracts;

namespace ResourceMapper.Common.Server.Sample
{
    public class SampleService: ISampleService
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
                _logger.Information("Getting {daySpan} days ago", request);
                response.Data ??=new SampleGetDateTimeResponse();
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
    }
}
