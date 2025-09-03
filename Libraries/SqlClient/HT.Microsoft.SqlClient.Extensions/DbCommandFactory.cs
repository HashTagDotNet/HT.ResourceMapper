using System.Data;
using HT.Microsoft.SqlClient.Extensions.Abstractions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;
using Microsoft.Data.SqlClient;

namespace HT.Microsoft.SqlClient.Extensions
{
    /// <inheritdoc/>
    public class DbCommandFactory : IDbCommandFactory
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly DbConnectorOptions _dbConnectorOptions;

        internal DbCommandFactory(IDbConnectionFactory cnFactory, DbConnectorOptions dbConnectorOptions)
        {
            _connectionFactory = cnFactory;
            _dbConnectorOptions = dbConnectorOptions;
        }

        /// <summary>
        /// <inheritdoc cref="Sproc"/>
        /// </summary>
        public SqlCommand Sproc(string procName,int? commandTimeOutSecs)
        {
            var cmd = _connectionFactory.NewConnection().CreateCommand();
            cmd.CommandText = procName;
            cmd.CommandTimeout = commandTimeOutSecs ?? cmd.CommandTimeout;
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Disposed += cmd_Disposed;
            return cmd;
        }

        private void cmd_Disposed(object sender, System.EventArgs e)
        {
            var cmd = sender as SqlCommand;
            if (cmd?.Connection != null && cmd.Connection.State == ConnectionState.Open)
            {
                try
                {
                    cmd.Connection.Close();
                    cmd.Connection.Dispose();
                    cmd.Disposed -= cmd_Disposed;
                }
                catch
                {
#if DEBUG
                    throw;
#endif
                    // ignore close exceptions and let operating system handle it
                }
            }
        }

        /// <summary>
        /// <inheritdoc cref="Text"/>
        /// </summary>
        public SqlCommand Text(string sql,int? commandTimeOutSecs)
        {
            var cmd = _connectionFactory.NewConnection().CreateCommand();
            cmd.CommandText = sql;
            
            cmd.CommandTimeout = commandTimeOutSecs ?? cmd.CommandTimeout;
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.Disposed += cmd_Disposed;
            
            return cmd;
        }
        

    }
}
