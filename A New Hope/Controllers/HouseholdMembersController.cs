using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// HouseholdMembersController
    /// --------------------------
    /// This controller manages DIO operations for HouseholdMember records.
    ///
    /// Conceptually, HouseholdMember records represent additional people tied to a "client" (ClientUser).
    /// Each HouseholdMember has:
    /// - A required foreign key to a domain user acting as the "client" (ClientUserId)
    /// - Basic identity fields (FirstName/LastName)
    /// - Optional date-related fields (DateOfBirth, AgeAsOfDate)
    ///
    /// Key behaviors implemented here:
    /// - Uses Entity Framework Core (ApplicationDbContext) for database operations.
    /// - Uses ILogger for structured request tracing and error diagnostics.
    /// - Applies SOFT DELETE semantics by setting DeletedAt (instead of removing rows).
    /// - Filters out soft-deleted household members in all read/edit/delete flows.
    /// - Populates a dropdown of eligible ClientUser options for Create/Edit forms.
    ///
    /// Notes on audit fields:
    /// - CreatedAt/UpdatedAt are set using UTC timestamps.
    /// - CreatedByUserId/UpdatedByUserId are currently set to null until auth/user tracking is implemented.
    /// </summary>
    public class HouseholdMembersController : Controller
    {
        /// <summary>
        /// EF Core DbContext for reading/writing HouseholdMembers and related entities.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Logger for this controller. Output destination depends on Program.cs logging configuration.
        /// </summary>
        private readonly ILogger<HouseholdMembersController> _logger;

        /// <summary>
        /// Constructor with dependency injection.
        /// </summary>
        public HouseholdMembersController(ApplicationDbContext context, ILogger<HouseholdMembersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: HouseholdMembers
        /// <summary>
        /// Displays a list of household members (non-deleted only).
        ///
        /// Query behavior:
        /// - Filters out soft-deleted rows (DeletedAt == null).
        /// - Eager-loads ClientUser (domain user) so the view can show client details without extra queries.
        /// - Orders by LastName then FirstName for readable alphabetical list display.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading HouseholdMembers Index page");

            // Query and materialize a list of active (non-deleted) household members.
            var householdMembers = await _context.HouseholdMembers
                .Where(h => h.DeletedAt == null)
                .Include(h => h.ClientUser)
                .OrderBy(h => h.LastName)
                .ThenBy(h => h.FirstName)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} household members", householdMembers.Count);

            // Render Views/HouseholdMembers/Index.cshtml with the list.
            return View(householdMembers);
        }

        // GET: HouseholdMembers/Details/5
        /// <summary>
        /// Displays detail information for a single HouseholdMember by its primary key Id.
        ///
        /// Behavior:
        /// - Returns 404 if id is null or record is not found (or soft-deleted).
        /// - Eager-loads ClientUser for display context.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            // Route is missing the identifier.
            if (id == null)
            {
                _logger.LogWarning("Details requested with null id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for HouseholdMember Id {Id}", id);

            // Load the household member if it exists and is not soft deleted.
            var householdMember = await _context.HouseholdMembers
                .Where(h => h.DeletedAt == null)
                .Include(h => h.ClientUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            // If not found, return 404.
            if (householdMember == null)
            {
                _logger.LogWarning("HouseholdMember Id {Id} not found", id);
                return NotFound();
            }

            // Render Views/HouseholdMembers/Details.cshtml
            return View(householdMember);
        }

        // GET: HouseholdMembers/Create
        /// <summary>
        /// Shows the Create form for a new HouseholdMember.
        ///
        /// Important:
        /// - HouseholdMember requires ClientUserId, so we populate a ClientUser dropdown list.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create HouseholdMember page");

            // Preload dropdown options so the view can show a user-friendly <select>.
            await PopulateDropdowns();

            return View();
        }

        // POST: HouseholdMembers/Create
        /// <summary>
        /// Processes form submission to create a new HouseholdMember.
        ///
        /// Security:
        /// - [ValidateAntiForgeryToken] provides CSRF protection.
        ///
        /// Binding:
        /// - [Bind(...)] limits which fields are accepted from the form (prevents over-posting).
        ///
        /// Validation:
        /// - Removes navigation properties from ModelState because forms do not post them.
        /// - If invalid, repopulates dropdowns and re-renders the Create view.
        ///
        /// Audit:
        /// - Sets CreatedAt/UpdatedAt and placeholder CreatedBy/UpdatedBy user IDs.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientUserId,FirstName,LastName,DateOfBirth,AgeAsOfDate")] HouseholdMember householdMember)
        {
            _logger.LogInformation("Attempting to create HouseholdMember for ClientUserId {ClientUserId}", householdMember.ClientUserId);

            // Navigation properties are not posted back by standard HTML forms.
            // Removing them prevents false validation failures.
            ModelState.Remove(nameof(HouseholdMember.ClientUser));
            ModelState.Remove(nameof(HouseholdMember.CreatedByUser));
            ModelState.Remove(nameof(HouseholdMember.UpdatedByUser));

            // If validation fails, rebuild dropdown list and return the view with validation messages.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create HouseholdMember failed validation for ClientUserId {ClientUserId}", householdMember.ClientUserId);
                await PopulateDropdowns(householdMember.ClientUserId);
                return View(householdMember);
            }

            // Set audit metadata.
            var now = DateTime.UtcNow;
            householdMember.CreatedAt = now;
            householdMember.UpdatedAt = now;
            householdMember.CreatedByUserId = null; // Placeholder until auth integration.
            householdMember.UpdatedByUserId = null; // Placeholder until auth integration.

            // Stage the new record for insertion.
            _context.Add(householdMember);

            try
            {
                // Persist insert to database.
                await _context.SaveChangesAsync();

                _logger.LogInformation("HouseholdMember created successfully for ClientUserId {ClientUserId}", householdMember.ClientUserId);

                // Return to list view after successful create.
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // Commonly indicates constraint/FK issues or other DB update failures.
                _logger.LogError(ex, "Error creating HouseholdMember for ClientUserId {ClientUserId}", householdMember.ClientUserId);

                // User-facing message (avoid showing raw DB exception details).
                ModelState.AddModelError("", "Unable to save household member.");

                // Repopulate dropdown so the view can render correctly.
                await PopulateDropdowns(householdMember.ClientUserId);

                return View(householdMember);
            }
        }

        // GET: HouseholdMembers/Edit/5
        /// <summary>
        /// Shows the Edit form for an existing HouseholdMember.
        ///
        /// Behavior:
        /// - Returns 404 if id is null or record is not found (or soft-deleted).
        /// - Populates dropdowns with the current ClientUser selection for UI convenience.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for HouseholdMember Id {Id}", id);

            // Load the existing household member record (only if not soft deleted).
            var householdMember = await _context.HouseholdMembers
                .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);

            if (householdMember == null)
            {
                _logger.LogWarning("HouseholdMember Id {Id} not found for edit", id);
                return NotFound();
            }

            // Populate ClientUser dropdown so the edit form can show current selection.
            await PopulateDropdowns(householdMember.ClientUserId);

            return View(householdMember);
        }

        // POST: HouseholdMembers/Edit/5
        /// <summary>
        /// Processes form submission to update an existing HouseholdMember.
        ///
        /// Parameters:
        /// - id: route Id
        /// - formModel: posted model (limited by [Bind] to editable properties)
        ///
        /// Safety checks:
        /// - Confirms the route Id matches the posted model Id.
        /// - Removes navigation properties from ModelState (not posted by forms).
        ///
        /// Update strategy:
        /// - Loads the existing entity (ensures not soft-deleted).
        /// - Copies editable fields from formModel to existing.
        /// - Updates audit metadata (UpdatedAt/UpdatedByUserId).
        ///
        /// Error handling:
        /// - DbUpdateConcurrencyException: record changed/deleted since it was loaded.
        /// - DbUpdateException: general DB update failures.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,ClientUserId,FirstName,LastName,DateOfBirth,AgeAsOfDate")] HouseholdMember formModel)
        {
            _logger.LogInformation("Attempting to edit HouseholdMember Id {Id}", id);

            // Ensure route identifier matches the model identifier to prevent mismatched/tampered posts.
            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            // Remove navigation properties from ModelState because they are not posted by the form.
            ModelState.Remove(nameof(HouseholdMember.ClientUser));
            ModelState.Remove(nameof(HouseholdMember.CreatedByUser));
            ModelState.Remove(nameof(HouseholdMember.UpdatedByUser));

            // If validation fails, rebuild dropdown and return the view so the user can fix inputs.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit HouseholdMember failed validation for Id {Id}", id);
                await PopulateDropdowns(formModel.ClientUserId);
                return View(formModel);
            }

            // Load the existing entity from DB so we can apply changes safely.
            var existing = await _context.HouseholdMembers
                .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);

            if (existing == null)
            {
                _logger.LogWarning("HouseholdMember Id {Id} not found during edit save", id);
                return NotFound();
            }

            // Copy editable fields from the posted model to the tracked entity.
            existing.ClientUserId = formModel.ClientUserId;
            existing.FirstName = formModel.FirstName;
            existing.LastName = formModel.LastName;
            existing.DateOfBirth = formModel.DateOfBirth;
            existing.AgeAsOfDate = formModel.AgeAsOfDate;

            // Update audit metadata.
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // Placeholder until auth integration.

            try
            {
                // Persist updates.
                await _context.SaveChangesAsync();

                _logger.LogInformation("HouseholdMember Id {Id} updated successfully", id);

                // Redirect to list after successful edit.
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Concurrency exceptions indicate the underlying row has changed or disappeared.
                // The pattern here checks existence and rethrows if it still exists.
                if (!await HouseholdMemberExists(formModel.Id))
                {
                    _logger.LogWarning("HouseholdMember Id {Id} no longer exists during concurrency check", id);
                    return NotFound();
                }

                // If it still exists, rethrow to bubble up to global exception handling.
                throw;
            }
            catch (DbUpdateException ex)
            {
                // General DB update error (constraints, FK issues, etc.).
                _logger.LogError(ex, "Error updating HouseholdMember Id {Id}", id);

                // Friendly UI message.
                ModelState.AddModelError("", "Unable to save changes.");

                // Rebuild dropdown so the view can render properly after an error.
                await PopulateDropdowns(formModel.ClientUserId);

                return View(formModel);
            }
        }

        // GET: HouseholdMembers/Delete/5
        /// <summary>
        /// Shows Delete confirmation for a HouseholdMember.
        ///
        /// Notes:
        /// - This GET does not delete data. It loads the entity for user confirmation.
        /// - Soft delete is executed in DeleteConfirmed.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogWarning("Loading Delete confirmation for HouseholdMember Id {Id}", id);

            // Load the record for confirmation view (excluding soft-deleted records).
            var householdMember = await _context.HouseholdMembers
                .Where(h => h.DeletedAt == null)
                .Include(h => h.ClientUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (householdMember == null)
            {
                _logger.LogWarning("HouseholdMember Id {Id} not found for delete", id);
                return NotFound();
            }

            return View(householdMember);
        }

        // POST: HouseholdMembers/Delete/5
        /// <summary>
        /// Executes the delete operation (soft delete) for a HouseholdMember.
        ///
        /// Soft delete strategy:
        /// - Sets DeletedAt (marks record as deleted)
        /// - Updates UpdatedAt and UpdatedByUserId
        /// - Does not physically remove the row
        ///
        /// Error handling:
        /// - If SaveChanges fails, store an error in TempData and redirect back to the confirmation page.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting HouseholdMember Id {Id}", id);

            // Load the entity to be soft deleted (must be non-deleted).
            var householdMember = await _context.HouseholdMembers
                .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);

            if (householdMember == null)
            {
                _logger.LogWarning("HouseholdMember Id {Id} not found during delete", id);
                return NotFound();
            }

            // Apply soft delete and audit fields.
            householdMember.DeletedAt = DateTime.UtcNow;
            householdMember.UpdatedAt = DateTime.UtcNow;
            householdMember.UpdatedByUserId = null; // Placeholder until auth integration.

            try
            {
                // Persist the soft delete.
                await _context.SaveChangesAsync();

                _logger.LogInformation("HouseholdMember Id {Id} soft deleted", id);
            }
            catch (DbUpdateException ex)
            {
                // If delete fails, log and return to the confirmation screen with an error message.
                _logger.LogError(ex, "Error soft deleting HouseholdMember Id {Id}", id);

                // TempData persists for one redirect so the Delete view can show the message.
                TempData["ErrorMessage"] = "Unable to delete household member.";

                return RedirectToAction(nameof(Delete), new { id });
            }

            // After successful soft delete, return to list.
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Populates the ClientUser dropdown list for Create/Edit views.
        ///
        /// Data source:
        /// - _context.DomainUsers (your application's "domain users" table)
        ///
        /// Filtering:
        /// - Excludes deleted users (DeletedAt == null)
        /// - Includes only active users (IsActive == true)
        ///
        /// Output:
        /// - ViewData["ClientUserId"] is a SelectList with:
        ///     - value: Id
        ///     - display: "LastName, FirstName (Email)"
        /// </summary>
        private async Task PopulateDropdowns(ulong? selectedClientUserId = null)
        {
            _logger.LogDebug("Populating ClientUser dropdown for HouseholdMember");

            // Query eligible users for selection.
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

            // Store SelectList in ViewData for Razor to build a <select> element.
            ViewData["ClientUserId"] = new SelectList(users, "Id", "DisplayName", selectedClientUserId);
        }

        /// <summary>
        /// Helper method used by the Edit POST concurrency handler.
        /// Confirms whether a non-deleted HouseholdMember still exists.
        /// </summary>
        private async Task<bool> HouseholdMemberExists(ulong id)
        {
            return await _context.HouseholdMembers
                .AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}