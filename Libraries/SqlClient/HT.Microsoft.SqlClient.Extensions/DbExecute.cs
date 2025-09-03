using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using HT.Microsoft.SqlClient.Extensions.Abstractions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;
using Microsoft.Data.SqlClient;

namespace HT.Microsoft.SqlClient.Extensions
{
    /// <summary>
    /// <inheritdoc cref="IDbExecute"/>
    /// </summary>
    public partial class DbExecute : IDbExecute
    {
        // https://stackoverflow.com/questions/29664/how-to-catch-sqlserver-timeout-exceptions
        public const int SQL_TIMEOUT_ERROR = -2;
        public const int SQL_GENERALNETWORK_ERROR = 11;
        public const int SQL_DEADLOCK = 1205;

        private readonly IDbConnectionFactory _cnFactory;
        private readonly DbConnectorOptions _options;

        internal DbExecute(DbConnectorOptions options, IDbConnectionFactory connectionFactory)
        {
            _cnFactory = connectionFactory;
            _options = options;
        }

        /// <summary>
        /// <inheritdoc cref="IDbExecute.ExecuteScalarAsync{T}"/>
        /// </summary>
        public async Task<T> ExecuteScalarAsync<T>(SqlCommand command, T nullValue = default(T),int? commandTimeOutSecs=null,CancellationToken cancellationToken=default)
        {
            if (command?.Connection == null) throw new ArgumentException($"{nameof(SqlCommand.Connection)}", "Command or Command.Connection is null");
            command.CommandText = command.CommandText.Trim();
            command.CommandTimeout = commandTimeOutSecs ?? command.CommandTimeout;
            var isOriginalConnectionOpen = command.Connection.State == ConnectionState.Open;

            if (!isOriginalConnectionOpen)
            {
                await _cnFactory.OpenAsync(command, cancellationToken).ConfigureAwait(false);
            }
            var currentDelayMs = 0;
            try
            {
                for (int currentAttemptCount = 0; currentAttemptCount < (_options?.CommandRetryCount ?? DbConnectorOptions.DEFAULT_COMMAND_RETRYCOUNT); currentAttemptCount++)
                {
                    try
                    {
                        var dbVal = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                        if (dbVal == DBNull.Value)
                        {
                            return nullValue;
                        }

                        return (T) dbVal;
                    }
                    catch (SqlException ex) when ((ex.Number == SQL_TIMEOUT_ERROR || ex.Number == SQL_GENERALNETWORK_ERROR) && currentAttemptCount < (_options?.CommandRetryCount ?? DbConnectorOptions.DEFAULT_COMMAND_RETRYCOUNT))
                    {
                        currentDelayMs = getNextDelayMs(currentDelayMs, currentAttemptCount, _options);
                        await Task.Delay(currentDelayMs, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                if (!isOriginalConnectionOpen) await _cnFactory.CloseAsync(command, cancellationToken).ConfigureAwait(false);
            }
            return default(T);
        }
        /// <inheritdoc />
        public async Task<SqlDataReader> ExecuteReaderAsync(SqlCommand command, int? commandTimeOutSecs = null, CancellationToken cancellationToken = default)
        {
            if (command?.Connection == null) throw new ArgumentException($"{nameof(SqlCommand.Connection)}", "Command or Command.Connection is null");
            command.CommandText = command.CommandText.Trim();
            command.CommandTimeout = commandTimeOutSecs ?? command.CommandTimeout;

            if (command.Connection.State == ConnectionState.Closed)
            {
                if (_cnFactory != null) await _cnFactory.OpenAsync(command, cancellationToken).ConfigureAwait(false);
            }

            int currentDelayMs = 0;
            for (int currentAttemptCount = 0; currentAttemptCount < (_options?.CommandRetryCount??DbConnectorOptions.DEFAULT_COMMAND_RETRYCOUNT); currentAttemptCount++)
            {

                try
                {
                    return await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (SqlException ex) when ((ex.Number == SQL_TIMEOUT_ERROR || ex.Number == SQL_GENERALNETWORK_ERROR) && currentAttemptCount < (_options?.CommandRetryCount ?? DbConnectorOptions.DEFAULT_COMMAND_RETRYCOUNT))
                {
                    currentDelayMs = getNextDelayMs(currentDelayMs, currentAttemptCount, _options);
                    await Task.Delay(currentDelayMs, cancellationToken).ConfigureAwait(false);
                }
            }
            return default;
        }


    

        private int getNextDelayMs(int currentDelayMs, int currentAttemptCount, DbConnectorOptions options)
        {
            double nextDelaySeconds = (double)currentDelayMs *
                                 (options?.CommandRetryIntervalSecs ??
                                  DbConnectorOptions.DEFAULT_COMMAND_RETRYINTERVAL);

            return Convert.ToInt32(nextDelaySeconds) * 1000;

        }

        /// <inheritdoc/>
        public async Task<List<T>> ExecuteQueryAsync<T>(SqlCommand command, Func<SqlDataReader, T> mapper, int? commandTimeOutSecs = null, CancellationToken cancellationToken = default)
        {
            if (command?.Connection == null) throw new ArgumentException($"{nameof(SqlCommand.Connection)}", "Command or Command.Connection is null");
            command.CommandText = command.CommandText.Trim();
            var isOriginalConnectionOpen = command.Connection.State == ConnectionState.Open;
            command.CommandTimeout = commandTimeOutSecs ?? Math.Max(command.CommandTimeout,60);

            try
            {
                using var dr = await ExecuteReaderAsync(command, commandTimeOutSecs, cancellationToken).ConfigureAwait(false);

                var retList = new List<T>();
                while (await executeReaderAsync(dr,cancellationToken).ConfigureAwait(false))
                {
                    retList.Add(mapper(dr));
                }

                return retList;
            }
            finally
            {
                if (isOriginalConnectionOpen)
                {
                    await _cnFactory.CloseAsync(command, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc/>
        public async Task ExecuteActionAsync(SqlCommand command, Action<SqlDataReader> mapper, int? commandTimeOutSecs = null, CancellationToken cancellationToken = default)
        {
            if (command?.Connection == null) throw new ArgumentException($"{nameof(SqlCommand.Connection)}", "Command or Command.Connection is null");
            command.CommandText = command.CommandText.Trim();
            var isOriginalConnectionOpen = command.Connection.State == ConnectionState.Open;
            command.CommandTimeout = commandTimeOutSecs ?? command.CommandTimeout;

            try
            {
                await using var dr = await ExecuteReaderAsync(command, commandTimeOutSecs, cancellationToken).ConfigureAwait(false);
                
                while (await executeReaderAsync(dr,cancellationToken).ConfigureAwait(false))
                {
                    mapper(dr);
                }

            }
            finally
            {
                if (isOriginalConnectionOpen)
                {
                    await _cnFactory.CloseAsync(command, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<bool> executeReaderAsync(SqlDataReader dr, CancellationToken cancellationToken = default)
        {
            var currentDelayMs = 0;
            for (int currentAttemptCount = 0; currentAttemptCount < (_options?.CommandRetryCount ?? DbConnectorOptions.DEFAULT_COMMAND_RETRYCOUNT); currentAttemptCount++)
            {
                try
                {
                    return await dr.ReadAsync(cancellationToken);
                }
                catch (SqlException ex) when ((ex.Number == SQL_TIMEOUT_ERROR || ex.Number == SQL_GENERALNETWORK_ERROR) && currentAttemptCount < (_options?.CommandRetryCount ?? DbConnectorOptions.DEFAULT_COMMAND_RETRYCOUNT))
                {
                    currentDelayMs = getNextDelayMs(currentDelayMs, currentAttemptCount, _options);
                    await Task.Delay(currentDelayMs, cancellationToken).ConfigureAwait(false);
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public async Task<T> ExecuteRowAsync<T>(SqlCommand command, Func<SqlDataReader, T> mapper, int? commandTimeOutSecs = null, CancellationToken cancellationToken = default)
        {
            if (command?.Connection == null) throw new ArgumentException($"{nameof(SqlCommand.Connection)}", "Command or Command.Connection is null");
            command.CommandText = command.CommandText.Trim();
            command.CommandTimeout = commandTimeOutSecs ?? command.CommandTimeout;
            var isOriginalConnectionOpen = command.Connection.State == ConnectionState.Open;

            try
            {
                using var dr = await ExecuteReaderAsync(command, commandTimeOutSecs, cancellationToken).ConfigureAwait(false);
                while (await executeReaderAsync(dr, cancellationToken).ConfigureAwait(false))
                {
                    return mapper(dr);
                }
                return default(T);
            }
            finally
            {
                if (isOriginalConnectionOpen)
                {
                    await _cnFactory.CloseAsync(command, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        /// <inheritdoc />
        public async Task ExecuteNonQueryAsync(SqlCommand command, int? commandTimeOutSecs = null, CancellationToken cancellationToken = default)
        {
            if (command?.Connection == null) throw new ArgumentException($"{nameof(SqlCommand.Connection)}", "Command or Command.Connection is null");
            command.CommandText = command.CommandText.Trim();
            command.CommandTimeout = commandTimeOutSecs ?? command.CommandTimeout;
            bool isOriginalStateOpen = command.Connection.State == ConnectionState.Open;
            if (!isOriginalStateOpen)
            {
                await _cnFactory.OpenAsync(command, cancellationToken).ConfigureAwait(false);
            }
            var currentDelayMs = 0;
            try
            {
                for (int currentAttemptCount = 0; currentAttemptCount < (_options?.CommandRetryCount ?? DbConnectorOptions.DEFAULT_COMMAND_RETRYCOUNT); currentAttemptCount++)
                {
                    try
                    {
                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                        break;
                    }
                    catch (SqlException ex) when ((ex.Number == SQL_TIMEOUT_ERROR || ex.Number == SQL_GENERALNETWORK_ERROR) && currentAttemptCount < (_options?.CommandRetryCount ?? DbConnectorOptions.DEFAULT_COMMAND_RETRYCOUNT))
                    {
                        currentDelayMs = getNextDelayMs(currentDelayMs, currentAttemptCount, _options);
                        await Task.Delay(currentDelayMs, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                if (!isOriginalStateOpen) await _cnFactory.CloseAsync(command, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
