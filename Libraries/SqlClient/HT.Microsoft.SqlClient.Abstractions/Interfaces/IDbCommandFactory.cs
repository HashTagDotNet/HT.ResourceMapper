using Microsoft.Data.SqlClient;

namespace HT.Msft.SqlClient.Extensions.Abstractions.Interfaces
{
    /// <summary>
    /// Generates Read/Write and Read/Only SQL Commands
    /// </summary>
    public interface IDbCommandFactory
    {
        /// <summary>
        /// Generate a command configured for calling a stored procedure. It is the caller's responsibility to dispose of returned instance.
        /// </summary>
        /// <param name="procName"></param>
        /// <param name="commandTimeOutSecs">Overrides default/configured command timeout. (default: 30 seconds)</param>
        SqlCommand Sproc(string procName, int? commandTimeOutSecs=null);

        /// <summary>
        /// Generate a command configured for using native SQL. It is the caller's responsibility to dispose of returned instance.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="commandTimeOutSecs">Overrides default/configured command timeout. (default: 30 seconds)</param>
        SqlCommand Text(string sql, int? commandTimeOutSecs=null);
    }
}
