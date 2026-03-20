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
    /// plus Step 1 of the referral wizard workflow.
    /// </summary>
    public class ReferralsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReferralsController> _logger;

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
        /// select an existing referring organization or add a new one.
        /// </summary>
        public async Task<IActionResult> WizardStep1()
        {
            _logger.LogInformation("Loading Referral Wizard Step 1 page");

            var vm = new ReferralWizardStep1ViewModel();
            await PopulateWizardStep1Dropdowns(vm);

            return View(vm);
        }

        // POST: Referrals/WizardStep1
        /// <summary>
        /// Handles Step 1 of the referral wizard:
        /// validates either existing organization selection or new organization creation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WizardStep1(ReferralWizardStep1ViewModel vm)
        {
            _logger.LogInformation("Attempting to submit Referral Wizard Step 1");

            NormalizeReferralWizardStep1(vm);
            await ApplyReferralWizardStep1ValidationAsync(vm);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Referral Wizard Step 1 failed validation");
                await PopulateWizardStep1Dropdowns(vm);
                return View(vm);
            }

            ulong referringOrganizationId;

            if (vm.HasSelectedExistingOrganization)
            {
                referringOrganizationId = vm.SelectedReferringOrganizationId!.Value;

                _logger.LogInformation(
                    "Referral Wizard Step 1 completed using existing ReferringOrganization Id {ReferringOrganizationId}",
                    referringOrganizationId);
            }
            else
            {
                var now = DateTime.UtcNow;

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
                    CreatedByUserId = null, // Replace when auth/user tracking is added.
                    UpdatedByUserId = null  // Replace when auth/user tracking is added.
                };

                _context.ReferringOrganizations.Add(newOrganization);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error creating new Referring Organization during Referral Wizard Step 1");

                    ModelState.AddModelError(string.Empty, "Unable to save new referring organization.");
                    await PopulateWizardStep1Dropdowns(vm);
                    return View(vm);
                }

                referringOrganizationId = newOrganization.Id;

                _logger.LogInformation(
                    "Referral Wizard Step 1 created new ReferringOrganization Id {ReferringOrganizationId}",
                    referringOrganizationId);
            }

            TempData["ReferralWizard.ReferringOrganizationId"] = referringOrganizationId.ToString();

            // Placeholder until Step 2 is built.
            TempData["SuccessMessage"] = $"Step 1 complete. Selected Referring Organization Id: {referringOrganizationId}";

            return RedirectToAction(nameof(WizardStep1));
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
        /// Populates dropdown values for Referral Wizard Step 1.
        /// </summary>
        private async Task PopulateWizardStep1Dropdowns(ReferralWizardStep1ViewModel vm)
        {
            _logger.LogDebug("Populating dropdowns for Referral Wizard Step 1");

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

            _logger.LogDebug("Referral Wizard Step 1 dropdown populated with {Count} organizations", vm.ExistingOrganizations.Count);
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
        /// </summary>
        private async Task ApplyReferralWizardStep1ValidationAsync(ReferralWizardStep1ViewModel model)
        {
            bool selectedExisting = model.HasSelectedExistingOrganization;
            bool enteredNew = model.HasStartedNewOrganization;

            if (!selectedExisting && !enteredNew)
            {
                ModelState.AddModelError(string.Empty, "Select an existing organization or enter a new organization.");
                return;
            }

            if (selectedExisting && enteredNew)
            {
                ModelState.AddModelError(string.Empty, "Choose either an existing organization or enter a new one, not both.");
                return;
            }

            if (selectedExisting)
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

                return;
            }

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