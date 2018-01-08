using System;
using CoreConnectToEndPointLib;

namespace CoreConnectToEndPointConsole
{
    class Program
    {
        private static EndPointConnector _Connector = null;

        private static EndPointConnector Connector
        {
            get
            {
                return _Connector ?? (_Connector = new EndPointConnector(
                    "Endpoint=sb://i3cmhub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=YbTeJ1rqqtsd/VDY4WDgqPNYCEiLNWGnmiIBYredGoY=",
                    "messages/events",
                           new Func<string, bool>[]
                           {
                               (s) =>
                               {
                                   Console.WriteLine("Receive " + s);
                                   return true;
                               }
                           }
                       ));
            }
        }

        static void Main(string[] args)
        {
            Connector.connect();
            Console.WriteLine("Press ESC to stop");
            do
            {
                while (!Console.KeyAvailable)
                {
                    // Do something
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }
    }
}
