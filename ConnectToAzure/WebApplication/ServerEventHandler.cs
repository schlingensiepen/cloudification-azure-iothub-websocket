using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Net.WebSockets;
using ConnectToEndPointLib;
using Microsoft.Web.WebSockets;

namespace WebApplication
{
    public class ServerEventHandler : WebSocketHandler
    {
        private EndPointConnector _Connector = null;

        private String ConnectionString = getConfig("ConnectionString");            
        private String[] SampleDevices = getConfig("SampleDevices").Split(new []{';'}).Select(s => s.Trim()).ToArray();
        private String IotHubD2cEndpoint = getConfig("IotHubD2cEndpoint");


        static string getConfig(string key)
        {

            var configFileName = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                Environment.MachineName + ".config");
            if (File.Exists(configFileName))
            {
                foreach (var line in File.ReadAllLines(configFileName))
                {
                    if (line.StartsWith(key + "="))
                    {
                        return line.Substring(line.IndexOf("=") + 1);
                    }
                }
            }

            System.Configuration.Configuration rootWebConfig1 =
                System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/");
            if (rootWebConfig1.AppSettings.Settings.Count > 0)
            {
                System.Configuration.KeyValueConfigurationElement customSetting =
                    rootWebConfig1.AppSettings.Settings[key];
                if (customSetting != null)
                    return customSetting.Value;
            }
            return null;
        }



        private EndPointConnector Connector
        {
            get
            {
                return _Connector ?? (_Connector = new EndPointConnector(
                            ConnectionString,
                           IotHubD2cEndpoint,
                           new Func<string, string, bool>[]
                           {
                               (scope, message) =>
                               {
                                   base.Send($"{scope} - {message}");
                                   foreach (var sampleDevice in SampleDevices)
                                   {
                                       if (scope.ToLower().Trim() == sampleDevice.ToString().Trim()) continue;
                                       Connector.sendData(sampleDevice, message);
                                   }
                                   return true;
                               }
                           }
                       ));
            }
        }



        public override void OnOpen()
        {
            Debug.WriteLine("OnOpen");
            base.Send("You connected to the WebSocket at " + DateTime.Now.ToString("s"));
            try
            {
                Connector.connect();
                base.Send("Connected");
            }
            catch (Exception e)
            {
                base.Send(e.ToString());
                throw;
            }
        }

        public override void OnMessage(string message)
        {
            // Echo message
            Debug.WriteLine("OnMessage");
            base.Send(message);
            foreach (var sampleDevice in SampleDevices)
            {
                Connector.sendData(sampleDevice, message);
            }
        }

        public override void OnClose()
        {
            // Free resources, close connections, etc.
            Debug.WriteLine("OnClose");
            base.OnClose();
        }
    }
}