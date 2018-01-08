using ConnectToEndPointLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConnectToAzure
{
    public partial class Form1 : Form
    {

        string ConnectionString = "";
        string IotHubD2cEndpoint = "";

        string getConfig(string key)
        {
            string configFileName =
                Path.Combine(
                    Path.GetDirectoryName(Application.ExecutablePath),
                    Environment.MachineName + ".config");
            if (File.Exists(configFileName))
            {
                ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
                configMap.ExeConfigFilename = configFileName;
                Configuration config =
                    ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
                if (config.AppSettings.Settings.AllKeys.Contains(key))
                    return config.AppSettings.Settings[key].Value;
            }
            return ConfigurationManager.AppSettings[key];
        }

        public Form1()
        {
            InitializeComponent();
            button1.Enabled = true;
            button2.Enabled = false;
            ConnectionString = getConfig("ConnectionString");
            Console.WriteLine("Using ConnectionString:" + ConnectionString);
            IotHubD2cEndpoint = getConfig("IotHubD2cEndpoint");
            Console.WriteLine("Using IotHubD2cEndpoint:" + IotHubD2cEndpoint);
        }

        private EndPointConnector _Connector = null;

        private EndPointConnector Connector
        {
            get
            {
                return _Connector ?? (_Connector = new EndPointConnector(ConnectionString, IotHubD2cEndpoint,
                           new Func<string, string, bool>[]
                           {
                               (scope, message) =>
                               {
                                   Console.WriteLine($"Receive [{scope}] {message}");
                                   return true;
                               }
                           }
                       ));
            }
        } 

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = true;
            Connector.connect();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = true;
            button2.Enabled = false;
            Connector.disConnect();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            Connector.sendData(textBox1.Text, textBox2.Text);
        }
    }
}
