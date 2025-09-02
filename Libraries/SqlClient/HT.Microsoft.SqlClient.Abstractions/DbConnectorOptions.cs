using System;

namespace HT.Msft.SqlClient.Extensions.Abstractions
{
    /// <summary>
    /// Settings that determine how application connects to the database
    /// </summary>
    public class DbConnectorOptions
    {
        public const int DEFAULT_CONNECT_RETRYCOUNT = 1;
        public const int DEFAULT_CONNECT_RETRYINTERVAL_SECS = 10;
        public const int DEFAULT_CONNECT_TIMEOUT_SECS = 15;

        public const int DEFAULT_COMMAND_TIMEOUT_SECS = 30;
        public const int DEFAULT_COMMAND_RETRYCOUNT = 3;
        public const double DEFAULT_COMMAND_RETRYINTERVAL = 1.0D;

        /// <summary>
        /// Name to provide on SQL connection. Automatically appends '(R/O)' for read only intents. Maximum length 128 characters 
        /// </summary>
        /// <remarks>
        /// This property corresponds to the "Application Name" and "app" keys
        /// within the connection string. An application name can be 128 characters or less.
        /// </remarks>
        public string ApplicationName { get; set; }

        /// <summary>
        /// The number of reconnection attempted after identifying that there was an idle
        /// connection failure. This must be an integer between 0 and 255.
        /// Set to 0 to disable reconnecting on idle connection failures. (optional. Use ConnectionString value if provided, default: 1)
        /// </summary>
        /// <exception cref="ArgumentException">If <see cref="ConnectRetryCount"/> is less than 0 or greater than 255</exception>
        public int? ConnectRetryCount { get; set; }

        /// <summary>
        /// Gets or sets the length of time (in seconds) to wait for a connection to the
        /// server before terminating the attempt and generating an error. (optional. Use ConnectionString if provided, default: 15)
        /// </summary>
        /// <remarks>
        /// This property corresponds to the "Connect Timeout", "connection timeout",
        /// and "timeout" keys within the connection string. When opening a connection to
        /// a Azure SQL Database, set the connection timeout to 30 seconds. Valid values
        /// are greater than or equal to 0 and less than or equal to 2147483647
        /// </remarks>
        /// <exception cref="ArgumentException">Valid values
        /// are greater than or equal to 0 and less than or equal to 2147483647
        /// </exception>
        public int? ConnectTimeOutSecs { get; set; }

        /// <summary>
        /// Amount of time (in seconds) between each reconnection attempt after identifying
        /// that there was an idle connection failure. This must be an integer between 1
        /// and 60. (optional. Use ConnectionString value if provided, default:10 seconds)
        /// </summary>
        /// <remarks>
        /// This property corresponds to the "Connect Retry Interval" key within
        /// the connection string. This value
        /// is applied after the first reconnection attempt. When a broken connection is
        /// detected, the client immediately attempts to reconnect; this is the first reconnection
        /// attempt and only occurs if `ConnectRetryCount` is greater than 0. If the first
        /// reconnection attempt fails and `ConnectRetryCount` is greater than 1, the client
        /// waits `ConnectRetryInterval` to try the second and subsequent reconnection attempts.
        /// </remarks>
        public int? ConnectRetryIntervalSecs { get; set; }

        /// <summary>
        /// The default wait time (in seconds) before terminating the attempt to execute
        /// a command and generating an error. A value of 0 indicates no limit. (default: 30 seconds)
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Valid values
        /// are greater than or equal to 0 and less than or equal to 2147483647
        /// </exception>
        public int? CommandTimeOutSecs { get; set; }

        /// <summary>
        /// Number of seconds to wait between executing command attempts if command timed out.
        /// Set to 0 if command should not be retried on timeout. On command timeout, the last the retry interval
        /// is multiplied by <see cref="CommandRetryIntervalSecs"/> giving new retry interval.  If <see cref="CommandRetryIntervalSecs"/>
        /// is greater than 1, retry interval results in a simple exponential back off. 
        /// (default: 0 seconds, don't retry)
        /// </summary>
        /// <remarks>WARNING: make sure the combination of <see cref="CommandTimeOutSecs"/>,<see cref="CommandRetryCount"/>, and <see cref="CommandTimeOutSecs"/>
        /// do not exceed a reasonable amount of time.
        /// </remarks>
        public double CommandRetryIntervalSecs { get; set; }

        /// <summary>
        /// Number of command timeout retries before command is considered an error and throws native exception (default: 3)
        /// </summary>
        public int? CommandRetryCount { get; set; }

        /// <summary>
        /// Gets or sets the minimum number of connections allowed in the connection pool
        ///     for this specific connection string.
        /// </summary>
        public int? MinPoolSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of connections allowed in the connection pool
        ///     for this specific connection string
        /// </summary>
        public int? MaxPoolSize { get; set; }

        /// <summary>
        ///     Gets or sets a Boolean value that indicates whether the connection will be pooled
        ///     or explicitly opened every time that the connection is requested. (default: true)
        /// </summary>
        public bool? EnablePooling { get; set; }
    }
}
