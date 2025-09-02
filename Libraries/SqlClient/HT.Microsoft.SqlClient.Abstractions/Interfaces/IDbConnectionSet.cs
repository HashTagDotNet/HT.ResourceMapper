using Microsoft.Data.SqlClient;

namespace HT.Msft.SqlClient.Extensions.Abstractions.Interfaces
{

    /// <summary>
    /// Generate a new <see cref="SqlConnection"/> from a <see cref="ConnectionString"/>.
    /// </summary>
    public interface IDbConnectionSet
    {
        /// <summary>
        /// Return a new SqlConnection instance to <see cref="ConnectionString"/>.  It is the caller's responsiblity to dispose of this instance
        /// </summary>
        SqlConnection Connection { get; }
        string ConnectionString { get; set; }
    }
}
