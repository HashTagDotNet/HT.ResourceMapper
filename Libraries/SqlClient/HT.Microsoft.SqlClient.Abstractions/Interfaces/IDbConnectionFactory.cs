using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces
{
    /// <summary>
    /// Create, open, and close SQL connections to the database
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary>
        /// Open a connection, retrying up to configured number of times (default: 3).  Already open connections are ignored.   Call performs retry using a simple linear back off strategy.
        /// </summary>
        /// <param name="connection">Connection to open.  If already open, do not reopen</param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentNullException">When <paramref name="connection"/> is null</exception>
        /// <exception cref="SqlException">When connection fails to open after maximum retries</exception>
        Task<SqlConnection> OpenAsync(SqlConnection connection,CancellationToken cancellationToken=default);
        
        /// <summary>
        /// Returns a new, opened connection.  It is the caller's responsibility to dispose of this instance.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SqlConnection> OpenAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Open the connection set in this command, retrying up to a configured number of times (default: 3).  Already open connections are ignored.   Call performs retry using a simple linear back off strategy and configured
        /// on SqlConnection
        /// </summary>
        /// <param name="command">SqlCommand with embedded connection</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">When <paramref name="command"/> is null or embedded connection is null</exception>
        /// <exception cref="SqlException">When connection fails to open after maximum retries</exception>
        Task<SqlConnection> OpenAsync(SqlCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Open a connection, retrying up to configured number of times (default: 3).  Already open connections are ignored.   Call performs retry using a simple linear back off strategy.
        /// </summary>
        /// <param name="connection">Connection to open.  If already open, do not reopen</param>
        /// <exception cref="ArgumentNullException">When <paramref name="connection"/> is null</exception>
        /// <exception cref="SqlException">When connection fails to open after maximum retries</exception>
        SqlConnection Open(SqlConnection connection);

        /// <summary>
        /// Returns a new, opened connection.  It is the caller's responsibility to dispose of this instance.
        /// </summary>
        /// <returns></returns>
        SqlConnection Open();
        /// <summary>
        /// Open the connection set in this command, retrying up to a configured number of times (default: 3).  Already open connections are ignored.   Call performs retry using a simple linear back off strategy and configured
        /// on SqlConnection
        /// </summary>
        /// <param name="command">SqlCommand with embedded connection</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">When <paramref name="command"/> is null or embedded connection is null</exception>
        /// <exception cref="SqlException">When connection fails to open after maximum retries</exception>
        SqlConnection Open(SqlCommand command);

        /// <summary>
        /// Returns a new, closed connection.  It is the caller's responsibility to dispose of this instance.
        /// </summary>
        SqlConnection NewConnection();

        

        /// <summary>
        /// Close an open connection, retrying up to a configured number of times (default: 3).  Non-open connections are ignored.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="SqlException">When connection fails to open after maximum retries</exception>
        Task<SqlConnection> CloseAsync(SqlConnection connection,CancellationToken cancellationToken=default);

        /// <summary>
        /// Close the connection embedded into <paramref name="command"/>.  Already open connections are ignored.
        /// </summary>
        /// <param name="command">Hydrated command whose connection should be closed</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="SqlException">When connection fails to open after maximum retries</exception>
        Task<SqlConnection> CloseAsync(SqlCommand command, CancellationToken cancellationToken=default);

        /// <summary>
        /// Close an open connection, retrying up to a configured number of times (default: 3).  Non-open connections are ignored.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// <exception cref="SqlException">When connection fails to open after maximum retries</exception>
        SqlConnection Close(SqlConnection connection);

        /// <summary>
        /// Close the connection embedded into <paramref name="command"/>.  Already open connections are ignored.
        /// </summary>
        /// <param name="command">Hydrated command whose connection should be closed</param>
        /// <returns></returns>
        /// <exception cref="SqlException">When connection fails to open after maximum retries</exception>
        SqlConnection Close(SqlCommand command);
        /// <summary>
        /// Returns the resolve connection string (a connection string with dbOptions applied).
        /// </summary>
        string ConnectionString { get; }
    }
}
