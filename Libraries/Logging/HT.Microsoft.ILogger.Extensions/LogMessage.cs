using System;
using Microsoft.Extensions.Logging;

namespace HT.Msft.ILogger.Extensions
{
    public class LogMessage
    {
        public EventId? Id { get; set; }
        public string MessageFormat { get; set; }
        public object[] MessageArguments { get; set; }
        public Exception Exception { get; set; }
        public object Context { get; set; }
        public string Method { get; set; }
        public int LineNumber { get; set; }
        public LogLevel LogLevel { get; set; }
    }
}
