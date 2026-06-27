using BloodBank.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BloodBank.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View("Landing");
        }

        public IActionResult Landing()
        {
            return View();
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
}
