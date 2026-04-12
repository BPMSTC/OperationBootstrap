using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages create, read, update, and soft delete operations for inventory choice group items.
    /// </summary>
    public class InventoryChoiceGroupItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryChoiceGroupItemsController> _logger;

        /// <summary>
        /// Creates the controller with the required database context and logger.
        /// </summary>
        public InventoryChoiceGroupItemsController(ApplicationDbContext context, ILogger<InventoryChoiceGroupItemsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: InventoryChoiceGroupItems
        /// <summary>
        /// Displays all non-deleted inventory choice group items.
        /// </summary>
        public async Task<IActionResult> Index(ulong? inventoryChoiceGroupId = null)
        {
            try
            {
                _logger.LogInformation("Loading InventoryChoiceGroupItems Index page");

                // Build the base query for active inventory choice group items.
                var query = _context.InventoryChoiceGroupItems
                    .Where(i => i.DeletedAt == null)
                    .Include(i => i.InventoryChoiceGroup)
                    .Include(i => i.InventoryItem)
                        .ThenInclude(ii => ii.Category)
                            .ThenInclude(c => c.CategoryGroup)
                    .AsQueryable();

                // Apply a choice group filter when one is provided.
                if (inventoryChoiceGroupId.HasValue)
                {
                    query = query.Where(i => i.InventoryChoiceGroupId == inventoryChoiceGroupId.Value);

                    // Load the selected choice group for display metadata.
                    var selectedGroup = await _context.InventoryChoiceGroups
                        .Where(g => g.DeletedAt == null)
                        .FirstOrDefaultAsync(g => g.Id == inventoryChoiceGroupId.Value);

                    if (selectedGroup != null)
                    {
                        ViewData["SelectedChoiceGroupId"] = selectedGroup.Id;
                        ViewData["SelectedChoiceGroupName"] =
                            string.IsNullOrWhiteSpace(selectedGroup.DisplayLabel)
                                ? selectedGroup.Name
                                : selectedGroup.DisplayLabel;
                    }
                }

                // Retrieve the filtered inventory choice group items for display.
                var inventoryChoiceGroupItems = await query
                    .OrderBy(i => i.InventoryChoiceGroup.Name)
                    .ThenBy(i => i.InventoryItem.Name)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} inventory choice group items", inventoryChoiceGroupItems.Count);

                return View(inventoryChoiceGroupItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inventory choice group items list");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading inventory choice group items.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: InventoryChoiceGroupItems/Details/5
        /// <summary>
        /// Displays details for a single non-deleted inventory choice group item.
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

                _logger.LogInformation("Fetching details for InventoryChoiceGroupItem Id {Id}", id);

                // Retrieve the requested active inventory choice group item.
                var inventoryChoiceGroupItem = await _context.InventoryChoiceGroupItems
                    .Where(i => i.DeletedAt == null)
                    .Include(i => i.InventoryChoiceGroup)
                    .Include(i => i.InventoryItem)
                        .ThenInclude(ii => ii.Category)
                            .ThenInclude(c => c.CategoryGroup)
                    .FirstOrDefaultAsync(m => m.Id == id);

                // Return not found when the inventory choice group item does not exist.
                if (inventoryChoiceGroupItem == null)
                {
                    _logger.LogWarning("InventoryChoiceGroupItem Id {Id} not found", id);
                    return NotFound();
                }

                return View(inventoryChoiceGroupItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for InventoryChoiceGroupItem Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading inventory choice group item details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: InventoryChoiceGroupItems/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public async Task<IActionResult> Create(ulong? inventoryChoiceGroupId = null)
        {
            try
            {
                _logger.LogInformation("Loading Create InventoryChoiceGroupItem page");

                // Populate dropdown values for the create form.
                await PopulateDropdowns(inventoryChoiceGroupId, null);

                // Build the default model for the create view.
                var model = new InventoryChoiceGroupItem();

                if (inventoryChoiceGroupId.HasValue)
                {
                    model.InventoryChoiceGroupId = inventoryChoiceGroupId.Value;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Create InventoryChoiceGroupItem page");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the create form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: InventoryChoiceGroupItems/Create
        /// <summary>
        /// Creates a new inventory choice group item after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("InventoryChoiceGroupId,InventoryItemId,IsActive")] InventoryChoiceGroupItem inventoryChoiceGroupItem)
        {
            try
            {
                _logger.LogInformation("Attempting to create InventoryChoiceGroupItem");

                // Remove navigation properties that are not posted by the form.
                ModelState.Remove(nameof(InventoryChoiceGroupItem.InventoryChoiceGroup));
                ModelState.Remove(nameof(InventoryChoiceGroupItem.InventoryItem));
                ModelState.Remove(nameof(InventoryChoiceGroupItem.CreatedByUser));
                ModelState.Remove(nameof(InventoryChoiceGroupItem.UpdatedByUser));

                // Apply business-rule validation before saving.
                await ApplyInventoryChoiceGroupItemValidationAsync(inventoryChoiceGroupItem);

                // Return the form with dropdowns restored when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create InventoryChoiceGroupItem failed validation");
                    await PopulateDropdowns(inventoryChoiceGroupItem.InventoryChoiceGroupId, inventoryChoiceGroupItem.InventoryItemId);
                    return View(inventoryChoiceGroupItem);
                }

                // Set audit fields for the new inventory choice group item record.
                var now = DateTime.UtcNow;
                inventoryChoiceGroupItem.CreatedAt = now;
                inventoryChoiceGroupItem.UpdatedAt = now;
                inventoryChoiceGroupItem.CreatedByUserId = null; // Replace when auth integration is added.
                inventoryChoiceGroupItem.UpdatedByUserId = null; // Replace when auth integration is added.

                // Queue the new inventory choice group item for insert.
                _context.Add(inventoryChoiceGroupItem);

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("InventoryChoiceGroupItem created successfully");
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error creating InventoryChoiceGroupItem");

                    ModelState.AddModelError("", "Unable to save inventory choice group item.");
                    await PopulateDropdowns(inventoryChoiceGroupItem.InventoryChoiceGroupId, inventoryChoiceGroupItem.InventoryItemId);
                    return View(inventoryChoiceGroupItem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating InventoryChoiceGroupItem");
                ModelState.AddModelError("", "An unexpected error occurred while creating the inventory choice group item.");
                await PopulateDropdowns(inventoryChoiceGroupItem.InventoryChoiceGroupId, inventoryChoiceGroupItem.InventoryItemId);
                return View(inventoryChoiceGroupItem);
            }
        }

        // GET: InventoryChoiceGroupItems/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted inventory choice group item.
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

                _logger.LogInformation("Loading Edit page for InventoryChoiceGroupItem Id {Id}", id);

                // Retrieve the requested active inventory choice group item for editing.
                var inventoryChoiceGroupItem = await _context.InventoryChoiceGroupItems
                    .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

                // Return not found when the inventory choice group item does not exist.
                if (inventoryChoiceGroupItem == null)
                {
                    _logger.LogWarning("InventoryChoiceGroupItem Id {Id} not found for edit", id);
                    return NotFound();
                }

                // Populate dropdown values using the current record selections.
                await PopulateDropdowns(inventoryChoiceGroupItem.InventoryChoiceGroupId, inventoryChoiceGroupItem.InventoryItemId);
                return View(inventoryChoiceGroupItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit page for InventoryChoiceGroupItem Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: InventoryChoiceGroupItems/Edit/5
        /// <summary>
        /// Updates an existing inventory choice group item after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,InventoryChoiceGroupId,InventoryItemId,IsActive")] InventoryChoiceGroupItem formModel)
        {
            try
            {
                _logger.LogInformation("Attempting to edit InventoryChoiceGroupItem Id {Id}", id);

                // Ensure the route id matches the posted model id.
                if (id != formModel.Id)
                {
                    _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                    return NotFound();
                }

                // Remove navigation properties that are not posted by the form.
                ModelState.Remove(nameof(InventoryChoiceGroupItem.InventoryChoiceGroup));
                ModelState.Remove(nameof(InventoryChoiceGroupItem.InventoryItem));
                ModelState.Remove(nameof(InventoryChoiceGroupItem.CreatedByUser));
                ModelState.Remove(nameof(InventoryChoiceGroupItem.UpdatedByUser));

                // Apply business-rule validation before saving.
                await ApplyInventoryChoiceGroupItemValidationAsync(formModel, formModel.Id);

                // Return the form with dropdowns restored when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Edit InventoryChoiceGroupItem failed validation for Id {Id}", id);
                    await PopulateDropdowns(formModel.InventoryChoiceGroupId, formModel.InventoryItemId);
                    return View(formModel);
                }

                // Retrieve the existing active inventory choice group item record.
                var existing = await _context.InventoryChoiceGroupItems
                    .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

                // Return not found when the target record no longer exists.
                if (existing == null)
                {
                    _logger.LogWarning("InventoryChoiceGroupItem Id {Id} not found during edit save", id);
                    return NotFound();
                }

                // Copy validated form values into the tracked entity.
                existing.InventoryChoiceGroupId = formModel.InventoryChoiceGroupId;
                existing.InventoryItemId = formModel.InventoryItemId;
                existing.IsActive = formModel.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = null; // Replace when auth integration is added.

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("InventoryChoiceGroupItem Id {Id} updated successfully", id);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Check whether the record was deleted during the edit attempt.
                    if (!await InventoryChoiceGroupItemExists(formModel.Id))
                    {
                        _logger.LogWarning("InventoryChoiceGroupItem Id {Id} no longer exists during concurrency check", id);
                        return NotFound();
                    }

                    _logger.LogError(ex, "Concurrency error updating InventoryChoiceGroupItem Id {Id}", id);
                    throw;
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error updating InventoryChoiceGroupItem Id {Id}", id);

                    ModelState.AddModelError("", "Unable to save changes.");
                    await PopulateDropdowns(formModel.InventoryChoiceGroupId, formModel.InventoryItemId);
                    return View(formModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error editing InventoryChoiceGroupItem Id {Id}", id);
                ModelState.AddModelError("", "An unexpected error occurred while updating the inventory choice group item.");
                await PopulateDropdowns(formModel.InventoryChoiceGroupId, formModel.InventoryItemId);
                return View(formModel);
            }
        }

        // GET: InventoryChoiceGroupItems/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted inventory choice group item.
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

                _logger.LogInformation("Loading Delete confirmation for InventoryChoiceGroupItem Id {Id}", id);

                // Retrieve the requested active inventory choice group item.
                var inventoryChoiceGroupItem = await _context.InventoryChoiceGroupItems
                    .Where(i => i.DeletedAt == null)
                    .Include(i => i.InventoryChoiceGroup)
                    .Include(i => i.InventoryItem)
                        .ThenInclude(ii => ii.Category)
                            .ThenInclude(c => c.CategoryGroup)
                    .FirstOrDefaultAsync(m => m.Id == id);

                // Return not found when the inventory choice group item does not exist.
                if (inventoryChoiceGroupItem == null)
                {
                    _logger.LogWarning("InventoryChoiceGroupItem Id {Id} not found for delete", id);
                    return NotFound();
                }

                return View(inventoryChoiceGroupItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete page for InventoryChoiceGroupItem Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the delete page.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: InventoryChoiceGroupItems/Delete/5
        /// <summary>
        /// Soft deletes an inventory choice group item.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            try
            {
                _logger.LogWarning("Soft deleting InventoryChoiceGroupItem Id {Id}", id);

                // Retrieve the active inventory choice group item targeted for soft delete.
                var inventoryChoiceGroupItem = await _context.InventoryChoiceGroupItems
                    .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

                // Return not found when the inventory choice group item does not exist.
                if (inventoryChoiceGroupItem == null)
                {
                    _logger.LogWarning("InventoryChoiceGroupItem Id {Id} not found during delete", id);
                    return NotFound();
                }

                // Apply soft-delete and audit values.
                inventoryChoiceGroupItem.DeletedAt = DateTime.UtcNow;
                inventoryChoiceGroupItem.UpdatedAt = DateTime.UtcNow;
                inventoryChoiceGroupItem.UpdatedByUserId = null; // Replace when auth integration is added.

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("InventoryChoiceGroupItem Id {Id} soft deleted", id);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error soft deleting InventoryChoiceGroupItem Id {Id}", id);

                    TempData["ErrorMessage"] = "Unable to delete inventory choice group item.";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting InventoryChoiceGroupItem Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the inventory choice group item.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        /// <summary>
        /// Populates the dropdowns for choice group and inventory item.
        /// </summary>
        private async Task PopulateDropdowns(ulong? selectedChoiceGroupId = null, ulong? selectedInventoryItemId = null)
        {
            _logger.LogDebug("Populating dropdowns for InventoryChoiceGroupItem");

            // Retrieve active choice groups for the dropdown list.
            var choiceGroups = await _context.InventoryChoiceGroups
                .Where(g => g.DeletedAt == null)
                .OrderBy(g => g.Name)
                .ToListAsync();

            // Retrieve active inventory items for the dropdown list.
            var inventoryItems = await _context.InventoryItems
                .Where(i => i.DeletedAt == null && i.Category.DeletedAt == null && i.Category.CategoryGroup.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .OrderBy(i => i.Category.CategoryGroup.Name)
                .ThenBy(i => i.Category.Name)
                .ThenBy(i => i.Name)
                .ToListAsync();

            // Build the choice group dropdown options.
            var choiceGroupOptions = choiceGroups
                .Select(g => new
                {
                    g.Id,
                    DisplayName = string.IsNullOrWhiteSpace(g.DisplayLabel) ? g.Name : g.DisplayLabel
                })
                .ToList();

            // Build the inventory item dropdown options.
            var inventoryItemOptions = inventoryItems
                .Select(i => new
                {
                    i.Id,
                    DisplayName = $"{i.Category.CategoryGroup.Name} - {i.Category.Name} - {i.Name}"
                })
                .ToList();

            // Store the dropdown options in ViewData.
            ViewData["InventoryChoiceGroupId"] = new SelectList(choiceGroupOptions, "Id", "DisplayName", selectedChoiceGroupId);
            ViewData["InventoryItemId"] = new SelectList(inventoryItemOptions, "Id", "DisplayName", selectedInventoryItemId);
        }

        /// <summary>
        /// Returns true if the non-deleted inventory choice group item exists.
        /// </summary>
        private async Task<bool> InventoryChoiceGroupItemExists(ulong id)
        {
            // Check whether the requested active inventory choice group item still exists.
            return await _context.InventoryChoiceGroupItems.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyInventoryChoiceGroupItemValidationAsync(InventoryChoiceGroupItem model, ulong? currentId = null)
        {
            // Validate that the selected choice group exists and is not deleted.
            var choiceGroupExists = await _context.InventoryChoiceGroups
                .AnyAsync(g => g.Id == model.InventoryChoiceGroupId && g.DeletedAt == null);

            if (!choiceGroupExists)
            {
                ModelState.AddModelError(nameof(InventoryChoiceGroupItem.InventoryChoiceGroupId), "Select a valid choice group.");
            }

            // Validate that the selected inventory item exists and is not deleted.
            var inventoryItemExists = await _context.InventoryItems
                .AnyAsync(i =>
                    i.Id == model.InventoryItemId &&
                    i.DeletedAt == null &&
                    i.Category.DeletedAt == null &&
                    i.Category.CategoryGroup.DeletedAt == null);

            if (!inventoryItemExists)
            {
                ModelState.AddModelError(nameof(InventoryChoiceGroupItem.InventoryItemId), "Select a valid inventory item.");
            }

            // Prevent duplicate active item assignments within the same choice group.
            var duplicateExists = await _context.InventoryChoiceGroupItems
                .AnyAsync(i =>
                    i.DeletedAt == null &&
                    i.Id != currentId &&
                    i.InventoryChoiceGroupId == model.InventoryChoiceGroupId &&
                    i.InventoryItemId == model.InventoryItemId);

            if (duplicateExists)
            {
                ModelState.AddModelError(nameof(InventoryChoiceGroupItem.InventoryItemId), "This inventory item is already assigned to the selected choice group.");
            }
        }
    }
}