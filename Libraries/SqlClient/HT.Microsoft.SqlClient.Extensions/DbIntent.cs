using System.Threading;
using System.Threading.Tasks;
using HT.Microsoft.SqlClient.Extensions.Abstractions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;
using Microsoft.Data.SqlClient;

namespace HT.Microsoft.SqlClient.Extensions
{
    /// <summary>
    /// <inheritdoc cref="IDbIntent"/>
    /// </summary>
    public class DbIntent : IDbIntent
    {
        public DbIntent(string connectionString, DbConnectorOptions options, bool isReadOnly)
        {
            Connection = new DbConnectionFactory(connectionString, options, isReadOnly);
            Command = new DbCommandFactory(Connection, options);
            
        }

        /// <summary>
        /// <inheritdoc cref="IDbIntent.Command"/>
        /// </summary>
        public IDbCommandFactory Command { get; set; }

        /// <summary>
        /// <inheritdoc cref="IDbIntent.Command"/>
        /// </summary>
        public IDbConnectionFactory Connection { get; set; }

        /// <summary>
        /// <inheritdoc cref="IDbIntent.SprocCommand"/>
        /// </summary>
        public SqlCommand SprocCommand(string procName, int? commandTimeOutSecs = null) =>
            Command?.Sproc(procName, commandTimeOutSecs);

        /// <summary>
        /// <inheritdoc cref="IDbIntent.TextCommand"/>
        /// </summary>
        public SqlCommand TextCommand(string nativeSql, int? commandTimeOutSecs = null) =>
            Command?.Text(nativeSql, commandTimeOutSecs);

        /// <summary>
        /// <inheritdoc cref="IDbIntent.NewConnection"/>
        /// </summary>
        /// <returns></returns>
        public SqlConnection NewConnection() => Connection?.NewConnection();

        /// <summary>
        /// <inheritdoc cref="IDbIntent.OpenConnectionAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken=default) => await Connection.OpenAsync(cancellationToken).ConfigureAwait(false);

    }
}
