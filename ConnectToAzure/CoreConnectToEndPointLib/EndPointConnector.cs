using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;

namespace CoreConnectToEndPointLib
{
    public class EndPointConnector
    {
        private string ConnectionString;
        private string IotHubD2cEndpoint;

        EventHubClient EventHubClientConnector;


        private async Task ReceiveMessagesFromDeviceAsync(string partitionId, CancellationToken ct)
        {

            var eventHubReceiver = EventHubClientConnector
                .CreateReceiver(
                    PartitionReceiver.DefaultConsumerGroupName, partitionId, PartitionReceiver.EndOfStream);
            while (true)
            {
                if (ct.IsCancellationRequested) break;
                var eventData = await eventHubReceiver.ReceiveAsync(100);
                if (eventData == null) continue;

                foreach (var eventDat in eventData)
                {
                    string data = Encoding.UTF8.GetString(eventDat.Body.Array);
                    Debug.WriteLine("Message received. Partition: {0} Data: '{1}'", partitionId, data);
                    foreach (var receiver in Receivers)
                    {
                        Debug.WriteLine("Processing success: " + receiver(data));
                    }
                }
            }
            Debug.WriteLine("Cancel Receiver task");
        }

        CancellationTokenSource CancelToken = new CancellationTokenSource();

        List<Func<string, bool>> Receivers = new List<Func<string, bool>>();

        public EndPointConnector(string connectionString, string iotHubD2cEndpoint, IEnumerable<Func<string, bool>> receivers = null)
        {
            ConnectionString = connectionString;
            IotHubD2cEndpoint = iotHubD2cEndpoint;
            if (receivers != null)
                Receivers.AddRange(receivers);
        }

        public void addReceiver(IEnumerable<Func<string, bool>> receivers)
        {
            Receivers.AddRange(receivers);
        }
        public void addReceiver(Func<string, bool> receiver)
        {
            Receivers.Add(receiver);
        }

        private bool running = false;
        public async void connect()
        {

            if (running) return;
            running = true;
            CancelToken = new CancellationTokenSource();

            var connectionStringBuilder = new EventHubsConnectionStringBuilder(Config.Instance["ConnectionString"])
            {
                EntityPath = Config.Instance["IotHubD2cEndpoint"]
            };


            EventHubClientConnector = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

            var tasks = new List<Task>();
            var runTimeInformation = await EventHubClientConnector.GetRuntimeInformationAsync();
            foreach (string partitionId in runTimeInformation.PartitionIds)
            {
                Debug.WriteLine("Found partition {0}\n", partitionId);
                tasks.Add(ReceiveMessagesFromDeviceAsync(partitionId, CancelToken.Token));
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
    }
}