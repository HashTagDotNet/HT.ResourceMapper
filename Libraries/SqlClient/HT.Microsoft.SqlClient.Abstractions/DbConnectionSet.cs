using HT.Msft.SqlClient.Extensions.Abstractions.Interfaces;
using Microsoft.Data.SqlClient;

namespace HT.Msft.SqlClient.Extensions.Abstractions
{
    /// <summary>
    /// Generate a new, closed, <see cref="SqlConnection"/> based on <see cref="ConnectionString"/>. Caller is responsible for disposing returned connection.
    /// </summary>
    public class DbConnectionSet : IDbConnectionSet
    {
        public SqlConnection Connection => new SqlConnection(ConnectionString);
        public string ConnectionString { get; set; }
    }
}
