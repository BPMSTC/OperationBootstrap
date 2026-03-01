using A_New_Hope.Models;
using A_New_Hope.Models.ViewModels;
using A_New_Hope.ViewModel;
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
                ReturnUrl = returnUrl ?? Url.Content("~/")
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
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            // Optional: enforce lockout before attempting sign-in
            if (await _userManager.IsLockedOutAsync(user))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This account is locked. Please contact an administrator.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return LocalRedirect(model.ReturnUrl ?? "~/");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This account has been locked due to multiple failed login attempts.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
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
            return RedirectToAction(nameof(Login));
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