using ResourceMapper.Feature1.Server.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ResourceMapper.Feature1.Server.Utils
{
    public static class RegisterFeature1Services
    {
        public static void AddFeature1Services(this IServiceCollection services)
        {
            services.AddScoped<IDateTimeService, DateTimeService>();
        }
    }
}
