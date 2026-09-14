using HT.Microsoft.SqlClient.Extensions;
using HT.Microsoft.SqlClient.Extensions.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ResourceMapper.Common.Server.Config;
using ResourceMapper.Common.Server.Explorer;
using ResourceMapper.Common.Server.Identity;
using ResourceMapper.Common.Server.Explorer.Interfaces;
using ResourceMapper.Common.Server.Resources;
using ResourceMapper.Common.Server.SavedViews;
using ResourceMapper.Common.Server.SavedViews.Interfaces;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Server.Settings;
using ResourceMapper.Common.Server.Settings.Interfaces;

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
            services.TryAddScoped<IExplorerRepository, ExplorerSqlRepository>();
            services.TryAddScoped<IExplorerService, ExplorerService>();
            services.TryAddScoped<IDiagramRepository, DiagramSqlRepository>();
            services.TryAddScoped<IDiagramService, DiagramService>();
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
        }
    }
}
