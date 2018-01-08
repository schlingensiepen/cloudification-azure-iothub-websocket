using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CoreWebApp.Controllers
{
    public class HomeController : Controller
    {
        // GET: /<controller>/
        public string Index(int? id)
        {
            return "Index..." + id;
        }

        public string Send(string id)
        {
            return Server.Instance.sendAll("Got message " + id).ToString();
        }

        public string Config(string id)
        {
            return CoreWebApp.Config.Instance[id];
        }


    }
}
