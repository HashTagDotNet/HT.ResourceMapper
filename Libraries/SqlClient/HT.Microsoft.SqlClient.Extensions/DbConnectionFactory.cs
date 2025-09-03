using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using HT.Microsoft.SqlClient.Extensions.Abstractions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;
using Microsoft.Data.SqlClient;

namespace HT.Microsoft.SqlClient.Extensions
{
    /// <summary>
    /// <inheritdoc cref="IDbConnectionFactory"/>
    /// </summary>
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _defaultConnectionString;
        private string _constructedConnectionString;
        private readonly DbConnectorOptions _options;
        private readonly bool _isReadOnly;

        private static class Properties
        {
            public const string ApplicationIntent = "Application Intent";
            public const string ApplicationIntentNoSpace = "ApplicationIntent";
            public const string ApplicationName = "Application Name";
            public const string CommandTimeout = "Commaind Timeout";
            public const string ConnectRetryCount = "Connect Retry Count";
            public const string ConnectRetryInterval = "Connect Retry Interval";
            public const string ConnectTimeout = "Connect Timeout";
            public const string EnablePooling = "Pooling";
            public const string MaxPoolSize = "Max Pool Size";
            public const string MinPoolSize = "Min Pool Size";
            public const string MultipleActiveResultSets = "Multiple Active Result Sets";
        }
        internal DbConnectionFactory(DbConnectorOptions options)
        {
            _options = options;
        }

