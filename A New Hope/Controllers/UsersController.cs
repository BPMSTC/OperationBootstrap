using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Models.Enums;
using A_New_Hope.Models.Inputs;
using A_New_Hope.Models.ViewModels;
using A_New_Hope.Models.ViewModels.ClientViewModels;
using A_New_Hope.Models.ViewModels.Referrals;
using A_New_Hope.Models.ViewModels.Users;
using A_New_Hope.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
        public async Task<IActionResult> Index(string searchTerm, string filter)
        {
            try
            {
                _logger.LogInformation("Retrieving users");

                var query = _context.DomainUsers
                    .Where(u => u.DeletedAt == null);

                // Search
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

                // Filter
                filter ??= "all";

                if (!User.IsInRole("Admin"))
                {
                    // Non-admins only see clients.
                    query = query.Where(u =>
                        u.UserType != UserType.Admin &&
                        u.UserType != UserType.Staff);
                }
                else
                {
                    switch (filter)
                    {
                        case "clients":
                            query = query.Where(u => u.UserType == UserType.Client);
                            break;

                        case "staff":
                            query = query.Where(u =>
                                u.UserType == UserType.Admin ||
                                u.UserType == UserType.Staff);
                            break;
                    }
                }

                // Retrieve the filtered users.
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
                    Email = u.Email ?? string.Empty,
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
                ViewData["UserFilter"] = filter;

                _logger.LogInformation("Retrieved {UserCount} users", users.Count);

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve users.");
                return View("Error");
            }
        }


        private const string WizardDraftKey = "WizardDraft";

        private void SaveDraft(DomainUser user, List<HouseholdMember> members)
        {
            var draft = new
            {
                User = user,
                Household = members
            };

            HttpContext.Session.SetString(
                WizardDraftKey,
                System.Text.Json.JsonSerializer.Serialize(draft)
            );
        }

        private (DomainUser user, List<HouseholdMember> household)? LoadDraft()
        {
            var json = HttpContext.Session.GetString(WizardDraftKey);

            if (string.IsNullOrEmpty(json))
                return null;

            var draft = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);

            return null; // (we’ll simplify below in controller usage)
        }




        // GET: Users/Details/5
        /// <summary>
        /// Displays details for a single non-deleted domain user.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            try
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
                    ClientIncomes = new List<ClientIncome>(),
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

                    vm.ClientIncomes = await _context.ClientIncomes
                        .Where(ci =>
                            ci.ClientProfileUserId == user.Id &&
                            ci.DeletedAt == null &&
                            ci.IsActive)
                        .OrderBy(ci => ci.IncomeType)
                        .ThenBy(ci => ci.Id)
                        .ToListAsync();

                    vm.Referrals = await _context.Referrals
                        .Include(r => r.ReferringOrganization)
                        .Where(r => r.ClientUserId == user.Id && r.DeletedAt == null)
                        .OrderByDescending(r => r.ReferredOn)
                        .ToListAsync();
                }

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load user details for UserId={UserId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading user details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Users/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public IActionResult Create()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load create user page.");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the create form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Users/Create
        /// <summary>
        /// Creates a new domain user after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Email,PhoneNumber,FirstName,LastName,AddressLine1,AddressLine2,City,State,PostalCode,DateOfBirth,DefaultPreference,UserType,IsActive")] DomainUser user)
        {
            try
            {
                _logger.LogInformation("Creating user Email = {Email}", user.Email);

                // Remove navigation properties that are not posted by the form.
                ModelState.Remove(nameof(DomainUser.CreatedByUser));
                ModelState.Remove(nameof(DomainUser.UpdatedByUser));
                ModelState.Remove(nameof(DomainUser.ClientProfile));

                // Normalize incoming values before business-rule validation.
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
                            IsUnhoused = false,
                            Incomes = new List<ClientIncomeEntryInput>()
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating user Email = {Email}", user?.Email);
                ModelState.AddModelError("", "An unexpected error occurred while creating the user.");
                return View(user);
            }
        }

        // GET: Users/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted domain user.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load edit form for UserId={UserId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Users/Edit/5
        /// <summary>
        /// Updates an existing domain user after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Email,PhoneNumber,FirstName,LastName,AddressLine1,AddressLine2,City,State,PostalCode,DateOfBirth,DefaultPreference,UserType,IsActive")] DomainUser formModel)
        {
            try
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
                catch (DbUpdateConcurrencyException ex)
                {
                    // Check whether the record was deleted during the edit attempt.
                    if (!await UserExists(formModel.Id))
                    {
                        _logger.LogError("User doesn't exist.");
                        return NotFound();
                    }

                    _logger.LogError(ex, "Concurrency error updating UserId={UserId}", id);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating user UserId={UserId}", id);
                ModelState.AddModelError("", "An unexpected error occurred while updating the user.");
                return View(formModel);
            }
        }

        // GET: Users/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted domain user.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load delete page for UserId={UserId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the delete page.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Users/Delete/5
        /// <summary>
        /// Soft deletes a domain user.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting user UserId={UserId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the user.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }



        // =========================================================
        // CREATE WIZARD
        // =========================================================


        public IActionResult CreateWizard()
        {
            var json = HttpContext.Session.GetString("WizardUser");
            var extrasJson = HttpContext.Session.GetString("WizardUserExtras");

            var vm = new UserWizardViewModel();

            // ================= USER =================
            if (!string.IsNullOrWhiteSpace(json))
            {
                vm.User = JsonSerializer.Deserialize<DomainUser>(json) ?? new DomainUser();
            }

            // ================= FULL RESTORE =================
            if (!string.IsNullOrWhiteSpace(extrasJson))
            {
                vm = JsonSerializer.Deserialize<UserWizardViewModel>(extrasJson)
                     ?? new UserWizardViewModel();
            }

            // ================= ENSURE AT LEAST ONE INCOME ROW =================
            if (vm.Incomes == null || vm.Incomes.Count == 0)
            {
                vm.Incomes = new List<UserIncomeInput>
        {
            new UserIncomeInput()
        };
            }

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWizard(UserWizardViewModel vm)
        {
            try
            {
                NormalizeDomainUser(vm.User);
                await ApplyDomainUserValidationAsync(vm.User);

                // ROLE RULE FIRST
                if (!User.IsInRole("Admin"))
                {
                    vm.User.UserType = UserType.Client;
                }

                // 🔴 HARD RULE: wipe income BEFORE persistence
                if (vm.User.UserType is UserType.Admin or UserType.Staff)
                {
                    vm.Incomes = new List<UserIncomeInput>();
                }

                // =========================
                // MODELSTATE CLEANUP
                // =========================

                ModelState.Remove(nameof(DomainUser.CreatedByUser));
                ModelState.Remove(nameof(DomainUser.UpdatedByUser));
                ModelState.Remove(nameof(DomainUser.ClientProfile));

                // Remove ALL income-related validation errors
                foreach (var key in ModelState.Keys
                    .Where(k => k.Contains("Income"))
                    .ToList())
                {
                    ModelState.Remove(key);
                }

                // Debug validation issues (keep for now)
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogWarning("ModelState error {Key}: {Error}",
                            state.Key, error.ErrorMessage);
                    }
                }

                // =========================
                // FINAL VALIDATION CHECK
                // =========================
                if (!ModelState.IsValid)
                {
                    return View(vm);
                }

                // =========================
                // SESSION PERSISTENCE
                // =========================

                HttpContext.Session.SetString(
                    "WizardUser",
                    JsonSerializer.Serialize(vm.User)
                );

                HttpContext.Session.SetString(
                    "WizardUserExtras",
                    JsonSerializer.Serialize(vm)
                );

                // =========================
                // NAVIGATION
                // =========================
                return vm.User.UserType is UserType.Admin or UserType.Staff
                    ? RedirectToAction("Finalize")
                    : RedirectToAction(nameof(HouseholdMembers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateWizard failed");
                ModelState.AddModelError("", "Error creating user draft.");
                return View(vm);
            }
        }

        // =========================================================
        // HOUSEHOLD MEMBERS 
        // =========================================================

        public IActionResult HouseholdMembers()
        {
            var userJson = HttpContext.Session.GetString("WizardUser");

            if (string.IsNullOrEmpty(userJson))
                return RedirectToAction(nameof(CreateWizard));

            var model = JsonSerializer.Deserialize<DomainUser>(userJson);

            var householdJson = HttpContext.Session.GetString("WizardHousehold");

            var members = string.IsNullOrWhiteSpace(householdJson)
                ? new List<HouseholdMember>()
                : JsonSerializer.Deserialize<List<HouseholdMember>>(householdJson) ?? new List<HouseholdMember>();

            // If empty, show one blank row
            if (members.Count == 0)
                members.Add(new HouseholdMember());

            ViewBag.HouseholdMembers = members;

            // ================= AUTO SET YES/NO =================
            ViewBag.HasHouseholdMembers =
                members.Any(m =>
                    !string.IsNullOrWhiteSpace(m.FirstName) ||
                    !string.IsNullOrWhiteSpace(m.LastName) ||
                    m.DateOfBirth != null ||
                    m.ApproximateAge != null
                )
                ? "Yes"
                : "No";

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HouseholdMembers(string HasHouseholdMembers, IFormCollection form)
        {
            var userJson = HttpContext.Session.GetString("WizardUser");

            if (string.IsNullOrEmpty(userJson))
                return RedirectToAction(nameof(CreateWizard));

            // ================= HANDLE "NO HOUSEHOLD" =================
            if (HasHouseholdMembers == "No")
            {
                HttpContext.Session.Remove("WizardHousehold");
                HttpContext.Session.SetString("HasHouseholdMembers", "No");
                return RedirectToAction(nameof(Finalize));
            }

            var members = new List<HouseholdMember>();
            int index = 0;

            while (true)
            {
                var key = $"HouseholdMembers[{index}].FirstName";

                if (!form.ContainsKey(key))
                    break;

                var firstName = form[key];

                if (string.IsNullOrWhiteSpace(firstName))
                {
                    index++;
                    continue;
                }

                members.Add(new HouseholdMember
                {
                    FirstName = firstName,
                    LastName = form[$"HouseholdMembers[{index}].LastName"],
                    DateOfBirth = DateTime.TryParse(form[$"HouseholdMembers[{index}].DateOfBirth"], out var dob) ? dob : null,
                    ApproximateAge = int.TryParse(form[$"HouseholdMembers[{index}].ApproximateAge"], out var age) ? age : null
                });

                index++;
            }

            // ALWAYS persist (this is the key fix)
            HttpContext.Session.SetString(
                "WizardHousehold",
                JsonSerializer.Serialize(members)
            );

            HttpContext.Session.SetString("HasHouseholdMembers", "Yes");

            return RedirectToAction(nameof(Finalize));
        }


        // =========================================================
        // FINALIZE (REVIEW PAGE)
        // =========================================================

        public IActionResult Finalize()
        {
            // ================= LOAD WIZARD STATE =================
            var stateJson = HttpContext.Session.GetString("WizardUserExtras");

            if (string.IsNullOrWhiteSpace(stateJson))
                return RedirectToAction(nameof(CreateWizard));

            var vm = JsonSerializer.Deserialize<UserWizardViewModel>(stateJson)
                     ?? new UserWizardViewModel();

            // ================= USER FALLBACK =================
            var userJson = HttpContext.Session.GetString("WizardUser");

            if (!string.IsNullOrWhiteSpace(userJson))
            {
                vm.User = JsonSerializer.Deserialize<DomainUser>(userJson)
                          ?? vm.User;
            }

            vm.User ??= new DomainUser();

            // ================= HOUSEHOLD =================
            var householdJson = HttpContext.Session.GetString("WizardHousehold");

            ViewBag.HouseholdMembers =
                string.IsNullOrWhiteSpace(householdJson)
                    ? new List<HouseholdMember>()
                    : JsonSerializer.Deserialize<List<HouseholdMember>>(householdJson)
                      ?? new List<HouseholdMember>();

            // ================= INCOME =================
            var incomes = vm.Incomes ?? new List<UserIncomeInput>();

            _logger.LogInformation("Income count loaded in Finalize: {Count}", incomes.Count);

            ViewBag.Incomes = incomes;

            return View(vm.User);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeConfirm()
        {
            try
            {
                var userJson = HttpContext.Session.GetString("WizardUser");

                if (string.IsNullOrWhiteSpace(userJson))
                    return RedirectToAction(nameof(CreateWizard));

                var user = JsonSerializer.Deserialize<DomainUser>(userJson);

                if (user == null)
                    return RedirectToAction(nameof(CreateWizard));

                var householdJson = HttpContext.Session.GetString("WizardHousehold");

                var household = string.IsNullOrWhiteSpace(householdJson)
                    ? new List<HouseholdMember>()
                    : JsonSerializer.Deserialize<List<HouseholdMember>>(householdJson)
                      ?? new List<HouseholdMember>();

                // OPTIONAL: income restore (NO DB mapping yet)
                var extrasJson = HttpContext.Session.GetString("WizardUserExtras");
                List<UserIncomeInput> incomes = new();

                if (!string.IsNullOrWhiteSpace(extrasJson))
                {
                    using var doc = JsonDocument.Parse(extrasJson);

                    if (doc.RootElement.TryGetProperty("Incomes", out var inc))
                    {
                        incomes = JsonSerializer.Deserialize<List<UserIncomeInput>>(inc.GetRawText())
                                  ?? new List<UserIncomeInput>();
                    }
                }

                // ================= USER SETUP =================
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                user.IsActive = true;
                user.Id = 0;

                _context.DomainUsers.Add(user);
                await _context.SaveChangesAsync();

                // ================= HOUSEHOLD =================
                foreach (var h in household)
                {
                    h.ClientUserId = user.Id;
                    h.Id = 0;
                }

                if (household.Count > 0)
                    _context.HouseholdMembers.AddRange(household);

                await _context.SaveChangesAsync();

                // ================= CLEANUP =================
                HttpContext.Session.Remove("WizardUser");
                HttpContext.Session.Remove("WizardHousehold");
                HttpContext.Session.Remove("WizardUserExtras");
                HttpContext.Session.Remove("HasHouseholdMembers");

                return RedirectToAction(nameof(Details), new { id = user.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FinalizeConfirm failed");

                TempData["ErrorMessage"] =
                    "An unexpected error occurred while saving the user. Please try again.";

                return RedirectToAction(nameof(CreateWizard));
            }
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
            model.FirstName = model.FirstName?.Trim() ?? string.Empty;
            model.LastName = model.LastName?.Trim() ?? string.Empty;

            model.Email = NullIfWhiteSpace(model.Email);
            model.PhoneNumber = NullIfWhiteSpace(model.PhoneNumber);
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
            if (!string.IsNullOrWhiteSpace(model.Email) && !IsValidEmail(model.Email))
            {
                ModelState.AddModelError(nameof(DomainUser.Email), "Email format is invalid.");
            }

            if (!string.IsNullOrWhiteSpace(model.PhoneNumber) && !IsValidPhoneNumber(model.PhoneNumber))
            {
                ModelState.AddModelError(nameof(DomainUser.PhoneNumber), "Enter a valid US phone number with 10 digits, or 11 digits starting with 1.");
            }

            if (string.IsNullOrWhiteSpace(model.FirstName))
            {
                ModelState.AddModelError(nameof(DomainUser.FirstName), "First name is required.");
            }
            else if (!IsValidPersonName(model.FirstName))
            {
                ModelState.AddModelError(nameof(DomainUser.FirstName), "First Name contains invalid characters.");
            }

            if (string.IsNullOrWhiteSpace(model.LastName))
            {
                ModelState.AddModelError(nameof(DomainUser.LastName), "Last name is required.");
            }
            else if (!IsValidPersonName(model.LastName))
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