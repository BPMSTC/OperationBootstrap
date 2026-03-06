using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// ClientProfilesController
    /// ------------------------
    /// This controller manages DIO operations for ClientProfile records.
    ///
    /// In this system, a ClientProfile appears to be keyed by UserId (not its own Id),
    /// which means:
    /// - The "primary identifier" passed around in routes is the UserId.
    /// - Each user should have 0..1 ClientProfile.
    ///
    /// Key behaviors in this controller:
    /// - Uses Entity Framework Core (ApplicationDbContext) to query and persist data.
    /// - Uses dependency-injected ILogger for structured logs at important points (list/load/save/errors).
    /// - Uses SOFT DELETE semantics: deletes set DeletedAt (and update audit fields) instead of removing rows.
    /// - Filters out soft-deleted records in most queries (DeletedAt == null).
    /// - Uses dropdown population to choose a UserId for Create scenarios.
    ///
    /// Notes on audit fields:
    /// - CreatedAt / UpdatedAt are set using DateTime.UtcNow for consistent server-side timestamps.
    /// - CreatedByUserId / UpdatedByUserId are set to null until auth/user tracking is implemented.
    /// </summary>
    public class ClientProfilesController : Controller
    {
        /// <summary>
        /// EF Core DbContext used to read/write ClientProfiles and related entities.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Structured logger for this controller.
        /// Output is controlled by Program.cs logging providers (Console/Debug/EventLog/etc.).
        /// </summary>
        private readonly ILogger<ClientProfilesController> _logger;

        /// <summary>
        /// Constructor with dependency injection.
        /// - ApplicationDbContext is registered in Program.cs and injected by the framework.
        /// - ILogger is injected and configured via the ASP.NET Core logging pipeline.
        /// </summary>
        public ClientProfilesController(ApplicationDbContext context, ILogger<ClientProfilesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: ClientProfiles
        /// <summary>
        /// Displays a list of client profiles (non-deleted only).
        ///
        /// Query behavior:
        /// - Filters out soft-deleted profiles (DeletedAt == null).
        /// - Eager-loads User so the view can display user fields without extra queries.
        /// - Sorts by user's last name, then first name for a stable, user-friendly ordering.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Fetching client profiles list");

            // Fetch profiles and their related User entity for display purposes.
            var clientProfiles = await _context.ClientProfiles
                .Where(c => c.DeletedAt == null)
                .Include(c => c.User)
                .OrderBy(c => c.User.LastName)
                .ThenBy(c => c.User.FirstName)
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} client profiles", clientProfiles.Count);

            // Pass the list to Views/ClientProfiles/Index.cshtml
            return View(clientProfiles);
        }

        // GET: ClientProfiles/Details/5
        /// <summary>
        /// Displays details for a single ClientProfile (identified by UserId).
        ///
        /// Parameters:
        /// - id: the UserId associated with the profile (nullable because the route may omit it)
        ///
        /// Behavior:
        /// - Returns NotFound if id is null or the profile cannot be found.
        /// - Filters out soft-deleted profiles.
        /// - Eager-loads User for display.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            // A null id indicates the route did not provide the identifier we need.
            if (id == null)
            {
                _logger.LogWarning("ClientProfile Details requested with null id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for ClientProfile UserId {Id}", id);

            // Find the profile for the given UserId, ensuring it's not soft-deleted.
            var clientProfile = await _context.ClientProfiles
                .Where(c => c.DeletedAt == null)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.UserId == id);

            // If not found, return 404.
            if (clientProfile == null)
            {
                _logger.LogWarning("ClientProfile UserId {Id} not found", id);
                return NotFound();
            }

            // Render the Details view with the entity.
            return View(clientProfile);
        }

        // GET: ClientProfiles/Create
        /// <summary>
        /// Shows the Create form for a ClientProfile.
        ///
        /// Important:
        /// - A ClientProfile is tied to a UserId, so the Create view needs a User dropdown.
        /// - PopulateDropdowns prepares ViewData["UserId"] with a SelectList.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create ClientProfile page");

            // Populate the selectable user list (users eligible for profiles).
            await PopulateDropdowns();

            return View();
        }

        // POST: ClientProfiles/Create
        /// <summary>
        /// Processes form submission to create a new ClientProfile.
        ///
        /// Security:
        /// - [ValidateAntiForgeryToken] provides CSRF protection.
        ///
        /// Binding:
        /// - [Bind(...)] limits what can be bound from the form (prevents over-posting).
        ///
        /// Validation details:
        /// - Navigation properties (User, CreatedByUser, UpdatedByUser) are removed from ModelState
        ///   because they are not posted by the form.
        ///
        /// Audit:
        /// - Sets CreatedAt/UpdatedAt timestamps and placeholder CreatedBy/UpdatedBy user IDs.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,EmploymentStatus,EarnedIncomeMonthly,IsUnhoused")] ClientProfile clientProfile)
        {
            _logger.LogInformation("Attempting to create ClientProfile for UserId {UserId}", clientProfile.UserId);

            // These are navigation properties (complex object references) and are not posted by HTML forms.
            // Leaving them in ModelState can produce validation errors even though the entity is valid.
            ModelState.Remove(nameof(ClientProfile.User));
            ModelState.Remove(nameof(ClientProfile.CreatedByUser));
            ModelState.Remove(nameof(ClientProfile.UpdatedByUser));

            // If validation failed, reload the dropdown (so the view can render properly)
            // and return the view with the user's entered values + validation messages.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create ClientProfile failed validation for UserId {UserId}", clientProfile.UserId);
                await PopulateDropdowns(clientProfile.UserId);
                return View(clientProfile);
            }

            // Set audit fields.
            var now = DateTime.UtcNow;
            clientProfile.CreatedAt = now;
            clientProfile.UpdatedAt = now;

            // Placeholder until Identity/auth integration is used to set the current user's ID.
            clientProfile.CreatedByUserId = null;
            clientProfile.UpdatedByUserId = null;

            // Stage the insert in EF's change tracker.
            _context.Add(clientProfile);

            try
            {
                // Persist the new record.
                await _context.SaveChangesAsync();

                _logger.LogInformation("ClientProfile created successfully for UserId {UserId}", clientProfile.UserId);

                // Redirect to Index after successful create.
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // DbUpdateException typically indicates DB-level constraints or update failures
                // (e.g., duplicate profile for the same user, if constrained, or FK issues).
                _logger.LogError(ex, "Error creating ClientProfile for UserId {UserId}", clientProfile.UserId);

                // Friendly message for the UI (avoid exposing internal DB errors).
                ModelState.AddModelError("", "Unable to save client profile.");

                // Repopulate dropdowns so the Create view can render correctly on error.
                await PopulateDropdowns(clientProfile.UserId);

                return View(clientProfile);
            }
        }

        // GET: ClientProfiles/Edit/5
        /// <summary>
        /// Shows the Edit form for an existing ClientProfile (identified by UserId).
        ///
        /// Behavior:
        /// - Returns 404 if id is null or the record isn't found (or has been soft-deleted).
        /// - Eager-loads User for display in the view if needed.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            // If route did not supply a user id, we cannot edit a profile.
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null UserId");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for ClientProfile UserId {UserId}", id);

            // Load the profile for this user (only if not soft-deleted).
            var clientProfile = await _context.ClientProfiles
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == id && c.DeletedAt == null);

            if (clientProfile == null)
            {
                _logger.LogWarning("ClientProfile UserId {UserId} not found for edit", id);
                return NotFound();
            }

            // Render the Edit view with the existing entity.
            return View(clientProfile);
        }

        // POST: ClientProfiles/Edit/5
        /// <summary>
        /// Processes form submission to update an existing ClientProfile.
        ///
        /// Parameters:
        /// - id: route UserId
        /// - formModel: posted model (limited by [Bind] to editable properties)
        ///
        /// Safety checks:
        /// - Confirms route id matches posted model UserId.
        /// - Removes navigation properties from ModelState because they are not posted by the form.
        ///
        /// Update strategy:
        /// - Loads the existing entity from DB (ensures it's not soft-deleted)
        /// - Updates only editable fields
        /// - Updates audit fields (UpdatedAt/UpdatedByUserId)
        ///
        /// Error handling:
        /// - DbUpdateConcurrencyException: record changed/deleted between load and save.
        /// - DbUpdateException: general DB update failures.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("UserId,EmploymentStatus,EarnedIncomeMonthly,IsUnhoused")] ClientProfile formModel)
        {
            _logger.LogInformation("Attempting to edit ClientProfile UserId {UserId}", id);

            // Ensure the route identifier matches the model identifier.
            // This prevents mismatched or tampered posts.
            if (id != formModel.UserId)
            {
                _logger.LogWarning("Edit mismatch: route UserId {RouteId} vs model UserId {ModelId}", id, formModel.UserId);
                return NotFound();
            }

            // Remove navigation props from model validation.
            ModelState.Remove(nameof(ClientProfile.User));
            ModelState.Remove(nameof(ClientProfile.CreatedByUser));
            ModelState.Remove(nameof(ClientProfile.UpdatedByUser));

            // If the model is invalid, re-render the view so the user can correct inputs.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit ClientProfile failed validation for UserId {UserId}", id);
                return View(formModel);
            }

            // Load the existing record from DB to apply updates safely.
            var existing = await _context.ClientProfiles
                .FirstOrDefaultAsync(c => c.UserId == id && c.DeletedAt == null);

            if (existing == null)
            {
                _logger.LogWarning("ClientProfile UserId {UserId} not found during edit save", id);
                return NotFound();
            }

            // Apply the editable fields from the posted form model.
            existing.EmploymentStatus = formModel.EmploymentStatus;
            existing.EarnedIncomeMonthly = formModel.EarnedIncomeMonthly;
            existing.IsUnhoused = formModel.IsUnhoused;

            // Update audit fields.
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null;

            try
            {
                // Persist changes.
                await _context.SaveChangesAsync();

                _logger.LogInformation("ClientProfile UserId {UserId} updated successfully", id);

                // Redirect to Index after successful edit.
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Concurrency exceptions occur when the underlying record has been changed or deleted
                // since it was loaded. Here you check existence and rethrow if still present.
                if (!await ClientProfileExists(formModel.UserId))
                {
                    _logger.LogWarning("ClientProfile UserId {UserId} no longer exists during concurrency check", id);
                    return NotFound();
                }

                // If the record still exists, rethrow to let middleware handle it
                // (or to bubble to a global exception handler).
                throw;
            }
            catch (DbUpdateException ex)
            {
                // General DB update error (constraints, connection issues, etc.).
                _logger.LogError(ex, "Error updating ClientProfile UserId {UserId}", id);

                // Friendly message to the UI.
                ModelState.AddModelError("", "Unable to save changes.");

                // Return the view with user inputs intact so they can retry.
                return View(formModel);
            }
        }

        // GET: ClientProfiles/Delete/5
        /// <summary>
        /// Shows the Delete confirmation page for a ClientProfile.
        ///
        /// Notes:
        /// - This GET action does not delete anything.
        /// - It loads the record (and User) so the view can display confirmation context.
        /// - Actual soft delete occurs in DeleteConfirmed.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null UserId");
                return NotFound();
            }

            _logger.LogWarning("Loading Delete confirmation for ClientProfile UserId {UserId}", id);

            // Load the record for confirmation display (soft-deleted records excluded).
            var clientProfile = await _context.ClientProfiles
                .Where(c => c.DeletedAt == null)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (clientProfile == null)
            {
                _logger.LogWarning("ClientProfile UserId {UserId} not found for delete", id);
                return NotFound();
            }

            return View(clientProfile);
        }

        // POST: ClientProfiles/Delete/5
        /// <summary>
        /// Executes the delete operation (soft delete) for a ClientProfile.
        ///
        /// Soft delete strategy:
        /// - Sets DeletedAt to UTC now
        /// - Updates UpdatedAt and UpdatedByUserId
        /// - Keeps the record in the database for history/audit and referential integrity
        ///
        /// Error handling:
        /// - If SaveChanges fails, store a TempData error message and redirect back to confirmation page.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting ClientProfile UserId {UserId}", id);

            // Load the record we intend to soft delete.
            var clientProfile = await _context.ClientProfiles
                .FirstOrDefaultAsync(c => c.UserId == id && c.DeletedAt == null);

            if (clientProfile == null)
            {
                _logger.LogWarning("ClientProfile UserId {UserId} not found during delete", id);
                return NotFound();
            }

            // Apply soft delete and audit updates.
            clientProfile.DeletedAt = DateTime.UtcNow;
            clientProfile.UpdatedAt = DateTime.UtcNow;
            clientProfile.UpdatedByUserId = null;

            try
            {
                // Persist the soft delete.
                await _context.SaveChangesAsync();

                _logger.LogInformation("ClientProfile UserId {UserId} soft deleted", id);
            }
            catch (DbUpdateException ex)
            {
                // If delete fails, log the error and return the user to the confirmation page.
                _logger.LogError(ex, "Error soft deleting ClientProfile UserId {UserId}", id);

                // TempData survives one redirect, so the Delete view can show the message.
                TempData["ErrorMessage"] = "Unable to delete client profile.";

                return RedirectToAction(nameof(Delete), new { id });
            }

            // After successful soft delete, return to list.
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Builds the dropdown list of eligible users for creating a ClientProfile.
        ///
        /// Key decisions:
        /// - Uses DomainUsers (not Identity users) which likely represent your application's "business" users table.
        /// - Filters out deleted and inactive users.
        /// - Orders by last/first name for a user-friendly dropdown.
        /// - Projects into an anonymous type with an Id + DisplayName so the dropdown shows a readable label.
        ///
        /// ViewData key:
        /// - ViewData["UserId"] is used by the Create view's UserId dropdown.
        /// </summary>
        private async Task PopulateDropdowns(ulong? selectedUserId = null)
        {
            _logger.LogDebug("Populating dropdown for ClientProfiles");

            // Load eligible users.
            // IMPORTANT: The query returns an anonymous object (Id + DisplayName),
            // so the SelectList uses "Id" as the value and "DisplayName" as the displayed text.
            var users = await _context.DomainUsers
                .Where(u => u.DeletedAt == null && u.IsActive)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new
                {
                    u.Id,
                    DisplayName = $"{u.LastName}, {u.FirstName} ({u.Email})"
                })
                .ToListAsync();

            // Store the SelectList in ViewData so Razor can build a <select> element.
            ViewData["UserId"] = new SelectList(users, "Id", "DisplayName", selectedUserId);
        }

        /// <summary>
        /// Helper method to check whether a non-deleted ClientProfile exists for a given UserId.
        ///
        /// Used by:
        /// - The concurrency exception handling in Edit (POST).
        /// </summary>
        private async Task<bool> ClientProfileExists(ulong id)
        {
            return await _context.ClientProfiles
                .AnyAsync(e => e.UserId == id && e.DeletedAt == null);
        }
    }
}