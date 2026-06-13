using HT.Microsoft.SqlClient.Extensions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ResourceMapper.Common.Server.Config;
using ResourceMapper.Common.Server.Resources;
using ResourceMapper.Common.Server.Resources.Interfaces;

namespace ResourceMapper.Common.Server.Utils
{
    public static class DependencyRegistration
    {
        public static void RegisterCommonDependencies(this IServiceCollection services)
        {
          
            services.TryAddSingleton(typeof(GlobalConfig));
            services.TryAddSingleton<IDbConnector>(services =>
            {
                var cfg = services.GetRequiredService<GlobalConfig>();
                return new DbConnector(cfg.ConnectionStrings.DatabaseReadWrite,cfg.ConnectionStrings.DatabaseReadWrite);
            });

            services.TryAddScoped<IResourceRepository,ResourceSqlRepository>();
            services.TryAddScoped<IResourceService,ResourceService>();
            services.TryAddScoped<IImportService,ImportService>();
        }
    }
}
