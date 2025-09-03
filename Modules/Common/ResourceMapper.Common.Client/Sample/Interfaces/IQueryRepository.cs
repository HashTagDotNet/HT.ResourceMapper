namespace ResourceMapper.Common.Client.Sample.Interfaces;

public interface ISampleRepository
{
    Task<DateTime> GetDaysAgoAysnc(int daySpan, CancellationToken cancellationToken);
}