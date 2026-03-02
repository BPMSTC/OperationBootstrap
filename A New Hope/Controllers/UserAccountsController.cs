using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// UserAccountsController
    /// ----------------------
    /// This controller manages the relationship between your "DomainUser" records
    /// (the application's business users table) and ASP.NET Core Identity users
    /// (ApplicationUser entries in the Identity tables).
    ///
    /// In your system, DomainUsers appear to represent the "real" user records you manage
    /// for the organization (clients, staff, admins). Identity users exist only when you want
    /// someone to log into the administration UI.
    ///
    /// High-level responsibilities:
    /// - Create an Identity login (ApplicationUser) for an existing DomainUser.
    /// - Display/manage login status and roles for that Identity user.
    /// - Reset the Identity user's password.
    /// - Enable/Disable login by using Identity lockout settings.
    /// - Keep DomainUser.IsActive in sync with Identity lockout status.
    ///
    /// Authorization:
    /// - This controller is restricted to Admins only via [Authorize(Roles = "Admin")].
    ///
    /// Notes:
    /// - This controller uses both:
    ///     - ApplicationDbContext (_context) to query DomainUsers and Identity user rows
    ///     - UserManager<ApplicationUser> (_userManager) for Identity-safe operations
    /// - Audit fields are updated using UTC timestamps and placeholder user IDs (null)
    ///   until you wire audit columns to the currently logged-in domain user.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class UserAccountsController : Controller
    {
        /// <summary>
        /// EF Core DbContext for:
        /// - DomainUsers (business users)
        /// - Identity users (ApplicationUser) through _context.Users
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Identity UserManager used for creating users, setting passwords, updating lockout,
        /// and managing Identity roles.
        /// </summary>
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Logger for structured logging (useful for diagnosing account creation, lockouts, role assignment, etc.).
        /// </summary>
        private readonly ILogger<UserAccountsController> _logger;

        /// <summary>
        /// Constructor with dependency injection:
        /// - ApplicationDbContext for database queries/updates
        /// - UserManager for Identity operations
        /// - ILogger for operational logging
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

        // GET: /UserAccounts/Create?domainUserId=123
        /// <summary>
        /// Creates an ASP.NET Core Identity login account (ApplicationUser) for an existing DomainUser.
        ///
        /// Input:
        /// - domainUserId: the primary key of the DomainUser you want to give a login to.
        ///
        /// Behavior / business rules:
        /// - DomainUser must exist and not be soft-deleted.
        /// - DomainUser must NOT be a Client (clients do not get logins in your admin system).
        /// - If an Identity user already exists for that DomainUser, redirect to Manage.
        /// - Otherwise:
        ///     1) Create a new ApplicationUser tied to DomainUserId.
        ///     2) Set a temporary password (currently hard-coded).
        ///     3) Sync lockout status based on DomainUser.IsActive.
        ///     4) Assign an Identity role based on DomainUser.UserType (Admin/Staff).
        ///     5) Redirect to Manage.
        ///
        /// Notes:
        /// - The temp password is currently shown to the admin via TempData.
        /// - In a production-ready system you would generally generate a random password,
        ///   force a reset, or email an onboarding link instead.
        /// </summary>
        public async Task<IActionResult> Create(ulong domainUserId)
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

            // Business rule: clients do not get login accounts (admin system is for staff/admin).
            if (domainUser.UserType == UserType.Client)
            {
                _logger.LogInformation("Create login failed, Identity user creation failed for DomainUserId={DomainUserID}", domainUserId);
                TempData["ErrorMessage"] = "Clients do not have login accounts.";
                return RedirectToAction("Index", "Users");
            }

            // Check if an Identity login already exists for this domain user.
            // Note: _context.Users here is the Identity users DbSet (ApplicationUser).
            var existingLogin = await _context.Users
                .FirstOrDefaultAsync(iu => iu.DomainUserId == domainUserId);

            if (existingLogin != null)
            {
                // Inform the admin that the account already exists and redirect to management page.
                TempData["InfoMessage"] = "This user already has a login account.";
                return RedirectToAction(nameof(Manage), new { domainUserId });
            }

            // Temporary password policy:
            // - Minimal placeholder for now. You may replace with a generated password later.
            // - This MUST meet your Identity password rules configured in Program.cs.
            var tempPassword = "ChangeMe123!";

            // Create the Identity user and link it back to the DomainUser via DomainUserId.
            var appUser = new ApplicationUser
            {
                UserName = domainUser.Email,
                Email = domainUser.Email,
                EmailConfirmed = true,  // Set true to avoid requiring email confirmation in your current setup.
                DomainUserId = domainUser.Id
            };

            // Create Identity user with the provided password.
            var createResult = await _userManager.CreateAsync(appUser, tempPassword);

            if (!createResult.Succeeded)
            {
                // Return errors to admin (joined as one string).
                // Note: This can include password policy failures, duplicate email/username, etc.
                TempData["ErrorMessage"] = string.Join(" | ", createResult.Errors.Select(e => e.Description));
                return RedirectToAction("Index", "Users");
            }

            // Sync login enabled/disabled with DomainUser.IsActive
            // ---------------------------------------------------
            // Identity uses lockout settings to disable login:
            // - LockoutEnabled must be true for lockout to apply
            // - LockoutEnd set to a future time means the account is locked until then
            appUser.LockoutEnabled = true;

            // If DomainUser is inactive, lock account "forever" by using MaxValue.
            // If active, allow login by setting LockoutEnd to null.
            appUser.LockoutEnd = domainUser.IsActive ? null : DateTimeOffset.MaxValue;

            var lockoutSyncResult = await _userManager.UpdateAsync(appUser);
            if (!lockoutSyncResult.Succeeded)
            {
                // Account exists, but lockout sync failed.
                // Redirect with a message so admin can intervene.
                TempData["ErrorMessage"] = "Login created, but could not sync login status: " +
                                           string.Join(" | ", lockoutSyncResult.Errors.Select(e => e.Description));
                return RedirectToAction("Index", "Users");
            }

            // Assign Identity role based on DomainUser.UserType
            // -------------------------------------------------
            // This determines authorization in your app (e.g., [Authorize(Roles="Admin")]).
            // Only Admin and Staff are mapped here; Client is excluded earlier.
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
                    // Account exists, but role assignment failed.
                    TempData["ErrorMessage"] = "Login created, but role assignment failed: " +
                                               string.Join(" | ", roleResult.Errors.Select(e => e.Description));
                    return RedirectToAction("Index", "Users");
                }
            }

            _logger.LogInformation("Login Account created for DomainUserId={DomainUserId}", domainUserId);

            // TempData is used so the next page load can display the message after redirect.
            TempData["SuccessMessage"] = $"Login created for {domainUser.Email}. Temporary password: {tempPassword}";

            // Redirect to Manage page to show account details (roles, lockout status, etc.).
            return RedirectToAction(nameof(Manage), new { domainUserId });
        }

        // GET: /UserAccounts/Manage?domainUserId=123
        /// <summary>
        /// Displays the management screen for an Identity login associated with a DomainUser.
        ///
        /// Input:
        /// - domainUserId: the DomainUser id being managed
        ///
        /// Behavior:
        /// - Loads DomainUser for UI context and validation.
        /// - Loads ApplicationUser (Identity user) tied to DomainUserId.
        /// - Loads Identity roles assigned to that ApplicationUser.
        ///
        /// Outputs:
        /// - Uses ViewBag to pass:
        ///     - DomainUser (business user record)
        ///     - IdentityUser (ApplicationUser / login account)
        ///     - IdentityRoles (list of role names)
        ///
        /// Note:
        /// - If no Identity user exists yet, the action redirects back to Users/Index with an info message.
        /// </summary>
        public async Task<IActionResult> Manage(ulong domainUserId)
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

            // Retrieve current Identity roles for display on the management screen.
            var roles = await _userManager.GetRolesAsync(appUser);

            // Pass data to the view (likely Views/UserAccounts/Manage.cshtml).
            ViewBag.DomainUser = domainUser;
            ViewBag.IdentityUser = appUser;
            ViewBag.IdentityRoles = roles;

            return View();
        }

        /// <summary>
        /// Resets an Identity user's password for a DomainUser.
        ///
        /// Input:
        /// - domainUserId: DomainUser id to locate the linked Identity user
        /// - newPassword: optional; if blank, uses default "ChangeMe123!"
        ///
        /// Behavior:
        /// - Verifies DomainUser exists (not deleted).
        /// - Verifies Identity user exists for that DomainUser.
        /// - Generates a password reset token and uses it to reset password (Identity-safe approach).
        /// - Stores success/error messages using TempData and redirects back to Manage.
        ///
        /// Notes:
        /// - This action does not enforce any "force password change at next login" behavior.
        /// - Password must meet Identity password rules configured in Program.cs.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ulong domainUserId, string? newPassword)
        {
            _logger.LogInformation("Password reset requested for DomainUserId={DomainUserId}", domainUserId);

            // Load the domain user so we can verify it exists and access display info (Email).
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

            // If admin didn't provide a password, use a default one.
            var passwordToUse = string.IsNullOrWhiteSpace(newPassword)
                ? "ChangeMe123!"
                : newPassword.Trim();

            // Identity reset flow:
            // - token is required (prevents arbitrary password changes without authorization checks)
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

        /// <summary>
        /// Disables an Identity login for a given DomainUser by setting a far-future lockout date.
        ///
        /// Input:
        /// - domainUserId: DomainUser id to locate linked Identity user.
        ///
        /// Behavior:
        /// - Verifies DomainUser exists and isn't deleted.
        /// - Verifies Identity user exists.
        /// - Sets LockoutEnabled=true and LockoutEnd to a far future date (effectively disabled).
        /// - Updates DomainUser.IsActive=false and updates audit fields.
        /// - Saves domain changes through DbContext.
        ///
        /// Notes:
        /// - LockoutEnd uses a hard-coded future date rather than DateTimeOffset.MaxValue.
        ///   Both are conceptually "very far future" for disabling.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableLogin(ulong domainUserId)
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

            // Ensure lockout is enabled, then set a lockout end date far in the future to block login.
            appUser.LockoutEnabled = true;

            // This date effectively disables login for "practical forever".
            appUser.LockoutEnd = new DateTimeOffset(new DateTime(2099, 12, 31, 23, 59, 59), TimeSpan.Zero); // disable login

            // Persist lockout changes through UserManager (Identity-safe update).
            var updateResult = await _userManager.UpdateAsync(appUser);
            if (!updateResult.Succeeded)
            {
                _logger.LogInformation("Could not disable login for DomainUserId={DomainUserId}", domainUserId);
                TempData["ErrorMessage"] = "Could not disable login: " +
                                           string.Join(" | ", updateResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Manage), new { domainUserId });
            }

            // Keep DomainUser active flag in sync
            // ----------------------------------
            // This ensures your "business" user record reflects the login state.
            domainUser.IsActive = false;
            domainUser.UpdatedAt = DateTime.UtcNow;
            domainUser.UpdatedByUserId = null; // wire to logged-in domain user later

            try
            {
                // Save DomainUser updates to the application database.
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Lockout was applied, but domain user record did not save successfully.
                _logger.LogInformation(ex, "Login was disabled, but the domain user status for DomainUserId={DomainUserId} could not be updated.", domainUserId);
                TempData["ErrorMessage"] = "Login was disabled, but the domain user status could not be updated.";
                return RedirectToAction(nameof(Manage), new { domainUserId });
            }

            _logger.LogInformation("Login was disabled for DomainUserId={DomainUserId}", domainUserId);
            TempData["SuccessMessage"] = $"Login disabled for {domainUser.Email}.";
            return RedirectToAction(nameof(Manage), new { domainUserId });
        }

        /// <summary>
        /// Enables an Identity login for a given DomainUser by clearing the lockout end date.
        ///
        /// Input:
        /// - domainUserId: DomainUser id to locate linked Identity user.
        ///
        /// Behavior:
        /// - Verifies DomainUser exists.
        /// - Verifies Identity user exists.
        /// - Sets LockoutEnd = null (account is no longer locked).
        /// - Updates DomainUser.IsActive=true and audit fields.
        /// - Saves domain changes through DbContext.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableLogin(ulong domainUserId)
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

            // Load linked Identity user.
            var appUser = await _context.Users
                .FirstOrDefaultAsync(iu => iu.DomainUserId == domainUserId);

            if (appUser == null)
            {
                _logger.LogInformation("No login account exists for DomainUserId={DomainUserId}", domainUserId);
                TempData["ErrorMessage"] = "No login account exists for this user.";
                return RedirectToAction("Index", "Users");
            }

            // Clearing lockout end date effectively enables login (assuming lockout isn't triggered otherwise).
            appUser.LockoutEnd = null;

            // Persist lockout change via UserManager.
            var updateResult = await _userManager.UpdateAsync(appUser);
            if (!updateResult.Succeeded)
            {
                _logger.LogInformation("Could not enable login for DomainUserId={DomainUserId}", domainUserId);
                TempData["ErrorMessage"] = "Could not enable login: " +
                                           string.Join(" | ", updateResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Manage), new { domainUserId });
            }

            // Keep DomainUser active flag in sync.
            domainUser.IsActive = true;
            domainUser.UpdatedAt = DateTime.UtcNow;
            domainUser.UpdatedByUserId = null; // wire to logged-in domain user later

            try
            {
                // Save DomainUser updates.
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Identity account was enabled, but domain user status did not update successfully.
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