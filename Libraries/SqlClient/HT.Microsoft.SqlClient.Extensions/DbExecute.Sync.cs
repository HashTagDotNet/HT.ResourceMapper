using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using HT.Msft.SqlClient.Extensions.Abstractions;
using HT.Msft.SqlClient.Extensions.Abstractions.Interfaces;
using Microsoft.Data.SqlClient;

namespace HT.Msft.SqlClient.Extensions
{
    public partial class DbExecute
    {
        /// <inheritdoc />
        public List<T> ExecuteQuery<T>(SqlCommand command, Func<SqlDataReader, T> mapper, int? commandTimeOutSecs = null)
        {
            if (command?.Connection == null) throw new ArgumentException($"{nameof(SqlCommand.Connection)}", "Command or Command.Connection is null");
            var isOriginalConnectionOpen = command.Connection.State == ConnectionState.Open;
            command.CommandTimeout = commandTimeOutSecs ?? command.CommandTimeout;

            try
            {
                using var dr = ExecuteReader(command, commandTimeOutSecs);

                var retList = new List<T>();
                while (executeReader(dr))
                {
                    retList.Add(mapper(dr));
                }

                return retList;
            }
            finally
            {
                if (isOriginalConnectionOpen)
                {
                    _cnFactory.Close(command).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc />
        public void ExecuteNonQuery(SqlCommand command, int? commandTimeOutSecs = null)
        {
            if (command?.Connection == null) throw new ArgumentException($"{nameof(SqlCommand.Connection)}", "Command or Command.Connection is null");
            command.CommandTimeout = commandTimeOutSecs ?? command.CommandTimeout;
            bool isOriginalStateOpen = command.Connection.State == ConnectionState.Open;
            if (!isOriginalStateOpen)
            {
                _cnFactory.Open(command);
            }
            var currentDelayMs = 0;
            try
            {
                for (int currentAttemptCount = 0; currentAttemptCount < (_options.CommandRetryCount ?? DbConnectorOptions.DEFAULT_COMMAND_RETRYCOUNT); currentAttemptCount++)
                {
                    try
                    {
                        command.ExecuteNonQuery();
                        break;
                    }
                    catch (SqlException ex) when ((ex.Number == SQL_TIMEOUT_ERROR || ex.Number == SQL_GENERALNETWORK_ERROR) && currentAttemptCount < (_options.CommandRetryCount ?? DbConnectorOptions.DEFAULT_COMMAND_RETRYCOUNT))
                    {
                        currentDelayMs = getNextDelayMs(currentDelayMs, currentAttemptCount, _options);
                        Thread.Sleep(currentDelayMs);
                    }
                }
            }
            finally
            {
                if (!isOriginalStateOpen) _cnFactory.Close(command).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public T ExecuteRow<T>(SqlCommand command, Func<SqlDataReader, T> mapper, int? commandTimeOutSecs = null)
        {
            if (command?.Connection == null) throw new ArgumentException($"{nameof(SqlCommand.Connection)}", "Command or Command.Connection is null");
            command.CommandTimeout = commandTimeOutSecs ?? command.CommandTimeout;
            var isOriginalConnectionOpen = command.Connection.State == ConnectionState.Open;

            try
            {
                using var dr = ExecuteReader(command, commandTimeOutSecs);
                while (executeReader(dr))
                {
                    return mapper(dr);
                }
                return default(T);
            }
            finally
            {
                if (isOriginalConnectionOpen)
                {
                    _cnFactory.Close(command);
                }
            }
        }

        /// <summary>
        /// <inheritdoc cref="IDbExecute.ExecuteScalarAsync{T}"/>
        /// </summary>
        public T ExecuteScalar<T>(SqlCommand command, T nullValue = default(T), int? commandTimeOutSecs = null)
        {
            if (command?.Connection == null) throw new ArgumentException($"{nameof(SqlCommand.Connection)}", "Command or Command.Connection is null");
            command.CommandTimeout = commandTimeOutSecs ?? command.CommandTimeout;
            var isOriginalConnectionOpen = command.Connection.State == ConnectionState.Open;

            if (!isOriginalConnectionOpen)
            {
                _cnFactory.Open(command);
            }
            var currentDelayMs = 0;
            try
            {
                for (int currentAttemptCount = 0; currentAttemptCount < (_options.CommandRetryCount ?? DbConnectorOptions.DEFAULT_COMMAND_RETRYCOUNT); currentAttemptCount++)
                {
                    try
                    {
                        var dbVal = command.ExecuteScalar();
                        if (dbVal == DBNull.Value)
                        {
                            return nullValue;
                        }

                        return (T)dbVal;
                    }
                    catch (SqlException ex) when ((ex.Number == SQL_TIMEOUT_ERROR || ex.Number == SQL_GENERALNETWORK_ERROR) && currentAttemptCount < (_options.CommandRetryCount ?? DbConnectorOptions.DEFAULT_COMMAND_RETRYCOUNT))
                    {
                        currentDelayMs = getNextDelayMs(currentDelayMs, currentAttemptCount, _options);
                        Thread.Sleep(currentDelayMs);
                    }
                }
            }
            finally
            {
                if (!isOriginalConnectionOpen) _cnFactory.Close(command);
            }
            return default(T);
        }

        /// <inheritdoc />
        public SqlDataReader ExecuteReader(SqlCommand command, int? commandTimeOutSecs = null)
        {
            if (command?.Connection == null) throw new ArgumentException($"{nameof(SqlCommand.Connection)}", "Command or Command.Connection is null");
            command.CommandTimeout = commandTimeOutSecs ?? command.CommandTimeout;

            if (command.Connection.State == ConnectionState.Closed)
            {
                if (_cnFactory != null) _cnFactory.Open(command).ConfigureAwait(false);
            }

            int currentDelayMs = 0;
            for (int currentAttemptCount = 0; currentAttemptCount < (_options.CommandRetryCount ?? DbConnectorOptions.DEFAULT_COMMAND_RETRYCOUNT); currentAttemptCount++)
            {
                try
                {
                    return command.ExecuteReader();
                }
                catch (SqlException ex) when ((ex.Number == SQL_TIMEOUT_ERROR || ex.Number == SQL_GENERALNETWORK_ERROR) && currentAttemptCount < (_options.CommandRetryCount ?? DbConnectorOptions.DEFAULT_COMMAND_RETRYCOUNT))
                {
                    currentDelayMs = getNextDelayMs(currentDelayMs, currentAttemptCount, _options);
                    Thread.Sleep(currentDelayMs);
                }
            }
            return default;
        }

        private bool executeReader(SqlDataReader dr)
        {
            var currentDelayMs = 0;
            for (int currentAttemptCount = 0; currentAttemptCount < (_options.CommandRetryCount ?? DbConnectorOptions.DEFAULT_COMMAND_RETRYCOUNT); currentAttemptCount++)
            {
                try
                {
                    return dr.Read();
                }
                catch (SqlException ex) when ((ex.Number == SQL_TIMEOUT_ERROR || ex.Number == SQL_GENERALNETWORK_ERROR) && currentAttemptCount < (_options.CommandRetryCount ?? DbConnectorOptions.DEFAULT_COMMAND_RETRYCOUNT))
                {
                    currentDelayMs = getNextDelayMs(currentDelayMs, currentAttemptCount, _options);
                    Thread.Sleep(currentDelayMs);
                }
            }
            return false;
        }
    }
}
