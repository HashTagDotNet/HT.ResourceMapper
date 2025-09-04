using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HT.Microsoft.SqlClient.Extensions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ResourceMapper.Common.Client.Config;
using ResourceMapper.Common.Client.Sample;
using ResourceMapper.Common.Client.Sample.Interfaces;

namespace ResourceMapper.Common.Client.Utils
{
    public static class DependencyRegistration
    {
        public static void RegisterCommonDependencies(this IServiceCollection services)
        {
            // Register other dependencies here
            services.TryAddSingleton(typeof(GlobalConfig));

            services.AddTransient<ISampleService, SampleService>();
            services.AddTransient<ISampleRepository, SampleSqlRepository>();

            services.AddHTSqlClient()
            services.TryAddSingleton<IDbConnector>(services =>
            {

            });
        }
    }
}
