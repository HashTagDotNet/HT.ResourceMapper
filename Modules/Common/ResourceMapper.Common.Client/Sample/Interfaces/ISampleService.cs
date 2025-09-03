using HT.Api.Service.Contracts;
using ResourceMapper.Common.Shared.Contracts;

namespace ResourceMapper.Common.Client.Sample.Interfaces
{
    public interface ISampleService
    {
        Task<ApiServiceResponse<SampleGetDateTimeResponse>> GetDaysAgoAsync(SampleGetDateTimeRequest request,
            CancellationToken cancellationToken);
    }
}
