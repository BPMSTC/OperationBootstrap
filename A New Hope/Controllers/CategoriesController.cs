using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// CategoriesController
    /// --------------------
    /// This controller provides DIO-style endpoints for managing Category records.
    ///
    /// Key behaviors implemented here:
    /// - Uses Entity Framework Core (ApplicationDbContext) to query and persist data.
    /// - Uses dependency-injected ILogger to emit structured logs for traceability.
    /// - Uses "soft delete" semantics: DeleteConfirmed sets DeletedAt rather than removing rows.
    /// - Populates dropdowns (CategoryGroup + Parent Category) for Create/Edit views.
    ///
    /// Notes:
    /// - Navigation properties (CategoryGroup, Parent) are *not* posted back from forms,
    ///   so ModelState entries for those properties are removed before validation checks.
    /// - Audit fields (CreatedAt/UpdatedAt/CreatedByUserId/UpdatedByUserId) are set here.
    ///   User IDs are currently set to null until authentication/user tracking is implemented.
    /// </summary>
    public class CategoriesController : Controller
    {
        /// <summary>
        /// EF Core database context used to read/write Categories and related entities.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Structured logger for this controller.
        /// Logs appear wherever logging providers are configured (console/debug/event log, etc.).
        /// </summary>
        private readonly ILogger<CategoriesController> _logger;

        /// <summary>
        /// Constructor with dependency injection.
        /// - ApplicationDbContext is provided by DI container (registered in Program.cs).
        /// - ILogger is provided by ASP.NET Core logging infrastructure.
        /// </summary>
        public CategoriesController(
            ApplicationDbContext context,
            ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Categories
        /// <summary>
        /// Displays a list of categories.
        ///
        /// Data strategy:
        /// - Includes CategoryGroup so the view can display the group name without lazy-loading.
        /// - Includes Parent so the view can display parent name (if applicable) without extra queries.
        /// - Orders first by group, then by sort order (within group), then by name.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Informational log to show this endpoint ran and a list query is about to occur.
            _logger.LogInformation("Fetching category list");

            // Build the query and execute it asynchronously.
            // Include() ensures EF fetches the related entities in the same query.
            var categories = await _context.Categories
                .Include(c => c.CategoryGroup)
                .Include(c => c.Parent)
                .OrderBy(c => c.CategoryGroup.Name)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            // Log the resulting count for quick diagnostics in dev/test environments.
            _logger.LogInformation("Fetched {Count} categories", categories.Count);

            // Return the list to the Index view (Views/Categories/Index.cshtml).
            return View(categories);
        }

        // GET: Categories/Details/5
        /// <summary>
        /// Displays details for a single Category.
        ///
        /// Parameters:
        /// - id: the Category primary key (ulong). Nullable because the route may not provide it.
        ///
        /// Behavior:
        /// - Returns 404 (NotFound) if id is missing or record does not exist.
        /// - Includes CategoryGroup and Parent so the Details view has full context.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            // If the route didn't provide an id, treat this as a bad request for a resource.
            if (id == null)
            {
                _logger.LogWarning("Category Details requested with null id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for CategoryId {Id}", id);

            // Query for the category and eager-load related entities needed for the view.
            var category = await _context.Categories
                .Include(c => c.CategoryGroup)
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(m => m.Id == id);

            // If no record exists with that id, return 404.
            if (category == null)
            {
                _logger.LogWarning("Category {Id} not found", id);
                return NotFound();
            }

            // Render the Details view with the category model.
            return View(category);
        }

        // GET: Categories/Create
        /// <summary>
        /// Shows the Create form.
        ///
        /// Important:
        /// - Dropdown values are loaded here (CategoryGroups and Parent Category options).
        /// </summary>
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create Category page");

            // Populate ViewData dictionaries used by the Create view for dropdown fields.
            await PopulateDropdowns();

            return View();
        }

        // POST: Categories/Create
        /// <summary>
        /// Accepts form post for creating a new Category.
        ///
        /// Security:
        /// - [ValidateAntiForgeryToken] ensures the POST includes a valid antiforgery token.
        ///
        /// Binding:
        /// - [Bind(...)] restricts which properties are bound from the form, preventing over-posting.
        ///
        /// Validation notes:
        /// - Navigation properties are removed from ModelState because the form does not post them.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryGroupId,ParentId,Name,SortOrder,IsActive")] Category category)
        {
            _logger.LogInformation(
                "Attempting to create category {Name} in CategoryGroupId {GroupId}",
                category.Name,
                category.CategoryGroupId);

            // These are navigation properties (complex objects) which aren't posted by the form.
            // If they remain in ModelState, MVC can mark the model invalid due to missing values.
            ModelState.Remove(nameof(Category.CategoryGroup));
            ModelState.Remove(nameof(Category.Parent));

            // If validation failed, reload dropdowns and redisplay the form with validation messages.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create Category failed validation");
                await PopulateDropdowns(category.CategoryGroupId, category.ParentId);
                return View(category);
            }

            // Set audit fields. Use UTC to avoid timezone issues across environments.
            var now = DateTime.UtcNow;
            category.CreatedAt = now;
            category.UpdatedAt = now;

            // These are placeholders until auth/user tracking is implemented.
            category.CreatedByUserId = null;
            category.UpdatedByUserId = null;

            // Stage the insert in EF's change tracker.
            _context.Add(category);

            try
            {
                // Persist to the database.
                await _context.SaveChangesAsync();

                // After SaveChangesAsync, EF will populate the new identity/PK (category.Id).
                _logger.LogInformation("Category {Name} created successfully (Id {Id})", category.Name, category.Id);

                // Return to the list view.
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // DbUpdateException commonly happens for constraint/index violations (e.g., unique name per group).
                _logger.LogError(ex, "Error creating category {Name}", category.Name);

                // Surface a friendly message to the user (without exposing low-level DB errors).
                ModelState.AddModelError("", "Unable to save. The category name may already exist in that category group.");

                // Rebuild dropdowns so the view can render properly.
                await PopulateDropdowns(category.CategoryGroupId, category.ParentId);

                // Return the same view with the user's entered values and an error message.
                return View(category);
            }
        }

        // GET: Categories/Edit/5
        /// <summary>
        /// Shows the Edit form for a specific Category.
        ///
        /// Behavior:
        /// - Returns 404 if id is missing or record cannot be found.
        /// - Populates dropdowns, excluding the category itself from Parent options
        ///   so a category can't be selected as its own parent via UI.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null id");
                return NotFound();
            }

            // FindAsync uses the primary key and can leverage the context cache if already loaded.
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category {Id} not found for edit", id);
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for CategoryId {Id}", id);

            // Populate dropdowns with current selections and exclude the category from parent candidates.
            await PopulateDropdowns(category.CategoryGroupId, category.ParentId, excludeCategoryId: category.Id);

            return View(category);
        }

        // POST: Categories/Edit/5
        /// <summary>
        /// Accepts form post for editing an existing Category.
        ///
        /// Parameters:
        /// - id: route parameter for the category being edited
        /// - formModel: bound form data (limited by [Bind] to prevent over-posting)
        ///
        /// Validation:
        /// - Ensures route id matches model id to prevent tampering/mismatched posts.
        /// - Removes navigation properties from ModelState (not posted).
        /// - Blocks selecting itself as its own parent (basic hierarchy safety rule).
        ///
        /// Update strategy:
        /// - Loads existing entity and updates only editable fields.
        /// - Updates UpdatedAt/UpdatedByUserId audit fields.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,CategoryGroupId,ParentId,Name,SortOrder,IsActive")] Category formModel)
        {
            _logger.LogInformation("Attempting to edit CategoryId {Id}", id);

            // Ensure the route id matches the posted model id.
            // If they differ, treat it as an invalid request for the resource.
            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route id {RouteId} vs model id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            // Navigation properties aren't part of the post, so remove them from ModelState.
            ModelState.Remove(nameof(Category.CategoryGroup));
            ModelState.Remove(nameof(Category.Parent));

            // Prevent a category from referencing itself as its parent (would create an immediate cycle).
            if (formModel.ParentId == formModel.Id)
            {
                _logger.LogWarning("Category {Id} attempted to be its own parent", id);
                ModelState.AddModelError("ParentId", "A category cannot be its own parent.");
            }

            // If validation failed, repopulate dropdowns and return the form for correction.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit CategoryId {Id} failed validation", id);
                await PopulateDropdowns(formModel.CategoryGroupId, formModel.ParentId, excludeCategoryId: formModel.Id);
                return View(formModel);
            }

            // Load the existing record to apply updates.
            var existing = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (existing == null)
            {
                _logger.LogWarning("Category {Id} not found during edit save", id);
                return NotFound();
            }

            // Apply only the fields the user is allowed to edit.
            // This avoids accidental updates to properties that aren't meant to change via this form.
            existing.CategoryGroupId = formModel.CategoryGroupId;
            existing.ParentId = formModel.ParentId;
            existing.Name = formModel.Name;
            existing.SortOrder = formModel.SortOrder;
            existing.IsActive = formModel.IsActive;

            // Update audit information.
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // Placeholder until auth/user tracking is implemented.

            try
            {
                // Save the modifications to the database.
                await _context.SaveChangesAsync();

                _logger.LogInformation("Category {Id} updated successfully", id);

                // Return to the Index listing after a successful update.
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // Typically indicates a constraint violation or DB update issue.
                _logger.LogError(ex, "Error updating CategoryId {Id}", id);

                // Friendly UI message rather than raw DB exception details.
                ModelState.AddModelError("", "Unable to save changes. The category name may already exist in that category group.");

                // Repopulate dropdowns so the view renders correctly after the error.
                await PopulateDropdowns(formModel.CategoryGroupId, formModel.ParentId, excludeCategoryId: formModel.Id);

                return View(formModel);
            }
        }

        // GET: Categories/Delete/5
        /// <summary>
        /// Shows the Delete confirmation page for a Category.
        ///
        /// Note:
        /// - This action does not delete anything; it only loads the entity and shows a confirmation view.
        /// - The actual soft delete happens in DeleteConfirmed.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null id");
                return NotFound();
            }

            // Load the record with related entities so the confirmation page can show context.
            var category = await _context.Categories
                .Include(c => c.CategoryGroup)
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                _logger.LogWarning("Category {Id} not found for delete", id);
                return NotFound();
            }

            // Warning level here is reasonable because delete is a potentially destructive action,
            // even though your implementation is a soft delete.
            _logger.LogWarning("Loading Delete confirmation for CategoryId {Id}", id);

            return View(category);
        }

        // POST: Categories/Delete/5
        /// <summary>
        /// Performs the delete operation (soft delete).
        ///
        /// Implementation detail:
        /// - This does NOT remove the row from the database.
        /// - Instead, it sets DeletedAt to the current time and updates audit fields.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting CategoryId {Id}", id);

            // Fetch the record to soft delete.
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                _logger.LogWarning("Category {Id} not found during delete", id);
                return NotFound();
            }

            // Soft delete: mark as deleted and update audit fields.
            // This preserves historical data and keeps referential integrity in place.
            category.DeletedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedByUserId = null; // Placeholder until auth/user tracking is implemented.

            // Persist the soft delete changes.
            await _context.SaveChangesAsync();

            _logger.LogInformation("Category {Id} soft deleted", id);

            // Return to the listing after deletion.
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Populates dropdown lists used by Create/Edit views.
        ///
        /// ViewData keys populated:
        /// - "CategoryGroupId": list of CategoryGroups
        /// - "ParentId": list of Categories to choose as parent
        ///
        /// Parameters:
        /// - selectedCategoryGroupId: which group should be pre-selected in the dropdown
        /// - selectedParentId: which parent category should be pre-selected in the dropdown
        /// - excludeCategoryId: optionally exclude a category (typically the current one during Edit)
        /// </summary>
        private async Task PopulateDropdowns(
            ulong? selectedCategoryGroupId = null,
            ulong? selectedParentId = null,
            ulong? excludeCategoryId = null)
        {
            _logger.LogDebug("Populating category dropdowns");

            // Load category groups ordered for a user-friendly dropdown experience.
            var categoryGroups = await _context.CategoryGroups
                .OrderBy(g => g.SortOrder)
                .ThenBy(g => g.Name)
                .ToListAsync();

            // Start building a query for parent category choices.
            // OrderBy(Name) provides stable alphabetical ordering in the dropdown.
            var categoriesQuery = _context.Categories
                .OrderBy(c => c.Name)
                .AsQueryable();

            // When editing a category, exclude it from the list of possible parents.
            // This prevents choosing itself as its parent via the dropdown list.
            if (excludeCategoryId.HasValue)
            {
                categoriesQuery = categoriesQuery.Where(c => c.Id != excludeCategoryId.Value);
            }

            // Execute the query and materialize the list for dropdown population.
            var parentCategories = await categoriesQuery.ToListAsync();

            // SelectList maps to HTML <select> options in Razor views.
            // - "Id" is the underlying value
            // - "Name" is what the user sees
            // - selected values help keep user choices on redisplay after validation errors
            ViewData["CategoryGroupId"] = new SelectList(categoryGroups, "Id", "Name", selectedCategoryGroupId);
            ViewData["ParentId"] = new SelectList(parentCategories, "Id", "Name", selectedParentId);
        }

        /// <summary>
        /// Helper method to check if a Category exists.
        ///
        /// Note:
        /// - Not used in the current controller logic, but can be useful for concurrency checks
        ///   or future enhancements where you need to validate existence without loading the entity.
        /// </summary>
        private async Task<bool> CategoryExists(ulong id)
        {
            return await _context.Categories.AnyAsync(e => e.Id == id);
        }
    }
}