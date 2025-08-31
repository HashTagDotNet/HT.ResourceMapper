using Microsoft.Extensions.DependencyInjection;

namespace HT.ResourceMapper.Server.Utils
{
    public static class DependencyInjectorServer
    {
        public static IServiceCollection MapDependencies(this IServiceCollection services)
        {
            return services;
        }
    }
}
