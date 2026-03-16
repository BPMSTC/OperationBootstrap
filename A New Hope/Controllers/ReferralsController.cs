using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages create, read, update, and soft delete operations for referrals.
    /// </summary>
    public class ReferralsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReferralsController> _logger;

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

            // Retrieve active referrals with related client and organization display data.
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
            // Reject requests with no id.
            if (id == null)
            {
                _logger.LogWarning("Details requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for Referral Id {Id}", id);

            // Retrieve the requested active referral with related client and organization data.
            var referral = await _context.Referrals
                .Where(r => r.DeletedAt == null)
                .Include(r => r.ClientUser)
                .Include(r => r.ReferringOrganization)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Return not found when the referral does not exist.
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

            // Populate dropdown values for the create form.
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

            // Remove navigation properties that are not posted by the form.
            ModelState.Remove(nameof(Referral.ClientUser));
            ModelState.Remove(nameof(Referral.ReferringOrganization));
            ModelState.Remove(nameof(Referral.CreatedByUser));
            ModelState.Remove(nameof(Referral.UpdatedByUser));

            // Normalize incoming values before business-rule validation.
            NormalizeReferral(referral);
            await ApplyReferralValidationAsync(referral);

            // Return the form with dropdowns restored when validation fails.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create Referral failed validation for ClientUserId {ClientUserId}", referral.ClientUserId);
                await PopulateDropdowns(referral.ClientUserId, referral.ReferringOrganizationId);
                return View(referral);
            }

            // Set audit fields for the new referral record.
            var now = DateTime.UtcNow;
            referral.CreatedAt = now;
            referral.UpdatedAt = now;
            referral.CreatedByUserId = null; // Replace when auth/user tracking is added.
            referral.UpdatedByUserId = null; // Replace when auth/user tracking is added.

            // Queue the new referral for insert.
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

        // GET: Referrals/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted referral.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            // Reject requests with no id.
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for Referral Id {Id}", id);

            // Retrieve the requested active referral for editing.
            var referral = await _context.Referrals
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            // Return not found when the referral does not exist.
            if (referral == null)
            {
                _logger.LogWarning("Referral Id {Id} not found for edit", id);
                return NotFound();
            }

            // Populate dropdown values using the current record selections.
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

            // Ensure the route id matches the posted model id.
            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            // Remove navigation properties that are not posted by the form.
            ModelState.Remove(nameof(Referral.ClientUser));
            ModelState.Remove(nameof(Referral.ReferringOrganization));
            ModelState.Remove(nameof(Referral.CreatedByUser));
            ModelState.Remove(nameof(Referral.UpdatedByUser));

            // Normalize incoming values before business-rule validation.
            NormalizeReferral(formModel);
            await ApplyReferralValidationAsync(formModel, formModel.Id);

            // Return the form with dropdowns restored when validation fails.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit Referral failed validation for Id {Id}", id);
                await PopulateDropdowns(formModel.ClientUserId, formModel.ReferringOrganizationId);
                return View(formModel);
            }

            // Retrieve the existing active referral record.
            var existing = await _context.Referrals
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            // Return not found when the target record no longer exists.
            if (existing == null)
            {
                _logger.LogWarning("Referral Id {Id} not found during edit save", id);
                return NotFound();
            }

            // Copy validated form values into the tracked entity.
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
                // Check whether the record was deleted during the edit attempt.
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
            // Reject requests with no id.
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for Referral Id {Id}", id);

            // Retrieve the requested active referral with related client and organization data.
            var referral = await _context.Referrals
                .Where(r => r.DeletedAt == null)
                .Include(r => r.ClientUser)
                .Include(r => r.ReferringOrganization)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Return not found when the referral does not exist.
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

            // Retrieve the active referral targeted for soft delete.
            var referral = await _context.Referrals
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            // Return not found when the referral does not exist.
            if (referral == null)
            {
                _logger.LogWarning("Referral Id {Id} not found during delete", id);
                return NotFound();
            }

            // Apply soft-delete and audit values.
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

            // Retrieve active users for the client dropdown.
            var users = await _context.DomainUsers
                .Where(u => u.DeletedAt == null)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            // Build display-friendly user dropdown options.
            var userOptions = users
                .Select(u => new
                {
                    u.Id,
                    DisplayName = $"{u.LastName}, {u.FirstName} ({u.Email})"
                })
                .ToList();

            // Retrieve active referring organizations for the organization dropdown.
            var organizations = await _context.ReferringOrganizations
                .Where(o => o.DeletedAt == null)
                .OrderBy(o => o.Name)
                .ToListAsync();

            // Store the client and organization dropdown options in ViewData.
            ViewData["ClientUserId"] = new SelectList(userOptions, "Id", "DisplayName", selectedClientUserId);
            ViewData["ReferringOrganizationId"] = new SelectList(organizations, "Id", "Name", selectedReferringOrganizationId);

            _logger.LogDebug("Dropdowns populated: {UsersCount} users, {OrgsCount} organizations", userOptions.Count, organizations.Count);
        }

        /// <summary>
        /// Returns true if the non-deleted referral exists.
        /// </summary>
        private async Task<bool> ReferralExists(ulong id)
        {
            // Check whether the requested active referral still exists.
            return await _context.Referrals.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Trims strings and converts blank optional values to null.
        /// </summary>
        private static void NormalizeReferral(Referral model)
        {
            // Normalize optional string values before validation and save.
            model.ReferredByName = NullIfWhiteSpace(model.ReferredByName);
            model.ReferredByPhoneNumber = NullIfWhiteSpace(model.ReferredByPhoneNumber);
            model.ReferredByEmail = NullIfWhiteSpace(model.ReferredByEmail);
            model.Notes = NullIfWhiteSpace(model.Notes);
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyReferralValidationAsync(Referral model, ulong? currentId = null)
        {
            // Validate that the selected client exists and is not deleted.
            var clientExists = await _context.DomainUsers
                .AnyAsync(u =>
                    u.Id == model.ClientUserId &&
                    u.DeletedAt == null &&
                    u.UserType == UserType.Client);

            if (!clientExists)
            {
                ModelState.AddModelError(nameof(Referral.ClientUserId), "Select a valid client.");
            }

            // Validate that the selected referring organization exists, is active, and is not deleted.
            var organizationExists = await _context.ReferringOrganizations
                .AnyAsync(o =>
                    o.Id == model.ReferringOrganizationId &&
                    o.DeletedAt == null &&
                    o.IsActive);

            if (!organizationExists)
            {
                ModelState.AddModelError(nameof(Referral.ReferringOrganizationId), "Select a valid active referring organization.");
            }

            // Validate that the selected referral status is defined.
            if (!Enum.IsDefined(typeof(ReferralStatus), model.Status))
            {
                ModelState.AddModelError(nameof(Referral.Status), "Select a valid referral status.");
            }

            // Prevent referral dates in the future.
            if (model.ReferredOn.Date > DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(nameof(Referral.ReferredOn), "Referral date cannot be in the future.");
            }

            // Ensure the validity range is chronologically correct.
            if (model.ValidFrom.HasValue && model.ValidTo.HasValue && model.ValidFrom.Value > model.ValidTo.Value)
            {
                ModelState.AddModelError(nameof(Referral.ValidTo), "Valid To must be on or after Valid From.");
            }

            // Ensure Valid From is not earlier than Referred On.
            if (model.ValidFrom.HasValue && model.ValidFrom.Value < model.ReferredOn)
            {
                ModelState.AddModelError(nameof(Referral.ValidFrom), "Valid From cannot be earlier than Referred On.");
            }

            // Ensure Valid To is not earlier than Referred On.
            if (model.ValidTo.HasValue && model.ValidTo.Value < model.ReferredOn)
            {
                ModelState.AddModelError(nameof(Referral.ValidTo), "Valid To cannot be earlier than Referred On.");
            }

            // Validate referring contact name characters when provided.
            if (!string.IsNullOrWhiteSpace(model.ReferredByName) && !IsValidPersonName(model.ReferredByName))
            {
                ModelState.AddModelError(nameof(Referral.ReferredByName), "Referred By Name contains invalid characters.");
            }

            // Validate referring contact phone number when provided.
            if (!string.IsNullOrWhiteSpace(model.ReferredByPhoneNumber) && !IsValidPhoneNumber(model.ReferredByPhoneNumber))
            {
                ModelState.AddModelError(nameof(Referral.ReferredByPhoneNumber), "Enter a valid US phone number with 10 digits, or 11 digits starting with 1.");
            }

            // Validate referring contact email when provided.
            if (!string.IsNullOrWhiteSpace(model.ReferredByEmail) && !IsValidEmail(model.ReferredByEmail))
            {
                ModelState.AddModelError(nameof(Referral.ReferredByEmail), "Email format is invalid.");
            }

            // Enforce the project note length limit when notes are provided.
            if (!string.IsNullOrWhiteSpace(model.Notes) && model.Notes.Length > 2000)
            {
                ModelState.AddModelError(nameof(Referral.Notes), "Notes cannot exceed 2000 characters.");
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
    }
}