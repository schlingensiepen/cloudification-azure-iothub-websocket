using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace SimulatedDevice
{
    public class Connector
    {
        static string iotHubUri = "i3cmhub.azure-devices.net";
        static string deviceKey = "D2VvUmrFbHIS9YEdXa2hmIE7N7f68qLeiOLN4llFKOI=";
        static string deviceName = "simulated";
        static DeviceClient deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceName, deviceKey), TransportType.Mqtt);

        public static async void SendDeviceToCloudMessagesAsync()
        {
            deviceClient.ProductInfo = "HappyPath_Simulated-CSharp";
            double minTemperature = 20;
            double minHumidity = 60;
            int messageId = 1;
            Random rand = new Random();

            double currentTemperature = minTemperature + rand.NextDouble() * 15;
            double currentHumidity = minHumidity + rand.NextDouble() * 20;

            var telemetryDataPoint = new
            {
                messageId = messageId++,
                deviceId = deviceName,
                temperature = currentTemperature,
                humidity = currentHumidity
            };
            var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
            var message = new Message(Encoding.ASCII.GetBytes(messageString));
            message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

            await deviceClient.SendEventAsync(message);
            Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
        }


        public static void Send()
        {
            Console.WriteLine("Send message");
            Task.Run(() => SendDeviceToCloudMessagesAsync());
        }

    }
}
