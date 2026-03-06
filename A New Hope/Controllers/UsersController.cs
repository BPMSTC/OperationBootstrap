using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// UsersController
    /// ---------------
    /// This controller manages CRUD operations for DomainUser records (your application's "business users" table).
    ///
    /// IMPORTANT DISTINCTION IN YOUR APP:
    /// - DomainUser (DomainUsers table): the canonical "person/user" record used by the business logic.
    /// - ApplicationUser (Identity users table): the login account used for authentication/authorization.
    ///
    /// In this project, not every DomainUser has an Identity login account.
    /// - Staff/Admin users typically do have logins.
    /// - Clients may exist as DomainUsers but are explicitly treated differently (often no login).
    ///
    /// High-level responsibilities of this controller:
    /// 1) List DomainUsers for staff/admin management.
    /// 2) Create, edit, soft-delete DomainUsers.
    /// 3) Display whether each DomainUser has a linked Identity login account.
    /// 4) When editing a DomainUser, keep Identity access in sync (role + login enabled/disabled)
    ///    for any linked Identity account.
    ///
    /// Authorization:
    /// - Restricted to Staff and Admin roles via [Authorize(Roles = "Staff,Admin")].
    ///
    /// Audit + deletion model:
    /// - Uses soft delete (DeletedAt timestamp) rather than physical deletion.
    /// - Uses CreatedAt/UpdatedAt timestamps and placeholder CreatedByUserId/UpdatedByUserId
    ///   until you wire them to the currently logged-in domain user.
    /// </summary>
    [Authorize(Roles = "Staff,Admin")]
    public class UsersController : Controller
    {
        /// <summary>
        /// EF Core DbContext used for DomainUsers and Identity users (ApplicationUser via _context.Users).
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Identity UserManager used to read/write Identity roles and lockout settings for ApplicationUser accounts.
        /// </summary>
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Logger used to trace operations (list/load/save/sync) and capture errors.
        /// </summary>
        private readonly ILogger<UsersController> _logger;

        /// <summary>
        /// Constructor with dependency injection for:
        /// - ApplicationDbContext (data access)
        /// - UserManager (Identity role/lockout management)
        /// - ILogger (observability)
        /// </summary>
        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<UsersController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Users
        /// <summary>
        /// Displays a list of DomainUsers.
        ///
        /// This action builds a view model list (DomainUserIndexRowViewModel) rather than returning raw entities.
        /// That view model includes:
        /// - DomainUser fields (name/email/contact/address, user type, status, etc.)
        /// - Flags/fields that indicate whether there is a linked Identity login account:
        ///     - HasLoginAccount (bool)
        ///     - IdentityUserId (string? - the Identity user's primary key)
        ///
        /// Implementation notes:
        /// - Loads DomainUsers first (non-deleted).
        /// - Loads Identity links next by querying Identity users that have DomainUserId populated.
        /// - Creates a dictionary mapping DomainUserId => IdentityUserId for fast lookups.
        /// - Projects DomainUsers into the index view model list.
        ///
        /// Error handling:
        /// - Wrapped in try/catch; on failure returns a generic Error view.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation($"Retrieving users");

            try
            {
                // -----------------------------
                // 1) Load DomainUsers (business users)
                // -----------------------------
                // Only non-deleted users are shown.
                var domainUsers = await _context.DomainUsers
                    .Where(u => u.DeletedAt == null)
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ThenBy(u => u.Email)
                    .ToListAsync();

                // -----------------------------
                // 2) Load Identity -> DomainUser links
                // -----------------------------
                // Identity users are stored in _context.Users (ApplicationUser).
                // DomainUserId is a nullable FK-like link to DomainUsers.
                var identityLinks = await _context.Users
                    .Where(iu => iu.DomainUserId != null)
                    .Select(iu => new { iu.Id, iu.DomainUserId })
                    .ToListAsync();

                // Build a dictionary for quick mapping:
                // DomainUserId (ulong) => IdentityUserId (string)
                var identityByDomainUserId = identityLinks
                    .Where(x => x.DomainUserId.HasValue)
                    .ToDictionary(x => x.DomainUserId!.Value, x => x.Id);

                // -----------------------------
                // 3) Project to a UI-friendly row model
                // -----------------------------
                // The view model list includes both DomainUser fields and Identity login metadata.
                var users = domainUsers.Select(u => new DomainUserIndexRowViewModel
                {
                    Id = u.Id,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    City = u.City,
                    State = u.State,
                    PostalCode = u.PostalCode,
                    DateOfBirth = u.DateOfBirth,
                    DefaultPreference = u.DefaultPreference,
                    UserType = u.UserType,
                    IsActive = u.IsActive,

                    // Determine whether a login exists by checking dictionary membership.
                    HasLoginAccount = identityByDomainUserId.ContainsKey(u.Id),

                    // If present, set the Identity user ID (string). Otherwise null.
                    IdentityUserId = identityByDomainUserId.TryGetValue(u.Id, out var identityId) ? identityId : null
                }).ToList();

                _logger.LogInformation("Retrieved {UserCount} users", users.Count);

                // Render Views/Users/Index.cshtml with the view model list.
                return View(users);
            }
            catch (Exception ex)
            {
                // If anything fails (DB connectivity, query issue, etc.), log the exception and show Error view.
                _logger.LogError(ex, "Failed to retrieve users.");

                return View("Error");
            }
        }

        // GET: Users/Details/5
        /// <summary>
        /// Displays details for a single DomainUser by Id.
        ///
        /// Behavior:
        /// - Returns 404 if id is null or user not found (or deleted).
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                _logger.LogInformation("Details requested with null id");
                return NotFound();
            }

            // Load the DomainUser (non-deleted only).
            var user = await _context.DomainUsers
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null)
            {
                _logger.LogInformation("User not found. UserId = {UserID}", id);
                return NotFound();
            }

            return View(user);
        }

        // GET: Users/Create
        /// <summary>
        /// Shows the Create form for a new DomainUser.
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        /// <summary>
        /// Processes form submission to create a new DomainUser.
        ///
        /// Security:
        /// - [ValidateAntiForgeryToken] provides CSRF protection.
        ///
        /// Binding:
        /// - [Bind(...)] limits which properties can be posted (prevents over-posting).
        ///
        /// Validation:
        /// - Removes navigation properties from ModelState because forms do not post navigation objects.
        ///
        /// Audit:
        /// - Sets CreatedAt/UpdatedAt and placeholder CreatedBy/UpdatedBy user IDs.
        ///
        /// Error handling:
        /// - Catches DbUpdateException to show a friendly UI message.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Email,PhoneNumber,FirstName,LastName,AddressLine1,AddressLine2,City,State,PostalCode,DateOfBirth,DefaultPreference,UserType,IsActive")] A_New_Hope.Models.DomainUser user)
        {
            _logger.LogInformation("creating user Email = {Email}", user.Email);

            // Navigation properties are not posted by the form
            // Removing these avoids false validation failures.
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.CreatedByUser));
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.UpdatedByUser));
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.ClientProfile));

            // If the posted model is invalid, re-render the form with validation messages.
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            // Set audit metadata (UTC timestamps).
            var now = DateTime.UtcNow;
            user.CreatedAt = now;
            user.UpdatedAt = now;
            user.CreatedByUserId = null; // set later when auth is implemented
            user.UpdatedByUserId = null; // set later when auth is implemented

            // Stage insert.
            _context.Add(user);

            try
            {
                // Persist to DB.
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to create user Email = {Email}", user.Email);

                ModelState.AddModelError("", "Unable to save user.");

                return View(user);
            }
        }

        // GET: Users/Edit/5
        /// <summary>
        /// Shows the Edit form for an existing DomainUser.
        ///
        /// Behavior:
        /// - Returns 404 if id is null or record not found (or deleted).
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Load the user being edited (non-deleted only).
            var user = await _context.DomainUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
            {
                _logger.LogWarning("user {user} not found.", user);
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Edit/5
        /// <summary>
        /// Processes form submission to update an existing DomainUser.
        ///
        /// Key concept:
        /// - DomainUsers represent business users. If they also have a login account (Identity user),
        ///   this method calls SyncIdentityAccessForDomainUserAsync(...) after saving DomainUser changes
        ///   to keep Identity role and lockout status consistent with DomainUser.UserType and DomainUser.IsActive.
        ///
        /// Security:
        /// - [ValidateAntiForgeryToken] provides CSRF protection.
        ///
        /// Binding:
        /// - [Bind(...)] limits posted properties to editable fields (prevents over-posting).
        ///
        /// Validation:
        /// - Removes navigation properties from ModelState.
        ///
        /// Error handling:
        /// - DbUpdateConcurrencyException: record changed/deleted since load.
        /// - InvalidOperationException: thrown by identity sync if role/lockout changes fail.
        /// - DbUpdateException: DB update failures.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Email,PhoneNumber,FirstName,LastName,AddressLine1,AddressLine2,City,State,PostalCode,DateOfBirth,DefaultPreference,UserType,IsActive")] A_New_Hope.Models.DomainUser formModel)
        {
            _logger.LogInformation("Updating user UserId={UserId}", id);

            // Enforce route/model id equality to prevent mismatched/tampered posts.
            if (id != formModel.Id)
            {
                _logger.LogWarning($"{id.ToString()} Not found.");
                return NotFound();
            }

            // Navigation properties are not posted by the form
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.CreatedByUser));
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.UpdatedByUser));
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.ClientProfile));

            // If validation fails, re-render the form with messages.
            if (!ModelState.IsValid)
            {
                return View(formModel);
            }

            // Load existing record to apply updates safely.
            var existing = await _context.DomainUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (existing == null)
            {
                return NotFound();
            }

            // Update editable fields only
            // (copy values from formModel into the tracked entity).
            existing.Email = formModel.Email;
            existing.PhoneNumber = formModel.PhoneNumber;
            existing.FirstName = formModel.FirstName;
            existing.LastName = formModel.LastName;
            existing.AddressLine1 = formModel.AddressLine1;
            existing.AddressLine2 = formModel.AddressLine2;
            existing.City = formModel.City;
            existing.State = formModel.State;
            existing.PostalCode = formModel.PostalCode;
            existing.DateOfBirth = formModel.DateOfBirth;
            existing.DefaultPreference = formModel.DefaultPreference;
            existing.UserType = formModel.UserType;
            existing.IsActive = formModel.IsActive;

            // Audit
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // set later when auth is implemented

            try
            {
                // Persist DomainUser changes first.
                await _context.SaveChangesAsync();

                // Keep Identity role + access in sync if this DomainUser has a login account
                await SyncIdentityAccessForDomainUserAsync(existing);

                _logger.LogInformation("User updated successfully. UserId={UserId}", id);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Concurrency issue: record may have been deleted/changed since it was loaded.
                if (!await UserExists(formModel.Id))
                {
                    _logger.LogError("User doesn't exist.");
                    return NotFound();
                }

                throw;
            }
            catch (InvalidOperationException ex)
            {
                // This exception is used in SyncIdentityAccessForDomainUserAsync(...) when Identity operations fail.
                // It is surfaced to the user as a ModelState error.
                ModelState.AddModelError("", ex.Message);

                _logger.LogError($"{ex.Message}");

                return View(formModel);
            }
            catch (DbUpdateException ex)
            {
                // General DB update failure.
                ModelState.AddModelError("", "Unable to save changes.");

                _logger.LogError($"{ex.Message}");

                return View(formModel);
            }
        }

        // GET: Users/Delete/5
        /// <summary>
        /// Shows the Delete confirmation page for a DomainUser.
        ///
        /// Notes:
        /// - This GET action does not delete anything.
        /// - Actual soft delete occurs in DeleteConfirmed.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogInformation($"{id} not found.");
                return NotFound();
            }

            // Load user for confirmation display (non-deleted only).
            var user = await _context.DomainUsers
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null)
            {
                _logger.LogInformation($"{user} not found.");
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        /// <summary>
        /// Executes the delete operation (soft delete) for a DomainUser.
        ///
        /// Soft delete strategy:
        /// - Sets DeletedAt
        /// - Updates UpdatedAt/UpdatedByUserId
        /// - Keeps the record in the DB for history/audit
        ///
        /// Error handling:
        /// - If SaveChanges fails, sets TempData["ErrorMessage"] and redirects back to the confirmation page.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            // Load the record to be deleted (must not already be soft deleted).
            var user = await _context.DomainUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            _logger.LogInformation("Soft deleting user UserId={UserId}", id);

            if (user == null)
            {
                _logger.LogInformation("UserId={UserId} not found.", id);
                return NotFound();
            }

            // Soft delete
            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedByUserId = null; // set later when auth is implemented

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = "Unable to delete user.";
                _logger.LogWarning(ex, "Failed to delete user UserId={UserId}", id);
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// SyncIdentityAccessForDomainUserAsync
        /// -----------------------------------
        /// This helper method keeps the Identity login account in sync with the DomainUser record.
        ///
        /// It performs TWO major sync operations:
        /// 1) Sync roles (Admin/Staff only)
        ///    - Removes any existing managed roles ("Admin" and "Staff") from the Identity user.
        ///    - Adds the target role based on DomainUser.UserType:
        ///        - Admin => "Admin"
        ///        - Staff => "Staff"
        ///        - Client/other => no managed role
        ///
        /// 2) Sync login enabled/disabled status
        ///    - Uses Identity lockout to control access:
        ///        - DomainUser.IsActive == true  => LockoutEnd = null (login enabled)
        ///        - DomainUser.IsActive == false => LockoutEnd = far-future date (login disabled)
        ///
        /// Behavior notes:
        /// - If no Identity user exists for this DomainUser, the method returns without throwing.
        /// - If Identity operations fail (removing roles, adding roles, updating lockout),
        ///   the method throws InvalidOperationException with a user-friendly message.
        /// </summary>
        private async Task SyncIdentityAccessForDomainUserAsync(DomainUser domainUser)
        {
            // Load the Identity user linked to this DomainUser.
            var appUser = await _context.Users
                .FirstOrDefaultAsync(iu => iu.DomainUserId == domainUser.Id);

            // No login account yet — nothing to sync
            if (appUser == null)
            {
                _logger.LogError($"{appUser} cannot be found.");
                return;
            }

            // -----------------------------
            // Sync roles (Admin / Staff only)
            // -----------------------------
            // Read the user's current Identity roles.
            var currentRoles = await _userManager.GetRolesAsync(appUser);

            // We only manage Admin/Staff roles here. Other roles (if any) are left alone.
            var managedRoles = currentRoles.Where(r => r == "Admin" || r == "Staff").ToList();

            // Remove existing managed roles first to ensure we don't end up with both Admin and Staff.
            if (managedRoles.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(appUser, managedRoles);
                if (!removeResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Failed to remove existing Identity roles: " +
                        string.Join(" | ", removeResult.Errors.Select(e => e.Description)));
                }
            }

            // Determine which role the Identity user should have based on DomainUser.UserType.
            var targetRole = domainUser.UserType switch
            {
                UserType.Admin => "Admin",
                UserType.Staff => "Staff",
                _ => null // Client = no login role
            };

            // If a target role exists, assign it.
            if (!string.IsNullOrWhiteSpace(targetRole))
            {
                var addResult = await _userManager.AddToRoleAsync(appUser, targetRole);
                if (!addResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Failed to assign Identity role: " +
                        string.Join(" | ", addResult.Errors.Select(e => e.Description)));
                }
            }

            // -----------------------------
            // Sync login enabled/disabled with DomainUser.IsActive
            // -----------------------------
            // Identity uses lockout to disable login:
            // - LockoutEnabled must be true
            // - LockoutEnd far in the future effectively disables login
            appUser.LockoutEnabled = true;

            if (domainUser.IsActive)
            {
                // Enable login (not locked out).
                appUser.LockoutEnd = null;
            }
            else
            {
                // Disable login (locked out until far future).
                appUser.LockoutEnd = new DateTimeOffset(new DateTime(2099, 12, 31, 23, 59, 59), TimeSpan.Zero);
            }

            // Persist Identity changes.
            var updateResult = await _userManager.UpdateAsync(appUser);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to sync Identity login status: " +
                    string.Join(" | ", updateResult.Errors.Select(e => e.Description)));
            }
        }

        /// <summary>
        /// Helper method used by the Edit POST concurrency handler.
        /// Confirms whether a non-deleted DomainUser exists for the provided Id.
        /// </summary>
        private async Task<bool> UserExists(ulong id)
        {
            return await _context.DomainUsers
                .AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}