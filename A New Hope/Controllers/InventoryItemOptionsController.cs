using A_New_Hope.Data;
using A_New_Hope.Models;
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
            _logger.LogInformation("Loading InventoryItemOptions Index page");

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

        // GET: InventoryItemOptions/Details/5
        /// <summary>
        /// Displays details for a single non-deleted inventory item option.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for InventoryItemOption Id {Id}", id);

            var inventoryItemOption = await _context.InventoryItemOptions
                .Where(o => o.DeletedAt == null)
                .Include(o => o.InventoryItem)
                    .ThenInclude(i => i.Category)
                        .ThenInclude(c => c.CategoryGroup)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inventoryItemOption == null)
            {
                _logger.LogWarning("InventoryItemOption Id {Id} not found", id);
                return NotFound();
            }

            return View(inventoryItemOption);
        }

        // GET: InventoryItemOptions/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create InventoryItemOption page");

            await PopulateDropdowns();
            return View();
        }

        // POST: InventoryItemOptions/Create
        /// <summary>
        /// Creates a new inventory item option after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("InventoryItemId,Name,SortOrder,IsActive")] InventoryItemOption inventoryItemOption)
        {
            _logger.LogInformation("Attempting to create InventoryItemOption {Name}", inventoryItemOption.Name);

            ModelState.Remove(nameof(InventoryItemOption.InventoryItem));
            ModelState.Remove(nameof(InventoryItemOption.CreatedByUser));
            ModelState.Remove(nameof(InventoryItemOption.UpdatedByUser));

            NormalizeInventoryItemOption(inventoryItemOption);
            await ApplyInventoryItemOptionValidationAsync(inventoryItemOption);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create InventoryItemOption failed validation for {Name}", inventoryItemOption.Name);
                await PopulateDropdowns(inventoryItemOption.InventoryItemId);
                return View(inventoryItemOption);
            }

            var now = DateTime.UtcNow;
            inventoryItemOption.CreatedAt = now;
            inventoryItemOption.UpdatedAt = now;
            inventoryItemOption.CreatedByUserId = null; // Replace when auth integration is added.
            inventoryItemOption.UpdatedByUserId = null; // Replace when auth integration is added.

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

        // GET: InventoryItemOptions/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted inventory item option.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for InventoryItemOption Id {Id}", id);

            var inventoryItemOption = await _context.InventoryItemOptions
                .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

            if (inventoryItemOption == null)
            {
                _logger.LogWarning("InventoryItemOption Id {Id} not found for edit", id);
                return NotFound();
            }

            await PopulateDropdowns(inventoryItemOption.InventoryItemId);
            return View(inventoryItemOption);
        }

        // POST: InventoryItemOptions/Edit/5
        /// <summary>
        /// Updates an existing inventory item option after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,InventoryItemId,Name,SortOrder,IsActive")] InventoryItemOption formModel)
        {
            _logger.LogInformation("Attempting to edit InventoryItemOption Id {Id}", id);

            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            ModelState.Remove(nameof(InventoryItemOption.InventoryItem));
            ModelState.Remove(nameof(InventoryItemOption.CreatedByUser));
            ModelState.Remove(nameof(InventoryItemOption.UpdatedByUser));

            NormalizeInventoryItemOption(formModel);
            await ApplyInventoryItemOptionValidationAsync(formModel, formModel.Id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit InventoryItemOption failed validation for Id {Id}", id);
                await PopulateDropdowns(formModel.InventoryItemId);
                return View(formModel);
            }

            var existing = await _context.InventoryItemOptions
                .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

            if (existing == null)
            {
                _logger.LogWarning("InventoryItemOption Id {Id} not found during edit save", id);
                return NotFound();
            }

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
            catch (DbUpdateConcurrencyException)
            {
                if (!await InventoryItemOptionExists(formModel.Id))
                {
                    _logger.LogWarning("InventoryItemOption Id {Id} no longer exists during concurrency check", id);
                    return NotFound();
                }

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

        // GET: InventoryItemOptions/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted inventory item option.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for InventoryItemOption Id {Id}", id);

            var inventoryItemOption = await _context.InventoryItemOptions
                .Where(o => o.DeletedAt == null)
                .Include(o => o.InventoryItem)
                    .ThenInclude(i => i.Category)
                        .ThenInclude(c => c.CategoryGroup)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inventoryItemOption == null)
            {
                _logger.LogWarning("InventoryItemOption Id {Id} not found for delete", id);
                return NotFound();
            }

            return View(inventoryItemOption);
        }

        // POST: InventoryItemOptions/Delete/5
        /// <summary>
        /// Soft deletes an inventory item option.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting InventoryItemOption Id {Id}", id);

            var inventoryItemOption = await _context.InventoryItemOptions
                .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

            if (inventoryItemOption == null)
            {
                _logger.LogWarning("InventoryItemOption Id {Id} not found during delete", id);
                return NotFound();
            }

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

        /// <summary>
        /// Populates the inventory item dropdown for the create and edit forms.
        /// </summary>
        private async Task PopulateDropdowns(ulong? selectedInventoryItemId = null)
        {
            _logger.LogDebug("Populating InventoryItem dropdown for InventoryItemOption");

            var inventoryItems = await _context.InventoryItems
                .Where(i => i.DeletedAt == null && i.Category.DeletedAt == null && i.Category.CategoryGroup.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .OrderBy(i => i.Category.CategoryGroup.Name)
                .ThenBy(i => i.Category.Name)
                .ThenBy(i => i.Name)
                .ToListAsync();

            var inventoryItemOptions = inventoryItems
                .Select(i => new
                {
                    i.Id,
                    DisplayName = $"{i.Category.CategoryGroup.Name} - {i.Category.Name} - {i.Name}"
                })
                .ToList();

            _logger.LogDebug("Loaded {Count} inventory items for dropdown", inventoryItemOptions.Count);

            ViewData["InventoryItemId"] = new SelectList(inventoryItemOptions, "Id", "DisplayName", selectedInventoryItemId);
        }

        /// <summary>
        /// Returns true if the non-deleted inventory item option exists.
        /// </summary>
        private async Task<bool> InventoryItemOptionExists(ulong id)
        {
            return await _context.InventoryItemOptions.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Trims strings and normalizes required values.
        /// </summary>
        private static void NormalizeInventoryItemOption(InventoryItemOption model)
        {
            model.Name = model.Name?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyInventoryItemOptionValidationAsync(InventoryItemOption model, ulong? currentId = null)
        {
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

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(InventoryItemOption.Name), "Option name is required.");
            }

            if (!string.IsNullOrWhiteSpace(model.Name) && !ContainsLetterOrDigit(model.Name))
            {
                ModelState.AddModelError(nameof(InventoryItemOption.Name), "Option name must contain letters or numbers.");
            }

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

        /// <summary>
        /// Returns true when the value contains at least one letter or digit.
        /// </summary>
        private static bool ContainsLetterOrDigit(string value)
        {
            return value.Any(char.IsLetterOrDigit);
        }
    }
}