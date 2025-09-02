using ResourceMapper.Feature1.Server.Interfaces;
using ResourceMapper.Feature1.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace ResourceMapper.Feature1.Server
{
    public class DateTimeService : IDateTimeService
    {
        private readonly ILogger<DateTimeService> _logger;

        public DateTimeService(ILogger<DateTimeService> logger)
        {
            _logger = logger;
        }

        public async Task<GetDateTimeResponse> GetDateTime()
        {
            var currentDateTime = DateTime.Now;
            var response = new GetDateTimeResponse
            {
                CurrentDateTime = currentDateTime
            };

            _logger.LogInformation("Returning server date time: {ServerDateTime}", currentDateTime);

            return await Task.FromResult(response);
        }
    }
}
