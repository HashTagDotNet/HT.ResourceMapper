using System;
using Microsoft.Extensions.Logging;

namespace HT.Msft.ILogger.Extensions
{
    public class LogMessageBuilder
    {
        private global::Microsoft.Extensions.Logging.ILogger _logger;
        private LogMessage _message;
        internal LogMessageBuilder(global::Microsoft.Extensions.Logging.ILogger logger, LogLevel logLevel,string methodName = "", int lineNumber = 0)
        {
            _logger = logger;
            _message = new LogMessage()
            {
                Method = methodName,
                LineNumber = lineNumber,
                LogLevel = logLevel
            };
        }

        public LogMessageBuilder Id(string id)
        {
            _message.Id = new EventId(-1,id);
            return this;
        }

        public LogMessageBuilder Id(EventId id)
        {
            _message.Id = id;
            return this;
        }
        public LogMessageBuilder Message(string message, params object[] arguments)
        {
            _message.MessageFormat = message;
            _message.MessageArguments = arguments;
            return this;
        }

        public LogMessageBuilder Catch(Exception ex)
        {
            _message.Exception = ex;
            return this;
        }
        public LogMessageBuilder Context(object context)
        {
            _message.Context = context;
            return this;
        }

        public LogMessageBuilder Method([System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
        {
            _message.Method = methodName;
            return this;
        }
        public LogMessageBuilder Line([System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            _message.LineNumber = lineNumber;
            return this;
        }

        public LogMessageBuilder For(LogLevel level)
        {
            _message.LogLevel = level;
            return this;
        }

        public void Log() => ILoggerExtensions.writeLog(_logger, _message);

        public void Log(string message, params object[] arguments)
        {
            _message.MessageFormat = message;
            _message.MessageArguments = null;
            ILoggerExtensions.writeLog(_logger, _message);
        }
        public void Log(object context,string message, params object[] arguments)
        {
            _message.MessageFormat = message;
            _message.MessageArguments = arguments;
            ILoggerExtensions.writeLog(_logger, _message);
        }
        public void Log(object context)
        {
            _message.Context = context;
            ILoggerExtensions.writeLog(_logger, _message);
        }

        public void Log(LogMessage message) => ILoggerExtensions.writeLog(_logger, message);
        public LogMessage LogMessage() => _message;

        
        public void Log(Action<LogMessage> message)
        {
            message?.Invoke(_message);
            ILoggerExtensions.writeLog(_logger, _message);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var retVal = "";
            return retVal;
        }
    }

 
}
