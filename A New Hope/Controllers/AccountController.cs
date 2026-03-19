using A_New_Hope.Models;
using A_New_Hope.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace A_New_Hope.Controllers
{
    [AllowAnonymous]
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
        public IActionResult Login(string? returnUrl = null)
        {
            var vm = new LoginViewModel
            {
                ReturnUrl = returnUrl ?? Url.Action("Index", "Home")
            };

            return View(vm);
        }

        // --------------------
        // POST: /Account/Login
        // --------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["LoginError"] = "Please fill in all fields.";
                return RedirectToAction("Landing", "Home");
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["LoginError"] = "Invalid email or password.";
                return RedirectToAction("Landing", "Home");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                TempData["LoginError"] = "This account is locked. Please contact an administrator.";
                return RedirectToAction("Landing", "Home");
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                TempData["LoginError"] = "Account locked due to multiple failed attempts.";
                return RedirectToAction("Landing", "Home");
            }

            TempData["LoginError"] = "Invalid email or password.";
            return RedirectToAction("Landing", "Home");
        }

        // --------------------
        // POST: /Account/Logout
        // --------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Landing", "Home");
        }

        // --------------------
        // GET: /Account/AccessDenied
        // --------------------
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}