using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages CRUD operations for DomainUser records.
    /// </summary>
    [Authorize(Roles = "Staff,Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UsersController> _logger;

        /// <summary>
        /// Creates the controller with the required services.
        /// </summary>
        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<UsersController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Users
        /// <summary>
        /// Displays all non-deleted domain users.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Retrieving users");

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
        /// <summary>
        /// Displays details for a single non-deleted domain user.
        /// </summary>
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
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        /// <summary>
        /// Creates a new domain user after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Email,PhoneNumber,FirstName,LastName,AddressLine1,AddressLine2,City,State,PostalCode,DateOfBirth,DefaultPreference,UserType,IsActive")] DomainUser user)
        {
            _logger.LogInformation("Creating user Email = {Email}", user.Email);

            ModelState.Remove(nameof(DomainUser.CreatedByUser));
            ModelState.Remove(nameof(DomainUser.UpdatedByUser));
            ModelState.Remove(nameof(DomainUser.ClientProfile));

            NormalizeDomainUser(user);
            await ApplyDomainUserValidationAsync(user);

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var now = DateTime.UtcNow;
            user.CreatedAt = now;
            user.UpdatedAt = now;
            user.CreatedByUserId = null;
            user.UpdatedByUserId = null;

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
        /// <summary>
        /// Shows the edit form for a single non-deleted domain user.
        /// </summary>
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
                _logger.LogWarning("User {UserId} not found for edit.", id);
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Edit/5
        /// <summary>
        /// Updates an existing domain user after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Email,PhoneNumber,FirstName,LastName,AddressLine1,AddressLine2,City,State,PostalCode,DateOfBirth,DefaultPreference,UserType,IsActive")] DomainUser formModel)
        {
            _logger.LogInformation("Updating user UserId={UserId}", id);

            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route id {RouteId} vs model id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            ModelState.Remove(nameof(DomainUser.CreatedByUser));
            ModelState.Remove(nameof(DomainUser.UpdatedByUser));
            ModelState.Remove(nameof(DomainUser.ClientProfile));

            NormalizeDomainUser(formModel);
            await ApplyDomainUserValidationAsync(formModel, formModel.Id);

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
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null;

            try
            {
                await _context.SaveChangesAsync();

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
                _logger.LogError("{Message}", ex.Message);
                return View(formModel);
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Unable to save changes.");
                _logger.LogError("{Message}", ex.Message);
                return View(formModel);
            }
        }

        // GET: Users/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted domain user.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogInformation("{Id} not found.", id);
                return NotFound();
            }

            var user = await _context.DomainUsers
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null)
            {
                _logger.LogInformation("User not found.");
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        /// <summary>
        /// Soft deletes a domain user.
        /// </summary>
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

            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedByUserId = null;

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
        /// Keeps a linked Identity account in sync with the domain user's role and active status.
        /// </summary>
        private async Task SyncIdentityAccessForDomainUserAsync(DomainUser domainUser)
        {
            var appUser = await _context.Users
                .FirstOrDefaultAsync(iu => iu.DomainUserId == domainUser.Id);

            if (appUser == null)
            {
                _logger.LogInformation("No linked Identity user found for DomainUserId {DomainUserId}.", domainUser.Id);
                return;
            }

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
                _ => null
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

            appUser.LockoutEnabled = true;

            if (domainUser.IsActive)
            {
                appUser.LockoutEnd = null;
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

        /// <summary>
        /// Trims strings and normalizes optional values.
        /// </summary>
        private static void NormalizeDomainUser(DomainUser model)
        {
            model.Email = model.Email?.Trim() ?? string.Empty;
            model.PhoneNumber = NullIfWhiteSpace(model.PhoneNumber);
            model.FirstName = NullIfWhiteSpace(model.FirstName);
            model.LastName = NullIfWhiteSpace(model.LastName);
            model.AddressLine1 = NullIfWhiteSpace(model.AddressLine1);
            model.AddressLine2 = NullIfWhiteSpace(model.AddressLine2);
            model.City = NullIfWhiteSpace(model.City);
            model.State = NullIfWhiteSpace(model.State)?.ToUpperInvariant();
            model.PostalCode = NullIfWhiteSpace(model.PostalCode);
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyDomainUserValidationAsync(DomainUser model, ulong? currentId = null)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(nameof(DomainUser.Email), "Email is required.");
            }

            if (!string.IsNullOrWhiteSpace(model.Email) && !IsValidEmail(model.Email))
            {
                ModelState.AddModelError(nameof(DomainUser.Email), "Email format is invalid.");
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var normalizedEmail = model.Email.ToLower();

                var duplicateEmailExists = await _context.DomainUsers
                    .AnyAsync(u =>
                        u.DeletedAt == null &&
                        u.Id != currentId &&
                        u.Email.ToLower() == normalizedEmail);

                if (duplicateEmailExists)
                {
                    ModelState.AddModelError(nameof(DomainUser.Email), "A user with this email already exists.");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.PhoneNumber) && !IsValidPhoneNumber(model.PhoneNumber))
            {
                ModelState.AddModelError(nameof(DomainUser.PhoneNumber), "Enter a valid US phone number with 10 digits, or 11 digits starting with 1.");
            }

            if (!string.IsNullOrWhiteSpace(model.FirstName) && !IsValidPersonName(model.FirstName))
            {
                ModelState.AddModelError(nameof(DomainUser.FirstName), "First Name contains invalid characters.");
            }

            if (!string.IsNullOrWhiteSpace(model.LastName) && !IsValidPersonName(model.LastName))
            {
                ModelState.AddModelError(nameof(DomainUser.LastName), "Last Name contains invalid characters.");
            }

            if (!string.IsNullOrWhiteSpace(model.City) && !IsValidCity(model.City))
            {
                ModelState.AddModelError(nameof(DomainUser.City), "City contains invalid characters.");
            }

            if (!string.IsNullOrWhiteSpace(model.State) && model.State != "WI")
            {
                ModelState.AddModelError(nameof(DomainUser.State), "State must be WI.");
            }

            if (!string.IsNullOrWhiteSpace(model.PostalCode) && !IsValidUsPostalCode(model.PostalCode))
            {
                ModelState.AddModelError(nameof(DomainUser.PostalCode), "Enter a valid US ZIP code or ZIP+4.");
            }

            if (model.DateOfBirth.HasValue)
            {
                var minDate = new DateOnly(1900, 1, 1);
                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                if (model.DateOfBirth.Value > today)
                {
                    ModelState.AddModelError(nameof(DomainUser.DateOfBirth), "Date of Birth cannot be in the future.");
                }

                if (model.DateOfBirth.Value < minDate)
                {
                    ModelState.AddModelError(nameof(DomainUser.DateOfBirth), "Date of Birth is earlier than the allowed minimum.");
                }
            }

            if (!Enum.IsDefined(typeof(PreferenceOption), model.DefaultPreference))
            {
                ModelState.AddModelError(nameof(DomainUser.DefaultPreference), "Select a valid default preference.");
            }

            if (!Enum.IsDefined(typeof(UserType), model.UserType))
            {
                ModelState.AddModelError(nameof(DomainUser.UserType), "Select a valid user type.");
            }
        }

        /// <summary>
        /// Returns null when the value is blank; otherwise returns the trimmed value.
        /// </summary>
        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Validates a practical US-style phone number.
        /// </summary>
        private static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (!Regex.IsMatch(phoneNumber, @"^\+?[0-9()\-\s]+$"))
            {
                return false;
            }

            var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

            if (digitsOnly.Length == 10)
            {
                return true;
            }

            if (digitsOnly.Length == 11 && digitsOnly.StartsWith("1"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Validates a practical email format for this project.
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            if (email.Contains(' '))
            {
                return false;
            }

            if (email.Count(c => c == '@') != 1)
            {
                return false;
            }

            if (email.Contains(".."))
            {
                return false;
            }

            var parts = email.Split('@');
            if (parts.Length != 2)
            {
                return false;
            }

            var localPart = parts[0];
            var domainPart = parts[1];

            if (string.IsNullOrWhiteSpace(localPart) || string.IsNullOrWhiteSpace(domainPart))
            {
                return false;
            }

            if (localPart.StartsWith('.') || localPart.EndsWith('.'))
            {
                return false;
            }

            if (domainPart.StartsWith('.') || domainPart.EndsWith('.'))
            {
                return false;
            }

            if (!domainPart.Contains('.'))
            {
                return false;
            }

            var domainLabels = domainPart.Split('.');
            if (domainLabels.Any(label => string.IsNullOrWhiteSpace(label)))
            {
                return false;
            }

            return Regex.IsMatch(localPart, @"^[A-Za-z0-9._+\-]+$")
                && Regex.IsMatch(domainPart, @"^[A-Za-z0-9.\-]+$");
        }

        /// <summary>
        /// Validates a person name using a practical character set.
        /// </summary>
        private static bool IsValidPersonName(string name)
        {
            return Regex.IsMatch(name, @"^[A-Za-z][A-Za-z\s'.-]*$");
        }

        /// <summary>
        /// Validates a city name using a practical character set.
        /// </summary>
        private static bool IsValidCity(string city)
        {
            return Regex.IsMatch(city, @"^[A-Za-z][A-Za-z\s'.-]*$");
        }

        /// <summary>
        /// Validates a US ZIP code or ZIP+4.
        /// </summary>
        private static bool IsValidUsPostalCode(string postalCode)
        {
            return Regex.IsMatch(postalCode, @"^\d{5}(-\d{4})?$");
        }

        /// <summary>
        /// Returns true if the non-deleted domain user exists.
        /// </summary>
        private async Task<bool> UserExists(ulong id)
        {
            return await _context.DomainUsers
                .AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}