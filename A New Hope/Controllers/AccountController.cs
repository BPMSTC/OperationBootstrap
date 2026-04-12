using A_New_Hope.Models;
using A_New_Hope.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages user sign-in, sign-out, and access-denied flows.
    /// </summary>
    [Authorize]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Creates the controller with the required Identity services.
        /// </summary>
        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // GET: Account/Login
        /// <summary>
        /// Displays the login page for unauthenticated users.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // Redirect authenticated users away from the login page.
            if (User.Identity?.IsAuthenticated == true)
            {
                // Redirect to the requested local return URL when provided.
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);

                return LocalRedirect("/Home/Index");
            }

            // Build the login view model for the page.
            var vm = new LoginViewModel
            {
                ReturnUrl = returnUrl ?? "/Home/Index"
            };

            // Show any login error passed through TempData.
            if (TempData["LoginError"] is string err)
                ViewBag.LoginError = err;

            return View(vm);
        }

        // POST: Account/Login
        /// <summary>
        /// Attempts to sign in the user with the submitted credentials.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                // Return to the login page when the posted model is invalid.
                if (!ModelState.IsValid)
                {
                    TempData["LoginError"] = "Please fill in all fields.";
                    return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
                }

                // Look up the user by email address.
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    TempData["LoginError"] = "Invalid email or password.";
                    return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
                }

                // Prevent sign-in when the account is already locked out.
                if (await _userManager.IsLockedOutAsync(user))
                {
                    TempData["LoginError"] = "This account is locked. Please contact an administrator.";
                    return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
                }

                // Attempt to sign in using the submitted password.
                var result = await _signInManager.PasswordSignInAsync(
                    user, model.Password, model.RememberMe, lockoutOnFailure: true);

                // Redirect to the requested local return URL or Home/Index on success.
                if (result.Succeeded)
                {
                    if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                        return LocalRedirect(model.ReturnUrl);

                    return LocalRedirect("/Home/Index");
                }

                // Show a lockout message when repeated failed attempts trigger lockout.
                if (result.IsLockedOut)
                {
                    TempData["LoginError"] = "Account locked due to multiple failed attempts.";
                    return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
                }

                // Show a generic failure message for invalid credentials.
                TempData["LoginError"] = "Invalid email or password.";
                return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
            }
            catch
            {
                TempData["LoginError"] = "An unexpected error occurred while trying to sign you in.";
                return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
            }
        }

        // POST: Account/Logout
        /// <summary>
        /// Signs out the current user and redirects to the landing page.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Sign out the current authenticated user.
                await _signInManager.SignOutAsync();
                return RedirectToAction("Landing", "Home");
            }
            catch
            {
                TempData["LoginError"] = "An unexpected error occurred while signing you out.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Account/AccessDenied
        /// <summary>
        /// Displays the access denied page.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}