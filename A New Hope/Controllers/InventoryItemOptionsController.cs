using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages create, read, update, and soft delete operations for inventory item options.
    /// </summary>
    public class InventoryItemOptionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryItemOptionsController> _logger;

        /// <summary>
        /// Creates the controller with the required database context and logger.
        /// </summary>
        public InventoryItemOptionsController(ApplicationDbContext context, ILogger<InventoryItemOptionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: InventoryItemOptions
        /// <summary>
        /// Displays all non-deleted inventory item options.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Loading InventoryItemOptions Index page");

                // Retrieve active inventory item options for display.
                var inventoryItemOptions = await _context.InventoryItemOptions
                    .Where(o => o.DeletedAt == null)
                    .Include(o => o.InventoryItem)
                        .ThenInclude(i => i.Category)
                            .ThenInclude(c => c.CategoryGroup)
                    .OrderBy(o => o.InventoryItem.Name)
                    .ThenBy(o => o.SortOrder)
                    .ThenBy(o => o.Name)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} inventory item options", inventoryItemOptions.Count);

                return View(inventoryItemOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inventory item options list");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading inventory item options.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: InventoryItemOptions/Details/5
        /// <summary>
        /// Displays details for a single non-deleted inventory item option.
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

                _logger.LogInformation("Fetching details for InventoryItemOption Id {Id}", id);

                // Retrieve the requested active inventory item option.
                var inventoryItemOption = await _context.InventoryItemOptions
                    .Where(o => o.DeletedAt == null)
                    .Include(o => o.InventoryItem)
                        .ThenInclude(i => i.Category)
                            .ThenInclude(c => c.CategoryGroup)
                    .FirstOrDefaultAsync(m => m.Id == id);

                // Return not found when the inventory item option does not exist.
                if (inventoryItemOption == null)
                {
                    _logger.LogWarning("InventoryItemOption Id {Id} not found", id);
                    return NotFound();
                }

                return View(inventoryItemOption);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for InventoryItemOption Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading inventory item option details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: InventoryItemOptions/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            try
            {
                _logger.LogInformation("Loading Create InventoryItemOption page");

                // Populate dropdown values for the create form.
                await PopulateDropdowns();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Create InventoryItemOption page");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the create form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: InventoryItemOptions/Create
        /// <summary>
        /// Creates a new inventory item option after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("InventoryItemId,Name,SortOrder,IsActive")] InventoryItemOption inventoryItemOption)
        {
            try
            {
                _logger.LogInformation("Attempting to create InventoryItemOption {Name}", inventoryItemOption.Name);

                // Remove navigation properties that are not posted by the form.
                ModelState.Remove(nameof(InventoryItemOption.InventoryItem));
                ModelState.Remove(nameof(InventoryItemOption.CreatedByUser));
                ModelState.Remove(nameof(InventoryItemOption.UpdatedByUser));

                // Normalize incoming values before business-rule validation.
                NormalizeInventoryItemOption(inventoryItemOption);
                await ApplyInventoryItemOptionValidationAsync(inventoryItemOption);

                // Return the form when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create InventoryItemOption failed validation for {Name}", inventoryItemOption.Name);
                    await PopulateDropdowns(inventoryItemOption.InventoryItemId);
                    return View(inventoryItemOption);
                }

                // Set audit fields for the new inventory item option record.
                var now = DateTime.UtcNow;
                inventoryItemOption.CreatedAt = now;
                inventoryItemOption.UpdatedAt = now;
                inventoryItemOption.CreatedByUserId = null; // Replace when auth integration is added.
                inventoryItemOption.UpdatedByUserId = null; // Replace when auth integration is added.

                // Queue the new inventory item option for insert.
                _context.Add(inventoryItemOption);

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("InventoryItemOption {Name} created successfully", inventoryItemOption.Name);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error creating InventoryItemOption {Name}", inventoryItemOption.Name);

                    ModelState.AddModelError("", "Unable to save inventory item option.");
                    await PopulateDropdowns(inventoryItemOption.InventoryItemId);
                    return View(inventoryItemOption);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating InventoryItemOption {Name}", inventoryItemOption?.Name);
                ModelState.AddModelError("", "An unexpected error occurred while creating the inventory item option.");

                await PopulateDropdowns(inventoryItemOption?.InventoryItemId);
                return View(inventoryItemOption ?? new InventoryItemOption());
            }
        }

        // GET: InventoryItemOptions/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted inventory item option.
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

                _logger.LogInformation("Loading Edit page for InventoryItemOption Id {Id}", id);

                // Retrieve the requested active inventory item option for editing.
                var inventoryItemOption = await _context.InventoryItemOptions
                    .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

                // Return not found when the inventory item option does not exist.
                if (inventoryItemOption == null)
                {
                    _logger.LogWarning("InventoryItemOption Id {Id} not found for edit", id);
                    return NotFound();
                }

                // Populate dropdown values using the current record selection.
                await PopulateDropdowns(inventoryItemOption.InventoryItemId);
                return View(inventoryItemOption);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit page for InventoryItemOption Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: InventoryItemOptions/Edit/5
        /// <summary>
        /// Updates an existing inventory item option after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,InventoryItemId,Name,SortOrder,IsActive")] InventoryItemOption formModel)
        {
            try
            {
                _logger.LogInformation("Attempting to edit InventoryItemOption Id {Id}", id);

                // Ensure the route id matches the posted model id.
                if (id != formModel.Id)
                {
                    _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                    return NotFound();
                }

                // Remove navigation properties that are not posted by the form.
                ModelState.Remove(nameof(InventoryItemOption.InventoryItem));
                ModelState.Remove(nameof(InventoryItemOption.CreatedByUser));
                ModelState.Remove(nameof(InventoryItemOption.UpdatedByUser));

                // Normalize incoming values before business-rule validation.
                NormalizeInventoryItemOption(formModel);
                await ApplyInventoryItemOptionValidationAsync(formModel, formModel.Id);

                // Return the form with dropdowns restored when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Edit InventoryItemOption failed validation for Id {Id}", id);
                    await PopulateDropdowns(formModel.InventoryItemId);
                    return View(formModel);
                }

                // Retrieve the existing active inventory item option record.
                var existing = await _context.InventoryItemOptions
                    .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

                // Return not found when the target record no longer exists.
                if (existing == null)
                {
                    _logger.LogWarning("InventoryItemOption Id {Id} not found during edit save", id);
                    return NotFound();
                }

                // Copy validated form values into the tracked entity.
                existing.InventoryItemId = formModel.InventoryItemId;
                existing.Name = formModel.Name;
                existing.SortOrder = formModel.SortOrder;
                existing.IsActive = formModel.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = null; // Replace when auth integration is added.

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("InventoryItemOption Id {Id} updated successfully", id);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Check whether the record was deleted during the edit attempt.
                    if (!await InventoryItemOptionExists(formModel.Id))
                    {
                        _logger.LogWarning("InventoryItemOption Id {Id} no longer exists during concurrency check", id);
                        return NotFound();
                    }

                    _logger.LogError(ex, "Concurrency error updating InventoryItemOption Id {Id}", id);
                    throw;
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error updating InventoryItemOption Id {Id}", id);

                    ModelState.AddModelError("", "Unable to save changes.");
                    await PopulateDropdowns(formModel.InventoryItemId);
                    return View(formModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error editing InventoryItemOption Id {Id}", id);
                ModelState.AddModelError("", "An unexpected error occurred while updating the inventory item option.");
                await PopulateDropdowns(formModel.InventoryItemId);
                return View(formModel);
            }
        }

        // GET: InventoryItemOptions/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted inventory item option.
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

                _logger.LogInformation("Loading Delete confirmation for InventoryItemOption Id {Id}", id);

                // Retrieve the requested active inventory item option.
                var inventoryItemOption = await _context.InventoryItemOptions
                    .Where(o => o.DeletedAt == null)
                    .Include(o => o.InventoryItem)
                        .ThenInclude(i => i.Category)
                            .ThenInclude(c => c.CategoryGroup)
                    .FirstOrDefaultAsync(m => m.Id == id);

                // Return not found when the inventory item option does not exist.
                if (inventoryItemOption == null)
                {
                    _logger.LogWarning("InventoryItemOption Id {Id} not found for delete", id);
                    return NotFound();
                }

                return View(inventoryItemOption);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete page for InventoryItemOption Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the delete page.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: InventoryItemOptions/Delete/5
        /// <summary>
        /// Soft deletes an inventory item option.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            try
            {
                _logger.LogWarning("Soft deleting InventoryItemOption Id {Id}", id);

                // Retrieve the active inventory item option targeted for soft delete.
                var inventoryItemOption = await _context.InventoryItemOptions
                    .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

                // Return not found when the inventory item option does not exist.
                if (inventoryItemOption == null)
                {
                    _logger.LogWarning("InventoryItemOption Id {Id} not found during delete", id);
                    return NotFound();
                }

                // Apply soft-delete and audit values.
                inventoryItemOption.DeletedAt = DateTime.UtcNow;
                inventoryItemOption.UpdatedAt = DateTime.UtcNow;
                inventoryItemOption.UpdatedByUserId = null; // Replace when auth integration is added.

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("InventoryItemOption Id {Id} soft deleted", id);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error soft deleting InventoryItemOption Id {Id}", id);

                    TempData["ErrorMessage"] = "Unable to delete inventory item option.";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting InventoryItemOption Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the inventory item option.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        /// <summary>
        /// Populates the inventory item dropdown for the create and edit forms.
        /// </summary>
        private async Task PopulateDropdowns(ulong? selectedInventoryItemId = null)
        {
            _logger.LogDebug("Populating InventoryItem dropdown for InventoryItemOption");

            // Retrieve active inventory items for the dropdown list.
            var inventoryItems = await _context.InventoryItems
                .Where(i => i.DeletedAt == null && i.Category.DeletedAt == null && i.Category.CategoryGroup.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .OrderBy(i => i.Category.CategoryGroup.Name)
                .ThenBy(i => i.Category.Name)
                .ThenBy(i => i.Name)
                .ToListAsync();

            // Build the inventory item dropdown options.
            var inventoryItemOptions = inventoryItems
                .Select(i => new
                {
                    i.Id,
                    DisplayName = $"{i.Category.CategoryGroup.Name} - {i.Category.Name} - {i.Name}"
                })
                .ToList();

            _logger.LogDebug("Loaded {Count} inventory items for dropdown", inventoryItemOptions.Count);

            // Store the dropdown options in ViewData.
            ViewData["InventoryItemId"] = new SelectList(inventoryItemOptions, "Id", "DisplayName", selectedInventoryItemId);
        }

        /// <summary>
        /// Returns true if the non-deleted inventory item option exists.
        /// </summary>
        private async Task<bool> InventoryItemOptionExists(ulong id)
        {
            // Check whether the requested active inventory item option still exists.
            return await _context.InventoryItemOptions.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Trims strings and normalizes required values.
        /// </summary>
        private static void NormalizeInventoryItemOption(InventoryItemOption model)
        {
            // Normalize and trim the required option name.
            model.Name = model.Name?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyInventoryItemOptionValidationAsync(InventoryItemOption model, ulong? currentId = null)
        {
            // Validate that the selected inventory item exists and is not deleted.
            var inventoryItemExists = await _context.InventoryItems
                .AnyAsync(i =>
                    i.Id == model.InventoryItemId &&
                    i.DeletedAt == null &&
                    i.Category.DeletedAt == null &&
                    i.Category.CategoryGroup.DeletedAt == null);

            if (!inventoryItemExists)
            {
                ModelState.AddModelError(nameof(InventoryItemOption.InventoryItemId), "Select a valid inventory item.");
            }

            // Require an option name.
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(InventoryItemOption.Name), "Option name is required.");
            }

            // Require at least one letter or number in the option name.
            if (!string.IsNullOrWhiteSpace(model.Name) &&
                !AddressValidation.ContainsLetterOrDigit(model.Name))
            {
                ModelState.AddModelError(nameof(InventoryItemOption.Name), "Option name must contain letters or numbers.");
            }

            // Prevent duplicate active option names within the same inventory item.
            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                var normalizedName = model.Name.ToLower();

                var duplicateExists = await _context.InventoryItemOptions
                    .AnyAsync(o =>
                        o.DeletedAt == null &&
                        o.Id != currentId &&
                        o.InventoryItemId == model.InventoryItemId &&
                        o.Name.ToLower() == normalizedName);

                if (duplicateExists)
                {
                    ModelState.AddModelError(nameof(InventoryItemOption.Name), "An option with this name already exists for the selected inventory item.");
                }
            }
        }
    }
}