using Microsoft.Data.SqlClient;

namespace HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces
{
    /// <summary>
    /// Generate a stored procedure or text command
    /// </summary>
    public interface IDbCommandSet
    {
        /// <summary>
        /// Returned command is a stored procedure command
        /// </summary>
        /// <param name="storedProcedureName"></param>
        /// <returns></returns>
        public SqlCommand Sproc(string storedProcedureName);

        /// <summary>
        /// Returned command is a SQL text command
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public SqlCommand Text(string sql);
    }
}
