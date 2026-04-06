/*

using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages create, read, update, and soft delete operations for referrals,
    /// plus the multi-step referral wizard workflow.
    /// </summary>
    public class ReferralsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReferralsController> _logger;

        private const string ReferralWizardSessionKey = "ReferralWizard.Step1";

        // Store the allowed 2-letter US state codes for validation.
        private static readonly HashSet<string> ValidUsStateCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "AL","AK","AZ","AR","CA","CO","CT","DE","FL","GA",
            "HI","ID","IL","IN","IA","KS","KY","LA","ME","MD",
            "MA","MI","MN","MS","MO","MT","NE","NV","NH","NJ",
            "NM","NY","NC","ND","OH","OK","OR","PA","RI","SC",
            "SD","TN","TX","UT","VT","VA","WA","WV","WI","WY","DC"
        };

        /// <summary>
        /// Creates the controller with the required database context and logger.
        /// </summary>
        public ReferralsController(ApplicationDbContext context, ILogger<ReferralsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Referrals
        /// <summary>
        /// Displays all non-deleted referrals.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading Referrals Index page");

            var referrals = await _context.Referrals
                .Where(r => r.DeletedAt == null)
                .Include(r => r.ClientUser)
                .Include(r => r.ReferringOrganization)
                .OrderByDescending(r => r.ReferredOn)
                .ThenBy(r => r.Id)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} referrals", referrals.Count);

            return View(referrals);
        }

        // GET: Referrals/Details/5
        /// <summary>
        /// Displays details for a single non-deleted referral.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for Referral Id {Id}", id);

            var referral = await _context.Referrals
                .Where(r => r.DeletedAt == null)
                .Include(r => r.ClientUser)
                .Include(r => r.ReferringOrganization)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (referral == null)
            {
                _logger.LogWarning("Referral Id {Id} not found", id);
                return NotFound();
            }

            return View(referral);
        }

        // GET: Referrals/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create Referral page");

            await PopulateDropdowns();
            return View();
        }

        // POST: Referrals/Create
        /// <summary>
        /// Creates a new referral after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientUserId,ReferringOrganizationId,ReferredOn,Status,ValidFrom,ValidTo,ReferredByName,ReferredByPhoneNumber,ReferredByEmail,Notes")] Referral referral)
        {
            _logger.LogInformation("Attempting to create Referral for ClientUserId {ClientUserId}", referral.ClientUserId);

            ModelState.Remove(nameof(Referral.ClientUser));
            ModelState.Remove(nameof(Referral.ReferringOrganization));
            ModelState.Remove(nameof(Referral.CreatedByUser));
            ModelState.Remove(nameof(Referral.UpdatedByUser));

            NormalizeReferral(referral);
            await ApplyReferralValidationAsync(referral);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create Referral failed validation for ClientUserId {ClientUserId}", referral.ClientUserId);
                await PopulateDropdowns(referral.ClientUserId, referral.ReferringOrganizationId);
                return View(referral);
            }

            var now = DateTime.UtcNow;
            referral.CreatedAt = now;
            referral.UpdatedAt = now;
            referral.CreatedByUserId = null; // Replace when auth/user tracking is added.
            referral.UpdatedByUserId = null; // Replace when auth/user tracking is added.

            _context.Add(referral);

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Referral Id {Id} created successfully", referral.Id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error creating Referral for ClientUserId {ClientUserId}", referral.ClientUserId);

                ModelState.AddModelError("", "Unable to save referral.");
                await PopulateDropdowns(referral.ClientUserId, referral.ReferringOrganizationId);
                return View(referral);
            }
        }

        // GET: Referrals/WizardStep1
        /// <summary>
        /// Shows Step 1 of the referral wizard:
        /// collect organization, client, profile, household, and referral draft details.
        /// This step saves nothing to the database.
        /// </summary>
        public async Task<IActionResult> WizardStep1()
        {
            _logger.LogInformation("Loading Referral Wizard Step 1 page");

            var vm = LoadWizardStep1FromSession() ?? new ReferralWizardStep1ViewModel();

            vm.HouseholdMembers ??= new List<ReferralWizardHouseholdMemberViewModel>();

            if (vm.HouseholdMembers.Count == 0)
            {
                vm.HouseholdMembers.Add(new ReferralWizardHouseholdMemberViewModel());
            }

            await PopulateWizardStep1Dropdowns(vm);
            return View(vm);
        }

        // POST: Referrals/WizardStep1
        /// <summary>
        /// Validates Step 1 and saves the wizard draft to session only.
        /// No database records are created in Step 1.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WizardStep1(ReferralWizardStep1ViewModel vm)
        {
            _logger.LogInformation("Attempting to submit Referral Wizard Step 1");

            vm.HouseholdMembers ??= new List<ReferralWizardHouseholdMemberViewModel>();

            NormalizeReferralWizardStep1(vm);
            await ApplyReferralWizardStep1ValidationAsync(vm);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Referral Wizard Step 1 failed validation");

                if (vm.HouseholdMembers.Count == 0)
                {
                    vm.HouseholdMembers.Add(new ReferralWizardHouseholdMemberViewModel());
                }

                await PopulateWizardStep1Dropdowns(vm);
                return View(vm);
            }

            SaveWizardStep1ToSession(vm);

            _logger.LogInformation("Referral Wizard Step 1 draft saved to session");

            return RedirectToAction(nameof(WizardStep2));
        }

        // GET: Referrals/WizardStep2
        /// <summary>
        /// Shows Step 2 of the referral wizard:
        /// review and confirm all draft data from Step 1.
        /// </summary>
        public async Task<IActionResult> WizardStep2()
        {
            _logger.LogInformation("Loading Referral Wizard Step 2 page");

            var vm = LoadWizardStep1FromSession();

            if (vm == null)
            {
                _logger.LogWarning("Referral Wizard Step 2 requested without Step 1 session data");
                TempData["ErrorMessage"] = "Your referral draft was not found. Please complete Step 1 again.";
                return RedirectToAction(nameof(WizardStep1));
            }

            vm.HouseholdMembers ??= new List<ReferralWizardHouseholdMemberViewModel>();

            if (vm.HouseholdMembers.Count == 0)
            {
                vm.HouseholdMembers.Add(new ReferralWizardHouseholdMemberViewModel());
            }

            await PopulateWizardStep1Dropdowns(vm);
            return View(vm);
        }

        // POST: Referrals/WizardStep2Confirm
        /// <summary>
        /// Final confirmation for the wizard.
        /// Creates organization, client, profile, household members, and referral in one transaction.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WizardStep2Confirm()
        {
            _logger.LogInformation("Attempting to confirm Referral Wizard Step 2");

            var vm = LoadWizardStep1FromSession();

            if (vm == null)
            {
                _logger.LogWarning("Referral Wizard Step 2 confirm requested without Step 1 session data");
                TempData["ErrorMessage"] = "Your referral draft was not found. Please complete Step 1 again.";
                return RedirectToAction(nameof(WizardStep1));
            }

            vm.HouseholdMembers ??= new List<ReferralWizardHouseholdMemberViewModel>();

            NormalizeReferralWizardStep1(vm);
            await ApplyReferralWizardStep1ValidationAsync(vm);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Referral Wizard Step 2 confirm failed validation re-check");
                await PopulateWizardStep1Dropdowns(vm);
                return View("WizardStep2", vm);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var now = DateTime.UtcNow;

                ulong referringOrganizationId;
                ulong clientUserId;

                // Organization
                if (vm.HasSelectedExistingOrganization)
                {
                    referringOrganizationId = vm.SelectedReferringOrganizationId!.Value;
                }
                else
                {
                    var newOrganization = new ReferringOrganization
                    {
                        Name = vm.NewOrganizationName!,
                        Type = vm.NewOrganizationType,
                        PrimaryContactName = vm.NewPrimaryContactName,
                        Email = vm.NewEmail,
                        PhoneNumber = vm.NewPhoneNumber,
                        AddressLine1 = vm.NewAddressLine1,
                        AddressLine2 = vm.NewAddressLine2,
                        City = vm.NewCity,
                        State = vm.NewState,
                        PostalCode = vm.NewPostalCode,
                        Notes = vm.NewNotes,
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now,
                        CreatedByUserId = null,
                        UpdatedByUserId = null
                    };

                    _context.ReferringOrganizations.Add(newOrganization);
                    await _context.SaveChangesAsync();

                    referringOrganizationId = newOrganization.Id;
                }

                // Client / Profile / Household
                if (vm.HasSelectedExistingClient)
                {
                    clientUserId = vm.SelectedClientUserId!.Value;
                }
                else
                {
                    var newClient = new DomainUser
                    {
                        Email = vm.NewClientEmail!,
                        PhoneNumber = vm.NewClientPhoneNumber,
                        FirstName = vm.NewClientFirstName,
                        LastName = vm.NewClientLastName,
                        AddressLine1 = vm.NewClientAddressLine1,
                        AddressLine2 = vm.NewClientAddressLine2,
                        City = vm.NewClientCity,
                        State = vm.NewClientState,
                        PostalCode = vm.NewClientPostalCode,
                        DateOfBirth = vm.NewClientDateOfBirth,
                        UserType = UserType.Client,
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now,
                        CreatedByUserId = null,
                        UpdatedByUserId = null
                    };

                    _context.DomainUsers.Add(newClient);
                    await _context.SaveChangesAsync();

                    clientUserId = newClient.Id;

                    var clientProfile = new ClientProfile
                    {
                        UserId = clientUserId,
                        EmploymentStatus = vm.NewClientEmploymentStatus,
                        EarnedIncomeMonthly = vm.NewClientEarnedIncomeMonthly,
                        IsUnhoused = vm.NewClientIsUnhoused,
                        CreatedAt = now,
                        UpdatedAt = now,
                        CreatedByUserId = null,
                        UpdatedByUserId = null
                    };

                    _context.ClientProfiles.Add(clientProfile);

                    foreach (var member in vm.HouseholdMembers.Where(h => h.HasStarted))
                    {
                        var householdMember = new HouseholdMember
                        {
                            ClientUserId = clientUserId,
                            FirstName = member.FirstName!,
                            LastName = member.LastName!,
                            DateOfBirth = member.DateOfBirth,
                            AgeAsOfDate = member.AgeAsOfDate,
                            CreatedAt = now,
                            UpdatedAt = now,
                            CreatedByUserId = null,
                            UpdatedByUserId = null
                        };

                        _context.HouseholdMembers.Add(householdMember);
                    }

                    await _context.SaveChangesAsync();
                }

                // Referral
                var referral = new Referral
                {
                    ClientUserId = clientUserId,
                    ReferringOrganizationId = referringOrganizationId,
                    ReferredOn = vm.ReferredOn!.Value,
                    Status = vm.Status!.Value,
                    ValidFrom = vm.ValidFrom,
                    ValidTo = vm.ValidTo,
                    ReferredByName = vm.ReferredByName,
                    ReferredByPhoneNumber = vm.ReferredByPhoneNumber,
                    ReferredByEmail = vm.ReferredByEmail,
                    Notes = vm.ReferralNotes,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedByUserId = null,
                    UpdatedByUserId = null
                };

                _context.Referrals.Add(referral);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                ClearWizardStep1Session();

                _logger.LogInformation("Referral Wizard completed successfully with Referral Id {ReferralId}", referral.Id);

                return RedirectToAction(nameof(Details), new { id = referral.Id });
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "Error confirming Referral Wizard Step 2");

                ModelState.AddModelError(string.Empty, "Unable to save the referral.");
                await PopulateWizardStep1Dropdowns(vm);
                return View("WizardStep2", vm);
            }
        }

        // GET: Referrals/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted referral.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for Referral Id {Id}", id);

            var referral = await _context.Referrals
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (referral == null)
            {
                _logger.LogWarning("Referral Id {Id} not found for edit", id);
                return NotFound();
            }

            await PopulateDropdowns(referral.ClientUserId, referral.ReferringOrganizationId);
            return View(referral);
        }

        // POST: Referrals/Edit/5
        /// <summary>
        /// Updates an existing referral after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,ClientUserId,ReferringOrganizationId,ReferredOn,Status,ValidFrom,ValidTo,ReferredByName,ReferredByPhoneNumber,ReferredByEmail,Notes")] Referral formModel)
        {
            _logger.LogInformation("Attempting to edit Referral Id {Id}", id);

            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            ModelState.Remove(nameof(Referral.ClientUser));
            ModelState.Remove(nameof(Referral.ReferringOrganization));
            ModelState.Remove(nameof(Referral.CreatedByUser));
            ModelState.Remove(nameof(Referral.UpdatedByUser));

            NormalizeReferral(formModel);
            await ApplyReferralValidationAsync(formModel, formModel.Id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit Referral failed validation for Id {Id}", id);
                await PopulateDropdowns(formModel.ClientUserId, formModel.ReferringOrganizationId);
                return View(formModel);
            }

            var existing = await _context.Referrals
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (existing == null)
            {
                _logger.LogWarning("Referral Id {Id} not found during edit save", id);
                return NotFound();
            }

            existing.ClientUserId = formModel.ClientUserId;
            existing.ReferringOrganizationId = formModel.ReferringOrganizationId;
            existing.ReferredOn = formModel.ReferredOn;
            existing.Status = formModel.Status;
            existing.ValidFrom = formModel.ValidFrom;
            existing.ValidTo = formModel.ValidTo;
            existing.ReferredByName = formModel.ReferredByName;
            existing.ReferredByPhoneNumber = formModel.ReferredByPhoneNumber;
            existing.ReferredByEmail = formModel.ReferredByEmail;
            existing.Notes = formModel.Notes;

            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // Replace when auth/user tracking is added.

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Referral Id {Id} updated successfully", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ReferralExists(formModel.Id))
                {
                    _logger.LogWarning("Referral Id {Id} no longer exists during concurrency check", id);
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating Referral Id {Id}", id);

                ModelState.AddModelError("", "Unable to save changes.");
                await PopulateDropdowns(formModel.ClientUserId, formModel.ReferringOrganizationId);
                return View(formModel);
            }
        }

        // GET: Referrals/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted referral.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for Referral Id {Id}", id);

            var referral = await _context.Referrals
                .Where(r => r.DeletedAt == null)
                .Include(r => r.ClientUser)
                .Include(r => r.ReferringOrganization)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (referral == null)
            {
                _logger.LogWarning("Referral Id {Id} not found for delete", id);
                return NotFound();
            }

            return View(referral);
        }

        // POST: Referrals/Delete/5
        /// <summary>
        /// Soft deletes a referral.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting Referral Id {Id}", id);

            var referral = await _context.Referrals
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (referral == null)
            {
                _logger.LogWarning("Referral Id {Id} not found during delete", id);
                return NotFound();
            }

            referral.DeletedAt = DateTime.UtcNow;
            referral.UpdatedAt = DateTime.UtcNow;
            referral.UpdatedByUserId = null; // Replace when auth/user tracking is added.

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Referral Id {Id} soft deleted", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error soft deleting Referral Id {Id}", id);

                TempData["ErrorMessage"] = "Unable to delete referral.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Saves the Step 1 wizard draft to session as JSON.
        /// </summary>
        private void SaveWizardStep1ToSession(ReferralWizardStep1ViewModel vm)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(vm);
            HttpContext.Session.SetString(ReferralWizardSessionKey, json);
        }

        /// <summary>
        /// Loads the Step 1 wizard draft from session.
        /// Returns null when no draft exists.
        /// </summary>
        private ReferralWizardStep1ViewModel? LoadWizardStep1FromSession()
        {
            var json = HttpContext.Session.GetString(ReferralWizardSessionKey);

            return string.IsNullOrWhiteSpace(json)
                ? null
                : System.Text.Json.JsonSerializer.Deserialize<ReferralWizardStep1ViewModel>(json);
        }

        /// <summary>
        /// Clears the current referral wizard draft from session.
        /// </summary>
        private void ClearWizardStep1Session()
        {
            HttpContext.Session.Remove(ReferralWizardSessionKey);
        }

        /// <summary>
        /// Populates dropdown lists for the create and edit forms.
        /// </summary>
        private async Task PopulateDropdowns(ulong? selectedClientUserId = null, ulong? selectedReferringOrganizationId = null)
        {
            _logger.LogDebug("Populating dropdowns for Referrals");

            var users = await _context.DomainUsers
                .Where(u => u.DeletedAt == null)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            var userOptions = users
                .Select(u => new
                {
                    u.Id,
                    DisplayName = $"{u.LastName}, {u.FirstName} ({u.Email})"
                })
                .ToList();

            var organizations = await _context.ReferringOrganizations
                .Where(o => o.DeletedAt == null)
                .OrderBy(o => o.Name)
                .ToListAsync();

            ViewData["ClientUserId"] = new SelectList(userOptions, "Id", "DisplayName", selectedClientUserId);
            ViewData["ReferringOrganizationId"] = new SelectList(organizations, "Id", "Name", selectedReferringOrganizationId);

            _logger.LogDebug("Dropdowns populated: {UsersCount} users, {OrgsCount} organizations", userOptions.Count, organizations.Count);
        }

        /// <summary>
        /// Populates dropdown values for Referral Wizard views.
        /// </summary>
        private async Task PopulateWizardStep1Dropdowns(ReferralWizardStep1ViewModel vm)
        {
            _logger.LogDebug("Populating dropdowns for Referral Wizard");

            vm.HouseholdMembers ??= new List<ReferralWizardHouseholdMemberViewModel>();

            var organizations = await _context.ReferringOrganizations
                .Where(o => o.DeletedAt == null && o.IsActive)
                .OrderBy(o => o.Name)
                .ToListAsync();

            vm.ExistingOrganizations = organizations
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = o.Name
                })
                .ToList();

            var clients = await _context.DomainUsers
                .Where(u => u.DeletedAt == null && u.UserType == UserType.Client && u.IsActive)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            vm.ExistingClients = clients
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace($"{u.LastName}{u.FirstName}".Trim())
                        ? u.Email
                        : $"{u.LastName ?? ""}, {u.FirstName ?? ""} ({u.Email})"
                })
                .ToList();

            vm.ReferralStatusOptions = Enum.GetValues(typeof(ReferralStatus))
                .Cast<ReferralStatus>()
                .Select(status => new SelectListItem
                {
                    Value = status.ToString(),
                    Text = status.ToString(),
                    Selected = vm.Status.HasValue && vm.Status.Value == status
                })
                .ToList();

            if (vm.HouseholdMembers.Count == 0)
            {
                vm.HouseholdMembers.Add(new ReferralWizardHouseholdMemberViewModel());
            }

            _logger.LogDebug(
                "Referral Wizard dropdowns populated with {OrganizationCount} organizations, {ClientCount} clients, and {StatusCount} statuses",
                vm.ExistingOrganizations.Count,
                vm.ExistingClients.Count,
                vm.ReferralStatusOptions.Count);
        }

        /// <summary>
        /// Returns true if the non-deleted referral exists.
        /// </summary>
        private async Task<bool> ReferralExists(ulong id)
        {
            return await _context.Referrals.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Trims strings and converts blank optional values to null.
        /// </summary>
        private static void NormalizeReferral(Referral model)
        {
            model.ReferredByName = NullIfWhiteSpace(model.ReferredByName);
            model.ReferredByPhoneNumber = NullIfWhiteSpace(model.ReferredByPhoneNumber);
            model.ReferredByEmail = NullIfWhiteSpace(model.ReferredByEmail);
            model.Notes = NullIfWhiteSpace(model.Notes);
        }

        /// <summary>
        /// Trims strings and converts blank optional values to null for Wizard Step 1.
        /// </summary>
        private static void NormalizeReferralWizardStep1(ReferralWizardStep1ViewModel model)
        {
            model.NewOrganizationName = string.IsNullOrWhiteSpace(model.NewOrganizationName)
                ? null
                : model.NewOrganizationName.Trim();

            model.NewOrganizationType = NullIfWhiteSpace(model.NewOrganizationType);
            model.NewPrimaryContactName = NullIfWhiteSpace(model.NewPrimaryContactName);
            model.NewEmail = NullIfWhiteSpace(model.NewEmail);
            model.NewPhoneNumber = NullIfWhiteSpace(model.NewPhoneNumber);
            model.NewAddressLine1 = NullIfWhiteSpace(model.NewAddressLine1);
            model.NewAddressLine2 = NullIfWhiteSpace(model.NewAddressLine2);
            model.NewCity = NullIfWhiteSpace(model.NewCity);
            model.NewState = NullIfWhiteSpace(model.NewState)?.ToUpperInvariant();
            model.NewPostalCode = NullIfWhiteSpace(model.NewPostalCode);
            model.NewNotes = NullIfWhiteSpace(model.NewNotes);

            model.NewClientFirstName = NullIfWhiteSpace(model.NewClientFirstName);
            model.NewClientLastName = NullIfWhiteSpace(model.NewClientLastName);
            model.NewClientEmail = NullIfWhiteSpace(model.NewClientEmail);
            model.NewClientPhoneNumber = NullIfWhiteSpace(model.NewClientPhoneNumber);
            model.NewClientAddressLine1 = NullIfWhiteSpace(model.NewClientAddressLine1);
            model.NewClientAddressLine2 = NullIfWhiteSpace(model.NewClientAddressLine2);
            model.NewClientCity = NullIfWhiteSpace(model.NewClientCity);
            model.NewClientState = NullIfWhiteSpace(model.NewClientState)?.ToUpperInvariant();
            model.NewClientPostalCode = NullIfWhiteSpace(model.NewClientPostalCode);
            model.NewClientEmploymentStatus = NullIfWhiteSpace(model.NewClientEmploymentStatus);

            model.ReferredByName = NullIfWhiteSpace(model.ReferredByName);
            model.ReferredByPhoneNumber = NullIfWhiteSpace(model.ReferredByPhoneNumber);
            model.ReferredByEmail = NullIfWhiteSpace(model.ReferredByEmail);
            model.ReferralNotes = NullIfWhiteSpace(model.ReferralNotes);

            if (model.HouseholdMembers != null)
            {
                foreach (var member in model.HouseholdMembers)
                {
                    member.FirstName = NullIfWhiteSpace(member.FirstName);
                    member.LastName = NullIfWhiteSpace(member.LastName);
                }
            }
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyReferralValidationAsync(Referral model, ulong? currentId = null)
        {
            var clientExists = await _context.DomainUsers
                .AnyAsync(u =>
                    u.Id == model.ClientUserId &&
                    u.DeletedAt == null &&
                    u.UserType == UserType.Client);

            if (!clientExists)
            {
                ModelState.AddModelError(nameof(Referral.ClientUserId), "Select a valid client.");
            }

            var organizationExists = await _context.ReferringOrganizations
                .AnyAsync(o =>
                    o.Id == model.ReferringOrganizationId &&
                    o.DeletedAt == null &&
                    o.IsActive);

            if (!organizationExists)
            {
                ModelState.AddModelError(nameof(Referral.ReferringOrganizationId), "Select a valid active referring organization.");
            }

            if (!Enum.IsDefined(typeof(ReferralStatus), model.Status))
            {
                ModelState.AddModelError(nameof(Referral.Status), "Select a valid referral status.");
            }

            if (model.ReferredOn.Date > DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(nameof(Referral.ReferredOn), "Referral date cannot be in the future.");
            }

            if (model.ValidFrom.HasValue && model.ValidTo.HasValue && model.ValidFrom.Value > model.ValidTo.Value)
            {
                ModelState.AddModelError(nameof(Referral.ValidTo), "Valid To must be on or after Valid From.");
            }

            if (model.ValidFrom.HasValue && model.ValidFrom.Value < model.ReferredOn)
            {
                ModelState.AddModelError(nameof(Referral.ValidFrom), "Valid From cannot be earlier than Referred On.");
            }

            if (model.ValidTo.HasValue && model.ValidTo.Value < model.ReferredOn)
            {
                ModelState.AddModelError(nameof(Referral.ValidTo), "Valid To cannot be earlier than Referred On.");
            }

            if (!string.IsNullOrWhiteSpace(model.ReferredByName) && !IsValidPersonName(model.ReferredByName))
            {
                ModelState.AddModelError(nameof(Referral.ReferredByName), "Referred By Name contains invalid characters.");
            }

            if (!string.IsNullOrWhiteSpace(model.ReferredByPhoneNumber) && !IsValidPhoneNumber(model.ReferredByPhoneNumber))
            {
                ModelState.AddModelError(nameof(Referral.ReferredByPhoneNumber), "Enter a valid US phone number with 10 digits, or 11 digits starting with 1.");
            }

            if (!string.IsNullOrWhiteSpace(model.ReferredByEmail) && !IsValidEmail(model.ReferredByEmail))
            {
                ModelState.AddModelError(nameof(Referral.ReferredByEmail), "Email format is invalid.");
            }

            if (!string.IsNullOrWhiteSpace(model.Notes) && model.Notes.Length > 2000)
            {
                ModelState.AddModelError(nameof(Referral.Notes), "Notes cannot exceed 2000 characters.");
            }
        }

        /// <summary>
        /// Applies business-rule validation for Referral Wizard Step 1.
        /// This validates the full draft but does not save anything.
        /// </summary>
        private async Task ApplyReferralWizardStep1ValidationAsync(ReferralWizardStep1ViewModel model)
        {
            // =========================================================
            // ORGANIZATION VALIDATION
            // =========================================================
            bool selectedExistingOrganization = model.HasSelectedExistingOrganization;
            bool enteredNewOrganization = model.HasStartedNewOrganization;

            if (!selectedExistingOrganization && !enteredNewOrganization)
            {
                ModelState.AddModelError(string.Empty, "Select an existing organization or enter a new organization.");
            }
            else if (selectedExistingOrganization && enteredNewOrganization)
            {
                ModelState.AddModelError(string.Empty, "Choose either an existing organization or enter a new one, not both.");
            }
            else if (selectedExistingOrganization)
            {
                var organizationExists = await _context.ReferringOrganizations
                    .AnyAsync(o =>
                        o.Id == model.SelectedReferringOrganizationId &&
                        o.DeletedAt == null &&
                        o.IsActive);

                if (!organizationExists)
                {
                    ModelState.AddModelError(nameof(model.SelectedReferringOrganizationId), "Select a valid active referring organization.");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.NewOrganizationName))
                {
                    ModelState.AddModelError(nameof(model.NewOrganizationName), "Organization name is required.");
                }
                else
                {
                    if (!ContainsLetterOrDigit(model.NewOrganizationName))
                    {
                        ModelState.AddModelError(nameof(model.NewOrganizationName), "Organization name must contain letters or numbers.");
                    }

                    var normalizedName = model.NewOrganizationName.ToLower();

                    var duplicateExists = await _context.ReferringOrganizations
                        .AnyAsync(r =>
                            r.DeletedAt == null &&
                            r.Name.ToLower() == normalizedName);

                    if (duplicateExists)
                    {
                        ModelState.AddModelError(nameof(model.NewOrganizationName), "An organization with this name already exists.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(model.NewOrganizationType) && !ContainsLetterOrDigit(model.NewOrganizationType))
                {
                    ModelState.AddModelError(nameof(model.NewOrganizationType), "Primary type of service must contain letters or numbers.");
                }

                if (!string.IsNullOrWhiteSpace(model.NewPrimaryContactName) && !IsValidPersonName(model.NewPrimaryContactName))
                {
                    ModelState.AddModelError(nameof(model.NewPrimaryContactName), "Contact person name contains invalid characters.");
                }

                if (!string.IsNullOrWhiteSpace(model.NewPhoneNumber) && !IsValidPhoneNumber(model.NewPhoneNumber))
                {
                    ModelState.AddModelError(nameof(model.NewPhoneNumber), "Enter a valid US phone number with 10 digits, or 11 digits starting with 1.");
                }

                if (!string.IsNullOrWhiteSpace(model.NewEmail) && !IsValidEmail(model.NewEmail))
                {
                    ModelState.AddModelError(nameof(model.NewEmail), "Email format is invalid.");
                }

                if (!string.IsNullOrWhiteSpace(model.NewAddressLine1) && !ContainsLetterOrDigit(model.NewAddressLine1))
                {
                    ModelState.AddModelError(nameof(model.NewAddressLine1), "Address Line 1 must contain letters or numbers.");
                }

                if (!string.IsNullOrWhiteSpace(model.NewAddressLine2) && !ContainsLetterOrDigit(model.NewAddressLine2))
                {
                    ModelState.AddModelError(nameof(model.NewAddressLine2), "Address Line 2 must contain letters or numbers.");
                }

                if (!string.IsNullOrWhiteSpace(model.NewCity) && !IsValidCity(model.NewCity))
                {
                    ModelState.AddModelError(nameof(model.NewCity), "City contains invalid characters.");
                }

                if (!string.IsNullOrWhiteSpace(model.NewState) && !IsValidUsStateCode(model.NewState))
                {
                    ModelState.AddModelError(nameof(model.NewState), "Enter a valid 2-letter US state code.");
                }

                if (!string.IsNullOrWhiteSpace(model.NewPostalCode) && !IsValidUsPostalCode(model.NewPostalCode))
                {
                    ModelState.AddModelError(nameof(model.NewPostalCode), "Enter a valid US ZIP code or ZIP+4.");
                }

                if (!string.IsNullOrWhiteSpace(model.NewNotes) && model.NewNotes.Length > 2000)
                {
                    ModelState.AddModelError(nameof(model.NewNotes), "Notes cannot exceed 2000 characters.");
                }
            }

            // =========================================================
            // CLIENT VALIDATION
            // =========================================================
            bool selectedExistingClient = model.HasSelectedExistingClient;
            bool enteredNewClient = model.HasStartedNewClient;

            if (!selectedExistingClient && !enteredNewClient)
            {
                ModelState.AddModelError(string.Empty, "Select an existing client or enter a new client.");
            }
            else if (selectedExistingClient && enteredNewClient)
            {
                ModelState.AddModelError(string.Empty, "Choose either an existing client or enter a new one, not both.");
            }
            else if (selectedExistingClient)
            {
                var clientExists = await _context.DomainUsers
                    .AnyAsync(u =>
                        u.Id == model.SelectedClientUserId &&
                        u.DeletedAt == null &&
                        u.UserType == UserType.Client &&
                        u.IsActive);

                if (!clientExists)
                {
                    ModelState.AddModelError(nameof(model.SelectedClientUserId), "Select a valid active client.");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.NewClientFirstName))
                {
                    ModelState.AddModelError(nameof(model.NewClientFirstName), "First name is required.");
                }
                else if (!IsValidPersonName(model.NewClientFirstName))
                {
                    ModelState.AddModelError(nameof(model.NewClientFirstName), "First name contains invalid characters.");
                }

                if (string.IsNullOrWhiteSpace(model.NewClientLastName))
                {
                    ModelState.AddModelError(nameof(model.NewClientLastName), "Last name is required.");
                }
                else if (!IsValidPersonName(model.NewClientLastName))
                {
                    ModelState.AddModelError(nameof(model.NewClientLastName), "Last name contains invalid characters.");
                }

                if (string.IsNullOrWhiteSpace(model.NewClientEmail))
                {
                    ModelState.AddModelError(nameof(model.NewClientEmail), "Email is required.");
                }
                else if (!IsValidEmail(model.NewClientEmail))
                {
                    ModelState.AddModelError(nameof(model.NewClientEmail), "Email format is invalid.");
                }
                else
                {
                    var duplicateClientExists = await _context.DomainUsers
                        .AnyAsync(u =>
                            u.DeletedAt == null &&
                            u.UserType == UserType.Client &&
                            u.Email.ToLower() == model.NewClientEmail.ToLower());

                    if (duplicateClientExists)
                    {
                        ModelState.AddModelError(nameof(model.NewClientEmail), "A client with this email address already exists.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(model.NewClientPhoneNumber) && !IsValidPhoneNumber(model.NewClientPhoneNumber))
                {
                    ModelState.AddModelError(nameof(model.NewClientPhoneNumber), "Enter a valid US phone number with 10 digits, or 11 digits starting with 1.");
                }

                if (!string.IsNullOrWhiteSpace(model.NewClientAddressLine1) && !ContainsLetterOrDigit(model.NewClientAddressLine1))
                {
                    ModelState.AddModelError(nameof(model.NewClientAddressLine1), "Address Line 1 must contain letters or numbers.");
                }

                if (!string.IsNullOrWhiteSpace(model.NewClientAddressLine2) && !ContainsLetterOrDigit(model.NewClientAddressLine2))
                {
                    ModelState.AddModelError(nameof(model.NewClientAddressLine2), "Address Line 2 must contain letters or numbers.");
                }

                if (!string.IsNullOrWhiteSpace(model.NewClientCity) && !IsValidCity(model.NewClientCity))
                {
                    ModelState.AddModelError(nameof(model.NewClientCity), "City contains invalid characters.");
                }

                if (!string.IsNullOrWhiteSpace(model.NewClientState) && !IsValidUsStateCode(model.NewClientState))
                {
                    ModelState.AddModelError(nameof(model.NewClientState), "Enter a valid 2-letter US state code.");
                }

                if (!string.IsNullOrWhiteSpace(model.NewClientPostalCode) && !IsValidUsPostalCode(model.NewClientPostalCode))
                {
                    ModelState.AddModelError(nameof(model.NewClientPostalCode), "Enter a valid US ZIP code or ZIP+4.");
                }

                if (model.NewClientDateOfBirth.HasValue && model.NewClientDateOfBirth.Value > DateOnly.FromDateTime(DateTime.UtcNow))
                {
                    ModelState.AddModelError(nameof(model.NewClientDateOfBirth), "Date of Birth cannot be in the future.");
                }

                // =========================================================
                // CLIENT PROFILE VALIDATION
                // =========================================================
                if (!string.IsNullOrWhiteSpace(model.NewClientEmploymentStatus) &&
                    !Regex.IsMatch(model.NewClientEmploymentStatus, @"^[A-Za-z0-9\s'.-]*$"))
                {
                    ModelState.AddModelError(nameof(model.NewClientEmploymentStatus), "Employment status contains invalid characters.");
                }

                if (model.NewClientEarnedIncomeMonthly.HasValue && model.NewClientEarnedIncomeMonthly.Value < 0)
                {
                    ModelState.AddModelError(nameof(model.NewClientEarnedIncomeMonthly), "Monthly earned income must be 0 or greater.");
                }

                // =========================================================
                // HOUSEHOLD MEMBER VALIDATION
                // =========================================================
                if (model.HouseholdMembers != null)
                {
                    for (int i = 0; i < model.HouseholdMembers.Count; i++)
                    {
                        var member = model.HouseholdMembers[i];

                        if (!member.HasStarted)
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(member.FirstName))
                        {
                            ModelState.AddModelError($"HouseholdMembers[{i}].FirstName", "First name is required.");
                        }
                        else if (!IsValidPersonName(member.FirstName))
                        {
                            ModelState.AddModelError($"HouseholdMembers[{i}].FirstName", "First name contains invalid characters.");
                        }

                        if (string.IsNullOrWhiteSpace(member.LastName))
                        {
                            ModelState.AddModelError($"HouseholdMembers[{i}].LastName", "Last name is required.");
                        }
                        else if (!IsValidPersonName(member.LastName))
                        {
                            ModelState.AddModelError($"HouseholdMembers[{i}].LastName", "Last name contains invalid characters.");
                        }

                        if (member.DateOfBirth.HasValue && member.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
                        {
                            ModelState.AddModelError($"HouseholdMembers[{i}].DateOfBirth", "Date of Birth cannot be in the future.");
                        }

                        if (member.AgeAsOfDate.HasValue && member.AgeAsOfDate.Value.Date > DateTime.UtcNow.Date)
                        {
                            ModelState.AddModelError($"HouseholdMembers[{i}].AgeAsOfDate", "Age As Of Date cannot be in the future.");
                        }

                        if (member.DateOfBirth.HasValue &&
                            member.AgeAsOfDate.HasValue &&
                            member.AgeAsOfDate.Value.Date < member.DateOfBirth.Value.Date)
                        {
                            ModelState.AddModelError($"HouseholdMembers[{i}].AgeAsOfDate", "Age As Of Date cannot be earlier than Date of Birth.");
                        }
                    }
                }
            }

            // =========================================================
            // REFERRAL DETAILS VALIDATION
            // =========================================================
            if (!model.ReferredOn.HasValue)
            {
                ModelState.AddModelError(nameof(model.ReferredOn), "Referral date is required.");
            }
            else
            {
                if (model.ReferredOn.Value.Date > DateTime.UtcNow.Date)
                {
                    ModelState.AddModelError(nameof(model.ReferredOn), "Referral date cannot be in the future.");
                }

                if (model.ValidFrom.HasValue && model.ValidFrom.Value.Date < model.ReferredOn.Value.Date)
                {
                    ModelState.AddModelError(nameof(model.ValidFrom), "Valid From cannot be earlier than Referral Date.");
                }

                if (model.ValidTo.HasValue && model.ValidTo.Value.Date < model.ReferredOn.Value.Date)
                {
                    ModelState.AddModelError(nameof(model.ValidTo), "Valid To cannot be earlier than Referral Date.");
                }
            }

            if (!model.Status.HasValue || !Enum.IsDefined(typeof(ReferralStatus), model.Status.Value))
            {
                ModelState.AddModelError(nameof(model.Status), "Select a valid referral status.");
            }

            if (model.ValidFrom.HasValue &&
                model.ValidTo.HasValue &&
                model.ValidFrom.Value.Date > model.ValidTo.Value.Date)
            {
                ModelState.AddModelError(nameof(model.ValidTo), "Valid To must be on or after Valid From.");
            }

            if (!string.IsNullOrWhiteSpace(model.ReferredByName) && !IsValidPersonName(model.ReferredByName))
            {
                ModelState.AddModelError(nameof(model.ReferredByName), "Referrer name contains invalid characters.");
            }

            if (!string.IsNullOrWhiteSpace(model.ReferredByPhoneNumber) && !IsValidPhoneNumber(model.ReferredByPhoneNumber))
            {
                ModelState.AddModelError(nameof(model.ReferredByPhoneNumber), "Enter a valid US phone number with 10 digits, or 11 digits starting with 1.");
            }

            if (!string.IsNullOrWhiteSpace(model.ReferredByEmail) && !IsValidEmail(model.ReferredByEmail))
            {
                ModelState.AddModelError(nameof(model.ReferredByEmail), "Email format is invalid.");
            }

            if (!string.IsNullOrWhiteSpace(model.ReferralNotes) && model.ReferralNotes.Length > 2000)
            {
                ModelState.AddModelError(nameof(model.ReferralNotes), "Notes cannot exceed 2000 characters.");
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
        /// Returns true when the value contains at least one letter or digit.
        /// </summary>
        private static bool ContainsLetterOrDigit(string value)
        {
            return value.Any(char.IsLetterOrDigit);
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
        /// Validates a city name using a practical US-style character set.
        /// </summary>
        private static bool IsValidCity(string city)
        {
            return Regex.IsMatch(city, @"^[A-Za-z][A-Za-z\s'.-]*$");
        }

        /// <summary>
        /// Validates a 2-letter US state code.
        /// </summary>
        private static bool IsValidUsStateCode(string state)
        {
            return state.Length == 2 && ValidUsStateCodes.Contains(state);
        }

        /// <summary>
        /// Validates a US ZIP code or ZIP+4.
        /// </summary>
        private static bool IsValidUsPostalCode(string postalCode)
        {
            return Regex.IsMatch(postalCode, @"^\d{5}(-\d{4})?$");
        }
    }
}
*/