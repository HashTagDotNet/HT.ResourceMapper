using HT.Api.Service.Contracts;
using ResourceMapper.Common.Shared.Contracts;

namespace ResourceMapper.Common.Server.Sample.Interfaces
{
    public interface ISampleService
    {
        Task<ApiServiceResponse<SampleGetDateTimeResponse>> GetDaysAgoAsync(SampleGetDateTimeRequest request,
            CancellationToken cancellationToken);
    }
}
