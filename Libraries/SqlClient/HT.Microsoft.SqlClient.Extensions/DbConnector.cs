using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using HT.Msft.SqlClient.Extensions.Abstractions;
using HT.Msft.SqlClient.Extensions.Abstractions.Interfaces;
using Microsoft.Data.SqlClient;

namespace HT.Msft.SqlClient.Extensions
{
    /// <summary>
    /// <inheritdoc cref="IDbConnector"/>
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Simple class")]
    public class DbConnector : IDbConnector
    {
        /// <summary>
        /// Partial connection to support wrapper methods
        /// </summary>
        private readonly DbConnectionFactory _connection;

        public DbConnector(string defaultConnectionString, DbConnectorOptions options=null) : this(defaultConnectionString, null, options)
        {

        }
        public DbConnector(string readWriteConnectionString, string readOnlyConnectionString, DbConnectorOptions options=null)
        {
            options ??= new DbConnectorOptions();
            RO = new DbIntent(readOnlyConnectionString ?? readWriteConnectionString, options, true);
            RW = new DbIntent(readWriteConnectionString ?? readOnlyConnectionString, options, false);
            _connection = new DbConnectionFactory(options);
            Execute = new DbExecute(options, _connection);
        }
        
        /// <summary>
        /// <inheritdoc cref="IDbConnector.RO"/>
        /// </summary>
        public IDbIntent RO { get; set; }

        /// <summary>
        /// <inheritdoc cref="IDbConnector.RW"/>
        /// </summary>
        public IDbIntent RW { get; set; }

        /// <summary>
        /// <inheritdoc cref="IDbConnector.Execute"/>
        /// </summary>
        public IDbExecute Execute { get; set; }

        /// <summary>
        /// <inheritdoc cref="IDbConnector.CloseAsync(SqlCommand,System.Threading.CancellationToken)"/>
        /// </summary>
        public async Task CloseAsync(SqlCommand command, CancellationToken cancellationToken = default)
        {
            await _connection.CloseAsync(command, cancellationToken);
        }
        /// <summary>
        /// <inheritdoc cref="IDbConnector.CloseAsync(SqlConnection,System.Threading.CancellationToken)"/> (convenience method)
        /// </summary>
        public async Task CloseAsync(SqlConnection connection, CancellationToken cancellationToken = default)
        {
            await _connection.CloseAsync(connection, cancellationToken);
        }

        /// <summary>
        /// <inheritdoc cref="IDbConnector.OpenAsync(SqlCommand,System.Threading.CancellationToken)"/> (convenience method)
        /// </summary>
        public async Task<SqlConnection> OpenAsync(SqlCommand command, CancellationToken cancellationToken = default)
        {
            return await _connection.OpenAsync(command, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <inheritdoc cref="IDbConnector.OpenAsync(SqlConnection,System.Threading.CancellationToken)"/> (convenience method)
        /// </summary>
        public async Task<SqlConnection> OpenAsync(SqlConnection connection, CancellationToken cancellationToken = default)
        {
            return await _connection.OpenAsync(connection, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Close(SqlCommand command)
        {
            _connection.Close(command);
        }

        /// <inheritdoc />
        public SqlConnection Open(SqlCommand command)
        {
            return _connection.Open(command);
        }

        /// <inheritdoc />
        public void Close(SqlConnection connection)
        {
            _connection.Close(connection);
        }

        /// <inheritdoc />
        public SqlConnection Open(SqlConnection connection)
        {
            return _connection.Open(connection);
        }
    }


}
