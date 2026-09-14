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
