namespace ResourceMapper.Common.Shared.Contracts
{
    public class SampleGetDateTimeRequest
    {
        public int DateOffsetToGet { get; set; }
        
        // Example: Additional properties that can be bound from query parameters
        // public string? Format { get; set; } = "yyyy-MM-dd HH:mm:ss";
        // public string? TimeZone { get; set; } = "UTC";
    }
    
    public class SampleGetDateTimeResponse
    {
        public DateTime FoundDate { get; set; }
    }
}
