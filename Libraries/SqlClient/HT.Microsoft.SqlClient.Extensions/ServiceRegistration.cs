using HT.Microsoft.SqlClient.Extensions.Abstractions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable InconsistentNaming

namespace HT.Microsoft.SqlClient.Extensions
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddHTSqlClient(this IServiceCollection services, string rwConnectionString, string roConnectionString, DbConnectorOptions options)
        {
            services.TryAddSingleton<IDbConnector>(new DbConnector(rwConnectionString, roConnectionString, options));

            return services;
        }
        public static IServiceCollection AddHTSqlClient(this IServiceCollection services, string connectionString, DbConnectorOptions options)
        {
            services.TryAddSingleton<IDbConnector>(new DbConnector(connectionString, options));

            return services;
        }
    }
}
