using A_New_Hope.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace A_New_Hope.Controllers
{
    public class HomeController : Controller
    {
        [AllowAnonymous]
        public IActionResult Index()
        {
            // Pass login error to view if present
            if (TempData["LoginError"] is string err)
                ViewBag.LoginError = err;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Landing()
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