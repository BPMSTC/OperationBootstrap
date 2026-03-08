using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages create, read, update, and soft delete operations for referring organizations.
    /// </summary>
    public class ReferringOrganizationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReferringOrganizationsController> _logger;

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
        public ReferringOrganizationsController(ApplicationDbContext context, ILogger<ReferringOrganizationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: ReferringOrganizations
        /// <summary>
        /// Displays all non-deleted referring organizations.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading Referring Organizations Index page");

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
            if (id == null)
            {
                _logger.LogWarning("Details requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for Referring Organization Id {Id}", id);

            var referringOrganization = await _context.ReferringOrganizations
                .Where(r => r.DeletedAt == null)
                .FirstOrDefaultAsync(m => m.Id == id);

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

            // Navigation properties are not posted by the form.
            ModelState.Remove(nameof(ReferringOrganization.Referrals));
            ModelState.Remove(nameof(ReferringOrganization.CreatedByUser));
            ModelState.Remove(nameof(ReferringOrganization.UpdatedByUser));

            // Normalize text input before applying business-rule validation.
            NormalizeReferringOrganization(referringOrganization);
            await ApplyReferringOrganizationValidationAsync(referringOrganization);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create Referring Organization failed validation for '{Name}'", referringOrganization.Name);
                return View(referringOrganization);
            }

            var now = DateTime.UtcNow;
            referringOrganization.CreatedAt = now;
            referringOrganization.UpdatedAt = now;
            referringOrganization.CreatedByUserId = null; // Replace when auth/user tracking is added.
            referringOrganization.UpdatedByUserId = null; // Replace when auth/user tracking is added.

            _context.Add(referringOrganization);

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Referring Organization Id {Id} created successfully", referringOrganization.Id);
                return RedirectToAction(nameof(Index));
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
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for Referring Organization Id {Id}", id);

            var referringOrganization = await _context.ReferringOrganizations
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

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

            // Prevent route/model mismatches.
            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            // Navigation properties are not posted by the form.
            ModelState.Remove(nameof(ReferringOrganization.Referrals));
            ModelState.Remove(nameof(ReferringOrganization.CreatedByUser));
            ModelState.Remove(nameof(ReferringOrganization.UpdatedByUser));

            // Normalize text input before applying business-rule validation.
            NormalizeReferringOrganization(formModel);
            await ApplyReferringOrganizationValidationAsync(formModel, formModel.Id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit Referring Organization failed validation for Id {Id}", id);
                return View(formModel);
            }

            // Load the tracked entity so only approved fields are updated.
            var existing = await _context.ReferringOrganizations
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (existing == null)
            {
                _logger.LogWarning("Referring Organization Id {Id} not found during edit save", id);
                return NotFound();
            }

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
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for Referring Organization Id {Id}", id);

            var referringOrganization = await _context.ReferringOrganizations
                .Where(r => r.DeletedAt == null)
                .FirstOrDefaultAsync(m => m.Id == id);

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

            var referringOrganization = await _context.ReferringOrganizations
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (referringOrganization == null)
            {
                _logger.LogWarning("Referring Organization Id {Id} not found during delete", id);
                return NotFound();
            }

            // Soft delete instead of physically removing the row.
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
            return await _context.ReferringOrganizations
                .AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Trims strings and converts blank optional values to null.
        /// </summary>
        private static void NormalizeReferringOrganization(ReferringOrganization model)
        {
            // Name is required, so keep it as an empty string instead of null for validation.
            model.Name = model.Name?.Trim() ?? string.Empty;

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
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.Name), "Organization name is required.");
            }

            if (!string.IsNullOrWhiteSpace(model.Name) && !ContainsLetterOrDigit(model.Name))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.Name), "Organization name must contain letters or numbers.");
            }

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

            if (!string.IsNullOrWhiteSpace(model.Type) && !ContainsLetterOrDigit(model.Type))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.Type), "Type must contain letters or numbers.");
            }

            if (!string.IsNullOrWhiteSpace(model.AddressLine1) && !ContainsLetterOrDigit(model.AddressLine1))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.AddressLine1), "Address Line 1 must contain letters or numbers.");
            }

            if (!string.IsNullOrWhiteSpace(model.AddressLine2) && !ContainsLetterOrDigit(model.AddressLine2))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.AddressLine2), "Address Line 2 must contain letters or numbers.");
            }

            if (!string.IsNullOrWhiteSpace(model.PhoneNumber) && !IsValidPhoneNumber(model.PhoneNumber))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.PhoneNumber), "Enter a valid US phone number with 10 digits, or 11 digits starting with 1.");
            }

            if (!string.IsNullOrWhiteSpace(model.Email) && !IsValidEmail(model.Email))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.Email), "Email format is invalid.");
            }

            if (!string.IsNullOrWhiteSpace(model.City) && !IsValidCity(model.City))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.City), "City contains invalid characters.");
            }

            if (!string.IsNullOrWhiteSpace(model.State) && !IsValidUsStateCode(model.State))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.State), "Enter a valid 2-letter US state code.");
            }

            if (!string.IsNullOrWhiteSpace(model.PostalCode) && !IsValidUsPostalCode(model.PostalCode))
            {
                ModelState.AddModelError(nameof(ReferringOrganization.PostalCode), "Enter a valid US ZIP code or ZIP+4.");
            }

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
        /// Validates a US-style phone number.
        /// Allows 10 digits, or 11 digits when the first digit is 1.
        /// Formatting characters such as spaces, hyphens, parentheses, and leading + are allowed.
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
        /// Validates a city name using a practical US-style character set.
        /// </summary>
        private static bool IsValidCity(string city)
        {
            return Regex.IsMatch(city, @"^[A-Za-z][A-Za-z\s'.-]*$");
        }

        /// <summary>
        /// Validates a person name using a practical character set.
        /// </summary>
        private static bool IsValidPersonName(string name)
        {
            return Regex.IsMatch(name, @"^[A-Za-z][A-Za-z\s'.-]*$");
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