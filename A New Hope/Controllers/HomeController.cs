using System.Diagnostics;
using A_New_Hope.Models;
using A_New_Hope.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Provides general site navigation pages such as Home, Privacy, Landing, and Error.
    /// </summary>
    public class HomeController : Controller
    {
        // GET: /Home/Index
        /// <summary>
        /// Displays the main home page for authenticated users.
        /// </summary>
        public IActionResult Index()
        {
            try
            {
                return View();
            }
            catch
            {
                return RedirectToAction(nameof(Error));
            }
        }

        // GET: /Home/Privacy
        /// <summary>
        /// Displays the privacy page.
        /// </summary>
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            try
            {
                return View();
            }
            catch
            {
                return RedirectToAction(nameof(Error));
            }
        }

        // GET: /
        /// <summary>
        /// Displays the public landing page and redirects authenticated users to the requested local page or Home/Index.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("/")]
        public IActionResult Landing(string? returnUrl = null)
        {
            try
            {
                // Redirect authenticated users away from the landing page.
                if (User.Identity?.IsAuthenticated == true)
                {
                    // Redirect to the requested local return URL when provided.
                    if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return LocalRedirect(returnUrl);

                    return LocalRedirect("/Home/Index");
                }

                // Build the login view model used by the landing page.
                var vm = new LoginViewModel
                {
                    ReturnUrl = returnUrl ?? "/Home/Index"
                };

                // Show any login error passed through TempData.
                if (TempData["LoginError"] is string err)
                    ViewBag.LoginError = err;

                return View(vm);
            }
            catch
            {
                return RedirectToAction(nameof(Error));
            }
        }

        // GET: /Home/Error
        /// <summary>
        /// Displays the error page with the current request id.
        /// </summary>
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            try
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // GET: /Home/ReferralPanel
        /// <summary>
        /// Displays the referral workflow panel.
        /// </summary>
        public IActionResult ReferralPanel()
        {
            try
            {
                return View();
            }
            catch
            {
                return RedirectToAction(nameof(Error));
            }
        }

        // GET: /Home/InventoryPanel
        /// <summary>
        /// Displays the inventory workflow panel.
        /// </summary>
        public IActionResult InventoryPanel()
        {
            try
            {
                return View();
            }
            catch
            {
                return RedirectToAction(nameof(Error));
            }
        }
    }
}