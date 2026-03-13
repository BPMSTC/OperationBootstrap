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
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading InventoryChoiceGroupItems Index page");

            var inventoryChoiceGroupItems = await _context.InventoryChoiceGroupItems
                .Where(i => i.DeletedAt == null)
                .Include(i => i.InventoryChoiceGroup)
                .Include(i => i.InventoryItem)
                    .ThenInclude(ii => ii.Category)
                        .ThenInclude(c => c.CategoryGroup)
                .OrderBy(i => i.InventoryChoiceGroup.Name)
                .ThenBy(i => i.SortOrder)
                .ThenBy(i => i.InventoryItem.Name)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} inventory choice group items", inventoryChoiceGroupItems.Count);

            return View(inventoryChoiceGroupItems);
        }

        // GET: InventoryChoiceGroupItems/Details/5
        /// <summary>
        /// Displays details for a single non-deleted inventory choice group item.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for InventoryChoiceGroupItem Id {Id}", id);

            var inventoryChoiceGroupItem = await _context.InventoryChoiceGroupItems
                .Where(i => i.DeletedAt == null)
                .Include(i => i.InventoryChoiceGroup)
                .Include(i => i.InventoryItem)
                    .ThenInclude(ii => ii.Category)
                        .ThenInclude(c => c.CategoryGroup)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inventoryChoiceGroupItem == null)
            {
                _logger.LogWarning("InventoryChoiceGroupItem Id {Id} not found", id);
                return NotFound();
            }

            return View(inventoryChoiceGroupItem);
        }

        // GET: InventoryChoiceGroupItems/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create InventoryChoiceGroupItem page");

            await PopulateDropdowns();
            return View();
        }

        // POST: InventoryChoiceGroupItems/Create
        /// <summary>
        /// Creates a new inventory choice group item after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("InventoryChoiceGroupId,InventoryItemId,SortOrder,IsActive")] InventoryChoiceGroupItem inventoryChoiceGroupItem)
        {
            _logger.LogInformation("Attempting to create InventoryChoiceGroupItem");

            ModelState.Remove(nameof(InventoryChoiceGroupItem.InventoryChoiceGroup));
            ModelState.Remove(nameof(InventoryChoiceGroupItem.InventoryItem));
            ModelState.Remove(nameof(InventoryChoiceGroupItem.CreatedByUser));
            ModelState.Remove(nameof(InventoryChoiceGroupItem.UpdatedByUser));

            await ApplyInventoryChoiceGroupItemValidationAsync(inventoryChoiceGroupItem);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create InventoryChoiceGroupItem failed validation");
                await PopulateDropdowns(inventoryChoiceGroupItem.InventoryChoiceGroupId, inventoryChoiceGroupItem.InventoryItemId);
                return View(inventoryChoiceGroupItem);
            }

            var now = DateTime.UtcNow;
            inventoryChoiceGroupItem.CreatedAt = now;
            inventoryChoiceGroupItem.UpdatedAt = now;
            inventoryChoiceGroupItem.CreatedByUserId = null; // Replace when auth integration is added.
            inventoryChoiceGroupItem.UpdatedByUserId = null; // Replace when auth integration is added.

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

        // GET: InventoryChoiceGroupItems/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted inventory choice group item.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for InventoryChoiceGroupItem Id {Id}", id);

            var inventoryChoiceGroupItem = await _context.InventoryChoiceGroupItems
                .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

            if (inventoryChoiceGroupItem == null)
            {
                _logger.LogWarning("InventoryChoiceGroupItem Id {Id} not found for edit", id);
                return NotFound();
            }

            await PopulateDropdowns(inventoryChoiceGroupItem.InventoryChoiceGroupId, inventoryChoiceGroupItem.InventoryItemId);
            return View(inventoryChoiceGroupItem);
        }

        // POST: InventoryChoiceGroupItems/Edit/5
        /// <summary>
        /// Updates an existing inventory choice group item after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,InventoryChoiceGroupId,InventoryItemId,SortOrder,IsActive")] InventoryChoiceGroupItem formModel)
        {
            _logger.LogInformation("Attempting to edit InventoryChoiceGroupItem Id {Id}", id);

            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            ModelState.Remove(nameof(InventoryChoiceGroupItem.InventoryChoiceGroup));
            ModelState.Remove(nameof(InventoryChoiceGroupItem.InventoryItem));
            ModelState.Remove(nameof(InventoryChoiceGroupItem.CreatedByUser));
            ModelState.Remove(nameof(InventoryChoiceGroupItem.UpdatedByUser));

            await ApplyInventoryChoiceGroupItemValidationAsync(formModel, formModel.Id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit InventoryChoiceGroupItem failed validation for Id {Id}", id);
                await PopulateDropdowns(formModel.InventoryChoiceGroupId, formModel.InventoryItemId);
                return View(formModel);
            }

            var existing = await _context.InventoryChoiceGroupItems
                .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

            if (existing == null)
            {
                _logger.LogWarning("InventoryChoiceGroupItem Id {Id} not found during edit save", id);
                return NotFound();
            }

            existing.InventoryChoiceGroupId = formModel.InventoryChoiceGroupId;
            existing.InventoryItemId = formModel.InventoryItemId;
            existing.SortOrder = formModel.SortOrder;
            existing.IsActive = formModel.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // Replace when auth integration is added.

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("InventoryChoiceGroupItem Id {Id} updated successfully", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await InventoryChoiceGroupItemExists(formModel.Id))
                {
                    _logger.LogWarning("InventoryChoiceGroupItem Id {Id} no longer exists during concurrency check", id);
                    return NotFound();
                }

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

        // GET: InventoryChoiceGroupItems/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted inventory choice group item.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for InventoryChoiceGroupItem Id {Id}", id);

            var inventoryChoiceGroupItem = await _context.InventoryChoiceGroupItems
                .Where(i => i.DeletedAt == null)
                .Include(i => i.InventoryChoiceGroup)
                .Include(i => i.InventoryItem)
                    .ThenInclude(ii => ii.Category)
                        .ThenInclude(c => c.CategoryGroup)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inventoryChoiceGroupItem == null)
            {
                _logger.LogWarning("InventoryChoiceGroupItem Id {Id} not found for delete", id);
                return NotFound();
            }

            return View(inventoryChoiceGroupItem);
        }

        // POST: InventoryChoiceGroupItems/Delete/5
        /// <summary>
        /// Soft deletes an inventory choice group item.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting InventoryChoiceGroupItem Id {Id}", id);

            var inventoryChoiceGroupItem = await _context.InventoryChoiceGroupItems
                .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

            if (inventoryChoiceGroupItem == null)
            {
                _logger.LogWarning("InventoryChoiceGroupItem Id {Id} not found during delete", id);
                return NotFound();
            }

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

        /// <summary>
        /// Populates the dropdowns for choice group and inventory item.
        /// </summary>
        private async Task PopulateDropdowns(ulong? selectedChoiceGroupId = null, ulong? selectedInventoryItemId = null)
        {
            _logger.LogDebug("Populating dropdowns for InventoryChoiceGroupItem");

            var choiceGroups = await _context.InventoryChoiceGroups
                .Where(g => g.DeletedAt == null)
                .OrderBy(g => g.SortOrder)
                .ThenBy(g => g.Name)
                .ToListAsync();

            var inventoryItems = await _context.InventoryItems
                .Where(i => i.DeletedAt == null && i.Category.DeletedAt == null && i.Category.CategoryGroup.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .OrderBy(i => i.Category.CategoryGroup.Name)
                .ThenBy(i => i.Category.Name)
                .ThenBy(i => i.Name)
                .ToListAsync();

            var choiceGroupOptions = choiceGroups
                .Select(g => new
                {
                    g.Id,
                    DisplayName = string.IsNullOrWhiteSpace(g.DisplayLabel) ? g.Name : g.DisplayLabel
                })
                .ToList();

            var inventoryItemOptions = inventoryItems
                .Select(i => new
                {
                    i.Id,
                    DisplayName = $"{i.Category.CategoryGroup.Name} - {i.Category.Name} - {i.Name}"
                })
                .ToList();

            ViewData["InventoryChoiceGroupId"] = new SelectList(choiceGroupOptions, "Id", "DisplayName", selectedChoiceGroupId);
            ViewData["InventoryItemId"] = new SelectList(inventoryItemOptions, "Id", "DisplayName", selectedInventoryItemId);
        }

        /// <summary>
        /// Returns true if the non-deleted inventory choice group item exists.
        /// </summary>
        private async Task<bool> InventoryChoiceGroupItemExists(ulong id)
        {
            return await _context.InventoryChoiceGroupItems.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyInventoryChoiceGroupItemValidationAsync(InventoryChoiceGroupItem model, ulong? currentId = null)
        {
            var choiceGroupExists = await _context.InventoryChoiceGroups
                .AnyAsync(g => g.Id == model.InventoryChoiceGroupId && g.DeletedAt == null);

            if (!choiceGroupExists)
            {
                ModelState.AddModelError(nameof(InventoryChoiceGroupItem.InventoryChoiceGroupId), "Select a valid choice group.");
            }

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