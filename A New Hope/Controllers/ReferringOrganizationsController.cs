using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Models.Inputs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using A_New_Hope.Models.ViewModels.Referrals;
using A_New_Hope.Services.Interfaces;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages create, read, update, and soft delete operations for referring organizations.
    /// </summary>
    public class ReferringOrganizationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReferringOrganizationsController> _logger;
        private readonly IReferringOrganizationService _referringOrganizationService;

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
        public ReferringOrganizationsController(
            ApplicationDbContext context,
            ILogger<ReferringOrganizationsController> logger,
            IReferringOrganizationService referringOrganizationService)
        {
            _context = context;
            _logger = logger;
            _referringOrganizationService = referringOrganizationService;
        }

        // GET: ReferringOrganizations
        /// <summary>
        /// Displays all non-deleted referring organizations.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading Referring Organizations Index page");

            // Retrieve active referring organizations for display.
            var referringOrganizations = await _context.ReferringOrganizations
                .Where(r => r.DeletedAt == null)
                .OrderBy(r => r.Name)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} referring organizations", referringOrganizations.Count);

            return View(referringOrganizations);
        }

        // GET: ReferringOrganizations/Details/5
        /// <summary>
        /// Displays details for a single non-deleted referring organization.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            // Reject requests with no id.
            if (id == null)
            {
                _logger.LogWarning("Details requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for Referring Organization Id {Id}", id);

            // Retrieve the requested active referring organization.
            var referringOrganization = await _context.ReferringOrganizations
                .Where(r => r.DeletedAt == null)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Return not found when the organization does not exist.
            if (referringOrganization == null)
            {
                _logger.LogWarning("Referring Organization Id {Id} not found", id);
                return NotFound();
            }

            return View(referringOrganization);
        }

        // GET: ReferringOrganizations/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public IActionResult Create()
        {
            _logger.LogInformation("Loading Create Referring Organization page");

            // Initialize the form with the default state value.
            var model = new ReferringOrganization
            {
                State = "WI"
            };

            return View(model);
        }

        // POST: ReferringOrganizations/Create
        /// <summary>
        /// Creates a new referring organization after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Type,PhoneNumber,Email,AddressLine1,AddressLine2,City,State,PostalCode,PrimaryContactName,Notes,IsActive")] ReferringOrganization referringOrganization)
        {
            _logger.LogInformation("Attempting to create Referring Organization '{Name}'", referringOrganization.Name);

            // Remove navigation properties that are not posted by the form.
            ModelState.Remove(nameof(ReferringOrganization.Referrals));
            ModelState.Remove(nameof(ReferringOrganization.CreatedByUser));
            ModelState.Remove(nameof(ReferringOrganization.UpdatedByUser));

            // Normalize text input before applying business-rule validation.
            NormalizeReferringOrganization(referringOrganization);
            await ApplyReferringOrganizationValidationAsync(referringOrganization);

            // Return the form when validation fails.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create Referring Organization failed validation for '{Name}'", referringOrganization.Name);
                return View(referringOrganization);
            }

            try
            {
                var input = new ReferringOrganizationEntryInput
                {
                    Name = referringOrganization.Name,
                    Type = referringOrganization.Type,
                    PrimaryContactName = referringOrganization.PrimaryContactName,
                    Email = referringOrganization.Email,
                    PhoneNumber = referringOrganization.PhoneNumber,
                    AddressLine1 = referringOrganization.AddressLine1,
                    AddressLine2 = referringOrganization.AddressLine2,
                    City = referringOrganization.City,
                    State = referringOrganization.State,
                    PostalCode = referringOrganization.PostalCode,
                    Notes = referringOrganization.Notes
                };

                var organizationId = await _referringOrganizationService.CreateAndReturnIdAsync(
                    input,
                    actingUserId: null);

                _logger.LogInformation("Referring Organization Id {Id} created successfully", organizationId);
                return RedirectToAction(nameof(Details), new { id = organizationId });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business validation failed while creating Referring Organization '{Name}'", referringOrganization.Name);

                ModelState.AddModelError(string.Empty, ex.Message);
                return View(referringOrganization);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argument validation failed while creating Referring Organization '{Name}'", referringOrganization.Name);

                ModelState.AddModelError(string.Empty, ex.Message);
                return View(referringOrganization);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error creating Referring Organization '{Name}'", referringOrganization.Name);

                ModelState.AddModelError("", "Unable to save referring organization.");
                return View(referringOrganization);
            }
        }

        // GET: ReferringOrganizations/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted referring organization.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            // Reject requests with no id.
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for Referring Organization Id {Id}", id);

            // Retrieve the requested active referring organization for editing.
            var referringOrganization = await _context.ReferringOrganizations
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            // Return not found when the organization does not exist.
            if (referringOrganization == null)
            {
                _logger.LogWarning("Referring Organization Id {Id} not found for edit", id);
                return NotFound();
            }

            return View(referringOrganization);
        }

        // POST: ReferringOrganizations/Edit/5
        /// <summary>
        /// Updates an existing referring organization after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Name,Type,PhoneNumber,Email,AddressLine1,AddressLine2,City,State,PostalCode,PrimaryContactName,Notes,IsActive")] ReferringOrganization formModel)
        {
            _logger.LogInformation("Attempting to edit Referring Organization Id {Id}", id);

            // Ensure the route id matches the posted model id.
            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            // Remove navigation properties that are not posted by the form.
            ModelState.Remove(nameof(ReferringOrganization.Referrals));
            ModelState.Remove(nameof(ReferringOrganization.CreatedByUser));
            ModelState.Remove(nameof(ReferringOrganization.UpdatedByUser));

            // Normalize text input before applying business-rule validation.
            NormalizeReferringOrganization(formModel);
            await ApplyReferringOrganizationValidationAsync(formModel, formModel.Id);

            // Return the form when validation fails.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit Referring Organization failed validation for Id {Id}", id);
                return View(formModel);
            }

            // Load the tracked entity so only approved fields are updated.
            var existing = await _context.ReferringOrganizations
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            // Return not found when the target record no longer exists.
            if (existing == null)
            {
                _logger.LogWarning("Referring Organization Id {Id} not found during edit save", id);
                return NotFound();
            }

            // Copy validated form values into the tracked entity.
            existing.Name = formModel.Name;
            existing.Type = formModel.Type;
            existing.PhoneNumber = formModel.PhoneNumber;
            existing.Email = formModel.Email;
            existing.AddressLine1 = formModel.AddressLine1;
            existing.AddressLine2 = formModel.AddressLine2;
            existing.City = formModel.City;
            existing.State = formModel.State;
            existing.PostalCode = formModel.PostalCode;
            existing.PrimaryContactName = formModel.PrimaryContactName;
            existing.Notes = formModel.Notes;
            existing.IsActive = formModel.IsActive;

            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // Replace when auth/user tracking is added.

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Referring Organization Id {Id} updated successfully", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Check whether the record was deleted during the edit attempt.
                if (!await ReferringOrganizationExists(formModel.Id))
                {
                    _logger.LogWarning("Referring Organization Id {Id} no longer exists during concurrency check", id);
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating Referring Organization Id {Id}", id);

                ModelState.AddModelError("", "Unable to save changes.");
                return View(formModel);
            }
        }

        // GET: ReferringOrganizations/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted referring organization.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            // Reject requests with no id.
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for Referring Organization Id {Id}", id);

            // Retrieve the requested active referring organization for delete confirmation.
            var referringOrganization = await _context.ReferringOrganizations
                .Where(r => r.DeletedAt == null)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Return not found when the organization does not exist.
            if (referringOrganization == null)
            {
                _logger.LogWarning("Referring Organization Id {Id} not found for delete", id);
                return NotFound();
            }

            return View(referringOrganization);
        }

        // POST: ReferringOrganizations/Delete/5
        /// <summary>
        /// Soft deletes a referring organization.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting Referring Organization Id {Id}", id);

            // Retrieve the active referring organization targeted for soft delete.
            var referringOrganization = await _context.ReferringOrganizations
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            // Return not found when the organization does not exist.
            if (referringOrganization == null)
            {
                _logger.LogWarning("Referring Organization Id {Id} not found during delete", id);
                return NotFound();
            }

            // Apply soft-delete and audit values.
            referringOrganization.DeletedAt = DateTime.UtcNow;
            referringOrganization.UpdatedAt = DateTime.UtcNow;
            referringOrganization.UpdatedByUserId = null; // Replace when auth/user tracking is added.

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Referring Organization Id {Id} soft deleted", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error soft deleting Referring Organization Id {Id}", id);

                TempData["ErrorMessage"] = "Unable to delete referring organization.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Returns true if the non-deleted organization exists.
        /// </summary>
        private async Task<bool> ReferringOrganizationExists(ulong id)
        {
            // Check whether the requested active organization still exists.
            return await _context.ReferringOrganizations
                .AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Trims strings and converts blank optional values to null.
        /// </summary>
        private static void NormalizeReferringOrganization(ReferringOrganization model)
        {
            // Keep the required organization name as an empty string instead of null for validation.
            model.Name = model.Name?.Trim() ?? string.Empty;

            // Normalize optional text values before validation and save.
            model.Type = NullIfWhiteSpace(model.Type);
            model.PhoneNumber = NullIfWhiteSpace(model.PhoneNumber);
            model.Email = NullIfWhiteSpace(model.Email);
            model.AddressLine1 = NullIfWhiteSpace(model.AddressLine1);
            model.AddressLine2 = NullIfWhiteSpace(model.AddressLine2);
            model.City = NullIfWhiteSpace(model.City);
            model.State = NullIfWhiteSpace(model.State)?.ToUpperInvariant();
            model.PostalCode = NullIfWhiteSpace(model.PostalCode);
            model.PrimaryContactName = NullIfWhiteSpace(model.PrimaryContactName);
            model.Notes = NullIfWhiteSpace(model.Notes);
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyReferringOrganizationValidationAsync(ReferringOrganization model, ulong? currentId = null)
        {
            // Require an organization name.
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.Name), "Organization name is required.");
            }

            // Require at least one letter or number in the organization name.
            if (!string.IsNullOrWhiteSpace(model.Name) && !ContainsLetterOrDigit(model.Name))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.Name), "Organization name must contain letters or numbers.");
            }

            // Prevent duplicate active organization names.
            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                // Only check against non-deleted rows and ignore the current record during edit.
                var normalizedName = model.Name.ToLower();

                var duplicateExists = await _context.ReferringOrganizations
                    .AnyAsync(r =>
                        r.DeletedAt == null &&
                        r.Id != currentId &&
                        r.Name.ToLower() == normalizedName);

                if (duplicateExists)
                {
                    ModelState.AddModelError(nameof(ReferringOrganization.Name), "An organization with this name already exists.");
                }
            }

            // Validate organization type content when provided.
            if (!string.IsNullOrWhiteSpace(model.Type) && !ContainsLetterOrDigit(model.Type))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.Type), "Type must contain letters or numbers.");
            }

            // Validate address line 1 content when provided.
            if (!string.IsNullOrWhiteSpace(model.AddressLine1) && !ContainsLetterOrDigit(model.AddressLine1))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.AddressLine1), "Address Line 1 must contain letters or numbers.");
            }

            // Validate address line 2 content when provided.
            if (!string.IsNullOrWhiteSpace(model.AddressLine2) && !ContainsLetterOrDigit(model.AddressLine2))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.AddressLine2), "Address Line 2 must contain letters or numbers.");
            }

            // Validate phone number format when provided.
            if (!string.IsNullOrWhiteSpace(model.PhoneNumber) && !IsValidPhoneNumber(model.PhoneNumber))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.PhoneNumber), "Enter a valid US phone number with 10 digits, or 11 digits starting with 1.");
            }

            // Validate email format when provided.
            if (!string.IsNullOrWhiteSpace(model.Email) && !IsValidEmail(model.Email))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.Email), "Email format is invalid.");
            }

            // Validate city characters when provided.
            if (!string.IsNullOrWhiteSpace(model.City) && !IsValidCity(model.City))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.City), "City contains invalid characters.");
            }

            // Validate state code when provided.
            if (!string.IsNullOrWhiteSpace(model.State) && !IsValidUsStateCode(model.State))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.State), "Enter a valid 2-letter US state code.");
            }

            // Validate ZIP code format when provided.
            if (!string.IsNullOrWhiteSpace(model.PostalCode) && !IsValidUsPostalCode(model.PostalCode))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.PostalCode), "Enter a valid US ZIP code or ZIP+4.");
            }

            // Validate primary contact name characters when provided.
            if (!string.IsNullOrWhiteSpace(model.PrimaryContactName) && !IsValidPersonName(model.PrimaryContactName))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.PrimaryContactName), "Primary contact name contains invalid characters.");
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
        /// Returns true when the value contains at least one letter or digit.
        /// </summary>
        private static bool ContainsLetterOrDigit(string value)
        {
            // Require at least one alphanumeric character in the value.
            return value.Any(char.IsLetterOrDigit);
        }

        /// <summary>
        /// Validates a US-style phone number.
        /// Allows 10 digits, or 11 digits when the first digit is 1.
        /// Formatting characters such as spaces, hyphens, parentheses, and leading + are allowed.
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
        /// Validates a city name using a practical US-style character set.
        /// </summary>
        private static bool IsValidCity(string city)
        {
            // Allow letters plus common punctuation for city names.
            return Regex.IsMatch(city, @"^[A-Za-z][A-Za-z\s'.-]*$");
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
        /// Validates a 2-letter US state code.
        /// </summary>
        private static bool IsValidUsStateCode(string state)
        {
            // Require a 2-letter state code from the approved US state set.
            return state.Length == 2 && ValidUsStateCodes.Contains(state);
        }

        /// <summary>
        /// Validates a US ZIP code or ZIP+4.
        /// </summary>
        private static bool IsValidUsPostalCode(string postalCode)
        {
            // Accept 5-digit ZIP codes and ZIP+4 values.
            return Regex.IsMatch(postalCode, @"^\d{5}(-\d{4})?$");
        }
    }
}