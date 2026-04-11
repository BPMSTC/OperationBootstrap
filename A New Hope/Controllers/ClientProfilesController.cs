using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages create, read, update, and soft delete operations for client profiles.
    /// </summary>
    public class ClientProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClientProfilesController> _logger;

        /// <summary>
        /// Creates the controller with the required database context and logger.
        /// </summary>
        public ClientProfilesController(ApplicationDbContext context, ILogger<ClientProfilesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: ClientProfiles
        /// <summary>
        /// Displays all non-deleted client profiles.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Fetching client profiles list");

            // Retrieve active client profiles with related user display data.
            var clientProfiles = await _context.ClientProfiles
                .Where(c => c.DeletedAt == null)
                .Include(c => c.User)
                .OrderBy(c => c.User.LastName)
                .ThenBy(c => c.User.FirstName)
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} client profiles", clientProfiles.Count);

            return View(clientProfiles);
        }

        // GET: ClientProfiles/Details/5
        /// <summary>
        /// Displays details for a single non-deleted client profile.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            // Reject requests with no id.
            if (id == null)
            {
                _logger.LogWarning("ClientProfile Details requested with null id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for ClientProfile UserId {Id}", id);

            // Retrieve the requested active client profile with related user data.
            var clientProfile = await _context.ClientProfiles
                .Where(c => c.DeletedAt == null)
                .Include(c => c.User)
                .Include(c => c.ClientIncomes)
                .FirstOrDefaultAsync(m => m.UserId == id);

            // Return not found when the client profile does not exist.
            if (clientProfile == null)
            {
                _logger.LogWarning("ClientProfile UserId {Id} not found", id);
                return NotFound();
            }

            return View(clientProfile);
        }

        // GET: ClientProfiles/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create ClientProfile page");

            // Populate dropdown values for the create form.
            await PopulateDropdowns();
            return View();
        }

        // POST: ClientProfiles/Create
        /// <summary>
        /// Creates a new client profile after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,EmploymentStatus,IsUnhoused")] ClientProfile clientProfile)
        {
            _logger.LogInformation("Attempting to create ClientProfile for UserId {UserId}", clientProfile.UserId);

            // Remove navigation properties that are not posted by the form.
            ModelState.Remove(nameof(ClientProfile.User));
            ModelState.Remove(nameof(ClientProfile.CreatedByUser));
            ModelState.Remove(nameof(ClientProfile.UpdatedByUser));

            // Normalize incoming values before business-rule validation.
            NormalizeClientProfile(clientProfile);
            await ApplyClientProfileValidationAsync(clientProfile);

            // Return the form with dropdowns restored when validation fails.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create ClientProfile failed validation for UserId {UserId}", clientProfile.UserId);
                await PopulateDropdowns(clientProfile.UserId);
                return View(clientProfile);
            }

            // Set audit fields for the new client profile record.
            var now = DateTime.UtcNow;
            clientProfile.CreatedAt = now;
            clientProfile.UpdatedAt = now;
            clientProfile.CreatedByUserId = null;
            clientProfile.UpdatedByUserId = null;

            // Queue the new client profile for insert.
            _context.Add(clientProfile);

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("ClientProfile created successfully for UserId {UserId}", clientProfile.UserId);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error creating ClientProfile for UserId {UserId}", clientProfile.UserId);

                ModelState.AddModelError("", "Unable to save client profile.");
                await PopulateDropdowns(clientProfile.UserId);
                return View(clientProfile);
            }
        }

        // GET: ClientProfiles/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted client profile.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            // Reject requests with no id.
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null UserId");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for ClientProfile UserId {UserId}", id);

            // Retrieve the requested active client profile for editing.
            var clientProfile = await _context.ClientProfiles
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == id && c.DeletedAt == null);

            // Return not found when the client profile does not exist.
            if (clientProfile == null)
            {
                _logger.LogWarning("ClientProfile UserId {UserId} not found for edit", id);
                return NotFound();
            }

            return View(clientProfile);
        }

        // POST: ClientProfiles/Edit/5
        /// <summary>
        /// Updates an existing client profile after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("UserId,EmploymentStatus,IsUnhoused")] ClientProfile formModel)
        {
            _logger.LogInformation("Attempting to edit ClientProfile UserId {UserId}", id);

            // Ensure the route id matches the posted model user id.
            if (id != formModel.UserId)
            {
                _logger.LogWarning("Edit mismatch: route UserId {RouteId} vs model UserId {ModelId}", id, formModel.UserId);
                return NotFound();
            }

            // Remove navigation properties that are not posted by the form.
            ModelState.Remove(nameof(ClientProfile.User));
            ModelState.Remove(nameof(ClientProfile.CreatedByUser));
            ModelState.Remove(nameof(ClientProfile.UpdatedByUser));

            // Normalize incoming values before business-rule validation.
            NormalizeClientProfile(formModel);
            await ApplyClientProfileValidationAsync(formModel, formModel.UserId);

            // Return the form when validation fails.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit ClientProfile failed validation for UserId {UserId}", id);
                return View(formModel);
            }

            // Retrieve the existing active client profile record.
            var existing = await _context.ClientProfiles
                .FirstOrDefaultAsync(c => c.UserId == id && c.DeletedAt == null);

            // Return not found when the target record no longer exists.
            if (existing == null)
            {
                _logger.LogWarning("ClientProfile UserId {UserId} not found during edit save", id);
                return NotFound();
            }

            // Copy validated form values into the tracked entity.
            existing.EmploymentStatus = formModel.EmploymentStatus;
            existing.IsUnhoused = formModel.IsUnhoused;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("ClientProfile UserId {UserId} updated successfully", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Check whether the record was deleted during the edit attempt.
                if (!await ClientProfileExists(formModel.UserId))
                {
                    _logger.LogWarning("ClientProfile UserId {UserId} no longer exists during concurrency check", id);
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating ClientProfile UserId {UserId}", id);

                ModelState.AddModelError("", "Unable to save changes.");
                return View(formModel);
            }
        }

        // GET: ClientProfiles/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted client profile.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            // Reject requests with no id.
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null UserId");
                return NotFound();
            }

            _logger.LogWarning("Loading Delete confirmation for ClientProfile UserId {UserId}", id);

            // Retrieve the requested active client profile with related user data.
            var clientProfile = await _context.ClientProfiles
                .Where(c => c.DeletedAt == null)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.UserId == id);

            // Return not found when the client profile does not exist.
            if (clientProfile == null)
            {
                _logger.LogWarning("ClientProfile UserId {UserId} not found for delete", id);
                return NotFound();
            }

            return View(clientProfile);
        }

        // POST: ClientProfiles/Delete/5
        /// <summary>
        /// Soft deletes a client profile.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting ClientProfile UserId {UserId}", id);

            // Retrieve the active client profile targeted for soft delete.
            var clientProfile = await _context.ClientProfiles
                .FirstOrDefaultAsync(c => c.UserId == id && c.DeletedAt == null);

            // Return not found when the client profile does not exist.
            if (clientProfile == null)
            {
                _logger.LogWarning("ClientProfile UserId {UserId} not found during delete", id);
                return NotFound();
            }

            // Apply soft-delete and audit values.
            clientProfile.DeletedAt = DateTime.UtcNow;
            clientProfile.UpdatedAt = DateTime.UtcNow;
            clientProfile.UpdatedByUserId = null;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("ClientProfile UserId {UserId} soft deleted", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error soft deleting ClientProfile UserId {UserId}", id);

                TempData["ErrorMessage"] = "Unable to delete client profile.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Populates the user dropdown for the create form.
        /// </summary>
        private async Task PopulateDropdowns(ulong? selectedUserId = null)
        {
            _logger.LogDebug("Populating dropdown for ClientProfiles");

            // Retrieve user ids that already have active client profiles.
            var usersWithProfiles = await _context.ClientProfiles
                .Where(cp => cp.DeletedAt == null)
                .Select(cp => cp.UserId)
                .ToListAsync();

            // Retrieve active client users that do not already have profiles.
            var users = await _context.DomainUsers
                .Where(u =>
                    u.DeletedAt == null &&
                    u.IsActive &&
                    u.UserType == UserType.Client &&
                    !usersWithProfiles.Contains(u.Id))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new
                {
                    u.Id,
                    DisplayName = $"{u.LastName}, {u.FirstName} ({u.Email})"
                })
                .ToListAsync();

            // Store the user dropdown options in ViewData.
            ViewData["UserId"] = new SelectList(users, "Id", "DisplayName", selectedUserId);
        }

        /// <summary>
        /// Returns true if the non-deleted client profile exists.
        /// </summary>
        private async Task<bool> ClientProfileExists(ulong id)
        {
            // Check whether the requested active client profile still exists.
            return await _context.ClientProfiles
                .AnyAsync(e => e.UserId == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Trims strings and converts blank optional values to null.
        /// </summary>
        private static void NormalizeClientProfile(ClientProfile model)
        {
            // Normalize optional string values before validation and save.
            model.EmploymentStatus = NullIfWhiteSpace(model.EmploymentStatus);
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyClientProfileValidationAsync(ClientProfile model, ulong? currentUserId = null)
        {
            // Validate that the selected user exists, is active, and is a client.
            var validUser = await _context.DomainUsers
                .AnyAsync(u =>
                    u.Id == model.UserId &&
                    u.DeletedAt == null &&
                    u.IsActive &&
                    u.UserType == UserType.Client);

            if (!validUser)
            {
                ModelState.AddModelError(nameof(ClientProfile.UserId), "Select a valid active client.");
            }

            // Prevent duplicate active client profiles for the same user.
            var duplicateProfileExists = await _context.ClientProfiles
                .AnyAsync(cp =>
                    cp.DeletedAt == null &&
                    cp.UserId == model.UserId &&
                    cp.UserId != currentUserId);

            if (duplicateProfileExists)
            {
                ModelState.AddModelError(nameof(ClientProfile.UserId), "A client profile already exists for the selected user.");
            }

            // Validate employment status content when provided.
            if (!string.IsNullOrWhiteSpace(model.EmploymentStatus) && !ContainsLetterOrDigit(model.EmploymentStatus))
            {
                ModelState.AddModelError(nameof(ClientProfile.EmploymentStatus), "Employment Status must contain letters or numbers.");
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
    }
}