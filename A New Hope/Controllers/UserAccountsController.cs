using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages the relationship between DomainUser records and ASP.NET Core Identity users.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class UserAccountsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserAccountsController> _logger;

        /// <summary>
        /// Creates the controller with the required database context, Identity manager, and logger.
        /// </summary>
        public UserAccountsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<UserAccountsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: UserAccounts/Create?domainUserId=123
        /// <summary>
        /// Creates an Identity login account for an existing DomainUser.
        /// </summary>
        public async Task<IActionResult> Create(ulong domainUserId)
        {
            try
            {
                _logger.LogInformation("Creating Login Account for DomainUserId={DomainuserId}", domainUserId);

                // Load the domain user, ensuring it exists and is not soft deleted.
                var domainUser = await _context.DomainUsers
                    .FirstOrDefaultAsync(u => u.Id == domainUserId && u.DeletedAt == null);

                if (domainUser == null)
                {
                    _logger.LogInformation("Create login failed: DomainUser not found, DomainUserId={DomainUserId}", domainUserId);
                    return NotFound();
                }

                // Business rule: clients do not get login accounts.
                if (domainUser.UserType == UserType.Client)
                {
                    _logger.LogInformation("Create login failed, Identity user creation failed for DomainUserId={DomainUserID}", domainUserId);
                    TempData["ErrorMessage"] = "Clients do not have login accounts.";
                    return RedirectToAction("Index", "Users");
                }

                // Check whether an Identity login already exists for this domain user.
                var existingLogin = await _context.Users
                    .FirstOrDefaultAsync(iu => iu.DomainUserId == domainUserId);

                if (existingLogin != null)
                {
                    TempData["InfoMessage"] = "This user already has a login account.";
                    return RedirectToAction(nameof(Manage), new { domainUserId });
                }

                // Use the temporary password currently configured for new logins.
                var tempPassword = "ChangeMe123!";

                // Create the Identity user and link it to the DomainUser.
                var appUser = new ApplicationUser
                {
                    UserName = domainUser.Email,
                    Email = domainUser.Email,
                    EmailConfirmed = true,
                    DomainUserId = domainUser.Id
                };

                // Create the Identity user with the temporary password.
                var createResult = await _userManager.CreateAsync(appUser, tempPassword);

                if (!createResult.Succeeded)
                {
                    TempData["ErrorMessage"] = string.Join(" | ", createResult.Errors.Select(e => e.Description));
                    return RedirectToAction("Index", "Users");
                }

                // Sync login enabled or disabled state with DomainUser.IsActive.
                appUser.LockoutEnabled = true;
                appUser.LockoutEnd = domainUser.IsActive ? null : DateTimeOffset.MaxValue;

                var lockoutSyncResult = await _userManager.UpdateAsync(appUser);
                if (!lockoutSyncResult.Succeeded)
                {
                    TempData["ErrorMessage"] = "Login created, but could not sync login status: " +
                                               string.Join(" | ", lockoutSyncResult.Errors.Select(e => e.Description));
                    return RedirectToAction("Index", "Users");
                }

                // Assign the Identity role based on DomainUser.UserType.
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

                // Show success feedback with the temporary password.
                TempData["SuccessMessage"] = $"Login created for {domainUser.Email}. Temporary password: {tempPassword}";

                return RedirectToAction(nameof(Manage), new { domainUserId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating Login Account for DomainUserId={DomainUserId}", domainUserId);
                TempData["ErrorMessage"] = "An unexpected error occurred while creating the login account.";
                return RedirectToAction("Index", "Users");
            }
        }

        // GET: UserAccounts/Manage?domainUserId=123
        /// <summary>
        /// Displays the management screen for an Identity login associated with a DomainUser.
        /// </summary>
        public async Task<IActionResult> Manage(ulong domainUserId)
        {
            try
            {
                // Load the domain user record for display and validation.
                var domainUser = await _context.DomainUsers
                    .FirstOrDefaultAsync(u => u.Id == domainUserId && u.DeletedAt == null);

                if (domainUser == null)
                {
                    _logger.LogInformation("Cannot manage user, DomainUserId={DomainUserId} was not found.", domainUserId);
                    return NotFound();
                }

                // Load the linked Identity user by DomainUserId.
                var appUser = await _context.Users
                    .FirstOrDefaultAsync(iu => iu.DomainUserId == domainUserId);

                if (appUser == null)
                {
                    _logger.LogInformation("Cannot manager user, DomainUserId={DomainUserId} returned null.", domainUserId);
                    TempData["InfoMessage"] = "No login account exists for this user yet.";
                    return RedirectToAction("Index", "Users");
                }

                // Retrieve current Identity roles for display.
                var roles = await _userManager.GetRolesAsync(appUser);

                // Pass data to the view.
                ViewBag.DomainUser = domainUser;
                ViewBag.IdentityUser = appUser;
                ViewBag.IdentityRoles = roles;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error managing login for DomainUserId={DomainUserId}", domainUserId);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the account management page.";
                return RedirectToAction("Index", "Users");
            }
        }

        // POST: UserAccounts/ResetPassword
        /// <summary>
        /// Resets the Identity password for the login associated with a DomainUser.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ulong domainUserId, string? newPassword)
        {
            try
            {
                _logger.LogInformation("Password reset requested for DomainUserId={DomainUserId}", domainUserId);

                // Load the domain user for validation and display context.
                var domainUser = await _context.DomainUsers
                    .FirstOrDefaultAsync(u => u.Id == domainUserId && u.DeletedAt == null);

                if (domainUser == null)
                    return NotFound();

                // Load the linked Identity user.
                var appUser = await _context.Users
                    .FirstOrDefaultAsync(iu => iu.DomainUserId == domainUserId);

                if (appUser == null)
                {
                    _logger.LogInformation("No login account exists for DomainUserId={DomainUserId}", domainUserId);
                    TempData["ErrorMessage"] = "No login account exists for this user.";
                    return RedirectToAction("Index", "Users");
                }

                // Use the provided password, or the default temporary password when blank.
                var passwordToUse = string.IsNullOrWhiteSpace(newPassword)
                    ? "ChangeMe123!"
                    : newPassword.Trim();

                // Reset the password through the Identity reset flow.
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error resetting password for DomainUserId={DomainUserId}", domainUserId);
                TempData["ErrorMessage"] = "An unexpected error occurred while resetting the password.";
                return RedirectToAction(nameof(Manage), new { domainUserId });
            }
        }

        // POST: UserAccounts/DisableLogin
        /// <summary>
        /// Disables an Identity login for a DomainUser by applying a far-future lockout date.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableLogin(ulong domainUserId)
        {
            try
            {
                _logger.LogInformation("Disabling login for DomainUserId={DomainUserId}", domainUserId);

                // Load the domain user.
                var domainUser = await _context.DomainUsers
                    .FirstOrDefaultAsync(u => u.Id == domainUserId && u.DeletedAt == null);

                if (domainUser == null)
                {
                    _logger.LogInformation("Cannot disable user.  DomainUserId={DomainUserId} returned null.", domainUserId);
                    return NotFound();
                }

                // Load the linked Identity user.
                var appUser = await _context.Users
                    .FirstOrDefaultAsync(iu => iu.DomainUserId == domainUserId);

                if (appUser == null)
                {
                    _logger.LogInformation("No login account exists for DomainUserId={DomainUserId}", domainUserId);
                    TempData["ErrorMessage"] = "No login account exists for this user.";
                    return RedirectToAction("Index", "Users");
                }

                // Enable lockout and set a far-future lockout date.
                appUser.LockoutEnabled = true;
                appUser.LockoutEnd = new DateTimeOffset(new DateTime(2099, 12, 31, 23, 59, 59), TimeSpan.Zero);

                // Persist lockout changes through UserManager.
                var updateResult = await _userManager.UpdateAsync(appUser);
                if (!updateResult.Succeeded)
                {
                    _logger.LogInformation("Could not disable login for DomainUserId={DomainUserId}", domainUserId);
                    TempData["ErrorMessage"] = "Could not disable login: " +
                                               string.Join(" | ", updateResult.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Manage), new { domainUserId });
                }

                // Keep DomainUser.IsActive in sync with the disabled login state.
                domainUser.IsActive = false;
                domainUser.UpdatedAt = DateTime.UtcNow;
                domainUser.UpdatedByUserId = null;

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error disabling login for DomainUserId={DomainUserId}", domainUserId);
                TempData["ErrorMessage"] = "An unexpected error occurred while disabling the login.";
                return RedirectToAction(nameof(Manage), new { domainUserId });
            }
        }

        // POST: UserAccounts/EnableLogin
        /// <summary>
        /// Enables an Identity login for a DomainUser by clearing the lockout end date.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableLogin(ulong domainUserId)
        {
            try
            {
                _logger.LogInformation("Enabling login for DomainUserId={DomainUserId}", domainUserId);

                // Load the domain user record.
                var domainUser = await _context.DomainUsers
                    .FirstOrDefaultAsync(u => u.Id == domainUserId && u.DeletedAt == null);

                if (domainUser == null)
                {
                    _logger.LogInformation("Could not enable login for DomainUserId={DomainUserId}. not found. ", domainUserId);
                    return NotFound();
                }

                // Load the linked Identity user.
                var appUser = await _context.Users
                    .FirstOrDefaultAsync(iu => iu.DomainUserId == domainUserId);

                if (appUser == null)
                {
                    _logger.LogInformation("No login account exists for DomainUserId={DomainUserId}", domainUserId);
                    TempData["ErrorMessage"] = "No login account exists for this user.";
                    return RedirectToAction("Index", "Users");
                }

                // Clear lockout to enable login.
                appUser.LockoutEnd = null;

                // Persist lockout changes via UserManager.
                var updateResult = await _userManager.UpdateAsync(appUser);
                if (!updateResult.Succeeded)
                {
                    _logger.LogInformation("Could not enable login for DomainUserId={DomainUserId}", domainUserId);
                    TempData["ErrorMessage"] = "Could not enable login: " +
                                               string.Join(" | ", updateResult.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Manage), new { domainUserId });
                }

                // Keep DomainUser.IsActive in sync with the enabled login state.
                domainUser.IsActive = true;
                domainUser.UpdatedAt = DateTime.UtcNow;
                domainUser.UpdatedByUserId = null;

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error enabling login for DomainUserId={DomainUserId}", domainUserId);
                TempData["ErrorMessage"] = "An unexpected error occurred while enabling the login.";
                return RedirectToAction(nameof(Manage), new { domainUserId });
            }
        }
    }
}