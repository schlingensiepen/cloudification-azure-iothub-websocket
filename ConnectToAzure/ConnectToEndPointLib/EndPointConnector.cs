using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.ServiceBus.Messaging;


namespace ConnectToEndPointLib
{
    public class EndPointConnector
    {
        private string ConnectionString;
        private string IotHubD2cEndpoint;

        EventHubClient EventHubClient;
        ServiceClient ServiceClient;


        private async Task ReceiveMessagesFromDeviceAsync(string partition, CancellationToken ct)
        {
            try
            {
            processData("internal", $"Start listening to Partition: {partition}");
            var eventHubReceiver = EventHubClient.GetDefaultConsumerGroup().CreateReceiver(partition, DateTime.UtcNow);
            while (true)
            {
                if (ct.IsCancellationRequested) break;
                EventData eventData = await eventHubReceiver.ReceiveAsync();
                if (eventData == null) continue;

                string data = Encoding.UTF8.GetString(eventData.GetBytes());
                string device = eventData.SystemProperties.ContainsKey("iothub-connection-device-id")
                    ? eventData.SystemProperties["iothub-connection-device-id"].ToString().Trim()
                    : null;
                Debug.WriteLine("Message received. Partition: {0} Data: '{1}'", partition, data);
                processData(device, data);
            }
            Debug.WriteLine("Cancel Receiver task");
            }
            catch (Exception ex)
            {
                processData("internal - exception", ex.ToString());
            }
        }

        private void processData(string scope, string data)
        {
            foreach (var receiver in Receivers)
            {
                Task.Run(() => receiver(scope, data));                
            }
        }

        CancellationTokenSource CancelToken = new CancellationTokenSource();

        List<Func<string, string, bool>> Receivers = new List<Func<string, string, bool>>();

        public EndPointConnector(
            string connectionString, 
            string iotHubD2cEndpoint, 
            IEnumerable<Func<string, string, bool>> receivers = null)
        {
            ConnectionString = connectionString;
            IotHubD2cEndpoint = iotHubD2cEndpoint;
            if (receivers != null)
                Receivers.AddRange(receivers);
        }

        public void addReceiver(IEnumerable<Func<string, string, bool>> receivers)
        {
            Receivers.AddRange(receivers.Select(r =>
            {
                r("internal", "Add as receiver");
                return r;
            }));
        }
        public void addReceiver(Func<string, string, bool> receiver)
        {
            receiver("internal", "Add as receiver");
            Receivers.Add(receiver);
        }

        private bool running = false;
        public void connect()
        {
            processData("internal","Connect Entered - running " + running.ToString());
            if (running) return;
            running = true;
            CancelToken = new CancellationTokenSource();
            try
            {

                processData("internal", "Try ServiceClient.CreateFromConnectionString");
                ServiceClient = ServiceClient.CreateFromConnectionString(ConnectionString);
                processData("internal", "ServiceClient created");

                processData("internal", "Try EventHubClient.CreateFromConnectionString");
                EventHubClient = EventHubClient.CreateFromConnectionString(ConnectionString, IotHubD2cEndpoint);
                processData("internal", "EventHubClient created");

                var d2cPartitions = EventHubClient.GetRuntimeInformation().PartitionIds;

                var tasks = new List<Task>();
                foreach (string partition in d2cPartitions)
                {
                    Debug.WriteLine("Found partition {0}\n", partition);
                    tasks.Add(ReceiveMessagesFromDeviceAsync(partition, CancelToken.Token));
                }
            }
            catch (Exception ex)
            {
                processData("internal - exception", ex.ToString());
            }
            //Task.WaitAll(tasks.ToArray());
        }

        public void disConnect()
        {
            if (!running) return;
            running = false;
            CancelToken.Cancel();
            Debug.WriteLine("Cancel");
        }


        private int sendCounter = 0;

        public async void  sendDataInternal(string device, string data)
        {
            Debug.WriteLine($"Transfer to {device} - {data}");
            var commandMessage = new Message(Encoding.UTF8.GetBytes(data));            
            while (ServiceClient == null)
            {
                await Task.Delay(8);
            }
            try
            {
                await ServiceClient.SendAsync(device, commandMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            Debug.WriteLine($"Transfered to {device} - {data}");
        }

        public bool sendData(string device, string data)
        {
            if (!running) return false;
            Task.Run(() => sendDataInternal(device, data));
            return true;
        }

    }
}