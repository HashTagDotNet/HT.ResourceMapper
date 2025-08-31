using Microsoft.Extensions.DependencyInjection;

namespace HT.ResourceMapper.Client.Utils
{
    public static class DependencyInjectorMapper
    {
        public static IServiceCollection MapDependencies(this IServiceCollection services)
        {
            return services;
        }
    }
}
