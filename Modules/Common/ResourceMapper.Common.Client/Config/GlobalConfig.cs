using HT.Microsoft.IConfiguration.Extensions;
using Microsoft.Extensions.Configuration;
using System.Text;
namespace ResourceMapper.Common.Client.Config
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
                DatabaseReadOnly = config!.GetString("ResourceMapper:ConnectionStrings:Database:RO");
                DatabaseReadWrite = config!.GetString("ResourceMapper:ConnectionStrings:Database:RW");
            }
        }
    }
}
