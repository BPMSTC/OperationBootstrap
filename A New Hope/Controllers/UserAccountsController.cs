using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserAccountsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserAccountsController> _logger;

        public UserAccountsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<UserAccountsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /UserAccounts/Create?domainUserId=123
        public async Task<IActionResult> Create(ulong domainUserId)
        {
            _logger.LogInformation("Creating Login Account for DomainUserId={DomainuserId}", domainUserId);

            var domainUser = await _context.DomainUsers
                .FirstOrDefaultAsync(u => u.Id == domainUserId && u.DeletedAt == null);

            if (domainUser == null)
            {
                _logger.LogInformation("Create login failed: DomainUser not found, DomainUserId={DomainUserId}", domainUserId);
                return NotFound();
            }

            if (domainUser.UserType == UserType.Client)
            {
                _logger.LogInformation("Create login failed, Identity user creation failed for DomainUserId={DomainUserID}", domainUserId);
                TempData["ErrorMessage"] = "Clients do not have login accounts.";
                return RedirectToAction("Index", "Users");
            }

            var existingLogin = await _context.Users
                .FirstOrDefaultAsync(iu => iu.DomainUserId == domainUserId);

            if (existingLogin != null)
            {
                TempData["InfoMessage"] = "This user already has a login account.";
                return RedirectToAction(nameof(Manage), new { domainUserId });
            }

            // Minimal temp password for now (replace later with generated password if you want)
            var tempPassword = "ChangeMe123!";

            var appUser = new ApplicationUser
            {
                UserName = domainUser.Email,
                Email = domainUser.Email,
                EmailConfirmed = true,
                DomainUserId = domainUser.Id
            };

            var createResult = await _userManager.CreateAsync(appUser, tempPassword);

            if (!createResult.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(" | ", createResult.Errors.Select(e => e.Description));
                return RedirectToAction("Index", "Users");
            }

            // Sync login enabled/disabled with DomainUser.IsActive
            appUser.LockoutEnabled = true;
            appUser.LockoutEnd = domainUser.IsActive ? null : DateTimeOffset.MaxValue;

            var lockoutSyncResult = await _userManager.UpdateAsync(appUser);
            if (!lockoutSyncResult.Succeeded)
            {
                TempData["ErrorMessage"] = "Login created, but could not sync login status: " +
                                           string.Join(" | ", lockoutSyncResult.Errors.Select(e => e.Description));
                return RedirectToAction("Index", "Users");
            }

            // Assign Identity role based on DomainUser.UserType
            var roleName = domainUser.UserType switch
            {
                UserType.Admin => "Admin",
                UserType.Staff => "Staff",
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(roleName))
            {
                var roleResult = await _userManager.AddToRoleAsync(appUser, roleName);
                if (!roleResult.Succeeded)
                {
                    TempData["ErrorMessage"] = "Login created, but role assignment failed: " +
                                               string.Join(" | ", roleResult.Errors.Select(e => e.Description));
                    return RedirectToAction("Index", "Users");
                }
            }

            _logger.LogInformation("Login Account created for DomainUserId={DomainUserId}", domainUserId);
            TempData["SuccessMessage"] = $"Login created for {domainUser.Email}. Temporary password: {tempPassword}";
            return RedirectToAction(nameof(Manage), new { domainUserId });
        }

        // GET: /UserAccounts/Manage?domainUserId=123
        public async Task<IActionResult> Manage(ulong domainUserId)
        {
            var domainUser = await _context.DomainUsers
                .FirstOrDefaultAsync(u => u.Id == domainUserId && u.DeletedAt == null);

            if (domainUser == null)
            {
                _logger.LogInformation("Cannot manage user, DomainUserId={DomainUserId} was not found.", domainUserId);
                return NotFound();
            }


            var appUser = await _context.Users
                .FirstOrDefaultAsync(iu => iu.DomainUserId == domainUserId);

            if (appUser == null)
            {
                _logger.LogInformation("Cannot manager user, DomainUserId={DomainUserId} returned null.", domainUserId);
                TempData["InfoMessage"] = "No login account exists for this user yet.";
                return RedirectToAction("Index", "Users");
            }

            var roles = await _userManager.GetRolesAsync(appUser);

            ViewBag.DomainUser = domainUser;
            ViewBag.IdentityUser = appUser;
            ViewBag.IdentityRoles = roles;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ulong domainUserId, string? newPassword)
        {
            _logger.LogInformation("Password reset requested for DomainUserId={DomainUserId}", domainUserId);

            var domainUser = await _context.DomainUsers
                .FirstOrDefaultAsync(u => u.Id == domainUserId && u.DeletedAt == null);

            if (domainUser == null)
                return NotFound();

            var appUser = await _context.Users
                .FirstOrDefaultAsync(iu => iu.DomainUserId == domainUserId);

            if (appUser == null)
            {
                _logger.LogInformation("No login account exists for DomainUserId={DomainUserId}", domainUserId);
                TempData["ErrorMessage"] = "No login account exists for this user.";
                return RedirectToAction("Index", "Users");
            }

            var passwordToUse = string.IsNullOrWhiteSpace(newPassword)
                ? "ChangeMe123!"
                : newPassword.Trim();

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(appUser);
            var resetResult = await _userManager.ResetPasswordAsync(appUser, resetToken, passwordToUse);

            if (!resetResult.Succeeded)
            {
                _logger.LogInformation("Password reset for DomainUserId={DomainUserId} failed.", domainUserId);
                TempData["ErrorMessage"] = "Password reset failed: " +
                                           string.Join(" | ", resetResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Manage), new { domainUserId });
            }

            _logger.LogInformation("Password successfully reset for DomainUserId={DomainUserId}", domainUserId);
            TempData["SuccessMessage"] = $"Password reset for {domainUser.Email}. Temporary password: {passwordToUse}";
            return RedirectToAction(nameof(Manage), new { domainUserId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableLogin(ulong domainUserId)
        {
            _logger.LogInformation("Disabling login for DomainUserId={DomainUserId}", domainUserId);
            var domainUser = await _context.DomainUsers
                .FirstOrDefaultAsync(u => u.Id == domainUserId && u.DeletedAt == null);

            if (domainUser == null)
            {
                _logger.LogInformation("Cannot disable user.  DomainUserId={DomainUserId} returned null.", domainUserId);
                return NotFound(); 
            }

            var appUser = await _context.Users
                .FirstOrDefaultAsync(iu => iu.DomainUserId == domainUserId);

            if (appUser == null)
            {
                _logger.LogInformation("No login account exists for DomainUserId={DomainUserId}", domainUserId);
                TempData["ErrorMessage"] = "No login account exists for this user.";
                return RedirectToAction("Index", "Users");
            }

            appUser.LockoutEnabled = true;

            appUser.LockoutEnd = new DateTimeOffset(new DateTime(2099, 12, 31, 23, 59, 59), TimeSpan.Zero); // disable login

            var updateResult = await _userManager.UpdateAsync(appUser);
            if (!updateResult.Succeeded)
            {
                _logger.LogInformation("Could not disable login for DomainUserId={DomainUserId}", domainUserId);
                TempData["ErrorMessage"] = "Could not disable login: " +
                                           string.Join(" | ", updateResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Manage), new { domainUserId });
            }

            // Keep DomainUser active flag in sync
            domainUser.IsActive = false;
            domainUser.UpdatedAt = DateTime.UtcNow;
            domainUser.UpdatedByUserId = null; // wire to logged-in domain user later

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogInformation(ex, "Login was disabled, but the domain user status for DomainUserId={DomainUserId} could not be updated.", domainUserId);
                TempData["ErrorMessage"] = "Login was disabled, but the domain user status could not be updated.";
                return RedirectToAction(nameof(Manage), new { domainUserId });
            }

            _logger.LogInformation("Login was disabled for DomainUserId={DomainUserId}", domainUserId);
            TempData["SuccessMessage"] = $"Login disabled for {domainUser.Email}.";
            return RedirectToAction(nameof(Manage), new { domainUserId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableLogin(ulong domainUserId)
        {
            _logger.LogInformation("Enabling login for DomainUserId={DomainUserId}", domainUserId);
            var domainUser = await _context.DomainUsers
                .FirstOrDefaultAsync(u => u.Id == domainUserId && u.DeletedAt == null);

            if (domainUser == null)
            {
                _logger.LogInformation("Could not enable login for DomainUserId={DomainUserId}. not found. ", domainUserId);
                return NotFound();
            }

            var appUser = await _context.Users
                .FirstOrDefaultAsync(iu => iu.DomainUserId == domainUserId);

            if (appUser == null)
            {
                _logger.LogInformation("No login account exists for DomainUserId={DomainUserId}", domainUserId);
                TempData["ErrorMessage"] = "No login account exists for this user.";
                return RedirectToAction("Index", "Users");
            }

            appUser.LockoutEnd = null;

            var updateResult = await _userManager.UpdateAsync(appUser);
            if (!updateResult.Succeeded)
            {
                _logger.LogInformation("Could not enable login for DomainUserId={DomainUserId}", domainUserId);
                TempData["ErrorMessage"] = "Could not enable login: " +
                                           string.Join(" | ", updateResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Manage), new { domainUserId });
            }

            // Keep DomainUser active flag in sync
            domainUser.IsActive = true;
            domainUser.UpdatedAt = DateTime.UtcNow;
            domainUser.UpdatedByUserId = null; // wire to logged-in domain user later

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _logger.LogInformation("Login was enabled for DomainUserId={DomainUserId}, but the domain user status could not be updated", domainUserId);
                TempData["ErrorMessage"] = "Login was enabled, but the domain user status could not be updated.";
                return RedirectToAction(nameof(Manage), new { domainUserId });
            }

            _logger.LogInformation("Login enabled for DomainUserId={DomainUserId}", domainUserId);
            TempData["SuccessMessage"] = $"Login enabled for {domainUser.Email}.";
            return RedirectToAction(nameof(Manage), new { domainUserId });
        }
    }
}