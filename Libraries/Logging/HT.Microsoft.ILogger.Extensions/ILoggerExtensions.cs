using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using HT.Collections;
using Microsoft.Extensions.Logging;

// ReSharper disable ConvertToUsingDeclaration

// ReSharper disable InconsistentNaming

namespace HT.Microsoft.ILogger.Extensions
{
    /// <summary>
    /// Logger extensions that provide deconstruction of scope, context on log calls, and log prefixes./// 
    /// </summary>
    public static partial class ILoggerExtensions
    {
#pragma warning disable IDE1006 // Naming Styles
        private static readonly JsonSerializerOptions __serializerOptions = new JsonSerializerOptions
#pragma warning restore IDE1006 // Naming Styles
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static void Log(this global::Microsoft.Extensions.Logging.ILogger logger, Action<LogMessage> message)
        {
            var logMessage = new LogMessage();
            message?.Invoke(logMessage);
            ILoggerExtensions.writeLog(logger, logMessage);
        }

        /// <summary>
        /// Dummy class for creating a non-op scope
        /// </summary>
        public class NullDisposable : IDisposable
        {
            /// <inheritdoc />
            public void Dispose() { }
        }

        /// <summary>
        /// Start a ILogger scope by deconstructing <paramref name="state"/>.  This is a heavy operation and should be used with care.  By default, state will not attach.
        /// </summary>
        /// <param name="logger">Logger that will have scope attached.</param>
        /// <param name="state">Context to attach to this logger</param>
        /// <param name="allowedScopeLevel">Minimum level of logging that will allow deconstruction of <paramref name="state"/>.
        /// If not specified, scope is ALWAYS attached. Default is null so scope will ALWAYS be attached to logger.  Set <paramref name="allowedScopeLevel"/>.None to disable attaching scope</param>
        /// <returns></returns>
        public static IDisposable StartScope(this global::Microsoft.Extensions.Logging.ILogger logger, object state,
                                             LogLevel? allowedScopeLevel = null)
        {
            try
            {
                if (state == null) return new NullDisposable();
                if (allowedScopeLevel.HasValue)
                {
                    if (allowedScopeLevel.Value == LogLevel.None || !logger.IsEnabled(allowedScopeLevel.Value))
                    {
                        return new NullDisposable();
                    }
                }

                var scopeDictionary = buildScopeContext(state);
                return scopeDictionary == null ? new NullDisposable() : logger.BeginScope(scopeDictionary);
            }
            catch
            {
                return logger.BeginScope(state); // native ILogger behavior
            }
        }

        private static Dictionary<string, object> buildScopeContext(object context, Dictionary<string, object> existingScope = null)
        {
            Dictionary<string, object> retVal = existingScope ??= new();
            if (context == null) return retVal;

            var typeName = context.GetType().Name;
            if (typeName.Contains("Anon"))
            {
                typeName = "ctx";
            }
            try
            {
                var jsonProps = SerializeToFlatJson(context, __serializerOptions);
                if (jsonProps != null && jsonProps.Count > 0)
                {
                    foreach (var jsonProp in jsonProps)
                    {
                        retVal.AddIfNotExists($"{typeName}:{jsonProp.Key}", jsonProp.Value);
                    }
                }
            }
            catch (Exception contextException) // error serializing context to flat json 
            {
                retVal.Add("ScopeBuilderError", contextException.ToString());
                try
                {
                    var jsonString = JsonSerializer.Serialize(context, __serializerOptions); //attempt to serialize to normal json
                    retVal.Add(typeName, jsonString);
                }
                catch (Exception ex) // final attempt at storing context in scope
                {
                    retVal.Add("ScopeBuilderFatalError", ex.ToString());
                    retVal.Add(typeName, context.ToString());
                }
            }
            return retVal;
        }

        internal static void writeLog(global::Microsoft.Extensions.Logging.ILogger logger, LogMessage message)
        {
            if (message == null || logger == null || !logger.IsEnabled(message.LogLevel) || message.LogLevel == LogLevel.None) return;
            writeLog(logger, message.LogLevel, message.Id, message.Exception, message.MessageFormat,
                message.MessageArguments, message.Context, message.Method, message.LineNumber);
        }

        private static EventId _defaultEvent = new(-4);

