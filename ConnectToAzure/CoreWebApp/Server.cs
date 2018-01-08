using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConnectToEndPointLib;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.EventHubs;

namespace CoreWebApp
{
    public class Server
    {

        public class ClientHandler
        {
            public string Name = "";
            private List<WebSocket> Sockets = new List<WebSocket>();

            public bool removeSocket(WebSocket socket)
            {
                if (!Sockets.Contains(socket)) return false;
                Sockets.Remove(socket);
                return true;
            }

            public bool addSocket(WebSocket socket)
            {
                if (Sockets.Contains(socket)) return false;
                Sockets.Add(socket);
                return true;
            }

            public bool moveSocket(WebSocket socket, ClientHandler target)
            {
                if (!removeSocket(socket)) return false;
                return target.addSocket(socket);
            }


            public void send(string message)
            {
                byte[] buf = Encoding.UTF8.GetBytes(message);
                send(buf, buf.Length, WebSocketMessageType.Text);
            }

            public void send(
                byte[] buffer,
                int count,
                WebSocketMessageType msgType)
            {
                int buflen = 4 * 1024;
                for (int start = 0; start < count; start += buflen)
                {
                    int len = ((start + buflen) > count) ? count - start : buflen;
                    Task.WaitAll(
                        Sockets.Select(s =>
                                s.SendAsync(
                                    new ArraySegment<byte>(
                                        buffer,
                                        start,
                                        len),
                                    msgType,
                                    (start + buflen >= count),
                                    CancellationToken.None))
                            .ToArray());
                }
            }

            public ClientHandler(string name)
            {
                Name = name;
            }
        }

        private SortedList<string, ClientHandler> ClientHandlers = new SortedList<string, ClientHandler>();


        public static Server _Instance;
        public static Server Instance
        {
            get { return _Instance ?? (_Instance = new Server()); }
        }


        public Server()
        {
            ClientHandlers.Add("", new ClientHandler(""));
        }

        public bool send(string clientName, string message)
        {
            if (!ClientHandlers.ContainsKey(clientName)) return false;
            try
            {
                ClientHandlers[clientName].send(message);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool sendAll(string message)
        {
            try
            {
                foreach (var handler in ClientHandlers.Values)
                {
                    handler.send(message);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task Echo(HttpContext context, WebSocket webSocket)
        {
            ClientHandlers[""].addSocket(webSocket);
            var buf = new byte[1024 * 4];

            List<byte> buffer = new List<byte>();

            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                //await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                buffer.AddRange(buf.Take(result.Count));
                if (result.EndOfMessage)
                {
                    byte[] message = buffer.ToArray();
                    buffer.Clear();
                    foreach (var handler in ClientHandlers.Values)
                    {
                        handler.send(message, message.Length, result.MessageType);
                    }
                }
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            foreach (var handler in ClientHandlers.Values)
            {
                if (handler.removeSocket(webSocket)) break;
            }
        }

        //private EndPointConnector Connector = null;

        private EventHubClient Connector = null;
        private String lockObj = "";

        public void setupConnector()
        {
            /*
            lock (lockObj)
            {
                if (Connector != null) return;

                Connector = new EndPointConnector(
                    Config.Instance["ConnectionString"],
                    Config.Instance["IotHubD2cEndpoint"],
                    new Func<string, bool>[]
                    {
                        (s) =>
                        {
                            sendAll(s);
                            return true;
                        }
                    }
                );
                Connector.connect();
            }
            */
            
            lock (lockObj)
            {
                if (Connector != null) return;

                var connectionStringBuilder = new EventHubsConnectionStringBuilder(Config.Instance["ConnectionString"])
                {
                    EntityPath = Config.Instance["IotHubD2cEndpoint"]
                };


                Connector = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

                
                //var onMessageOptions = new OnMessageOptions();

                //onMessageOptions.ExceptionReceived += (sender, args) => HandleIt(args.Exception);
                /*
                Connector.OnMessage(
                    (arg) =>
                    {
                        Debug.WriteLine(arg.ContentType);
                    }, onMessageOptions);
                  */  
            }            
        }

        public async Task EchoIotHub(HttpContext context, WebSocket webSocket)
        {
            if (Connector == null)
            {
                Task.Run(() => setupConnector());
            }
            ClientHandlers[""].addSocket(webSocket);
            var buf = new byte[1024 * 4];

            List<byte> buffer = new List<byte>();

            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                //await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                buffer.AddRange(buf.Take(result.Count));
                if (result.EndOfMessage)
                {
                    byte[] message = buffer.ToArray();
                    buffer.Clear();
                    foreach (var handler in ClientHandlers.Values)
                    {
                        handler.send(message, message.Length, result.MessageType);
                    }
                }
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            foreach (var handler in ClientHandlers.Values)
            {
                if (handler.removeSocket(webSocket)) break;
            }
        }
    }
}
