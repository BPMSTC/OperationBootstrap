using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    [Authorize(Roles = "Staff,Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<UsersController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation($"Retrieving users");
            try
            {           
                var domainUsers = await _context.DomainUsers
                .Where(u => u.DeletedAt == null)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            var identityLinks = await _context.Users
                .Where(iu => iu.DomainUserId != null)
                .Select(iu => new { iu.Id, iu.DomainUserId })
                .ToListAsync();

            var identityByDomainUserId = identityLinks
                .Where(x => x.DomainUserId.HasValue)
                .ToDictionary(x => x.DomainUserId!.Value, x => x.Id);

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
                    HasLoginAccount = identityByDomainUserId.ContainsKey(u.Id),
                    IdentityUserId = identityByDomainUserId.TryGetValue(u.Id, out var identityId) ? identityId : null
                }).ToList();

                _logger.LogInformation("Retrieved {UserCount} users", users.Count);

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve users.");

                return View("Error");
            }
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                _logger.LogInformation("Details requested with null id");
                return NotFound();
            }

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
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Email,PhoneNumber,FirstName,LastName,AddressLine1,AddressLine2,City,State,PostalCode,DateOfBirth,DefaultPreference,UserType,IsActive")] A_New_Hope.Models.DomainUser user)
        {
            _logger.LogInformation(
                "creating user Email = {Email}", user.Email);

            // Navigation properties are not posted by the form
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.CreatedByUser));
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.UpdatedByUser));
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.ClientProfile));

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var now = DateTime.UtcNow;
            user.CreatedAt = now;
            user.UpdatedAt = now;
            user.CreatedByUserId = null; // set later when auth is implemented
            user.UpdatedByUserId = null; // set later when auth is implemented

            _context.Add(user);

            try
            {
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
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Email,PhoneNumber,FirstName,LastName,AddressLine1,AddressLine2,City,State,PostalCode,DateOfBirth,DefaultPreference,UserType,IsActive")] A_New_Hope.Models.DomainUser formModel)
        {
            _logger.LogInformation("Updating user UserId={UserId}", id);

            if (id != formModel.Id)
            {
                _logger.LogWarning($"{id.ToString()} Not found.");
                return NotFound();
            }

            // Navigation properties are not posted by the form
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.CreatedByUser));
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.UpdatedByUser));
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.ClientProfile));

            if (!ModelState.IsValid)
            {
                return View(formModel);
            }

            var existing = await _context.DomainUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (existing == null)
            {
                return NotFound();
            }

            // Update editable fields only
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
                await _context.SaveChangesAsync();

                // Keep Identity role + access in sync if this DomainUser has a login account
                await SyncIdentityAccessForDomainUserAsync(existing);

                _logger.LogInformation("User updated successfully. UserId={UserId}", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await UserExists(formModel.Id))
                {
                    _logger.LogError("User doesn't exist.");
                    return NotFound();
                }

                throw;
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                _logger.LogError($"{ex.Message}");
                return View(formModel);
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Unable to save changes.");
                _logger.LogError($"{ex.Message}");
                return View(formModel);
            }
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogInformation($"{id} not found.");
                return NotFound();
            }

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
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
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
                _logger.LogWarning("Failed to delete user UserId={UserId}", id);
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task SyncIdentityAccessForDomainUserAsync(DomainUser domainUser)
        {
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
            var currentRoles = await _userManager.GetRolesAsync(appUser);
            var managedRoles = currentRoles.Where(r => r == "Admin" || r == "Staff").ToList();

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

            var targetRole = domainUser.UserType switch
            {
                UserType.Admin => "Admin",
                UserType.Staff => "Staff",
                _ => null // Client = no login role
            };

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
            appUser.LockoutEnabled = true;

            if (domainUser.IsActive)
            {
                appUser.LockoutEnd = null; // enable login
            }
            else
            {
                appUser.LockoutEnd = new DateTimeOffset(new DateTime(2099, 12, 31, 23, 59, 59), TimeSpan.Zero);
            }

            var updateResult = await _userManager.UpdateAsync(appUser);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to sync Identity login status: " +
                    string.Join(" | ", updateResult.Errors.Select(e => e.Description)));
            }
        }

        private async Task<bool> UserExists(ulong id)
        {
            return await _context.DomainUsers
                .AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}