using System.Diagnostics;
using A_New_Hope.Models;
using A_New_Hope.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace A_New_Hope.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet("/")]
        public IActionResult Landing(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);

                return LocalRedirect("/Home/Index");
            }

            var vm = new LoginViewModel
            {
                ReturnUrl = returnUrl ?? "/Home/Index"
            };

            if (TempData["LoginError"] is string err)
                ViewBag.LoginError = err;

            return View(vm);
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult ReferralPanel()
        {
            return View();
        }

        public IActionResult InventoryPanel()
        {
            return View();
        }
    }
}