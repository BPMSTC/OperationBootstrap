using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// InventoryItemsController
    /// ------------------------
    /// This controller manages DIO operations for InventoryItem records.
    ///
    /// In your domain, InventoryItems appear to represent trackable resources/items that can be:
    /// - Assigned a Category (which belongs to a CategoryGroup)
    /// - Marked as Baseline items (likely core/default inventory items)
    /// - Marked as Available (inventory state)
    /// - Marked as Active (business-level enable/disable)
    ///
    /// Key behaviors implemented here:
    /// - Uses Entity Framework Core (ApplicationDbContext) to query and persist InventoryItem data.
    /// - Uses dependency-injected ILogger for structured logging (request tracing + error diagnostics).
    /// - Uses SOFT DELETE semantics: DeleteConfirmed sets DeletedAt rather than physically removing rows.
    /// - Filters out soft-deleted inventory items from lists and lookups (DeletedAt == null).
    /// - Populates a Category dropdown where each option displays "CategoryGroup - Category" for clarity.
    ///
    /// Notes on audit fields:
    /// - CreatedAt / UpdatedAt are set using DateTime.UtcNow.
    /// - CreatedByUserId / UpdatedByUserId are currently set to null until auth/user tracking is implemented.
    /// </summary>
    public class InventoryItemsController : Controller
    {
        /// <summary>
        /// EF Core DbContext used to access InventoryItems, Categories, and CategoryGroups.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Logger for this controller. Output depends on logging providers configured in Program.cs.
        /// </summary>
        private readonly ILogger<InventoryItemsController> _logger;

        /// <summary>
        /// Constructor with dependency injection.
        /// </summary>
        public InventoryItemsController(ApplicationDbContext context, ILogger<InventoryItemsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: InventoryItems
        /// <summary>
        /// Displays a list of inventory items (non-deleted only).
        ///
        /// Query behavior:
        /// - Filters out soft-deleted items (DeletedAt == null).
        /// - Eager-loads Category and CategoryGroup to show descriptive category information in the UI.
        /// - Orders by Category name, then Item name for user-friendly browsing.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading InventoryItems Index page");

            // Load inventory items + category hierarchy in a single query.
            var inventoryItems = await _context.InventoryItems
                .Where(i => i.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .OrderBy(i => i.Category.Name)
                .ThenBy(i => i.Name)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} inventory items", inventoryItems.Count);

            // Render Views/InventoryItems/Index.cshtml with the list model.
            return View(inventoryItems);
        }

        // GET: InventoryItems/Details/5
        /// <summary>
        /// Displays details for a single InventoryItem by primary key Id.
        ///
        /// Behavior:
        /// - Returns 404 if id is null or record not found (or soft deleted).
        /// - Eager-loads Category and CategoryGroup for display context.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            // Missing route id => cannot locate a record.
            if (id == null)
            {
                _logger.LogWarning("Details requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for InventoryItem Id {Id}", id);

            // Load the record and include related Category/CategoryGroup for display.
            var inventoryItem = await _context.InventoryItems
                .Where(i => i.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .FirstOrDefaultAsync(m => m.Id == id);

            // If the record does not exist (or is deleted), return 404.
            if (inventoryItem == null)
            {
                _logger.LogWarning("InventoryItem Id {Id} not found", id);
                return NotFound();
            }

            return View(inventoryItem);
        }

        // GET: InventoryItems/Create
        /// <summary>
        /// Shows the Create form for a new InventoryItem.
        ///
        /// Important:
        /// - InventoryItems require a CategoryId, so we populate the Category dropdown list first.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create InventoryItem page");

            // Populate ViewData["CategoryId"] for the <select> in the view.
            await PopulateDropdowns();

            return View();
        }

        // POST: InventoryItems/Create
        /// <summary>
        /// Processes form submission to create a new InventoryItem.
        ///
        /// Security:
        /// - [ValidateAntiForgeryToken] provides CSRF protection.
        ///
        /// Binding:
        /// - [Bind(...)] limits which fields can be posted/created (prevents over-posting).
        ///
        /// Validation:
        /// - Removes navigation properties from ModelState because forms do not post navigation objects.
        /// - If invalid, repopulates dropdowns and re-renders the Create view.
        ///
        /// Audit:
        /// - Sets CreatedAt/UpdatedAt and placeholder CreatedBy/UpdatedBy user IDs.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,CategoryId,IsBaseline,IsAvailable,IsActive")] InventoryItem inventoryItem)
        {
            _logger.LogInformation("Attempting to create InventoryItem {Name}", inventoryItem.Name);

            // Navigation properties are not included in form posts. Remove them from ModelState
            // to avoid incorrect validation failures.
            ModelState.Remove(nameof(InventoryItem.Category));
            ModelState.Remove(nameof(InventoryItem.CreatedByUser));
            ModelState.Remove(nameof(InventoryItem.UpdatedByUser));

            // If validation failed, repopulate dropdown and re-render the view with user inputs.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create InventoryItem failed validation for {Name}", inventoryItem.Name);
                await PopulateDropdowns(inventoryItem.CategoryId);
                return View(inventoryItem);
            }

            // Set audit fields.
            var now = DateTime.UtcNow;
            inventoryItem.CreatedAt = now;
            inventoryItem.UpdatedAt = now;
            inventoryItem.CreatedByUserId = null; // Placeholder until auth integration.
            inventoryItem.UpdatedByUserId = null; // Placeholder until auth integration.

            // Stage the insert.
            _context.Add(inventoryItem);

            try
            {
                // Persist to DB.
                await _context.SaveChangesAsync();

                _logger.LogInformation("InventoryItem {Name} created successfully", inventoryItem.Name);

                // Return to list after successful create.
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // Typical causes: constraint violations, FK issues, DB connectivity, etc.
                _logger.LogError(ex, "Error creating InventoryItem {Name}", inventoryItem.Name);

                ModelState.AddModelError("", "Unable to save inventory item.");

                // Repopulate dropdown so the view renders correctly after error.
                await PopulateDropdowns(inventoryItem.CategoryId);

                return View(inventoryItem);
            }
        }

        // GET: InventoryItems/Edit/5
        /// <summary>
        /// Shows the Edit form for an existing InventoryItem.
        ///
        /// Behavior:
        /// - Returns 404 if id is null or record not found (or soft deleted).
        /// - Populates dropdowns so current Category selection is available in the form.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for InventoryItem Id {Id}", id);

            // Load the inventory item being edited (excluding deleted items).
            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

            if (inventoryItem == null)
            {
                _logger.LogWarning("InventoryItem Id {Id} not found for edit", id);
                return NotFound();
            }

            // Populate category dropdown with current selection.
            await PopulateDropdowns(inventoryItem.CategoryId);

            return View(inventoryItem);
        }

        // POST: InventoryItems/Edit/5
        /// <summary>
        /// Processes form submission to update an existing InventoryItem.
        ///
        /// Parameters:
        /// - id: route Id
        /// - formModel: posted model (limited by [Bind] to editable properties)
        ///
        /// Safety checks:
        /// - Confirms route Id matches posted model Id.
        /// - Removes navigation properties from ModelState because forms don't post them.
        ///
        /// Update strategy:
        /// - Loads the existing entity from DB (ensures not soft-deleted).
        /// - Copies editable fields to the tracked entity.
        /// - Updates audit metadata.
        ///
        /// Error handling:
        /// - DbUpdateConcurrencyException: record changed/deleted since load.
        /// - DbUpdateException: general save failure.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Name,CategoryId,IsBaseline,IsAvailable,IsActive")] InventoryItem formModel)
        {
            _logger.LogInformation("Attempting to edit InventoryItem Id {Id}", id);

            // Ensure the route id matches the posted model id.
            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            // Remove navigation properties from model validation.
            ModelState.Remove(nameof(InventoryItem.Category));
            ModelState.Remove(nameof(InventoryItem.CreatedByUser));
            ModelState.Remove(nameof(InventoryItem.UpdatedByUser));

            // If invalid, repopulate dropdown and return to view with validation messages.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit InventoryItem failed validation for Id {Id}", id);
                await PopulateDropdowns(formModel.CategoryId);
                return View(formModel);
            }

            // Load existing record to safely apply updates.
            var existing = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

            if (existing == null)
            {
                _logger.LogWarning("InventoryItem Id {Id} not found during edit save", id);
                return NotFound();
            }

            // Apply editable fields.
            existing.Name = formModel.Name;
            existing.CategoryId = formModel.CategoryId;
            existing.IsBaseline = formModel.IsBaseline;
            existing.IsAvailable = formModel.IsAvailable;
            existing.IsActive = formModel.IsActive;

            // Audit.
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // Placeholder until auth integration.

            try
            {
                // Persist changes.
                await _context.SaveChangesAsync();

                _logger.LogInformation("InventoryItem Id {Id} updated successfully", id);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Concurrency exceptions indicate the record has changed or disappeared since it was loaded.
                if (!await InventoryItemExists(formModel.Id))
                {
                    _logger.LogWarning("InventoryItem Id {Id} no longer exists during concurrency check", id);
                    return NotFound();
                }

                // If the record still exists, rethrow for global error handling.
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating InventoryItem Id {Id}", id);

                ModelState.AddModelError("", "Unable to save changes.");

                // Rebuild dropdown so the view can render properly.
                await PopulateDropdowns(formModel.CategoryId);

                return View(formModel);
            }
        }

        // GET: InventoryItems/Delete/5
        /// <summary>
        /// Shows the Delete confirmation page for an InventoryItem.
        ///
        /// Notes:
        /// - This GET action does not delete anything.
        /// - It loads the item and its category hierarchy for context in the confirmation screen.
        /// - Actual soft delete occurs in DeleteConfirmed.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for InventoryItem Id {Id}", id);

            // Load the item for confirmation display, including category hierarchy.
            var inventoryItem = await _context.InventoryItems
                .Where(i => i.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inventoryItem == null)
            {
                _logger.LogWarning("InventoryItem Id {Id} not found for delete", id);
                return NotFound();
            }

            return View(inventoryItem);
        }

        // POST: InventoryItems/Delete/5
        /// <summary>
        /// Executes the delete operation (soft delete) for an InventoryItem.
        ///
        /// Soft delete strategy:
        /// - Marks DeletedAt
        /// - Updates audit fields
        /// - Keeps the row for history/audit/referential integrity
        ///
        /// Error handling:
        /// - If SaveChanges fails, uses TempData to store an error message and redirects
        ///   back to the confirmation screen.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting InventoryItem Id {Id}", id);

            // Load the entity to delete (must not already be soft-deleted).
            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

            if (inventoryItem == null)
            {
                _logger.LogWarning("InventoryItem Id {Id} not found during delete", id);
                return NotFound();
            }

            // Apply soft delete + audit updates.
            inventoryItem.DeletedAt = DateTime.UtcNow;
            inventoryItem.UpdatedAt = DateTime.UtcNow;
            inventoryItem.UpdatedByUserId = null; // Placeholder until auth integration.

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("InventoryItem Id {Id} soft deleted", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error soft deleting InventoryItem Id {Id}", id);

                // TempData persists for one redirect so Delete view can show an error message.
                TempData["ErrorMessage"] = "Unable to delete inventory item.";

                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Populates the Category dropdown list for Create/Edit views.
        ///
        /// Data source:
        /// - Categories, including their CategoryGroup, so the display label can show group + category.
        ///
        /// Filtering:
        /// - Only categories not soft-deleted
        /// - Only categories whose group is not soft-deleted (CategoryGroup.DeletedAt == null)
        ///
        /// Display formatting:
        /// - Each dropdown option is "CategoryGroup.Name - Category.Name" to avoid confusion
        ///   when multiple groups may contain similarly named categories.
        ///
        /// ViewData:
        /// - ViewData["CategoryId"] is used by the view to render a <select> element.
        /// </summary>
        private async Task PopulateDropdowns(ulong? selectedCategoryId = null)
        {
            _logger.LogDebug("Populating Category dropdown for InventoryItem");

            // Load categories + groups in a predictable order.
            var categories = await _context.Categories
                .Where(c => c.DeletedAt == null && c.CategoryGroup.DeletedAt == null)
                .Include(c => c.CategoryGroup)
                .OrderBy(c => c.CategoryGroup.Name)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            // Create a simplified list of option objects so the SelectList can display a friendly label.
            var categoryOptions = categories
                .Select(c => new
                {
                    c.Id,
                    DisplayName = $"{c.CategoryGroup.Name} - {c.Name}"
                })
                .ToList();

            _logger.LogDebug("Loaded {Count} categories for dropdown", categoryOptions.Count);

            // Store SelectList in ViewData for Razor rendering.
            ViewData["CategoryId"] = new SelectList(categoryOptions, "Id", "DisplayName", selectedCategoryId);
        }

        /// <summary>
        /// Helper method used by Edit POST concurrency handling.
        /// Checks whether a non-deleted InventoryItem exists for the provided Id.
        /// </summary>
        private async Task<bool> InventoryItemExists(ulong id)
        {
            return await _context.InventoryItems.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}