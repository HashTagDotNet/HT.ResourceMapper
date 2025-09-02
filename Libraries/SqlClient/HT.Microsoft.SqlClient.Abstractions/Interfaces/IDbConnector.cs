using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace HT.Msft.SqlClient.Extensions.Abstractions.Interfaces
{
    /// <summary>
    /// Returns connections or contexts to a database.  Connection might be open or closed depending on connection type or connection reusable state.
    /// </summary>
    public interface IDbConnector
    {
        /// <summary>
        /// Connections to Read-only instance of a database. <inheritdoc cref="IDbIntent"/>
        /// </summary>
        IDbIntent RO { get; set; }
        
        /// <summary>
        /// Connections to Read/Write instance of a database. <inheritdoc cref="IDbIntent"/>
        /// </summary>
        IDbIntent RW { get; set; }

        /// <summary>
        /// <inheritdoc cref="IDbExecute"/>
        /// </summary>
        IDbExecute Execute { get; set; }

        /// <summary>
        /// <inheritdoc cref="IDbConnectionFactory.CloseAsync(Microsoft.Data.SqlClient.SqlCommand,System.Threading.CancellationToken)"/> (convenience method)
        ///</summary>
        Task CloseAsync(SqlCommand command,CancellationToken cancellationToken=default);
        /// <summary>
        /// <inheritdoc cref="IDbConnectionFactory.OpenAsync(Microsoft.Data.SqlClient.SqlCommand,System.Threading.CancellationToken)"/> (convenience method)
        ///</summary>
        Task<SqlConnection> OpenAsync(SqlCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// <inheritdoc cref="IDbConnectionFactory.CloseAsync(Microsoft.Data.SqlClient.SqlConnection,System.Threading.CancellationToken)"/> (convenience method)
        ///</summary>
        Task CloseAsync(SqlConnection connection, CancellationToken cancellationToken = default);

        /// <summary>
        /// <inheritdoc cref="IDbConnectionFactory.OpenAsync(Microsoft.Data.SqlClient.SqlConnection,System.Threading.CancellationToken)"/> (convenience method)
        ///</summary>
        Task<SqlConnection> OpenAsync(SqlConnection connection, CancellationToken cancellationToken = default);

        /// summary
        /// inheritdoc cref="IDbConnectionFactory.Close(Microsoft.Data.SqlClient.SqlCommand,System.Threading.CancellationToken)"/ (convenience method)
        ////summary
         void Close(SqlCommand command);
        /// summary
        /// inheritdoc cref="IDbConnectionFactory.Open(Microsoft.Data.SqlClient.SqlCommand,System.Threading.CancellationToken)"/ (convenience method)
        ////summary
        SqlConnection Open(SqlCommand command);

        /// summary
        /// inheritdoc cref="IDbConnectionFactory.Close(Microsoft.Data.SqlClient.SqlConnection,System.Threading.CancellationToken)"/ (convenience method)
        ////summary
         void Close(SqlConnection connection);

        /// summary
        /// inheritdoc cref="IDbConnectionFactory.Open(Microsoft.Data.SqlClient.SqlConnection,System.Threading.CancellationToken)"/ (convenience method)
        ////summary
        SqlConnection Open(SqlConnection connection);
    }



}

