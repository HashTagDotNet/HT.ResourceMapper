using MacroPoint.Azure.ResourceExplorer.SqlClient.Extensions;
using MacroPoint.Azure.ResourceExplorer.SqlClient.Extensions.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MacroPoint.Azure.ResourceExplorer.Common.Server.Config;
using MacroPoint.Azure.ResourceExplorer.Common.Server.Identity;
using MacroPoint.Azure.ResourceExplorer.Common.Server.Resources;
using MacroPoint.Azure.ResourceExplorer.Common.Server.SavedViews;
using MacroPoint.Azure.ResourceExplorer.Common.Server.SavedViews.Interfaces;
using MacroPoint.Azure.ResourceExplorer.Common.Server.Resources.Interfaces;
using MacroPoint.Azure.ResourceExplorer.Common.Server.Settings;
using MacroPoint.Azure.ResourceExplorer.Common.Server.Settings.Interfaces;

namespace MacroPoint.Azure.ResourceExplorer.Common.Server.Utils
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
            services.TryAddScoped<IImportRepository,ImportSqlRepository>();
            services.TryAddScoped<IImportService,ImportService>();
            services.TryAddScoped<IExportRepository,ExportSqlRepository>();
            services.TryAddScoped<IExportService,ExportService>();
            // Singleton: the owner is configuration and cannot vary per request while there is one.
            services.TryAddSingleton<ICurrentIdentity, ConfiguredIdentity>();
            services.TryAddScoped<IClientSettingsRepository, ClientSettingsSqlRepository>();
            services.TryAddScoped<IClientSettingsService, ClientSettingsService>();
            services.TryAddScoped<ISavedViewRepository, SavedViewSqlRepository>();
            services.TryAddScoped<ISavedViewService, SavedViewService>();
            // Per-circuit seam between the grid page and the layout's Saved Views menu.
            services.TryAddScoped<GridViewContext>();
        }
    }
}
