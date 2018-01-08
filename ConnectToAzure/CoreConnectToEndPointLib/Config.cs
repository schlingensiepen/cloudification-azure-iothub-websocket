using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Xml;


namespace CoreConnectToEndPointLib
{
    public class Config
    {

        public string Prefix = "iothub:";

        private static Config _Instance;
        public static Config Instance => _Instance ?? (_Instance = new Config());

        private IConfigurationRoot _MainConfig;
        private IConfigurationRoot MainConfig => _MainConfig ?? (_MainConfig = loadConfig("main"));

        private IConfigurationRoot _MachineConfig;

        private IConfigurationRoot MachineConfig =>
            _MachineConfig ?? (_MachineConfig = loadConfig(Environment.MachineName));

        IConfigurationRoot loadConfig(string name)
        {
            var configBuilder = new ConfigurationBuilder().AddXmlFile($"{name}.config");
            return configBuilder.Build();
        }

        private Config()
        {
        }

        public string this[string key]
        {
            get
            {
                if (key == null)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Main Config:");
                    foreach (var kvp in MainConfig.AsEnumerable())
                    {
                        sb.AppendLine($"{kvp.Key}:{kvp.Value}");
                    }
                    sb.AppendLine("Machine Config:");
                    foreach (var kvp in MachineConfig.AsEnumerable())
                    {
                        sb.AppendLine($"{kvp.Key}:{kvp.Value}");
                    }
                    return sb.ToString();
                }
                key = Prefix + key;
                string res = MachineConfig[key];
                if (res != null) return res;
                res = MainConfig[key];
                if (res != null) return res;
                return null;
            }
        }
    }
}
