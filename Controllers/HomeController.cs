using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Filters;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Aggregate]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }


        [CommandHandlerFor("TestApplication")]
        public IEvent TestApplication([FromBody]ChangePersonName command)
        {
            return AggregateEvent.CreateEvent(nameof(PersonNameChanged), command, new PersonNameChanged() {PersonName = command.PersonName}, 1);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ChangePersonName : Command
    {
        public String PersonName { get; set; }
    }

    public class PersonNameChanged
    {
        public String PersonName { get; set; }
    }
}