        public DbConnectionFactory(string connectionString, DbConnectorOptions options=null, bool isReadOnly = true)
        {
            _defaultConnectionString = connectionString;
            _options = options;
            _isReadOnly = isReadOnly;
        }
        /// <summary>
        /// <inheritdoc cref="ConnectionString"/>
        /// </summary>
        public string ConnectionString
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_constructedConnectionString))
                    return _constructedConnectionString;

                if (string.IsNullOrWhiteSpace(_defaultConnectionString))
                    throw new ArgumentNullException($"Cannot access property {nameof(ConnectionString)} when connection string is not initialized");

                SqlConnectionStringBuilder builder = new(_defaultConnectionString);

                // set convention based connection options if not otherwise specified in connection string
                if (!hasProperty(Properties.ApplicationIntent) && !hasProperty(Properties.ApplicationIntentNoSpace))
                {
                    builder.ApplicationIntent = _isReadOnly 
                        ? ApplicationIntent.ReadOnly 
                        : ApplicationIntent.ReadWrite;
                }

                if (!hasProperty(Properties.ApplicationName)) builder.ApplicationName = _options?.ApplicationName ?? builder.ApplicationName;
                if (!hasProperty(Properties.CommandTimeout)) builder.CommandTimeout = _options?.CommandTimeOutSecs ?? builder.CommandTimeout;
                if (!hasProperty(Properties.ConnectRetryCount)) builder.ConnectRetryCount = _options?.ConnectRetryCount ?? builder.ConnectRetryCount;
                if (!hasProperty(Properties.ConnectRetryInterval)) builder.ConnectRetryInterval = _options?.ConnectRetryIntervalSecs ?? builder.ConnectRetryInterval;
                if (!hasProperty(Properties.ConnectTimeout)) builder.ConnectTimeout = _options?.ConnectTimeOutSecs ?? builder.ConnectTimeout;
                if (!hasProperty(Properties.EnablePooling)) builder.Pooling = _options?.EnablePooling ?? builder.Pooling;
                if (!hasProperty(Properties.MaxPoolSize)) builder.MaxPoolSize = _options?.MaxPoolSize ?? builder.MaxPoolSize;
                if (!hasProperty(Properties.MinPoolSize)) builder.MinPoolSize = _options?.MinPoolSize ?? builder.MinPoolSize;
                if (!hasProperty(Properties.MultipleActiveResultSets)) builder.MultipleActiveResultSets = true;

               
                if (builder.ApplicationIntent == ApplicationIntent.ReadOnly && !builder.ApplicationName.Contains(" ro", StringComparison.InvariantCultureIgnoreCase)) //append R/O to application name if not already specified
                {
                    builder.ApplicationName += " ro";
                }
                if (builder.ApplicationIntent == ApplicationIntent.ReadWrite && !builder.ApplicationName.Contains(" rw", StringComparison.InvariantCultureIgnoreCase)) //append R/O to application name if not already specified
                {
                    builder.ApplicationName += " rw";
                }


                _constructedConnectionString = builder.ToString();
                return _constructedConnectionString;

                bool hasProperty(string propertyName)
                {
                    return _defaultConnectionString.Contains(propertyName, StringComparison.InvariantCultureIgnoreCase);
                }
            }
        }


 
        /// <summary>
        /// <inheritdoc cref="IDbConnectionFactory.NewConnection"/>
        /// </summary>
        public SqlConnection NewConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        /// <summary>
        /// <inheritdoc cref="OpenAsync(SqlConnection,System.Threading.CancellationToken)"/>
        /// </summary>
        public async Task<SqlConnection> OpenAsync(SqlConnection connection, CancellationToken cancellationToken = default)
        {
            if (connection == null) throw new ArgumentNullException($"{nameof(connection)}", "Connection is null.  Ensure connection is set before calling this method");
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false); // depend on SqlClient connection retry capability
                return connection;
            }
            return connection;
        }

        /// <summary>
        /// <inheritdoc cref="OpenAsync(System.Threading.CancellationToken)"/>
        /// </summary>
        public async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken = default)
        {
            var cn = NewConnection();
            return await OpenAsync(cn, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<SqlConnection> OpenAsync(SqlCommand command, CancellationToken cancellationToken = default)
        {
            return await OpenAsync(command?.Connection, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <inheritdoc cref="OpenAsync(SqlConnection,System.Threading.CancellationToken)"/>
        /// </summary>
        public SqlConnection Open(SqlConnection connection)
        {
            if (connection == null) throw new ArgumentNullException($"{nameof(connection)}", "Connection is null.  Ensure connection is set before calling this method");
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                connection.Open(); // depend on SqlClient connection retry capability
                return connection;
            }
            return connection;
        }

        /// <summary>
        /// <inheritdoc cref="OpenAsync(System.Threading.CancellationToken)"/>
        /// </summary>
        public SqlConnection Open()
        {
            var cn = NewConnection();
            return Open(cn);
        }

        /// <inheritdoc />
        public SqlConnection Open(SqlCommand command)
        {
            return Open(command?.Connection);
        }

        /// <inheritdoc />
        public async Task<SqlConnection> CloseAsync(SqlConnection connection, CancellationToken cancellationToken = default)
        {
            if (connection == null) return connection;
            if (connection.State != ConnectionState.Closed && connection.State != ConnectionState.Connecting)
            {
                for (int currentAttemptCount = 0; currentAttemptCount < _options.ConnectRetryCount; currentAttemptCount++)
                {
                    try
                    {
                        await connection.CloseAsync().ConfigureAwait(false);
                        break;
                    }
                    catch (SqlException ex) when ((ex.Number == DbExecute.SQL_TIMEOUT_ERROR || ex.Number == DbExecute.SQL_GENERALNETWORK_ERROR) && currentAttemptCount < (_options.ConnectRetryCount ?? DbConnectorOptions.DEFAULT_CONNECT_RETRYCOUNT))
                    {
                        await Task.Delay((_options.ConnectRetryIntervalSecs ?? DbConnectorOptions.DEFAULT_CONNECT_RETRYINTERVAL_SECS) * 1000, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                }
            }
            return connection;
        }

        /// <inheritdoc />
        public async Task<SqlConnection> CloseAsync(SqlCommand command, CancellationToken cancellationToken = default)
        {
            return await CloseAsync(command?.Connection, cancellationToken).ConfigureAwait(false);
        }

        public SqlConnection Close(SqlConnection connection)
        {
            if (connection == null) return connection;
            if (connection.State != ConnectionState.Closed && connection.State != ConnectionState.Connecting)
            {
                for (int currentAttemptCount = 0; currentAttemptCount < _options.ConnectRetryCount; currentAttemptCount++)
                {
                    try
                    {
                        connection.Close();
                        break;
                    }
                    catch (SqlException ex) when ((ex.Number == DbExecute.SQL_TIMEOUT_ERROR || ex.Number == DbExecute.SQL_GENERALNETWORK_ERROR) && currentAttemptCount < (_options.ConnectRetryCount ?? DbConnectorOptions.DEFAULT_CONNECT_RETRYCOUNT))
                    {
                        Thread.Sleep((_options.ConnectRetryIntervalSecs ?? DbConnectorOptions.DEFAULT_CONNECT_RETRYINTERVAL_SECS) * 1000);
                        continue;
                    }
                }
            }
            return connection;
        }

        /// <inheritdoc />
        public SqlConnection Close(SqlCommand command)
        {
            return Close(command?.Connection);
        }
    }
}
