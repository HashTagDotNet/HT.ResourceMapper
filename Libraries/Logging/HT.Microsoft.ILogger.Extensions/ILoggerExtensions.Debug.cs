using System;
using Microsoft.Extensions.Logging;

// ReSharper disable ArrangeMethodOrOperatorBody

namespace HT.Msft.ILogger.Extensions
{
    // ReSharper disable once InconsistentNaming


    public static partial class ILoggerExtensions
    {
        public static bool WillWriteDebug(this global::Microsoft.Extensions.Logging.ILogger logger) =>
            logger.IsEnabled(LogLevel.Debug);

        public static LogMessageBuilder AsDebug(this global::Microsoft.Extensions.Logging.ILogger logger,
                                                    [System.Runtime.CompilerServices.CallerMemberName] string methodName=null,
                                                    [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return new LogMessageBuilder(logger, LogLevel.Debug, methodName, lineNumber);
        }
        public static LogMessageBuilder AsDebug(this global::Microsoft.Extensions.Logging.ILogger logger,
                                                      Exception ex,
                                                      [System.Runtime.CompilerServices.CallerMemberName] string methodName=null,
                                                      [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return new LogMessageBuilder(logger, LogLevel.Debug, methodName, lineNumber).Catch(ex);
        }
        public static LogMessageBuilder AsDebug(this global::Microsoft.Extensions.Logging.ILogger logger,
                                                      Exception ex,
                                                      EventId id,
                                                      [System.Runtime.CompilerServices.CallerMemberName] string methodName=null,
                                                      [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return new LogMessageBuilder(logger, LogLevel.Debug, methodName, lineNumber).Catch(ex).Id(id);
        }
        public static LogMessageBuilder AsDebug(this global::Microsoft.Extensions.Logging.ILogger logger,
                                                      EventId id,
                                                      [System.Runtime.CompilerServices.CallerMemberName] string methodName=null,
                                                      [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return new LogMessageBuilder(logger, LogLevel.Debug, methodName, lineNumber).Id(id);
        }

        public static void Debug(this global::Microsoft.Extensions.Logging.ILogger logger,
                                       string message,
                                       [System.Runtime.CompilerServices.CallerMemberName] string methodName=null,
                                       [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            writeLog(logger,
                LogLevel.Debug,
                eventId: null,
                exception: null,
                messageFormat: message,
                messageArguments: null,
                context: null,
                methodName: methodName,
                lineNumber: lineNumber);
        }


        public static void Debug(this global::Microsoft.Extensions.Logging.ILogger logger,
                                       string message,
                                       object context,
                                       [System.Runtime.CompilerServices.CallerMemberName] string methodName=null,
                                       [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            writeLog(logger,
                LogLevel.Debug,
                eventId: null,
                exception: null,
                messageFormat: message,
                messageArguments: null,
                context: context,
                methodName: methodName,
                lineNumber: lineNumber);
        }
        public static void Debug(this global::Microsoft.Extensions.Logging.ILogger logger,
                                        EventId id,
                                       [System.Runtime.CompilerServices.CallerMemberName] string methodName=null,
                                       [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            writeLog(logger,
                LogLevel.Debug,
                eventId: id,
                exception: null,
                messageFormat: null,
                messageArguments: null,
                context: null,
                methodName: methodName,
                lineNumber: lineNumber);
        }

        public static void Debug(this global::Microsoft.Extensions.Logging.ILogger logger, EventId id,
                                       object context,
                                       [System.Runtime.CompilerServices.CallerMemberName] string methodName=null,
                                       [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            writeLog(logger,
                LogLevel.Debug,
                eventId: id,
                exception: null,
                messageFormat: null,
                messageArguments: null,
                context: context,
                methodName: methodName,
                lineNumber: lineNumber);
        }

        public static void Debug(this global::Microsoft.Extensions.Logging.ILogger logger, EventId id, string message, object context, [System.Runtime.CompilerServices.CallerMemberName] string methodName=null, [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            writeLog(logger,
                LogLevel.Debug,
                eventId: id,
                exception: null,
                messageFormat: message,
                messageArguments: null,
                context: context,
                methodName: methodName,
                lineNumber: lineNumber);
        }

        public static void Debug(this global::Microsoft.Extensions.Logging.ILogger logger, EventId id, string message)
        {
            writeLog(logger,
                LogLevel.Debug,
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