        internal static global::Microsoft.Extensions.Logging.ILogger writeLog(global::Microsoft.Extensions.Logging.ILogger logger,
                                                                      LogLevel logLevel,
                                                                      EventId? eventId,
                                                                      Exception exception,
                                                                      string messageFormat,
                                                                      object[] messageArguments,
                                                                      object context,
                                                                      string methodName,
                                                                      int? lineNumber)
        {
            if (logger == null || !logger.IsEnabled(logLevel) || logLevel == LogLevel.None) return logger;

            try
            {
                // build message and scope dictionary
                if (string.IsNullOrWhiteSpace(messageFormat) && exception != null) messageFormat = exception.Message;
                eventId ??= _defaultEvent;
                string messageToLog = buildMessage(logLevel, methodName, lineNumber, messageFormat, messageArguments, eventId);
                Dictionary<string, object> scopeDictionary = buildScopeDictionary(logLevel, methodName, lineNumber, context, exception);

                // write to ILogger
                if (scopeDictionary != null)
                {
                    const int blockSize = 1000;
                    if (scopeDictionary.Count < blockSize)
                    {
                        using (var _ = logger.BeginScope(scopeDictionary))
                        {
                            if (exception == null)
                            {
                                logger.Log(logLevel, eventId.Value, messageToLog);
                            }
                            else
                            {
                                logger.Log(logLevel, eventId.Value, exception, messageToLog);
                            }
                        }
                    }
                    else
                    {
                        int blockStartIndex = 0;

                        var blockScope = new Dictionary<string, object>(scopeDictionary.Skip(blockStartIndex).Take(blockSize));
                        while (blockScope.Count > 0)
                        {
                            string blockRange = $"{blockStartIndex}-{blockStartIndex + blockScope.Count - 1}";
                            string blockMessage = $"{messageToLog} [{blockRange}]";
                            blockScope.Add("Dimensions:Total", $"{scopeDictionary.Count}");
                            blockScope.Add("Dimensions:Block", blockRange);
                            using (var _ = logger.BeginScope(blockScope))
                            {
                                if (exception == null)
                                {
                                    logger.Log(logLevel, eventId.Value, blockMessage);
                                }
                                else
                                {
                                    logger.Log(logLevel, eventId.Value, exception, blockMessage);
                                }
                            }
                            blockStartIndex += blockSize;
                            blockScope = new Dictionary<string, object>(scopeDictionary.Skip(blockStartIndex).Take(500));
                        }
                    }
                }
                else // do not add scope to this log entry
                {
                    if (exception == null)
                    {
                        logger.Log(logLevel, eventId.Value, messageToLog);
                    }
                    else
                    {
                        logger.Log(logLevel, eventId.Value, exception, messageToLog);
                    }
                }
            }
            catch (Exception e)
            {
                // ReSharper disable once StringLiteralTypo
                logger.LogError(new EventId(9999, "LOGERR"), e, "Unhandled logging error. {LoggingError} Original Message: {OriginalMessage} Original Error: {OriginalError}", e.Message, messageFormat ?? "(null)", exception?.Message ?? "(null)");
            }
            return logger;
        }

        private static Dictionary<string, object> buildScopeDictionary(LogLevel logLevel, string method, int? lineNumber, object context, Exception exception)
        {
            var retVal = new Dictionary<string, object>();
            // always include well known scope entries
            if (!string.IsNullOrWhiteSpace(method))
            {
                retVal.Add("MethodName", method);
                retVal.Add("LineNumber", lineNumber?.ToString() ?? "--");
            }
            retVal.Add("LogLevel", logLevel);
            addActivityProperties(retVal);

            if (context == null && exception == null) return retVal;

            if (context != null)
            {
                buildScopeContext(context, retVal);
            }

            if (exception is { Data.Count: > 0 }) // add the detail of the 'data' property of the exception
            {
                foreach (object key in exception.Data.Keys)
                {
                    if (key == null) continue;
                    retVal.AddIfNotExists($"key[{key}]", exception.Data[key]);
                }
            }

            return retVal.Count > 0 ? retVal : null;
        }

        private static void addActivityProperties(Dictionary<string, object> retVal)
        {
            var activity = Activity.Current;
            if (activity == null) return;

            try
            {
                var jsonProps = SerializeToFlatJson(activity, __serializerOptions);
                if (jsonProps != null && jsonProps.Count > 0)
                {
                    foreach (var jsonProp in jsonProps)
                    {
                        retVal.AddIfNotExists($"Activity:{jsonProp.Key}", jsonProp.Value);
                    }
                }
            }
            catch (Exception)
            {
                retVal.Add("Activity:Id", activity.Id ?? "(null)");
                retVal.Add("Activity:Operation", activity.OperationName);
                if (!string.IsNullOrWhiteSpace(activity.DisplayName))
                {
                    retVal.Add("Activity:DisplayName", activity.DisplayName);
                }

                foreach (var bag in activity.Baggage)
                {
                    var key = bag.Key;
                    var value = bag.Value;
                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) return;
                    retVal.Add($"Activity:Baggage:{key}", value);
                }
                foreach (var tag in activity.Tags)
                {
                    var key = tag.Key;
                    var value = tag.Value;
                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) return;
                    retVal.Add($"Activity:Tags:{key}", value);
                }
            }
        }

        private static string buildMessage(LogLevel level, string methodName, int? lineNumber, string messageFormat, object[] messageArgs, EventId? id)
        {
            // message(args) | ex.Message
            var retVal = "";
            switch (level)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                case LogLevel.Warning:
                    if (id.Value.Id >= 0 && !string.IsNullOrWhiteSpace(id.Value.Name))
                    {
                        retVal = $"{id.Value.Name}({id.Value.Id})";
                    }
                    if (!string.IsNullOrWhiteSpace(methodName))
                    {
                        if (retVal.Length > 0) retVal += " ";

                        retVal += methodName;
                        if (lineNumber != null)
                        {
                            retVal = $"{retVal}({lineNumber})";
                        }
                    }

                    break;
                default:
                    if (!string.IsNullOrWhiteSpace(messageFormat) && !string.IsNullOrWhiteSpace(methodName))
                    {
                        if (!string.IsNullOrWhiteSpace(methodName))
                        {
                            retVal = methodName;
                        }

                        if (lineNumber != null && retVal.Length > 0)
                        {
                            retVal = $"{retVal}({lineNumber})";
                        }
                    }
                    break;
            }

            if (!string.IsNullOrWhiteSpace(messageFormat))
            {
                if (retVal.Length > 0) retVal += " ";
                if (messageArgs == null || messageArgs.Length == 0)
                {
                    retVal += messageFormat;
                }
                else
                {
                    retVal += string.Format(messageFormat, messageArgs);
                }

                return retVal;
            }

            return retVal;
        }

        private static Dictionary<string, object> SerializeToFlatJson(object obj, JsonSerializerOptions options)
        {
            var json = JsonSerializer.Serialize(obj, options);
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
        }
    }
}
