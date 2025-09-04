using HT.Microsoft.SqlClient.Extensions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ResourceMapper.Common.Server.Config;
using ResourceMapper.Common.Server.Sample;
using ResourceMapper.Common.Server.Sample.Interfaces;

namespace ResourceMapper.Common.Server.Utils
{
    public static class DependencyRegistration
    {
        public static void RegisterCommonDependencies(this IServiceCollection services)
        {
          
            services.TryAddSingleton(typeof(GlobalConfig));

            services.AddTransient<ISampleService, SampleService>();
            services.AddTransient<ISampleRepository, SampleSqlRepository>();

            
            services.TryAddSingleton<IDbConnector>(services =>
            {
                var cfg = services.GetRequiredService<GlobalConfig>();
                return new DbConnector(cfg.ConnectionStrings.DatabaseReadWrite,cfg.ConnectionStrings.DatabaseReadWrite);
            });
        }
    }
}
