using System.Text.RegularExpressions;
using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Models.Inputs;
using A_New_Hope.Models.ViewModels.ReferringOrganizations;
using A_New_Hope.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> Index(string? searchTerm)
        {
            try
            {
                _logger.LogInformation("Loading Referring Organizations Index page");

                // Build the base query for active referring organizations.
                var query = _context.ReferringOrganizations
                    .Where(r => r.DeletedAt == null);

                // Apply the search filter when one is provided.
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim();
                    var digitsOnly = new string(searchTerm.Where(char.IsDigit).ToArray());

                    query = query.Where(r =>
                        (r.Name != null && r.Name.Contains(searchTerm)) ||
                        (r.PrimaryContactName != null && r.PrimaryContactName.Contains(searchTerm)) ||
                        (r.Email != null && r.Email.Contains(searchTerm)) ||
                        (r.City != null && r.City.Contains(searchTerm)) ||
                        (r.State != null && r.State.Contains(searchTerm)) ||

                        // Safe phone search
                        (!string.IsNullOrEmpty(digitsOnly) &&
                            r.PhoneNumber != null &&
                            r.PhoneNumber.Replace(" ", "")
                                         .Replace("-", "")
                                         .Replace("(", "")
                                         .Replace(")", "")
                                         .Contains(digitsOnly))
                    );
                }

                // Retrieve the ordered organizations for display.
                var referringOrganizations = await query
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                ViewData["CurrentFilter"] = searchTerm;

                _logger.LogInformation("Loaded {Count} referring organizations", referringOrganizations.Count);

                return View(referringOrganizations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load referring organizations");
                return View("Error");
            }
        }

        // GET: ReferringOrganizations/Details/5
        /// <summary>
        /// Displays details for a single non-deleted referring organization.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            try
            {
                if (id == null)
                {
                    _logger.LogWarning("Details requested with null Id");
                    return NotFound();
                }

                _logger.LogInformation("Fetching details for Referring Organization Id {Id}", id);

                var referringOrganization = await _context.ReferringOrganizations
                    .Where(r => r.DeletedAt == null)
                    .Include(r => r.ReferringOrganizationServiceCategories)
                        .ThenInclude(rosc => rosc.ServiceCategory)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (referringOrganization == null)
                {
                    _logger.LogWarning("Referring Organization Id {Id} not found", id);
                    return NotFound();
                }

                // Map to edit VM (so Details supports edit mode)
                var vm = new A_New_Hope.Models.ViewModels.ReferringOrganizations.ReferringOrganizationEditViewModel
                {
                    Id = referringOrganization.Id,
                    Name = referringOrganization.Name,
                    PhoneNumber = referringOrganization.PhoneNumber,
                    Email = referringOrganization.Email,
                    AddressLine1 = referringOrganization.AddressLine1,
                    AddressLine2 = referringOrganization.AddressLine2,
                    City = referringOrganization.City,
                    State = referringOrganization.State,
                    PostalCode = referringOrganization.PostalCode,
                    PrimaryContactName = referringOrganization.PrimaryContactName,
                    Notes = referringOrganization.Notes,
                    IsActive = referringOrganization.IsActive,

                    SelectedServiceCategoryIds = referringOrganization
                        .ReferringOrganizationServiceCategories
                        .Select(x => x.ServiceCategoryId)
                        .ToList()
                };

                // IMPORTANT: reuse your existing helper so checkboxes populate
                await PopulateServiceCategoriesAsync(vm);

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for Referring Organization Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading referring organization details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: ReferringOrganizations/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            try
            {
                _logger.LogInformation("Loading Create Referring Organization page");

                // Build the default view model for the create view.
                var vm = new ReferringOrganizationEditViewModel
                {
                    State = "WI",
                    IsActive = true
                };

                // Populate dropdown values for the create form.
                await PopulateServiceCategoriesAsync(vm);

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Create Referring Organization page");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the create form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ReferringOrganizations/Create
        /// <summary>
        /// Creates a new referring organization after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReferringOrganizationEditViewModel vm)
        {
            try
            {
                _logger.LogInformation("Attempting to create Referring Organization '{Name}'", vm.Name);

                // Normalize incoming values before business-rule validation.
                NormalizeReferringOrganization(vm);
                await ApplyReferringOrganizationValidationAsync(vm);

                // Return the form when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create Referring Organization failed validation for '{Name}'", vm.Name);
                    await PopulateServiceCategoriesAsync(vm);
                    return View(vm);
                }

                try
                {
                    // Build the input model for the organization service.
                    var input = new ReferringOrganizationEntryInput
                    {
                        Name = vm.Name,
                        SelectedServiceCategoryIds = vm.SelectedServiceCategoryIds,
                        PrimaryContactName = vm.PrimaryContactName,
                        Email = vm.Email,
                        PhoneNumber = vm.PhoneNumber,
                        AddressLine1 = vm.AddressLine1,
                        AddressLine2 = vm.AddressLine2,
                        City = vm.City,
                        State = vm.State,
                        PostalCode = vm.PostalCode,
                        Notes = vm.Notes
                    };

                    // Create the organization and capture the new id.
                    var organizationId = await _referringOrganizationService.CreateAndReturnIdAsync(
                        input,
                        actingUserId: null);

                    if (!vm.IsActive)
                    {
                        var created = await _context.ReferringOrganizations
                            .FirstOrDefaultAsync(r => r.Id == organizationId && r.DeletedAt == null);

                        if (created != null)
                        {
                            created.IsActive = false;
                            created.UpdatedAt = DateTime.UtcNow;
                            created.UpdatedByUserId = null;
                            await _context.SaveChangesAsync();
                        }
                    }

                    _logger.LogInformation("Referring Organization Id {Id} created successfully", organizationId);
                    return RedirectToAction(nameof(Details), new { id = organizationId });
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Business validation failed while creating Referring Organization '{Name}'", vm.Name);
                    ModelState.AddModelError(string.Empty, ex.Message);
                    await PopulateServiceCategoriesAsync(vm);
                    return View(vm);
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "Argument validation failed while creating Referring Organization '{Name}'", vm.Name);
                    ModelState.AddModelError(string.Empty, ex.Message);
                    await PopulateServiceCategoriesAsync(vm);
                    return View(vm);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error creating Referring Organization '{Name}'", vm.Name);
                    ModelState.AddModelError("", "Unable to save referring organization.");
                    await PopulateServiceCategoriesAsync(vm);
                    return View(vm);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating Referring Organization '{Name}'", vm?.Name);
                ModelState.AddModelError("", "An unexpected error occurred while creating the referring organization.");

                vm ??= new ReferringOrganizationEditViewModel();
                await PopulateServiceCategoriesAsync(vm);

                return View(vm);
            }
        }

        // GET: ReferringOrganizations/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted referring organization.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            try
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
                    .Include(r => r.ReferringOrganizationServiceCategories)
                    .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

                // Return not found when the organization does not exist.
                if (referringOrganization == null)
                {
                    _logger.LogWarning("Referring Organization Id {Id} not found for edit", id);
                    return NotFound();
                }

                // Build the view model from the existing organization.
                var vm = new ReferringOrganizationEditViewModel
                {
                    Id = referringOrganization.Id,
                    Name = referringOrganization.Name,
                    SelectedServiceCategoryIds = referringOrganization.ReferringOrganizationServiceCategories
                        .Select(x => x.ServiceCategoryId)
                        .ToList(),
                    PhoneNumber = referringOrganization.PhoneNumber,
                    Email = referringOrganization.Email,
                    AddressLine1 = referringOrganization.AddressLine1,
                    AddressLine2 = referringOrganization.AddressLine2,
                    City = referringOrganization.City,
                    State = referringOrganization.State,
                    PostalCode = referringOrganization.PostalCode,
                    PrimaryContactName = referringOrganization.PrimaryContactName,
                    Notes = referringOrganization.Notes,
                    IsActive = referringOrganization.IsActive
                };

                // Populate dropdown values for the edit form.
                await PopulateServiceCategoriesAsync(vm);

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit page for Referring Organization Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ReferringOrganizations/Edit/5
        /// <summary>
        /// Updates an existing referring organization after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, ReferringOrganizationEditViewModel vm)
        {
            try
            {
                _logger.LogInformation("Attempting to edit Referring Organization Id {Id}", id);

                // Ensure the route id matches the posted model id.
                if (vm.Id != id)
                {
                    _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, vm.Id);
                    return NotFound();
                }

                // Normalize incoming values before business-rule validation.
                NormalizeReferringOrganization(vm);
                await ApplyReferringOrganizationValidationAsync(vm, id);

                // Return the form when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Edit Referring Organization failed validation for Id {Id}", id);
                    await PopulateServiceCategoriesAsync(vm);
                    return View(vm);
                }

                // Retrieve the existing active organization record.
                var existing = await _context.ReferringOrganizations
                    .Include(r => r.ReferringOrganizationServiceCategories)
                    .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

                // Return not found when the target record no longer exists.
                if (existing == null)
                {
                    _logger.LogWarning("Referring Organization Id {Id} not found during edit save", id);
                    return NotFound();
                }

                // Copy validated form values into the tracked entity.
                existing.Name = vm.Name!;
                existing.PhoneNumber = vm.PhoneNumber!;
                existing.Email = vm.Email!;
                existing.AddressLine1 = vm.AddressLine1!;
                existing.AddressLine2 = vm.AddressLine2;
                existing.City = vm.City!;
                existing.State = vm.State!;
                existing.PostalCode = vm.PostalCode!;
                existing.PrimaryContactName = vm.PrimaryContactName;
                existing.Notes = vm.Notes;
                existing.IsActive = vm.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = null;

                var selectedCategoryIds = vm.SelectedServiceCategoryIds
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();

                var validCategoryIds = await _context.ServiceCategories
                    .Where(c => c.DeletedAt == null && c.IsActive && selectedCategoryIds.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync();

                if (validCategoryIds.Count != selectedCategoryIds.Count)
                {
                    ModelState.AddModelError(nameof(vm.SelectedServiceCategoryIds), "One or more selected service categories are invalid.");
                    await PopulateServiceCategoriesAsync(vm);
                    return View(vm);
                }

                // Replace the organization's service category assignments with the selected set.
                _context.ReferringOrganizationServiceCategories.RemoveRange(existing.ReferringOrganizationServiceCategories);

                existing.ReferringOrganizationServiceCategories = validCategoryIds
                    .Select(categoryId => new ReferringOrganizationServiceCategory
                    {
                        ReferringOrganizationId = existing.Id,
                        ServiceCategoryId = categoryId
                    })
                    .ToList();

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Referring Organization Id {Id} updated successfully", id);
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Check whether the record was deleted during the edit attempt.
                    if (!await ReferringOrganizationExists(id))
                    {
                        _logger.LogWarning("Referring Organization Id {Id} no longer exists during concurrency check", id);
                        return NotFound();
                    }

                    _logger.LogError(ex, "Concurrency error updating Referring Organization Id {Id}", id);
                    throw;
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error updating Referring Organization Id {Id}", id);
                    ModelState.AddModelError("", "Unable to save changes.");
                    await PopulateServiceCategoriesAsync(vm);
                    return View(vm);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error editing Referring Organization Id {Id}", id);
                ModelState.AddModelError("", "An unexpected error occurred while updating the referring organization.");
                await PopulateServiceCategoriesAsync(vm);
                return View(vm);
            }
        }

        // GET: ReferringOrganizations/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted referring organization.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete page for Referring Organization Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the delete page.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ReferringOrganizations/Delete/5
        /// <summary>
        /// Soft deletes a referring organization.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting Referring Organization Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the referring organization.";
                return RedirectToAction(nameof(Delete), new { id });
            }
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
        private static void NormalizeReferringOrganization(ReferringOrganizationEditViewModel model)
        {
            model.Name = model.Name?.Trim() ?? string.Empty;
            model.PhoneNumber = model.PhoneNumber?.Trim() ?? string.Empty;
            model.Email = model.Email?.Trim() ?? string.Empty;
            model.AddressLine1 = model.AddressLine1?.Trim() ?? string.Empty;
            model.City = model.City?.Trim() ?? string.Empty;
            model.State = (model.State?.Trim() ?? string.Empty).ToUpperInvariant();
            model.PostalCode = model.PostalCode?.Trim() ?? string.Empty;

            model.AddressLine2 = NullIfWhiteSpace(model.AddressLine2);
            model.PrimaryContactName = NullIfWhiteSpace(model.PrimaryContactName);
            model.Notes = NullIfWhiteSpace(model.Notes);

            model.SelectedServiceCategoryIds ??= new List<ulong>();
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyReferringOrganizationValidationAsync(ReferringOrganizationEditViewModel model, ulong? currentId = null)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "Organization name is required.");
            }

            if (!string.IsNullOrWhiteSpace(model.Name) && !ContainsLetterOrDigit(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "Organization name must contain letters or numbers.");
            }

            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                var normalizedName = model.Name.ToLower();

                var duplicateExists = await _context.ReferringOrganizations
                    .AnyAsync(r =>
                        r.DeletedAt == null &&
                        r.Id != currentId &&
                        r.Name.ToLower() == normalizedName);

                if (duplicateExists)
                {
                    ModelState.AddModelError(nameof(model.Name), "An organization with this name already exists.");
                }
            }

            if (model.SelectedServiceCategoryIds == null || !model.SelectedServiceCategoryIds.Any())
            {
                ModelState.AddModelError(nameof(model.SelectedServiceCategoryIds), "Select at least one service category.");
            }

            if (!string.IsNullOrWhiteSpace(model.AddressLine1) && !ContainsLetterOrDigit(model.AddressLine1))
            {
                ModelState.AddModelError(nameof(model.AddressLine1), "Address Line 1 must contain letters or numbers.");
            }

            if (!string.IsNullOrWhiteSpace(model.AddressLine2) && !ContainsLetterOrDigit(model.AddressLine2))
            {
                ModelState.AddModelError(nameof(model.AddressLine2), "Address Line 2 must contain letters or numbers.");
            }

            if (!string.IsNullOrWhiteSpace(model.PhoneNumber) && !IsValidPhoneNumber(model.PhoneNumber))
            {
                ModelState.AddModelError(nameof(model.PhoneNumber), "Enter a valid US phone number with 10 digits, or 11 digits starting with 1.");
            }

            if (!string.IsNullOrWhiteSpace(model.Email) && !IsValidEmail(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email format is invalid.");
            }

            if (!string.IsNullOrWhiteSpace(model.City) && !IsValidCity(model.City))
            {
                ModelState.AddModelError(nameof(model.City), "City contains invalid characters.");
            }

            if (!string.IsNullOrWhiteSpace(model.State) && !IsValidUsStateCode(model.State))
            {
                ModelState.AddModelError(nameof(model.State), "Enter a valid 2-letter US state code.");
            }

            if (!string.IsNullOrWhiteSpace(model.PostalCode) && !IsValidUsPostalCode(model.PostalCode))
            {
                ModelState.AddModelError(nameof(model.PostalCode), "Enter a valid US ZIP code or ZIP+4.");
            }

            if (!string.IsNullOrWhiteSpace(model.PrimaryContactName) && !IsValidPersonName(model.PrimaryContactName))
            {
                ModelState.AddModelError(nameof(model.PrimaryContactName), "Primary contact name contains invalid characters.");
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

        /// <summary>
        /// Populates the available service categories for the create and edit forms.
        /// </summary>
        private async Task PopulateServiceCategoriesAsync(ReferringOrganizationEditViewModel vm)
        {
            vm.SelectedServiceCategoryIds ??= new List<ulong>();

            vm.AvailableServiceCategories = await _context.ServiceCategories
                .Where(c => c.DeletedAt == null && c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = vm.SelectedServiceCategoryIds.Contains(c.Id)
                })
                .ToListAsync();
        }
    }
}