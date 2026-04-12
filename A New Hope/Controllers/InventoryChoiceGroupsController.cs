using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages create, read, update, and soft delete operations for inventory choice groups.
    /// </summary>
    public class InventoryChoiceGroupsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryChoiceGroupsController> _logger;

        /// <summary>
        /// Creates the controller with the required database context and logger.
        /// </summary>
        public InventoryChoiceGroupsController(ApplicationDbContext context, ILogger<InventoryChoiceGroupsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: InventoryChoiceGroups
        /// <summary>
        /// Displays all non-deleted inventory choice groups.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Loading InventoryChoiceGroups Index page");

                // Retrieve active inventory choice groups for display.
                var inventoryChoiceGroups = await _context.InventoryChoiceGroups
                    .Where(g => g.DeletedAt == null)
                    .OrderBy(g => g.Name)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} inventory choice groups", inventoryChoiceGroups.Count);

                return View(inventoryChoiceGroups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inventory choice groups list");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading inventory choice groups.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: InventoryChoiceGroups/Details/5
        /// <summary>
        /// Displays details for a single non-deleted inventory choice group.
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

                _logger.LogInformation("Fetching details for InventoryChoiceGroup Id {Id}", id);

                // Retrieve the requested active inventory choice group with active related items.
                var inventoryChoiceGroup = await _context.InventoryChoiceGroups
                    .Where(g => g.DeletedAt == null)
                    .Include(g => g.InventoryChoiceGroupItems
                        .Where(i => i.DeletedAt == null))
                        .ThenInclude(i => i.InventoryItem)
                            .ThenInclude(ii => ii.Category)
                                .ThenInclude(c => c.CategoryGroup)
                    .FirstOrDefaultAsync(m => m.Id == id);

                // Return not found when the inventory choice group does not exist.
                if (inventoryChoiceGroup == null)
                {
                    _logger.LogWarning("InventoryChoiceGroup Id {Id} not found", id);
                    return NotFound();
                }

                return View(inventoryChoiceGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for InventoryChoiceGroup Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading inventory choice group details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: InventoryChoiceGroups/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public IActionResult Create()
        {
            try
            {
                _logger.LogInformation("Loading Create InventoryChoiceGroup page");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Create InventoryChoiceGroup page");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the create form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: InventoryChoiceGroups/Create
        /// <summary>
        /// Creates a new inventory choice group after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,MaxSelections,DisplayLabel,SortOrder,IsActive")] InventoryChoiceGroup inventoryChoiceGroup)
        {
            try
            {
                _logger.LogInformation("Attempting to create InventoryChoiceGroup {Name}", inventoryChoiceGroup.Name);

                // Remove navigation properties that are not posted by the form.
                ModelState.Remove(nameof(InventoryChoiceGroup.CreatedByUser));
                ModelState.Remove(nameof(InventoryChoiceGroup.UpdatedByUser));
                ModelState.Remove(nameof(InventoryChoiceGroup.InventoryChoiceGroupItems));

                // Normalize incoming values before business-rule validation.
                NormalizeInventoryChoiceGroup(inventoryChoiceGroup);
                await ApplyInventoryChoiceGroupValidationAsync(inventoryChoiceGroup);

                // Return the form when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create InventoryChoiceGroup failed validation for {Name}", inventoryChoiceGroup.Name);
                    return View(inventoryChoiceGroup);
                }

                // Set audit fields for the new inventory choice group record.
                var now = DateTime.UtcNow;
                inventoryChoiceGroup.CreatedAt = now;
                inventoryChoiceGroup.UpdatedAt = now;
                inventoryChoiceGroup.CreatedByUserId = null; // Replace when auth integration is added.
                inventoryChoiceGroup.UpdatedByUserId = null; // Replace when auth integration is added.

                // Queue the new inventory choice group for insert.
                _context.Add(inventoryChoiceGroup);

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("InventoryChoiceGroup {Name} created successfully", inventoryChoiceGroup.Name);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error creating InventoryChoiceGroup {Name}", inventoryChoiceGroup.Name);

                    ModelState.AddModelError("", "Unable to save inventory choice group.");
                    return View(inventoryChoiceGroup);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating InventoryChoiceGroup {Name}", inventoryChoiceGroup?.Name);
                ModelState.AddModelError("", "An unexpected error occurred while creating the inventory choice group.");
                return View(inventoryChoiceGroup);
            }
        }

        // GET: InventoryChoiceGroups/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted inventory choice group.
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

                _logger.LogInformation("Loading Edit page for InventoryChoiceGroup Id {Id}", id);

                // Retrieve the requested active inventory choice group for editing.
                var inventoryChoiceGroup = await _context.InventoryChoiceGroups
                    .FirstOrDefaultAsync(g => g.Id == id && g.DeletedAt == null);

                // Return not found when the inventory choice group does not exist.
                if (inventoryChoiceGroup == null)
                {
                    _logger.LogWarning("InventoryChoiceGroup Id {Id} not found for edit", id);
                    return NotFound();
                }

                return View(inventoryChoiceGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit page for InventoryChoiceGroup Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: InventoryChoiceGroups/Edit/5
        /// <summary>
        /// Updates an existing inventory choice group after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Name,MaxSelections,DisplayLabel,SortOrder,IsActive")] InventoryChoiceGroup formModel)
        {
            try
            {
                _logger.LogInformation("Attempting to edit InventoryChoiceGroup Id {Id}", id);

                // Ensure the route id matches the posted model id.
                if (id != formModel.Id)
                {
                    _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                    return NotFound();
                }

                // Remove navigation properties that are not posted by the form.
                ModelState.Remove(nameof(InventoryChoiceGroup.CreatedByUser));
                ModelState.Remove(nameof(InventoryChoiceGroup.UpdatedByUser));
                ModelState.Remove(nameof(InventoryChoiceGroup.InventoryChoiceGroupItems));

                // Normalize incoming values before business-rule validation.
                NormalizeInventoryChoiceGroup(formModel);
                await ApplyInventoryChoiceGroupValidationAsync(formModel, formModel.Id);

                // Return the form when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Edit InventoryChoiceGroup failed validation for Id {Id}", id);
                    return View(formModel);
                }

                // Retrieve the existing active inventory choice group record.
                var existing = await _context.InventoryChoiceGroups
                    .FirstOrDefaultAsync(g => g.Id == id && g.DeletedAt == null);

                // Return not found when the target record no longer exists.
                if (existing == null)
                {
                    _logger.LogWarning("InventoryChoiceGroup Id {Id} not found during edit save", id);
                    return NotFound();
                }

                // Copy validated form values into the tracked entity.
                existing.Name = formModel.Name;
                existing.MaxSelections = formModel.MaxSelections;
                existing.DisplayLabel = formModel.DisplayLabel;
                existing.IsActive = formModel.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = null; // Replace when auth integration is added.

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("InventoryChoiceGroup Id {Id} updated successfully", id);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Check whether the record was deleted during the edit attempt.
                    if (!await InventoryChoiceGroupExists(formModel.Id))
                    {
                        _logger.LogWarning("InventoryChoiceGroup Id {Id} no longer exists during concurrency check", id);
                        return NotFound();
                    }

                    _logger.LogError(ex, "Concurrency error updating InventoryChoiceGroup Id {Id}", id);
                    throw;
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error updating InventoryChoiceGroup Id {Id}", id);

                    ModelState.AddModelError("", "Unable to save changes.");
                    return View(formModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error editing InventoryChoiceGroup Id {Id}", id);
                ModelState.AddModelError("", "An unexpected error occurred while updating the inventory choice group.");
                return View(formModel);
            }
        }

        // GET: InventoryChoiceGroups/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted inventory choice group.
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

                _logger.LogInformation("Loading Delete confirmation for InventoryChoiceGroup Id {Id}", id);

                // Retrieve the requested active inventory choice group for delete confirmation.
                var inventoryChoiceGroup = await _context.InventoryChoiceGroups
                    .Where(g => g.DeletedAt == null)
                    .FirstOrDefaultAsync(m => m.Id == id);

                // Return not found when the inventory choice group does not exist.
                if (inventoryChoiceGroup == null)
                {
                    _logger.LogWarning("InventoryChoiceGroup Id {Id} not found for delete", id);
                    return NotFound();
                }

                return View(inventoryChoiceGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete page for InventoryChoiceGroup Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the delete page.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: InventoryChoiceGroups/Delete/5
        /// <summary>
        /// Soft deletes an inventory choice group.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            try
            {
                _logger.LogWarning("Soft deleting InventoryChoiceGroup Id {Id}", id);

                // Retrieve the active inventory choice group targeted for soft delete.
                var inventoryChoiceGroup = await _context.InventoryChoiceGroups
                    .FirstOrDefaultAsync(g => g.Id == id && g.DeletedAt == null);

                // Return not found when the inventory choice group does not exist.
                if (inventoryChoiceGroup == null)
                {
                    _logger.LogWarning("InventoryChoiceGroup Id {Id} not found during delete", id);
                    return NotFound();
                }

                // Apply soft-delete and audit values.
                inventoryChoiceGroup.DeletedAt = DateTime.UtcNow;
                inventoryChoiceGroup.UpdatedAt = DateTime.UtcNow;
                inventoryChoiceGroup.UpdatedByUserId = null; // Replace when auth integration is added.

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("InventoryChoiceGroup Id {Id} soft deleted", id);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error soft deleting InventoryChoiceGroup Id {Id}", id);

                    TempData["ErrorMessage"] = "Unable to delete inventory choice group.";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting InventoryChoiceGroup Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the inventory choice group.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        /// <summary>
        /// Returns true if the non-deleted inventory choice group exists.
        /// </summary>
        private async Task<bool> InventoryChoiceGroupExists(ulong id)
        {
            // Check whether the requested active inventory choice group still exists.
            return await _context.InventoryChoiceGroups.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Trims strings and normalizes required values.
        /// </summary>
        private static void NormalizeInventoryChoiceGroup(InventoryChoiceGroup model)
        {
            // Normalize and trim required string values.
            model.Name = model.Name?.Trim() ?? string.Empty;
            model.DisplayLabel = string.IsNullOrWhiteSpace(model.DisplayLabel)
                ? null
                : model.DisplayLabel.Trim();
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyInventoryChoiceGroupValidationAsync(InventoryChoiceGroup model, ulong? currentId = null)
        {
            // Require a group name.
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(InventoryChoiceGroup.Name), "Group name is required.");
            }

            // Require at least one letter or number in the group name.
            if (!string.IsNullOrWhiteSpace(model.Name) && !ContainsLetterOrDigit(model.Name))
            {
                ModelState.AddModelError(nameof(InventoryChoiceGroup.Name), "Group name must contain letters or numbers.");
            }

            // Require at least one allowed selection.
            if (model.MaxSelections < 1)
            {
                ModelState.AddModelError(nameof(InventoryChoiceGroup.MaxSelections), "Maximum selections must be at least 1.");
            }

            // Prevent duplicate active group names.
            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                var normalizedName = model.Name.ToLower();

                var duplicateExists = await _context.InventoryChoiceGroups
                    .AnyAsync(g =>
                        g.DeletedAt == null &&
                        g.Id != currentId &&
                        g.Name.ToLower() == normalizedName);

                if (duplicateExists)
                {
                    ModelState.AddModelError(nameof(InventoryChoiceGroup.Name), "A choice group with this name already exists.");
                }
            }
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