namespace MacroPoint.Azure.ResourceExplorer.Common.Server.Sample.Interfaces;

public interface ISampleRepository
{
    Task<DateTime> GetDaysAgoAysnc(int daySpan, CancellationToken cancellationToken);
}