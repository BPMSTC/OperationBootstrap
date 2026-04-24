using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages create, read, update, and soft delete operations for household members.
    /// </summary>
    public class HouseholdMembersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HouseholdMembersController> _logger;

        /// <summary>
        /// Creates the controller with the required database context and logger.
        /// </summary>
        public HouseholdMembersController(ApplicationDbContext context, ILogger<HouseholdMembersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: HouseholdMembers
        /// <summary>
        /// Displays all non-deleted household members.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Loading HouseholdMembers Index page");

                // Retrieve active household members with their related client user.
                var householdMembers = await _context.HouseholdMembers
                    .Where(h => h.DeletedAt == null)
                    .Include(h => h.ClientUser)
                    .OrderBy(h => h.LastName)
                    .ThenBy(h => h.FirstName)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} household members", householdMembers.Count);

                return View(householdMembers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading household members list");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading household members.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: HouseholdMembers/Details/5
        /// <summary>
        /// Displays details for a single non-deleted household member.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            try
            {
                // Reject requests with no id.
                if (id == null)
                {
                    _logger.LogWarning("Details requested with null id");
                    return NotFound();
                }

                _logger.LogInformation("Fetching details for HouseholdMember Id {Id}", id);

                // Retrieve the requested active household member with related client user data.
                var householdMember = await _context.HouseholdMembers
                    .Where(h => h.DeletedAt == null)
                    .Include(h => h.ClientUser)
                    .FirstOrDefaultAsync(m => m.Id == id);

                // Return not found when the household member does not exist.
                if (householdMember == null)
                {
                    _logger.LogWarning("HouseholdMember Id {Id} not found", id);
                    return NotFound();
                }

                return View(householdMember);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for HouseholdMember Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading household member details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: HouseholdMembers/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            try
            {
                _logger.LogInformation("Loading Create HouseholdMember page");

                // Populate dropdown values for the create form.
                await PopulateDropdowns();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Create HouseholdMember page");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the create form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: HouseholdMembers/Create
        /// <summary>
        /// Creates a new household member after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientUserId,FirstName,LastName,DateOfBirth,ApproximateAge")] HouseholdMember householdMember)
        {
            try
            {
                _logger.LogInformation("Attempting to create HouseholdMember for ClientUserId {ClientUserId}", householdMember.ClientUserId);

                // Remove navigation properties that are not posted by the form.
                ModelState.Remove(nameof(HouseholdMember.ClientUser));
                ModelState.Remove(nameof(HouseholdMember.CreatedByUser));
                ModelState.Remove(nameof(HouseholdMember.UpdatedByUser));

                // Normalize incoming values before business-rule validation.
                NormalizeHouseholdMember(householdMember);
                await ApplyHouseholdMemberValidationAsync(householdMember);

                // Return the form with dropdowns restored when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create HouseholdMember failed validation for ClientUserId {ClientUserId}", householdMember.ClientUserId);
                    await PopulateDropdowns(householdMember.ClientUserId);
                    return View(householdMember);
                }

                // Set audit fields for the new household member record.
                var now = DateTime.UtcNow;
                householdMember.CreatedAt = now;
                householdMember.UpdatedAt = now;
                householdMember.CreatedByUserId = null; // Placeholder until auth integration.
                householdMember.UpdatedByUserId = null; // Placeholder until auth integration.

                // Queue the new household member for insert.
                _context.Add(householdMember);

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("HouseholdMember created successfully for ClientUserId {ClientUserId}", householdMember.ClientUserId);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error creating HouseholdMember for ClientUserId {ClientUserId}", householdMember.ClientUserId);

                    ModelState.AddModelError("", "Unable to save household member.");
                    await PopulateDropdowns(householdMember.ClientUserId);
                    return View(householdMember);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating HouseholdMember for ClientUserId {ClientUserId}", householdMember?.ClientUserId);
                ModelState.AddModelError("", "An unexpected error occurred while creating the household member.");

                await PopulateDropdowns(householdMember?.ClientUserId);
                return View(householdMember ?? new HouseholdMember());
            }
        }

        // GET: HouseholdMembers/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted household member.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            try
            {
                // Reject requests with no id.
                if (id == null)
                {
                    _logger.LogWarning("Edit requested with null id");
                    return NotFound();
                }

                _logger.LogInformation("Loading Edit page for HouseholdMember Id {Id}", id);

                // Retrieve the requested active household member for editing.
                var householdMember = await _context.HouseholdMembers
                    .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);

                // Return not found when the household member does not exist.
                if (householdMember == null)
                {
                    _logger.LogWarning("HouseholdMember Id {Id} not found for edit", id);
                    return NotFound();
                }

                // Populate dropdown values using the current record selection.
                await PopulateDropdowns(householdMember.ClientUserId);

                return View(householdMember);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit page for HouseholdMember Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: HouseholdMembers/Edit/5
        /// <summary>
        /// Updates an existing household member after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,ClientUserId,FirstName,LastName,DateOfBirth,ApproximateAge")] HouseholdMember formModel)
        {
            try
            {
                _logger.LogInformation("Attempting to edit HouseholdMember Id {Id}", id);

                // Ensure the route id matches the posted model id.
                if (id != formModel.Id)
                {
                    _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                    return NotFound();
                }

                // Remove navigation properties that are not posted by the form.
                ModelState.Remove(nameof(HouseholdMember.ClientUser));
                ModelState.Remove(nameof(HouseholdMember.CreatedByUser));
                ModelState.Remove(nameof(HouseholdMember.UpdatedByUser));

                // Normalize incoming values before business-rule validation.
                NormalizeHouseholdMember(formModel);
                await ApplyHouseholdMemberValidationAsync(formModel, formModel.Id);

                // Return the form with dropdowns restored when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Edit HouseholdMember failed validation for Id {Id}", id);
                    await PopulateDropdowns(formModel.ClientUserId);
                    return View(formModel);
                }

                // Retrieve the existing active household member record.
                var existing = await _context.HouseholdMembers
                    .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);

                // Return not found when the target record no longer exists.
                if (existing == null)
                {
                    _logger.LogWarning("HouseholdMember Id {Id} not found during edit save", id);
                    return NotFound();
                }

                // Copy validated form values into the tracked entity.
                existing.ClientUserId = formModel.ClientUserId;
                existing.FirstName = formModel.FirstName;
                existing.LastName = formModel.LastName;
                existing.DateOfBirth = formModel.DateOfBirth;
                existing.ApproximateAge = formModel.ApproximateAge;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = null; // Placeholder until auth integration.

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("HouseholdMember Id {Id} updated successfully", id);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Check whether the record was deleted during the edit attempt.
                    if (!await HouseholdMemberExists(formModel.Id))
                    {
                        _logger.LogWarning("HouseholdMember Id {Id} no longer exists during concurrency check", id);
                        return NotFound();
                    }

                    _logger.LogError(ex, "Concurrency error updating HouseholdMember Id {Id}", id);
                    throw;
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error updating HouseholdMember Id {Id}", id);

                    ModelState.AddModelError("", "Unable to save changes.");
                    await PopulateDropdowns(formModel.ClientUserId);
                    return View(formModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error editing HouseholdMember Id {Id}", id);
                ModelState.AddModelError("", "An unexpected error occurred while updating the household member.");
                await PopulateDropdowns(formModel.ClientUserId);
                return View(formModel);
            }
        }

        // GET: HouseholdMembers/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted household member.
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

                _logger.LogWarning("Loading Delete confirmation for HouseholdMember Id {Id}", id);

                // Retrieve the requested active household member with related client user data.
                var householdMember = await _context.HouseholdMembers
                    .Where(h => h.DeletedAt == null)
                    .Include(h => h.ClientUser)
                    .FirstOrDefaultAsync(m => m.Id == id);

                // Return not found when the household member does not exist.
                if (householdMember == null)
                {
                    _logger.LogWarning("HouseholdMember Id {Id} not found for delete", id);
                    return NotFound();
                }

                return View(householdMember);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete page for HouseholdMember Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the delete page.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: HouseholdMembers/Delete/5
        /// <summary>
        /// Soft deletes a household member.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            try
            {
                _logger.LogWarning("Soft deleting HouseholdMember Id {Id}", id);

                // Retrieve the active household member targeted for soft delete.
                var householdMember = await _context.HouseholdMembers
                    .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);

                // Return not found when the household member does not exist.
                if (householdMember == null)
                {
                    _logger.LogWarning("HouseholdMember Id {Id} not found during delete", id);
                    return NotFound();
                }

                // Apply soft-delete and audit values.
                householdMember.DeletedAt = DateTime.UtcNow;
                householdMember.UpdatedAt = DateTime.UtcNow;
                householdMember.UpdatedByUserId = null; // Placeholder until auth integration.

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("HouseholdMember Id {Id} soft deleted", id);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error soft deleting HouseholdMember Id {Id}", id);

                    TempData["ErrorMessage"] = "Unable to delete household member.";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting HouseholdMember Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the household member.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        /// <summary>
        /// Populates the client user dropdown for the create and edit forms.
        /// </summary>
        private async Task PopulateDropdowns(ulong? selectedClientUserId = null)
        {
            _logger.LogDebug("Populating ClientUser dropdown for HouseholdMember");

            // Retrieve active client users for the dropdown list.
            var users = await _context.DomainUsers
                .Where(u =>
                    u.DeletedAt == null &&
                    u.IsActive &&
                    u.UserType == UserType.Client)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new
                {
                    u.Id,
                    DisplayName = $"{u.LastName}, {u.FirstName} ({u.Email})"
                })
                .ToListAsync();

            // Store the client user dropdown options in ViewData.
            ViewData["ClientUserId"] = new SelectList(users, "Id", "DisplayName", selectedClientUserId);
        }

        /// <summary>
        /// Returns true if the non-deleted household member exists.
        /// </summary>
        private async Task<bool> HouseholdMemberExists(ulong id)
        {
            // Check whether the requested active household member still exists.
            return await _context.HouseholdMembers
                .AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Trims required string values.
        /// </summary>
        private static void NormalizeHouseholdMember(HouseholdMember model)
        {
            // Normalize and trim required name values.
            model.FirstName = model.FirstName?.Trim() ?? string.Empty;
            model.LastName = model.LastName?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyHouseholdMemberValidationAsync(HouseholdMember model, ulong? currentId = null)
        {
            // Validate that the selected client exists, is active, and is not deleted.
            var validClient = await _context.DomainUsers
                .AnyAsync(u =>
                    u.Id == model.ClientUserId &&
                    u.DeletedAt == null &&
                    u.IsActive &&
                    u.UserType == UserType.Client);

            if (!validClient)
            {
                ModelState.AddModelError(nameof(HouseholdMember.ClientUserId), "Select a valid active client.");
            }

            // Require first name for all household members.
            if (string.IsNullOrWhiteSpace(model.FirstName))
            {
                ModelState.AddModelError(nameof(HouseholdMember.FirstName), "First Name is required.");
            }

            // Require last name for all household members.
            if (string.IsNullOrWhiteSpace(model.LastName))
            {
                ModelState.AddModelError(nameof(HouseholdMember.LastName), "Last Name is required.");
            }

            // Validate first name characters when provided.
            if (!string.IsNullOrWhiteSpace(model.FirstName) &&
                !PersonValidation.IsValidPersonName(model.FirstName))
            {
                ModelState.AddModelError(nameof(HouseholdMember.FirstName), "First Name contains invalid characters.");
            }

            // Validate last name characters when provided.
            if (!string.IsNullOrWhiteSpace(model.LastName) &&
                !PersonValidation.IsValidPersonName(model.LastName))
            {
                ModelState.AddModelError(nameof(HouseholdMember.LastName), "Last Name contains invalid characters.");
            }

            // Validate date of birth range when provided.
            if (model.DateOfBirth.HasValue)
            {
                var minDate = new DateTime(1900, 1, 1);

                if (model.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
                {
                    ModelState.AddModelError(nameof(HouseholdMember.DateOfBirth), "Date of Birth cannot be in the future.");
                }

                if (model.DateOfBirth.Value.Date < minDate)
                {
                    ModelState.AddModelError(nameof(HouseholdMember.DateOfBirth), "Date of Birth is earlier than the allowed minimum.");
                }
            }

            // Require either Date of Birth or Approximate Age.
            if (!model.DateOfBirth.HasValue && !model.ApproximateAge.HasValue)
            {
                ModelState.AddModelError(nameof(HouseholdMember.DateOfBirth), "Enter either Date of Birth or Approximate Age.");
                ModelState.AddModelError(nameof(HouseholdMember.ApproximateAge), "Enter either Date of Birth or Approximate Age.");
            }

            // Optional: block both being entered at once.
            if (model.DateOfBirth.HasValue && model.ApproximateAge.HasValue)
            {
                ModelState.AddModelError(nameof(HouseholdMember.DateOfBirth), "Enter either Date of Birth or Approximate Age, not both.");
                ModelState.AddModelError(nameof(HouseholdMember.ApproximateAge), "Enter either Date of Birth or Approximate Age, not both.");
            }
        }
    }
}