using MacroPoint.Azure.ResourceExplorer.Configuration.Extensions;
using Microsoft.Extensions.Configuration;

namespace MacroPoint.Azure.ResourceExplorer.Common.Server.Config
{
    public class GlobalConfig
    {
        public ConnectionStringsConfig ConnectionStrings { get; set; }
        public GlobalConfig(IConfiguration config)
        {
            ConnectionStrings = new ConnectionStringsConfig(config);
            AzureIds = new AzureIdsConfig(config);
        }
        public AzureIdsConfig AzureIds { get; set; }

        /// <summary>Legacy markers for the old-vs-new Azure id rule, and the quick-add default domain.</summary>
        public class AzureIdsConfig
        {
            /// <summary>Comma-separated subscription ids whose resources are legacy ("Old").</summary>
            public string[] LegacySubscriptionIds { get; set; }
            /// <summary>Comma-separated resource-name / resource-group prefixes that mark a legacy ("Old") resource.</summary>
            public string[] LegacyNamePrefixes { get; set; }
            /// <summary>Domain value quick add assigns when the user does not pick one; empty when unset.</summary>
            public string DefaultDomain { get; set; }
            public AzureIdsConfig(IConfiguration config)
            {
                LegacySubscriptionIds = config.GetStringArray("MacroPoint:Azure:ResourceExplorer:AzureIds:LegacySubscriptionIds");
                LegacyNamePrefixes = config.GetStringArray("MacroPoint:Azure:ResourceExplorer:AzureIds:LegacyNamePrefixes");
                DefaultDomain = config.GetString("MacroPoint:Azure:ResourceExplorer:DefaultDomain", string.Empty);
            }
        }
        public class ConnectionStringsConfig
        {
            public string DatabaseReadOnly { get; set; }
            public string DatabaseReadWrite { get; set; }
            public ConnectionStringsConfig(IConfiguration config)
            {
                DatabaseReadOnly = config!.GetString("MacroPoint:Azure:ResourceExplorer:ConnectionStrings:Database:RO");
                DatabaseReadWrite = config!.GetString("MacroPoint:Azure:ResourceExplorer:ConnectionStrings:Database:RW");
            }
        }
    }
}
