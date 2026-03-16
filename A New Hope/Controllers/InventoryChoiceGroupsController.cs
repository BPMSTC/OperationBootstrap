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
            _logger.LogInformation("Loading InventoryChoiceGroups Index page");

            var inventoryChoiceGroups = await _context.InventoryChoiceGroups
                .Where(g => g.DeletedAt == null)
                .OrderBy(g => g.SortOrder)
                .ThenBy(g => g.Name)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} inventory choice groups", inventoryChoiceGroups.Count);

            return View(inventoryChoiceGroups);
        }

        // GET: InventoryChoiceGroups/Details/5
        /// <summary>
        /// Displays details for a single non-deleted inventory choice group.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for InventoryChoiceGroup Id {Id}", id);

            var inventoryChoiceGroup = await _context.InventoryChoiceGroups
                .Where(g => g.DeletedAt == null)
                .Include(g => g.InventoryChoiceGroupItems
                    .Where(i => i.DeletedAt == null))
                    .ThenInclude(i => i.InventoryItem)
                        .ThenInclude(ii => ii.Category)
                            .ThenInclude(c => c.CategoryGroup)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inventoryChoiceGroup == null)
            {
                _logger.LogWarning("InventoryChoiceGroup Id {Id} not found", id);
                return NotFound();
            }

            return View(inventoryChoiceGroup);
        }

        // GET: InventoryChoiceGroups/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public IActionResult Create()
        {
            _logger.LogInformation("Loading Create InventoryChoiceGroup page");
            return View();
        }

        // POST: InventoryChoiceGroups/Create
        /// <summary>
        /// Creates a new inventory choice group after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,MaxSelections,DisplayLabel,SortOrder,IsActive")] InventoryChoiceGroup inventoryChoiceGroup)
        {
            _logger.LogInformation("Attempting to create InventoryChoiceGroup {Name}", inventoryChoiceGroup.Name);

            ModelState.Remove(nameof(InventoryChoiceGroup.CreatedByUser));
            ModelState.Remove(nameof(InventoryChoiceGroup.UpdatedByUser));
            ModelState.Remove(nameof(InventoryChoiceGroup.InventoryChoiceGroupItems));

            NormalizeInventoryChoiceGroup(inventoryChoiceGroup);
            await ApplyInventoryChoiceGroupValidationAsync(inventoryChoiceGroup);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create InventoryChoiceGroup failed validation for {Name}", inventoryChoiceGroup.Name);
                return View(inventoryChoiceGroup);
            }

            var now = DateTime.UtcNow;
            inventoryChoiceGroup.CreatedAt = now;
            inventoryChoiceGroup.UpdatedAt = now;
            inventoryChoiceGroup.CreatedByUserId = null; // Replace when auth integration is added.
            inventoryChoiceGroup.UpdatedByUserId = null; // Replace when auth integration is added.

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

        // GET: InventoryChoiceGroups/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted inventory choice group.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for InventoryChoiceGroup Id {Id}", id);

            var inventoryChoiceGroup = await _context.InventoryChoiceGroups
                .FirstOrDefaultAsync(g => g.Id == id && g.DeletedAt == null);

            if (inventoryChoiceGroup == null)
            {
                _logger.LogWarning("InventoryChoiceGroup Id {Id} not found for edit", id);
                return NotFound();
            }

            return View(inventoryChoiceGroup);
        }

        // POST: InventoryChoiceGroups/Edit/5
        /// <summary>
        /// Updates an existing inventory choice group after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Name,MaxSelections,DisplayLabel,SortOrder,IsActive")] InventoryChoiceGroup formModel)
        {
            _logger.LogInformation("Attempting to edit InventoryChoiceGroup Id {Id}", id);

            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            ModelState.Remove(nameof(InventoryChoiceGroup.CreatedByUser));
            ModelState.Remove(nameof(InventoryChoiceGroup.UpdatedByUser));
            ModelState.Remove(nameof(InventoryChoiceGroup.InventoryChoiceGroupItems));

            NormalizeInventoryChoiceGroup(formModel);
            await ApplyInventoryChoiceGroupValidationAsync(formModel, formModel.Id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit InventoryChoiceGroup failed validation for Id {Id}", id);
                return View(formModel);
            }

            var existing = await _context.InventoryChoiceGroups
                .FirstOrDefaultAsync(g => g.Id == id && g.DeletedAt == null);

            if (existing == null)
            {
                _logger.LogWarning("InventoryChoiceGroup Id {Id} not found during edit save", id);
                return NotFound();
            }

            existing.Name = formModel.Name;
            existing.MaxSelections = formModel.MaxSelections;
            existing.DisplayLabel = formModel.DisplayLabel;
            existing.SortOrder = formModel.SortOrder;
            existing.IsActive = formModel.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // Replace when auth integration is added.

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("InventoryChoiceGroup Id {Id} updated successfully", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await InventoryChoiceGroupExists(formModel.Id))
                {
                    _logger.LogWarning("InventoryChoiceGroup Id {Id} no longer exists during concurrency check", id);
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating InventoryChoiceGroup Id {Id}", id);

                ModelState.AddModelError("", "Unable to save changes.");
                return View(formModel);
            }
        }

        // GET: InventoryChoiceGroups/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted inventory choice group.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for InventoryChoiceGroup Id {Id}", id);

            var inventoryChoiceGroup = await _context.InventoryChoiceGroups
                .Where(g => g.DeletedAt == null)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inventoryChoiceGroup == null)
            {
                _logger.LogWarning("InventoryChoiceGroup Id {Id} not found for delete", id);
                return NotFound();
            }

            return View(inventoryChoiceGroup);
        }

        // POST: InventoryChoiceGroups/Delete/5
        /// <summary>
        /// Soft deletes an inventory choice group.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting InventoryChoiceGroup Id {Id}", id);

            var inventoryChoiceGroup = await _context.InventoryChoiceGroups
                .FirstOrDefaultAsync(g => g.Id == id && g.DeletedAt == null);

            if (inventoryChoiceGroup == null)
            {
                _logger.LogWarning("InventoryChoiceGroup Id {Id} not found during delete", id);
                return NotFound();
            }

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

        /// <summary>
        /// Returns true if the non-deleted inventory choice group exists.
        /// </summary>
        private async Task<bool> InventoryChoiceGroupExists(ulong id)
        {
            return await _context.InventoryChoiceGroups.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Trims strings and normalizes required values.
        /// </summary>
        private static void NormalizeInventoryChoiceGroup(InventoryChoiceGroup model)
        {
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
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(InventoryChoiceGroup.Name), "Group name is required.");
            }

            if (!string.IsNullOrWhiteSpace(model.Name) && !ContainsLetterOrDigit(model.Name))
            {
                ModelState.AddModelError(nameof(InventoryChoiceGroup.Name), "Group name must contain letters or numbers.");
            }

            if (model.MaxSelections < 1)
            {
                ModelState.AddModelError(nameof(InventoryChoiceGroup.MaxSelections), "Maximum selections must be at least 1.");
            }

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
            return value.Any(char.IsLetterOrDigit);
        }
    }
}