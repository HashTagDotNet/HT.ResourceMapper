namespace ResourceMapper.Common.Shared.Contracts
{
    public class SampleGetDateTimeRequest
    {
        public int DateOffsetToGet { get; set; }
    }
    public class SampleGetDateTimeResponse
    {
        public DateTime FoundDate { get; set; }
    }
}
