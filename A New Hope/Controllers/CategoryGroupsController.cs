using System;
using System.Linq;
using System.Threading.Tasks;
using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// CategoryGroupsController
    /// ------------------------
    /// This controller provides DIO-style endpoints for managing CategoryGroup records.
    ///
    /// Key behaviors implemented here:
    /// - Uses Entity Framework Core (ApplicationDbContext) to query and persist CategoryGroup data.
    /// - Uses dependency-injected ILogger to emit structured logs for traceability (debugging, auditing, support).
    /// - Uses "soft delete" semantics: DeleteConfirmed sets DeletedAt rather than physically removing rows.
    /// - Uses standard ASP.NET MVC patterns:
    ///     - GET actions render views
    ///     - POST actions process form input
    ///     - [ValidateAntiForgeryToken] protects against CSRF attacks
    ///     - ModelState validation determines whether to proceed or re-render the form
    ///
    /// Notes on audit fields:
    /// - CreatedAt / UpdatedAt are set using UTC timestamps for consistency.
    /// - CreatedByUserId / UpdatedByUserId are currently set to null until authentication/user tracking is implemented.
    ///
    /// Important:
    /// - This file intentionally does not implement physical deletes.
    /// - Soft deletes are typically paired with a global query filter (not shown here) so "deleted" rows
    ///   are hidden in most queries. If no query filter exists, then deleted items may still appear
    ///   unless explicitly filtered out.
    /// </summary>
    public class CategoryGroupsController : Controller
    {
        /// <summary>
        /// EF Core DbContext used to access CategoryGroups table and related persistence operations.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Structured logger for this controller.
        /// Logs go to whatever providers are configured in Program.cs (Console/Debug/EventLog, etc.).
        /// </summary>
        private readonly ILogger<CategoryGroupsController> _logger;

        /// <summary>
        /// Constructor with dependency injection.
        /// - ApplicationDbContext is provided by DI container (registered in Program.cs).
        /// - ILogger is provided by ASP.NET Core's logging infrastructure.
        /// </summary>
        public CategoryGroupsController(
            ApplicationDbContext context,
            ILogger<CategoryGroupsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: CategoryGroups
        /// <summary>
        /// Displays a list of CategoryGroups.
        ///
        /// Ordering:
        /// - SortOrder first (so your manually-curated ordering is respected)
        /// - Name next (stable alphabetical ordering within the same SortOrder)
        ///
        /// Returns:
        /// - View(categoryGroups) where categoryGroups is a fully materialized List<CategoryGroup>.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Informational log to show that the listing endpoint was hit.
            _logger.LogInformation("Fetching category groups list");

            // Query all category groups, ordered for a predictable display in the Index view.
            var categoryGroups = await _context.CategoryGroups
                .OrderBy(cg => cg.SortOrder)
                .ThenBy(cg => cg.Name)
                .ToListAsync();

            // Log how many records were returned (useful in debugging and confirming seeded data exists).
            _logger.LogInformation("Fetched {Count} category groups", categoryGroups.Count);

            // Render the view with the list model (Views/CategoryGroups/Index.cshtml).
            return View(categoryGroups);
        }

        // GET: CategoryGroups/Details/5
        /// <summary>
        /// Displays details for a single CategoryGroup.
        ///
        /// Parameters:
        /// - id: CategoryGroup primary key (nullable because a route might not supply it).
        ///
        /// Behavior:
        /// - Returns 404 (NotFound) if id is missing or the record is not found.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            // If the route does not provide an id, treat it as a not-found resource request.
            if (id == null)
            {
                _logger.LogWarning("CategoryGroup Details requested with null id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for CategoryGroupId {Id}", id);

            // Load the specific CategoryGroup by id.
            // FirstOrDefaultAsync returns null if not found.
            var categoryGroup = await _context.CategoryGroups
                .FirstOrDefaultAsync(m => m.Id == id);

            // If no record exists, return 404.
            if (categoryGroup == null)
            {
                _logger.LogWarning("CategoryGroup {Id} not found", id);
                return NotFound();
            }

            // Render the Details view with the entity.
            return View(categoryGroup);
        }

        // GET: CategoryGroups/Create
        /// <summary>
        /// Shows the Create form for a new CategoryGroup.
        ///
        /// This is a simple page load action:
        /// - no DB access is required here (no dropdowns needed)
        /// - returns Views/CategoryGroups/Create.cshtml
        /// </summary>
        public IActionResult Create()
        {
            _logger.LogInformation("Loading Create CategoryGroup page");
            return View();
        }

        // POST: CategoryGroups/Create
        /// <summary>
        /// Processes form submission to create a new CategoryGroup.
        ///
        /// Security:
        /// - [ValidateAntiForgeryToken] ensures this POST cannot be replayed by a third-party site (CSRF).
        ///
        /// Binding:
        /// - [Bind("Name,SortOrder,IsActive")] limits what can be posted/updated (prevents over-posting).
        ///
        /// Validation:
        /// - ModelState.IsValid must be true before we attempt to save to the database.
        ///
        /// Audit:
        /// - CreatedAt / UpdatedAt set to UTC now.
        /// - CreatedByUserId / UpdatedByUserId set to null until auth is implemented.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,SortOrder,IsActive")] CategoryGroup categoryGroup)
        {
            _logger.LogInformation(
                "Attempting to create CategoryGroup {Name}",
                categoryGroup.Name);

            // If model validation fails (e.g., required fields missing, max length exceeded),
            // return the view with the model so Razor can show validation messages.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create CategoryGroup failed validation");
                return View(categoryGroup);
            }

            // Set audit fields using UTC timestamps (best practice for server-side time).
            var now = DateTime.UtcNow;
            categoryGroup.CreatedAt = now;
            categoryGroup.UpdatedAt = now;

            // Placeholder until you wire up Identity user IDs into these audit fields.
            categoryGroup.CreatedByUserId = null;
            categoryGroup.UpdatedByUserId = null;

            // Stage the new entity for insertion.
            _context.Add(categoryGroup);

            try
            {
                // Persist changes to the database.
                await _context.SaveChangesAsync();

                // After SaveChangesAsync, EF will populate the PK (categoryGroup.Id).
                _logger.LogInformation(
                    "CategoryGroup {Name} created successfully (Id {Id})",
                    categoryGroup.Name,
                    categoryGroup.Id);

                // Redirect back to the listing after successful create.
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // DbUpdateException typically indicates a constraint/index problem or a DB-level issue.
                // Example: unique name constraint (if implemented).
                _logger.LogError(
                    ex,
                    "Error creating CategoryGroup {Name}",
                    categoryGroup.Name);

                // Show a user-friendly message (avoid exposing raw DB error details to UI).
                ModelState.AddModelError("", "Unable to save. The category group name may already exist.");

                // Return the view so the user can correct input without losing what they entered.
                return View(categoryGroup);
            }
        }

        // GET: CategoryGroups/Edit/5
        /// <summary>
        /// Shows the Edit form for an existing CategoryGroup.
        ///
        /// Behavior:
        /// - Returns 404 if id is missing or record cannot be found.
        /// - Loads the record and returns it to Views/CategoryGroups/Edit.cshtml.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            // Missing id means we cannot identify which record to edit.
            if (id == null)
            {
                _logger.LogWarning("Edit CategoryGroup requested with null id");
                return NotFound();
            }

            // FindAsync uses PK lookup and can be slightly more efficient.
            var categoryGroup = await _context.CategoryGroups.FindAsync(id);
            if (categoryGroup == null)
            {
                _logger.LogWarning("CategoryGroup {Id} not found for edit", id);
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for CategoryGroupId {Id}", id);

            // Return the edit view with the existing data populated into the form.
            return View(categoryGroup);
        }

        // POST: CategoryGroups/Edit/5
        /// <summary>
        /// Processes form submission to update an existing CategoryGroup.
        ///
        /// Parameters:
        /// - id: route parameter for the category group being edited
        /// - formModel: posted model (limited by [Bind] to prevent over-posting)
        ///
        /// Safety checks:
        /// - Confirms route id matches posted model id (prevents mismatched tampered posts).
        ///
        /// Update strategy:
        /// - Loads existing entity from database
        /// - Updates only editable fields
        /// - Updates audit fields (UpdatedAt/UpdatedByUserId)
        ///
        /// Errors:
        /// - DbUpdateException: typically unique constraint or other DB update failure.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Name,SortOrder,IsActive")] CategoryGroup formModel)
        {
            _logger.LogInformation("Attempting to edit CategoryGroupId {Id}", id);

            // Make sure the posted model matches the route.
            if (id != formModel.Id)
            {
                _logger.LogWarning(
                    "Edit mismatch: route id {RouteId} vs model id {ModelId}",
                    id,
                    formModel.Id);

                return NotFound();
            }

            // If validation fails, re-render the view with validation messages and user input.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit CategoryGroupId {Id} failed validation", id);
                return View(formModel);
            }

            // Load the existing entity from DB so we can apply updates safely.
            var existing = await _context.CategoryGroups.FirstOrDefaultAsync(cg => cg.Id == id);
            if (existing == null)
            {
                _logger.LogWarning("CategoryGroup {Id} not found during edit save", id);
                return NotFound();
            }

            // Apply editable changes only.
            // This avoids accidentally overwriting audit fields or other properties.
            existing.Name = formModel.Name;
            existing.SortOrder = formModel.SortOrder;
            existing.IsActive = formModel.IsActive;

            // Update audit metadata.
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // Placeholder until auth is implemented.

            try
            {
                // Persist changes to the database.
                await _context.SaveChangesAsync();

                _logger.LogInformation("CategoryGroup {Id} updated successfully", id);

                // Redirect to listing after successful edit.
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // Most commonly indicates uniqueness constraint collisions or related DB update issues.
                _logger.LogError(
                    ex,
                    "Error updating CategoryGroupId {Id}",
                    id);

                // Friendly message for UI; avoids exposing internal DB exception details.
                ModelState.AddModelError("", "Unable to save changes. The category group name may already exist.");

                // Return the view so the user can correct input.
                return View(formModel);
            }
        }

        // GET: CategoryGroups/Delete/5
        /// <summary>
        /// Shows the Delete confirmation page for a CategoryGroup.
        ///
        /// Notes:
        /// - This GET action does not delete anything.
        /// - It loads the entity and displays a confirmation view.
        /// - The actual soft delete happens in DeleteConfirmed.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            // Without an id, we cannot locate a record to delete.
            if (id == null)
            {
                _logger.LogWarning("Delete CategoryGroup requested with null id");
                return NotFound();
            }

            // Load the entity for display in the confirmation view.
            var categoryGroup = await _context.CategoryGroups
                .FirstOrDefaultAsync(m => m.Id == id);

            // Return 404 if the record no longer exists (or never existed).
            if (categoryGroup == null)
            {
                _logger.LogWarning("CategoryGroup {Id} not found for delete", id);
                return NotFound();
            }

            // Warning level log because delete is a destructive action, even though it's soft delete.
            _logger.LogWarning("Loading Delete confirmation for CategoryGroupId {Id}", id);

            // Render the delete confirmation view.
            return View(categoryGroup);
        }

        // POST: CategoryGroups/Delete/5
        /// <summary>
        /// Executes the delete action (soft delete) after confirmation.
        ///
        /// Soft delete approach:
        /// - Sets DeletedAt to current UTC time
        /// - Updates UpdatedAt and UpdatedByUserId
        /// - Does NOT remove the row from the database
        ///
        /// Why soft delete:
        /// - Preserves historical references and auditability
        /// - Avoids referential integrity issues if other records point to this entity
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting CategoryGroupId {Id}", id);

            // Fetch the record we intend to soft delete.
            var categoryGroup = await _context.CategoryGroups.FirstOrDefaultAsync(cg => cg.Id == id);
            if (categoryGroup == null)
            {
                _logger.LogWarning("CategoryGroup {Id} not found during delete", id);
                return NotFound();
            }

            // Apply soft delete flags/fields.
            categoryGroup.DeletedAt = DateTime.UtcNow;
            categoryGroup.UpdatedAt = DateTime.UtcNow;
            categoryGroup.UpdatedByUserId = null; // Placeholder until auth is implemented.

            // Persist the soft delete.
            await _context.SaveChangesAsync();

            _logger.LogInformation("CategoryGroup {Id} soft deleted", id);

            // Redirect to list after deletion.
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Helper method to check whether a CategoryGroup exists by primary key.
        ///
        /// Note:
        /// - This helper is not used by the current controller logic.
        /// - It can be useful for future concurrency checks or validations.
        /// </summary>
        private async Task<bool> CategoryGroupExists(ulong id)
        {
            return await _context.CategoryGroups.AnyAsync(e => e.Id == id);
        }
    }
}