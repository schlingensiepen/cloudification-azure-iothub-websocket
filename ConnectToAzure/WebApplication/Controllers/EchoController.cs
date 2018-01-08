using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.WebSockets;

namespace WebApplication
{
    public class EchoController : ApiController
    {
        public HttpResponseMessage Get()
        {
            Debug.WriteLine("TTT");
            var currentContext = HttpContext.Current;
            if (currentContext.IsWebSocketRequest ||
                currentContext.IsWebSocketRequestUpgrading)
            {
                currentContext.AcceptWebSocketRequest(ProcessWebsocketSession);
            }

            return Request.CreateResponse(HttpStatusCode.SwitchingProtocols);
        }

        private Task ProcessWebsocketSession(AspNetWebSocketContext context)
        {
            var handler = new ServerEventHandler();
            var processTask = handler.ProcessWebSocketRequestAsync(context);
            return processTask;
        }
    }
}