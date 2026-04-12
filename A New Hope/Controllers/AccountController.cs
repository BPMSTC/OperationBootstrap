using A_New_Hope.Models;
using A_New_Hope.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace A_New_Hope.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // --------------------
        // GET: /Account/Login
        // --------------------
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
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

        // --------------------
        // POST: /Account/Login
        // --------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["LoginError"] = "Please fill in all fields.";
                return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["LoginError"] = "Invalid email or password.";
                return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                TempData["LoginError"] = "This account is locked. Please contact an administrator.";
                return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return LocalRedirect(model.ReturnUrl);

                return LocalRedirect("/Home/Index");
            }

            if (result.IsLockedOut)
            {
                TempData["LoginError"] = "Account locked due to multiple failed attempts.";
                return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
            }

            TempData["LoginError"] = "Invalid email or password.";
            return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
        }

        // --------------------
        // POST: /Account/Logout
        // --------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Landing", "Home");
        }

        // --------------------
        // GET: /Account/AccessDenied
        // --------------------
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}