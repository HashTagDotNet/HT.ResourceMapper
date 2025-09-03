using System.Runtime.Serialization;

namespace HT.ApiContracts.Client.Models
{
    /// <summary>
    /// Severity level of the message.  This is not a standard HTTP status code, but a message severity level, similar to log levels.
    /// </summary>
    /// <remarks>
    /// Based on industry syslog severities. Note values are reverse of syslog values to help in filtering scenarios. See <a href="https://en.wikipedia.org/wiki/Syslog">SysLog</a>
    /// </remarks>
    public enum MessageLevel
    {
        /// <summary>
        /// API doesn't want to assign a severity level
        /// </summary>
        [EnumMember(Value = "none")]
        None = 0,

        /// <summary>
        /// Messages that contain information normally of use only when debugging a program.
        /// System messages are rarely exposed to normal API developers and almost never directly in response
        /// </summary>
        [EnumMember(Value = "sys")]
        System = 1,

        /// <summary>
        /// Informational messages might be attached.  Generally safe for caller to ignore these at runtime
        /// </summary>
        [EnumMember(Value = "info")]
        Info = 2,

        /// <summary>
        /// Normal but significant conditions. 
        /// Conditions that are not error conditions, but that may require special handling.  Often accompanying successful error codes.
        /// An error that is not critical to the operation. A 'Warning'
        /// (e.g. missing expected value--default used, user not validated--account locked until validation)
        /// </summary>
        [EnumMember(Value = "notice")]
        Notice = 3,

        /// <summary>
        /// An error occured during API request handling.
        /// </summary>
        [EnumMember(Value = "err")]
        Error = 4,

        /// <summary>
        /// Condition occurred that must be remedied before further usage of API (e.g. account locked, quota exceeded, internal database failure).
        /// These are used very, very rarely and often not at all.
        /// </summary>
        [EnumMember(Value = "crit")]
        Critical = 5,
    }
}
