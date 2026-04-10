using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Models.Inputs;
using A_New_Hope.Models.ViewModels;
using A_New_Hope.Models.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using A_New_Hope.Models.ViewModels.Referrals;
using A_New_Hope.Services.Interfaces;

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
        private readonly IClientCreationService _clientCreationService;

        /// <summary>
        /// Creates the controller with the required services.
        /// </summary>
        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<UsersController> logger,
            IClientCreationService clientCreationService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _clientCreationService = clientCreationService;
        }

        // GET: Users
        /// <summary>
        /// Displays all non-deleted domain users.
        /// </summary>
        public async Task<IActionResult> Index(string searchTerm)
        {
            _logger.LogInformation("Retrieving users");

            try
            {
                var query = _context.DomainUsers
                    .Where(u => u.DeletedAt == null);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim();
                    var digitsOnly = new string(searchTerm.Where(char.IsDigit).ToArray());

                    query = query.Where(u =>
                        (u.Email != null && u.Email.Contains(searchTerm)) ||

                        (u.FirstName != null && u.FirstName.Contains(searchTerm)) ||
                        (u.LastName != null && u.LastName.Contains(searchTerm)) ||

                        (u.FirstName != null && u.LastName != null &&
                            (u.FirstName + " " + u.LastName).Contains(searchTerm)) ||

                        (u.City != null && u.City.Contains(searchTerm)) ||
                        (u.State != null && u.State.Contains(searchTerm)) ||
                        (u.PostalCode != null && u.PostalCode.Contains(searchTerm)) ||

                        (u.AddressLine1 != null && u.AddressLine1.Contains(searchTerm)) ||
                        (u.AddressLine2 != null && u.AddressLine2.Contains(searchTerm)) ||

                        (u.PhoneNumber != null &&
                            u.PhoneNumber.Replace(" ", "").Replace("-", "")
                            .Contains(digitsOnly))
                    );
                }

                var domainUsers = await query
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

                ViewData["CurrentFilter"] = searchTerm;

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
            // Reject requests with no id.
            if (id == null)
            {
                _logger.LogInformation("Details requested with null id");
                return NotFound();
            }

            // Retrieve the requested non-deleted domain user.
            var user = await _context.DomainUsers
                .Where(u => u.DeletedAt == null)
                .Include(u => u.CreatedByUser)
                .Include(u => u.UpdatedByUser)
                .Include(u => u.ClientProfile)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Return not found when the user does not exist.
            if (user == null)
            {
                _logger.LogInformation("User not found. UserId = {UserID}", id);
                return NotFound();
            }

            var linkedApplicationUser = await _context.Users
                .Where(au => au.DomainUserId == user.Id)
                .Select(au => new
                {
                    au.Id
                })
                .FirstOrDefaultAsync();

            var vm = new UserDetailsViewModel
            {
                User = user,
                ClientProfile = user.ClientProfile,
                HouseholdMembers = new List<HouseholdMember>(),
                Referrals = new List<Referral>(),
                HasLoginAccount = linkedApplicationUser != null,
                IdentityUserId = linkedApplicationUser?.Id
            };

            if (user.UserType == UserType.Client)
            {
                vm.HouseholdMembers = await _context.HouseholdMembers
                    .Where(h => h.ClientUserId == user.Id && h.DeletedAt == null)
                    .OrderBy(h => h.LastName)
                    .ThenBy(h => h.FirstName)
                    .ToListAsync();

                vm.Referrals = await _context.Referrals
                    .Include(r => r.ReferringOrganization)
                    .Where(r => r.ClientUserId == user.Id && r.DeletedAt == null)
                    .OrderByDescending(r => r.ReferredOn)
                    .ToListAsync();
            }

            return View(vm);
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

            // Client path: use the reusable client-creation service.
            if (user.UserType == UserType.Client)
            {
                try
                {
                    var input = new ClientEntryInput
                    {
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        AddressLine1 = user.AddressLine1,
                        AddressLine2 = user.AddressLine2,
                        City = user.City,
                        State = user.State,
                        PostalCode = user.PostalCode,
                        DateOfBirth = user.DateOfBirth,
                        EmploymentStatus = null,
                        EarnedIncomeMonthly = null,
                        IsUnhoused = false
                    };

                    var clientId = await _clientCreationService.CreateClientAndReturnIdAsync(
                        input,
                        householdInputs: new List<HouseholdMemberEntryInput>(),
                        actingUserId: null);

                    _logger.LogInformation("Client user created successfully. UserId = {UserId}", clientId);
                    return RedirectToAction(nameof(Details), new { id = clientId });
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Business validation failed while creating client Email = {Email}", user.Email);

                    ModelState.AddModelError(string.Empty, ex.Message);
                    return View(user);
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "Argument validation failed while creating client Email = {Email}", user.Email);

                    ModelState.AddModelError(string.Empty, ex.Message);
                    return View(user);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Failed to create client Email = {Email}", user.Email);

                    ModelState.AddModelError("", "Unable to save user.");
                    return View(user);
                }
            }

            // Non-client path: keep existing direct-create behavior for Staff/Admin.
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
            // Reject requests with no id.
            if (id == null)
            {
                return NotFound();
            }

            // Retrieve the requested non-deleted user for editing.
            var user = await _context.DomainUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            // Return not found when the user does not exist.
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

            // Ensure the route id matches the posted model id.
            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route id {RouteId} vs model id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            // Remove navigation properties that are not posted by the form.
            ModelState.Remove(nameof(DomainUser.CreatedByUser));
            ModelState.Remove(nameof(DomainUser.UpdatedByUser));
            ModelState.Remove(nameof(DomainUser.ClientProfile));

            // Normalize incoming values before business-rule validation.
            NormalizeDomainUser(formModel);
            await ApplyDomainUserValidationAsync(formModel, formModel.Id);

            // Return the form when validation fails.
            if (!ModelState.IsValid)
            {
                return View(formModel);
            }

            // Retrieve the existing non-deleted domain user record.
            var existing = await _context.DomainUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            // Return not found when the target record no longer exists.
            if (existing == null)
            {
                return NotFound();
            }

            // Copy validated form values into the tracked entity.
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

                // Sync linked Identity role and lockout status after saving domain changes.
                await SyncIdentityAccessForDomainUserAsync(existing);

                _logger.LogInformation("User updated successfully. UserId={UserId}", id);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Check whether the record was deleted during the edit attempt.
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
            // Reject requests with no id.
            if (id == null)
            {
                _logger.LogInformation("{Id} not found.", id);
                return NotFound();
            }

            // Retrieve the requested non-deleted user for delete confirmation.
            var user = await _context.DomainUsers
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Return not found when the user does not exist.
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
            // Retrieve the active domain user targeted for soft delete.
            var user = await _context.DomainUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            _logger.LogInformation("Soft deleting user UserId={UserId}", id);

            // Return not found when the user does not exist.
            if (user == null)
            {
                _logger.LogInformation("UserId={UserId} not found.", id);
                return NotFound();
            }

            // Apply soft-delete and audit values.
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
            // Find the linked Identity account for this domain user.
            var appUser = await _context.Users
                .FirstOrDefaultAsync(iu => iu.DomainUserId == domainUser.Id);

            // Exit when no linked Identity account exists.
            if (appUser == null)
            {
                _logger.LogInformation("No linked Identity user found for DomainUserId {DomainUserId}.", domainUser.Id);
                return;
            }

            // Retrieve the user's current Identity roles.
            var currentRoles = await _userManager.GetRolesAsync(appUser);
            var managedRoles = currentRoles.Where(r => r == "Admin" || r == "Staff").ToList();

            // Remove existing managed roles before applying the new one.
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

            // Determine the correct Identity role from the domain user type.
            var targetRole = domainUser.UserType switch
            {
                UserType.Admin => "Admin",
                UserType.Staff => "Staff",
                _ => null
            };

            // Assign the appropriate managed role when applicable.
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

            // Ensure lockout is enabled so inactive users can be blocked from login.
            appUser.LockoutEnabled = true;

            // Clear or apply lockout based on the domain user's active status.
            if (domainUser.IsActive)
            {
                appUser.LockoutEnd = null;
            }
            else
            {
                appUser.LockoutEnd = new DateTimeOffset(new DateTime(2099, 12, 31, 23, 59, 59), TimeSpan.Zero);
            }

            // Save Identity account changes.
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
            // Normalize and trim user-entered string values.
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
            // Require email for all domain users.
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(nameof(DomainUser.Email), "Email is required.");
            }

            // Validate email format when provided.
            if (!string.IsNullOrWhiteSpace(model.Email) && !IsValidEmail(model.Email))
            {
                ModelState.AddModelError(nameof(DomainUser.Email), "Email format is invalid.");
            }

            // Prevent duplicate active email addresses.
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

            // Validate phone number format when provided.
            if (!string.IsNullOrWhiteSpace(model.PhoneNumber) && !IsValidPhoneNumber(model.PhoneNumber))
            {
                ModelState.AddModelError(nameof(DomainUser.PhoneNumber), "Enter a valid US phone number with 10 digits, or 11 digits starting with 1.");
            }

            // Validate first name characters when provided.
            if (!string.IsNullOrWhiteSpace(model.FirstName) && !IsValidPersonName(model.FirstName))
            {
                ModelState.AddModelError(nameof(DomainUser.FirstName), "First Name contains invalid characters.");
            }

            // Validate last name characters when provided.
            if (!string.IsNullOrWhiteSpace(model.LastName) && !IsValidPersonName(model.LastName))
            {
                ModelState.AddModelError(nameof(DomainUser.LastName), "Last Name contains invalid characters.");
            }

            // Validate city characters when provided.
            if (!string.IsNullOrWhiteSpace(model.City) && !IsValidCity(model.City))
            {
                ModelState.AddModelError(nameof(DomainUser.City), "City contains invalid characters.");
            }

            // Restrict state values to Wisconsin for this project.
            if (!string.IsNullOrWhiteSpace(model.State) && model.State != "WI")
            {
                ModelState.AddModelError(nameof(DomainUser.State), "State must be WI.");
            }

            // Validate ZIP code format when provided.
            if (!string.IsNullOrWhiteSpace(model.PostalCode) && !IsValidUsPostalCode(model.PostalCode))
            {
                ModelState.AddModelError(nameof(DomainUser.PostalCode), "Enter a valid US ZIP code or ZIP+4.");
            }

            // Validate date of birth range when provided.
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

            // Validate that the selected default preference is defined.
            if (!Enum.IsDefined(typeof(PreferenceOption), model.DefaultPreference))
            {
                ModelState.AddModelError(nameof(DomainUser.DefaultPreference), "Select a valid default preference.");
            }

            // Validate that the selected user type is defined.
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
            // Convert blank strings to null after trimming.
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Validates a practical US-style phone number.
        /// </summary>
        private static bool IsValidPhoneNumber(string phoneNumber)
        {
            // Reject characters outside the allowed phone number pattern.
            if (!Regex.IsMatch(phoneNumber, @"^\+?[0-9()\-\s]+$"))
            {
                return false;
            }

            // Strip formatting characters to validate digit count.
            var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // Accept standard 10-digit US phone numbers.
            if (digitsOnly.Length == 10)
            {
                return true;
            }

            // Accept 11-digit US phone numbers only when starting with 1.
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
            // Reject spaces in email addresses.
            if (email.Contains(' '))
            {
                return false;
            }

            // Require exactly one @ symbol.
            if (email.Count(c => c == '@') != 1)
            {
                return false;
            }

            // Reject consecutive periods.
            if (email.Contains(".."))
            {
                return false;
            }

            // Split the email into local and domain parts.
            var parts = email.Split('@');
            if (parts.Length != 2)
            {
                return false;
            }

            var localPart = parts[0];
            var domainPart = parts[1];

            // Require non-empty local and domain parts.
            if (string.IsNullOrWhiteSpace(localPart) || string.IsNullOrWhiteSpace(domainPart))
            {
                return false;
            }

            // Reject local parts starting or ending with a period.
            if (localPart.StartsWith('.') || localPart.EndsWith('.'))
            {
                return false;
            }

            // Reject domain parts starting or ending with a period.
            if (domainPart.StartsWith('.') || domainPart.EndsWith('.'))
            {
                return false;
            }

            // Require a dot in the domain portion.
            if (!domainPart.Contains('.'))
            {
                return false;
            }

            // Reject empty domain labels.
            var domainLabels = domainPart.Split('.');
            if (domainLabels.Any(label => string.IsNullOrWhiteSpace(label)))
            {
                return false;
            }

            // Validate local and domain characters using project regex rules.
            return Regex.IsMatch(localPart, @"^[A-Za-z0-9._+\-]+$")
                && Regex.IsMatch(domainPart, @"^[A-Za-z0-9.\-]+$");
        }

        /// <summary>
        /// Validates a person name using a practical character set.
        /// </summary>
        private static bool IsValidPersonName(string name)
        {
            // Allow letters plus common punctuation for personal names.
            return Regex.IsMatch(name, @"^[A-Za-z][A-Za-z\s'.-]*$");
        }

        /// <summary>
        /// Validates a city name using a practical character set.
        /// </summary>
        private static bool IsValidCity(string city)
        {
            // Allow letters plus common punctuation for city names.
            return Regex.IsMatch(city, @"^[A-Za-z][A-Za-z\s'.-]*$");
        }

        /// <summary>
        /// Validates a US ZIP code or ZIP+4.
        /// </summary>
        private static bool IsValidUsPostalCode(string postalCode)
        {
            // Accept 5-digit ZIP codes and ZIP+4 values.
            return Regex.IsMatch(postalCode, @"^\d{5}(-\d{4})?$");
        }

        /// <summary>
        /// Returns true if the non-deleted domain user exists.
        /// </summary>
        private async Task<bool> UserExists(ulong id)
        {
            // Check whether the requested active domain user still exists.
            return await _context.DomainUsers
                .AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}