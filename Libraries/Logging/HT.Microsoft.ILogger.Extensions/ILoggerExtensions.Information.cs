using System;
using Microsoft.Extensions.Logging;

// ReSharper disable ArrangeMethodOrOperatorBody

namespace HT.Microsoft.ILogger.Extensions
{
    // ReSharper disable once InconsistentNaming


    public static partial class ILoggerExtensions
    {
        public static bool WillWriteInformation(this global::Microsoft.Extensions.Logging.ILogger logger) =>
            logger.IsEnabled(LogLevel.Information);

        public static LogMessageBuilder AsInformation(this global::Microsoft.Extensions.Logging.ILogger logger,
                                                    [System.Runtime.CompilerServices.CallerMemberName] string methodName=null,
                                                    [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return new LogMessageBuilder(logger,LogLevel.Information,methodName,lineNumber);
        }
        public static LogMessageBuilder AsInformation(this global::Microsoft.Extensions.Logging.ILogger logger,
                                                      Exception ex,
                                                      [System.Runtime.CompilerServices.CallerMemberName] string methodName=null,
                                                      [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return new LogMessageBuilder(logger, LogLevel.Information, methodName, lineNumber).Catch(ex);
        }
        public static LogMessageBuilder AsInformation(this global::Microsoft.Extensions.Logging.ILogger logger,
                                                      Exception ex,
                                                      EventId id,
                                                      [System.Runtime.CompilerServices.CallerMemberName] string methodName=null,
                                                      [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return new LogMessageBuilder(logger, LogLevel.Information, methodName, lineNumber).Catch(ex).Id(id);
        }
        public static LogMessageBuilder AsInformation(this global::Microsoft.Extensions.Logging.ILogger logger,
                                                      EventId id,
                                                      [System.Runtime.CompilerServices.CallerMemberName] string methodName=null,
                                                      [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return new LogMessageBuilder(logger, LogLevel.Information, methodName, lineNumber).Id(id);
        }

        public static void Information(this global::Microsoft.Extensions.Logging.ILogger logger,
                                       string message,
                                       [System.Runtime.CompilerServices.CallerMemberName] string methodName=null,
                                       [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            writeLog(logger,
                LogLevel.Information,
                eventId: null,
                exception: null,
                messageFormat: message,
                messageArguments: null,
                context: null,
                methodName: methodName,
                lineNumber: lineNumber);
        }

       
        public static void Information(this global::Microsoft.Extensions.Logging.ILogger logger,
                                       string message,
                                       object context,
                                       [System.Runtime.CompilerServices.CallerMemberName] string methodName=null,
                                       [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            writeLog(logger,
                LogLevel.Information,
                eventId: null,
                exception: null,
                messageFormat: message,
                messageArguments: null,
                context: context,
                methodName: methodName,
                lineNumber: lineNumber);
        }
        public static void Information(this global::Microsoft.Extensions.Logging.ILogger logger,
                                        EventId id,
                                       [System.Runtime.CompilerServices.CallerMemberName] string methodName=null,
                                       [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            writeLog(logger,
                LogLevel.Information,
                eventId: id,
                exception: null,
                messageFormat: null,
                messageArguments: null,
                context: null,
                methodName: methodName,
                lineNumber: lineNumber);
        }

        public static void Information(this global::Microsoft.Extensions.Logging.ILogger logger, EventId id,
                                       object context,
                                       [System.Runtime.CompilerServices.CallerMemberName] string methodName=null,
                                       [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            writeLog(logger,
                LogLevel.Information,
                eventId: id,
                exception: null,
                messageFormat: null,
                messageArguments: null,
                context: context,
                methodName: methodName,
                lineNumber: lineNumber);
        }

        public static void Information(this global::Microsoft.Extensions.Logging.ILogger logger, EventId id, string message, object context, [System.Runtime.CompilerServices.CallerMemberName] string methodName=null, [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            writeLog(logger,
                LogLevel.Information,
                eventId: id,
                exception: null,
                messageFormat: message,
                messageArguments: null,
                context: context,
                methodName: methodName,
                lineNumber: lineNumber);
        }

        public static void Information(this global::Microsoft.Extensions.Logging.ILogger logger, EventId id, string message)
        {
            writeLog(logger,
                LogLevel.Information,
                eventId: id,
                exception: null,
                messageFormat: message,
                messageArguments: null,
                context: null,
                methodName: null,
                lineNumber: 0);
        }


      
    }
}
