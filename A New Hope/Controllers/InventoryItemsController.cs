using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages create, read, update, and soft delete operations for inventory items.
    /// </summary>
    public class InventoryItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryItemsController> _logger;

        /// <summary>
        /// Creates the controller with the required database context and logger.
        /// </summary>
        public InventoryItemsController(ApplicationDbContext context, ILogger<InventoryItemsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: InventoryItems
        /// <summary>
        /// Displays all non-deleted inventory items.
        /// </summary>
        public async Task<IActionResult> Index(string? search)
        {
            try
            {
                _logger.LogInformation("Loading InventoryItems Index page");

                // Build the base query for active inventory items with related data.
                IQueryable<A_New_Hope.Models.InventoryItem> inventoryQuery = _context.InventoryItems
                    .Where(i => i.DeletedAt == null)
                    .Include(i => i.Category)
                        .ThenInclude(c => c.CategoryGroup)
                    .Include(i => i.InventoryItemOptions.Where(o => o.DeletedAt == null));

                // Apply the search filter when one is provided.
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string lowerSearch = search.ToLower();

                    inventoryQuery = inventoryQuery.Where(i =>
                        // Item name
                        (i.Name != null && i.Name.ToLower().Contains(lowerSearch)) ||
                        // Category name
                        (i.Category != null && i.Category.Name != null && i.Category.Name.ToLower().Contains(lowerSearch)) ||
                        // Category group name
                        (i.Category != null && i.Category.CategoryGroup != null && i.Category.CategoryGroup.Name != null &&
                            i.Category.CategoryGroup.Name.ToLower().Contains(lowerSearch)) ||
                        // Option names
                        (i.InventoryItemOptions.Any(o => o.Name != null && o.Name.ToLower().Contains(lowerSearch)))
                    );
                }

                // Retrieve the ordered inventory items for display.
                var inventoryItems = await inventoryQuery
                    .OrderBy(i => i.Category.Name)
                    .ThenBy(i => i.Name)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} inventory items", inventoryItems.Count);

                return View(inventoryItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load inventory items");
                return View("Error");
            }
        }

        // GET: InventoryItems/Details/5
        /// <summary>
        /// Displays details for a single non-deleted inventory item.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            try
            {
                // Reject requests with no id.
                if (id == null)
                {
                    _logger.LogWarning("Details requested with null Id");
                    return NotFound();
                }

                _logger.LogInformation("Fetching details for InventoryItem Id {Id}", id);

                // Retrieve the requested active inventory item with related category data.
                var inventoryItem = await _context.InventoryItems
                    .Where(i => i.DeletedAt == null)
                    .Include(i => i.Category)
                        .ThenInclude(c => c.CategoryGroup)
                    .Include(i => i.InventoryItemOptions
                        .Where(o => o.DeletedAt == null))
                    .FirstOrDefaultAsync(m => m.Id == id);

                // Return not found when the inventory item does not exist.
                if (inventoryItem == null)
                {
                    _logger.LogWarning("InventoryItem Id {Id} not found", id);
                    return NotFound();
                }

                return View(inventoryItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for InventoryItem Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading inventory item details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: InventoryItems/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            try
            {
                _logger.LogInformation("Loading Create InventoryItem page");

                // Populate dropdown values for the create form.
                await PopulateDropdowns();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Create InventoryItem page");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the create form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: InventoryItems/Create
        /// <summary>
        /// Creates a new inventory item after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,CategoryId,IsBaseline,IsAvailable,IsActive")] InventoryItem inventoryItem)
        {
            try
            {
                _logger.LogInformation("Attempting to create InventoryItem {Name}", inventoryItem.Name);

                // Remove navigation properties that are not posted by the form.
                ModelState.Remove(nameof(InventoryItem.Category));
                ModelState.Remove(nameof(InventoryItem.CreatedByUser));
                ModelState.Remove(nameof(InventoryItem.UpdatedByUser));

                // Normalize incoming values before business-rule validation.
                NormalizeInventoryItem(inventoryItem);
                await ApplyInventoryItemValidationAsync(inventoryItem);

                // Return the form with dropdowns restored when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create InventoryItem failed validation for {Name}", inventoryItem.Name);
                    await PopulateDropdowns(inventoryItem.CategoryId);
                    return View(inventoryItem);
                }

                // Set audit fields for the new inventory item record.
                var now = DateTime.UtcNow;
                inventoryItem.CreatedAt = now;
                inventoryItem.UpdatedAt = now;
                inventoryItem.CreatedByUserId = null; // Replace when auth integration is added.
                inventoryItem.UpdatedByUserId = null; // Replace when auth integration is added.

                // Queue the new inventory item for insert.
                _context.Add(inventoryItem);

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("InventoryItem {Name} created successfully", inventoryItem.Name);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error creating InventoryItem {Name}", inventoryItem.Name);

                    ModelState.AddModelError("", "Unable to save inventory item.");
                    await PopulateDropdowns(inventoryItem.CategoryId);
                    return View(inventoryItem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating InventoryItem {Name}", inventoryItem?.Name);
                ModelState.AddModelError("", "An unexpected error occurred while creating the inventory item.");

                await PopulateDropdowns(inventoryItem?.CategoryId);
                return View(inventoryItem ?? new InventoryItem());
            }
        }

        // GET: InventoryItems/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted inventory item.
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

                _logger.LogInformation("Loading Edit page for InventoryItem Id {Id}", id);

                // Retrieve the requested active inventory item for editing.
                var inventoryItem = await _context.InventoryItems
                    .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

                // Return not found when the inventory item does not exist.
                if (inventoryItem == null)
                {
                    _logger.LogWarning("InventoryItem Id {Id} not found for edit", id);
                    return NotFound();
                }

                // Populate dropdown values using the current record selection.
                await PopulateDropdowns(inventoryItem.CategoryId);

                return View(inventoryItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit page for InventoryItem Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: InventoryItems/Edit/5
        /// <summary>
        /// Updates an existing inventory item after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Name,CategoryId,IsBaseline,IsAvailable,IsActive")] InventoryItem formModel)
        {
            try
            {
                _logger.LogInformation("Attempting to edit InventoryItem Id {Id}", id);

                // Ensure the route id matches the posted model id.
                if (id != formModel.Id)
                {
                    _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                    return NotFound();
                }

                // Remove navigation properties that are not posted by the form.
                ModelState.Remove(nameof(InventoryItem.Category));
                ModelState.Remove(nameof(InventoryItem.CreatedByUser));
                ModelState.Remove(nameof(InventoryItem.UpdatedByUser));

                // Normalize incoming values before business-rule validation.
                NormalizeInventoryItem(formModel);
                await ApplyInventoryItemValidationAsync(formModel, formModel.Id);

                // Return the form with dropdowns restored when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Edit InventoryItem failed validation for Id {Id}", id);
                    await PopulateDropdowns(formModel.CategoryId);
                    return View(formModel);
                }

                // Retrieve the existing active inventory item record.
                var existing = await _context.InventoryItems
                    .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

                // Return not found when the target record no longer exists.
                if (existing == null)
                {
                    _logger.LogWarning("InventoryItem Id {Id} not found during edit save", id);
                    return NotFound();
                }

                // Copy validated form values into the tracked entity.
                existing.Name = formModel.Name;
                existing.CategoryId = formModel.CategoryId;
                existing.IsBaseline = formModel.IsBaseline;
                existing.IsAvailable = formModel.IsAvailable;
                existing.IsActive = formModel.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = null; // Replace when auth integration is added.

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("InventoryItem Id {Id} updated successfully", id);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Check whether the record was deleted during the edit attempt.
                    if (!await InventoryItemExists(formModel.Id))
                    {
                        _logger.LogWarning("InventoryItem Id {Id} no longer exists during concurrency check", id);
                        return NotFound();
                    }

                    _logger.LogError(ex, "Concurrency error updating InventoryItem Id {Id}", id);
                    throw;
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error updating InventoryItem Id {Id}", id);

                    ModelState.AddModelError("", "Unable to save changes.");
                    await PopulateDropdowns(formModel.CategoryId);
                    return View(formModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error editing InventoryItem Id {Id}", id);
                ModelState.AddModelError("", "An unexpected error occurred while updating the inventory item.");
                await PopulateDropdowns(formModel.CategoryId);
                return View(formModel);
            }
        }

        // GET: InventoryItems/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted inventory item.
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

                _logger.LogInformation("Loading Delete confirmation for InventoryItem Id {Id}", id);

                // Retrieve the requested active inventory item with related category data.
                var inventoryItem = await _context.InventoryItems
                    .Where(i => i.DeletedAt == null)
                    .Include(i => i.Category)
                        .ThenInclude(c => c.CategoryGroup)
                    .FirstOrDefaultAsync(m => m.Id == id);

                // Return not found when the inventory item does not exist.
                if (inventoryItem == null)
                {
                    _logger.LogWarning("InventoryItem Id {Id} not found for delete", id);
                    return NotFound();
                }

                return View(inventoryItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete page for InventoryItem Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the delete page.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: InventoryItems/Delete/5
        /// <summary>
        /// Soft deletes an inventory item.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            try
            {
                _logger.LogWarning("Soft deleting InventoryItem Id {Id}", id);

                // Retrieve the active inventory item targeted for soft delete.
                var inventoryItem = await _context.InventoryItems
                    .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

                // Return not found when the inventory item does not exist.
                if (inventoryItem == null)
                {
                    _logger.LogWarning("InventoryItem Id {Id} not found during delete", id);
                    return NotFound();
                }

                // Apply soft-delete and audit values.
                inventoryItem.DeletedAt = DateTime.UtcNow;
                inventoryItem.UpdatedAt = DateTime.UtcNow;
                inventoryItem.UpdatedByUserId = null; // Replace when auth integration is added.

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("InventoryItem Id {Id} soft deleted", id);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error soft deleting InventoryItem Id {Id}", id);

                    TempData["ErrorMessage"] = "Unable to delete inventory item.";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting InventoryItem Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the inventory item.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        /// <summary>
        /// Populates the category dropdown for the create and edit forms.
        /// </summary>
        private async Task PopulateDropdowns(ulong? selectedCategoryId = null)
        {
            _logger.LogDebug("Populating Category dropdown for InventoryItem");

            // Retrieve active categories with active category groups for the dropdown list.
            var categories = await _context.Categories
                .Where(c => c.DeletedAt == null && c.CategoryGroup.DeletedAt == null)
                .Include(c => c.CategoryGroup)
                .OrderBy(c => c.CategoryGroup.Name)
                .ThenBy(c => c.Name)
                .ToListAsync();

            // Build display-friendly category dropdown options.
            var categoryOptions = categories
                .Select(c => new
                {
                    c.Id,
                    DisplayName = $"{c.CategoryGroup.Name} - {c.Name}"
                })
                .ToList();

            _logger.LogDebug("Loaded {Count} categories for dropdown", categoryOptions.Count);

            // Store the category dropdown options in ViewData.
            ViewData["CategoryId"] = new SelectList(categoryOptions, "Id", "DisplayName", selectedCategoryId);
        }

        /// <summary>
        /// Returns true if the non-deleted inventory item exists.
        /// </summary>
        private async Task<bool> InventoryItemExists(ulong id)
        {
            // Check whether the requested active inventory item still exists.
            return await _context.InventoryItems.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Trims strings and normalizes required values.
        /// </summary>
        private static void NormalizeInventoryItem(InventoryItem model)
        {
            // Normalize and trim the required inventory item name.
            model.Name = model.Name?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyInventoryItemValidationAsync(InventoryItem model, ulong? currentId = null)
        {
            // Validate that the selected category exists and is not deleted.
            var categoryExists = await _context.Categories
                .AnyAsync(c =>
                    c.Id == model.CategoryId &&
                    c.DeletedAt == null &&
                    c.CategoryGroup.DeletedAt == null);

            if (!categoryExists)
            {
                ModelState.AddModelError(nameof(InventoryItem.CategoryId), "Select a valid category.");
            }

            // Require an inventory item name.
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(InventoryItem.Name), "Inventory item name is required.");
            }

            // Require at least one letter or number in the inventory item name.
            if (!string.IsNullOrWhiteSpace(model.Name) &&
                !AddressValidation.ContainsLetterOrDigit(model.Name))
            {
                ModelState.AddModelError(nameof(InventoryItem.Name), "Inventory item name must contain letters or numbers.");
            }

            // Prevent duplicate active inventory item names within the same category.
            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                var normalizedName = model.Name.ToLower();

                var duplicateExists = await _context.InventoryItems
                    .AnyAsync(i =>
                        i.DeletedAt == null &&
                        i.Id != currentId &&
                        i.CategoryId == model.CategoryId &&
                        i.Name.ToLower() == normalizedName);

                if (duplicateExists)
                {
                    ModelState.AddModelError(nameof(InventoryItem.Name), "An inventory item with this name already exists in the selected category.");
                }
            }
        }
    }
}