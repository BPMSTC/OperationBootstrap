using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages create, read, update, soft delete, and wizard-based update flows
    /// for inventory items.
    /// </summary>
    public class InventoryItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryItemsController> _logger;

        private const string InventoryWizardSessionKey = "InventoryWizardStep1Draft";

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
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading InventoryItems Index page");

            var inventoryItems = await _context.InventoryItems
                .Where(i => i.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .Include(i => i.InventoryItemOptions
                    .Where(o => o.DeletedAt == null))
                .OrderBy(i => i.Category.Name)
                .ThenBy(i => i.Name)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} inventory items", inventoryItems.Count);

            return View(inventoryItems);
        }

        // GET: InventoryItems/Details/5
        /// <summary>
        /// Displays details for a single non-deleted inventory item.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for InventoryItem Id {Id}", id);

            var inventoryItem = await _context.InventoryItems
                .Where(i => i.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .Include(i => i.InventoryItemOptions
                    .Where(o => o.DeletedAt == null))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inventoryItem == null)
            {
                _logger.LogWarning("InventoryItem Id {Id} not found", id);
                return NotFound();
            }

            return View(inventoryItem);
        }

        // ============================================================
        // INVENTORY WIZARD
        // ============================================================

        // GET: InventoryItems/WizardStep1
        /// <summary>
        /// Displays Step 1 of the Inventory Item Wizard.
        /// Step 1 collects draft data only and does not write to the database.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> WizardStep1(ulong? existingInventoryItemId = null)
        {
            _logger.LogInformation("Loading Inventory Wizard Step 1");

            var model = GetInventoryWizardDraftFromSession() ?? new InventoryWizardStep1ViewModel();

            if (existingInventoryItemId.HasValue)
            {
                model.ActionType = "Update";
                model.ExistingInventoryItemId = existingInventoryItemId;
                await BackfillWizardDraftFromExistingItemAsync(model);
                model.IsExistingItemLoaded = true;
            }

            await PopulateWizardDropdownsAsync(model);
            await PopulateWizardDisplayFieldsAsync(model);

            return View(model);
        }

        // POST: InventoryItems/WizardStep1
        /// <summary>
        /// Validates and stores the Step 1 draft in Session, then redirects to Step 2.
        /// No database writes occur here.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WizardStep1(InventoryWizardStep1ViewModel model)
        {
            _logger.LogInformation("Posting Inventory Wizard Step 1 with ActionType {ActionType}", model.ActionType);

            NormalizeInventoryWizardDraft(model);

            if (string.Equals(model.ActionType, "Create", StringComparison.OrdinalIgnoreCase))
            {
                model.ExistingInventoryItemId = null;
                model.ExistingInventoryItemDisplayName = null;
                model.IsExistingItemLoaded = false;
            }
            else if (string.Equals(model.ActionType, "Update", StringComparison.OrdinalIgnoreCase) &&
                     model.ExistingInventoryItemId.HasValue)
            {
                model.IsExistingItemLoaded = true;
            }
            else
            {
                model.IsExistingItemLoaded = false;
            }

            await ApplyInventoryWizardValidationAsync(model);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Inventory Wizard Step 1 failed validation");
                await PopulateWizardDropdownsAsync(model);
                await PopulateWizardDisplayFieldsAsync(model);
                return View(model);
            }

            await PopulateWizardDisplayFieldsAsync(model);
            SaveInventoryWizardDraftToSession(model);

            _logger.LogInformation("Inventory Wizard Step 1 draft stored in Session");
            return RedirectToAction(nameof(WizardStep2));
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoadExistingItem(InventoryWizardStep1ViewModel model)
        {
            _logger.LogInformation("Loading selected InventoryItem into Wizard Step 1");

            model.ActionType = "Update";

            if (!model.ExistingInventoryItemId.HasValue)
            {
                ModelState.AddModelError(nameof(model.ExistingInventoryItemId), "Select an existing inventory item to load.");
                model.IsExistingItemLoaded = false;
                await PopulateWizardDropdownsAsync(model);
                await PopulateWizardDisplayFieldsAsync(model);
                return View("WizardStep1", model);
            }

            await BackfillWizardDraftFromExistingItemAsync(model);
            model.IsExistingItemLoaded = true;

            await PopulateWizardDropdownsAsync(model);
            await PopulateWizardDisplayFieldsAsync(model);

            return View("WizardStep1", model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetToCreate(InventoryWizardStep1ViewModel model)
        {
            _logger.LogInformation("Resetting Inventory Wizard Step 1 to Create mode");

            var resetModel = new InventoryWizardStep1ViewModel
            {
                ActionType = "Create",
                IsAvailable = true,
                IsActive = true,
                IsBaseline = false,
                ExistingInventoryItemId = null,
                ExistingInventoryItemDisplayName = null,
                CategoryDisplayName = null,
                IsExistingItemLoaded = false,
                Name = string.Empty,
                CategoryId = null
            };

            await PopulateWizardDropdownsAsync(resetModel);
            await PopulateWizardDisplayFieldsAsync(resetModel);

            return View("WizardStep1", resetModel);
        }



        // GET: InventoryItems/WizardStep2
        /// <summary>
        /// Displays the read-only confirmation page for the Inventory Item Wizard.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> WizardStep2()
        {
            _logger.LogInformation("Loading Inventory Wizard Step 2");

            var model = GetInventoryWizardDraftFromSession();
            if (model == null)
            {
                _logger.LogWarning("Inventory Wizard Step 2 requested without a Session draft");
                TempData["ErrorMessage"] = "Your inventory wizard draft was not found. Please complete Step 1 again.";
                return RedirectToAction(nameof(WizardStep1));
            }

            await PopulateWizardDisplayFieldsAsync(model);
            return View(model);
        }

        // POST: InventoryItems/WizardStep2Confirm
        /// <summary>
        /// Confirms the wizard draft and creates or updates the InventoryItem
        /// in one transaction.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WizardStep2Confirm()
        {
            _logger.LogInformation("Confirming Inventory Wizard Step 2");

            var model = GetInventoryWizardDraftFromSession();
            if (model == null)
            {
                _logger.LogWarning("Inventory Wizard confirmation attempted without a Session draft");
                TempData["ErrorMessage"] = "Your inventory wizard draft was not found. Please complete Step 1 again.";
                return RedirectToAction(nameof(WizardStep1));
            }

            NormalizeInventoryWizardDraft(model);
            await ApplyInventoryWizardValidationAsync(model);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Inventory Wizard Step 2 confirmation failed validation; returning to Step 1");
                await PopulateWizardDropdownsAsync(model);
                await PopulateWizardDisplayFieldsAsync(model);
                return View("WizardStep1", model);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var now = DateTime.UtcNow;

                if (string.Equals(model.ActionType, "Create", StringComparison.OrdinalIgnoreCase))
                {
                    var newItem = new InventoryItem
                    {
                        Name = model.Name!.Trim(),
                        CategoryId = model.CategoryId!.Value,
                        IsBaseline = model.IsBaseline,
                        IsAvailable = model.IsAvailable,
                        IsActive = model.IsActive,
                        CreatedAt = now,
                        UpdatedAt = now,
                        CreatedByUserId = null,
                        UpdatedByUserId = null
                    };

                    _context.InventoryItems.Add(newItem);

                    _logger.LogInformation("Inventory Wizard creating new InventoryItem {Name}", newItem.Name);
                }
                else
                {
                    var existingItem = await _context.InventoryItems
                        .FirstOrDefaultAsync(i =>
                            i.Id == model.ExistingInventoryItemId &&
                            i.DeletedAt == null);

                    if (existingItem == null)
                    {
                        _logger.LogWarning("Inventory Wizard could not find InventoryItem Id {Id} during confirmation", model.ExistingInventoryItemId);
                        ModelState.AddModelError(nameof(model.ExistingInventoryItemId), "The selected inventory item could not be found.");
                        await PopulateWizardDropdownsAsync(model);
                        await PopulateWizardDisplayFieldsAsync(model);
                        return View("WizardStep1", model);
                    }

                    existingItem.Name = model.Name!.Trim();
                    existingItem.CategoryId = model.CategoryId!.Value;
                    existingItem.IsBaseline = model.IsBaseline;
                    existingItem.IsAvailable = model.IsAvailable;
                    existingItem.IsActive = model.IsActive;
                    existingItem.UpdatedAt = now;
                    existingItem.UpdatedByUserId = null;

                    _logger.LogInformation("Inventory Wizard updating InventoryItem Id {Id}", existingItem.Id);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ClearInventoryWizardDraftFromSession();

                TempData["SuccessMessage"] = "Inventory item changes were saved successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "Database error while confirming Inventory Wizard Step 2");
                ModelState.AddModelError(string.Empty, "Unable to save inventory item changes.");

                await PopulateWizardDropdownsAsync(model);
                await PopulateWizardDisplayFieldsAsync(model);
                return View("WizardStep2", model);
            }
        }

        // GET: InventoryItems/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create InventoryItem page");

            await PopulateDropdowns();
            return View();
        }

        // POST: InventoryItems/Create
        /// <summary>
        /// Creates a new inventory item after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,CategoryId,IsBaseline,IsAvailable,IsActive")] InventoryItem inventoryItem)
        {
            _logger.LogInformation("Attempting to create InventoryItem {Name}", inventoryItem.Name);

            ModelState.Remove(nameof(InventoryItem.Category));
            ModelState.Remove(nameof(InventoryItem.CreatedByUser));
            ModelState.Remove(nameof(InventoryItem.UpdatedByUser));

            NormalizeInventoryItem(inventoryItem);
            await ApplyInventoryItemValidationAsync(inventoryItem);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create InventoryItem failed validation for {Name}", inventoryItem.Name);
                await PopulateDropdowns(inventoryItem.CategoryId);
                return View(inventoryItem);
            }

            var now = DateTime.UtcNow;
            inventoryItem.CreatedAt = now;
            inventoryItem.UpdatedAt = now;
            inventoryItem.CreatedByUserId = null;
            inventoryItem.UpdatedByUserId = null;

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

        // GET: InventoryItems/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted inventory item.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for InventoryItem Id {Id}", id);

            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

            if (inventoryItem == null)
            {
                _logger.LogWarning("InventoryItem Id {Id} not found for edit", id);
                return NotFound();
            }

            await PopulateDropdowns(inventoryItem.CategoryId);

            return View(inventoryItem);
        }

        // POST: InventoryItems/Edit/5
        /// <summary>
        /// Updates an existing inventory item after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Name,CategoryId,IsBaseline,IsAvailable,IsActive")] InventoryItem formModel)
        {
            _logger.LogInformation("Attempting to edit InventoryItem Id {Id}", id);

            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            ModelState.Remove(nameof(InventoryItem.Category));
            ModelState.Remove(nameof(InventoryItem.CreatedByUser));
            ModelState.Remove(nameof(InventoryItem.UpdatedByUser));

            NormalizeInventoryItem(formModel);
            await ApplyInventoryItemValidationAsync(formModel, formModel.Id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit InventoryItem failed validation for Id {Id}", id);
                await PopulateDropdowns(formModel.CategoryId);
                return View(formModel);
            }

            var existing = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

            if (existing == null)
            {
                _logger.LogWarning("InventoryItem Id {Id} not found during edit save", id);
                return NotFound();
            }

            existing.Name = formModel.Name;
            existing.CategoryId = formModel.CategoryId;
            existing.IsBaseline = formModel.IsBaseline;
            existing.IsAvailable = formModel.IsAvailable;
            existing.IsActive = formModel.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("InventoryItem Id {Id} updated successfully", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await InventoryItemExists(formModel.Id))
                {
                    _logger.LogWarning("InventoryItem Id {Id} no longer exists during concurrency check", id);
                    return NotFound();
                }

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

        // GET: InventoryItems/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted inventory item.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for InventoryItem Id {Id}", id);

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
        /// Soft deletes an inventory item.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting InventoryItem Id {Id}", id);

            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

            if (inventoryItem == null)
            {
                _logger.LogWarning("InventoryItem Id {Id} not found during delete", id);
                return NotFound();
            }

            inventoryItem.DeletedAt = DateTime.UtcNow;
            inventoryItem.UpdatedAt = DateTime.UtcNow;
            inventoryItem.UpdatedByUserId = null;

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

        /// <summary>
        /// Populates the category dropdown for the create and edit forms.
        /// </summary>
        private async Task PopulateDropdowns(ulong? selectedCategoryId = null)
        {
            _logger.LogDebug("Populating Category dropdown for InventoryItem");

            var categories = await _context.Categories
                .Where(c =>
                    c.DeletedAt == null &&
                    c.IsActive &&
                    c.CategoryGroup.DeletedAt == null &&
                    c.CategoryGroup.IsActive)
                .Include(c => c.CategoryGroup)
                .OrderBy(c => c.CategoryGroup.Name)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var categoryOptions = categories
                .Select(c => new
                {
                    c.Id,
                    DisplayName = $"{c.CategoryGroup.Name} - {c.Name}"
                })
                .ToList();

            _logger.LogDebug("Loaded {Count} categories for dropdown", categoryOptions.Count);

            ViewData["CategoryId"] = new SelectList(categoryOptions, "Id", "DisplayName", selectedCategoryId);
        }

        /// <summary>
        /// Populates the wizard dropdowns for existing inventory items and categories.
        /// </summary>
        private async Task PopulateWizardDropdownsAsync(InventoryWizardStep1ViewModel model)
        {
            var inventoryItems = await _context.InventoryItems
                .Where(i => i.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .OrderBy(i => i.Category.CategoryGroup.Name)
                .ThenBy(i => i.Category.Name)
                .ThenBy(i => i.Name)
                .ToListAsync();

            model.ExistingInventoryItems = inventoryItems
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = $"{i.Category.CategoryGroup.Name} - {i.Category.Name} - {i.Name}",
                    Selected = model.ExistingInventoryItemId.HasValue && i.Id == model.ExistingInventoryItemId.Value
                })
                .ToList();

            var categories = await _context.Categories
                .Where(c =>
                    c.DeletedAt == null &&
                    c.IsActive &&
                    c.CategoryGroup.DeletedAt == null &&
                    c.CategoryGroup.IsActive)
                .Include(c => c.CategoryGroup)
                .OrderBy(c => c.CategoryGroup.Name)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            model.Categories = categories
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.CategoryGroup.Name} - {c.Name}",
                    Selected = model.CategoryId.HasValue && c.Id == model.CategoryId.Value
                })
                .ToList();
        }

        /// <summary>
        /// Sets display-only text fields used by the confirmation page.
        /// </summary>
        private async Task PopulateWizardDisplayFieldsAsync(InventoryWizardStep1ViewModel model)
        {
            model.ExistingInventoryItemDisplayName = null;
            model.CategoryDisplayName = null;

            if (model.ExistingInventoryItemId.HasValue)
            {
                var existingItem = await _context.InventoryItems
                    .Where(i => i.DeletedAt == null && i.Id == model.ExistingInventoryItemId.Value)
                    .Include(i => i.Category)
                        .ThenInclude(c => c.CategoryGroup)
                    .FirstOrDefaultAsync();

                if (existingItem != null)
                {
                    model.ExistingInventoryItemDisplayName =
                        $"{existingItem.Category.CategoryGroup.Name} - {existingItem.Category.Name} - {existingItem.Name}";
                }
            }

            if (model.CategoryId.HasValue)
            {
                var category = await _context.Categories
                    .Where(c => c.DeletedAt == null && c.Id == model.CategoryId.Value)
                    .Include(c => c.CategoryGroup)
                    .FirstOrDefaultAsync();

                if (category != null)
                {
                    model.CategoryDisplayName = $"{category.CategoryGroup.Name} - {category.Name}";
                }
            }
        }

        /// <summary>
        /// Loads the current values from the selected existing inventory item
        /// into the wizard draft for the update path.
        /// </summary>
        private async Task BackfillWizardDraftFromExistingItemAsync(InventoryWizardStep1ViewModel model)
        {
            if (!string.Equals(model.ActionType, "Update", StringComparison.OrdinalIgnoreCase) ||
                !model.ExistingInventoryItemId.HasValue)
            {
                return;
            }

            var existingItem = await _context.InventoryItems
                .Where(i => i.DeletedAt == null && i.Id == model.ExistingInventoryItemId.Value)
                .FirstOrDefaultAsync();

            if (existingItem == null)
            {
                return;
            }

            model.Name = existingItem.Name;
            model.CategoryId = existingItem.CategoryId;
            model.IsBaseline = existingItem.IsBaseline;
            model.IsAvailable = existingItem.IsAvailable;
            model.IsActive = existingItem.IsActive;
            model.IsExistingItemLoaded = true;
        }

        /// <summary>
        /// Validates the Inventory Wizard draft before moving to Step 2 or confirming.
        /// </summary>
        private async Task ApplyInventoryWizardValidationAsync(InventoryWizardStep1ViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ActionType) ||
                (!string.Equals(model.ActionType, "Create", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(model.ActionType, "Update", StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(model.ActionType), "Choose a valid wizard action.");
            }

            if (string.Equals(model.ActionType, "Update", StringComparison.OrdinalIgnoreCase))
            {
                if (!model.ExistingInventoryItemId.HasValue)
                {
                    ModelState.AddModelError(nameof(model.ExistingInventoryItemId), "Select an existing inventory item to update.");
                }
                else
                {
                    var existingItemExists = await _context.InventoryItems
                        .AnyAsync(i => i.Id == model.ExistingInventoryItemId.Value && i.DeletedAt == null);

                    if (!existingItemExists)
                    {
                        ModelState.AddModelError(nameof(model.ExistingInventoryItemId), "Select a valid existing inventory item.");
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "Item name is required.");
            }

            if (!model.CategoryId.HasValue)
            {
                ModelState.AddModelError(nameof(model.CategoryId), "Please select a category.");
            }

            var mappedItem = new InventoryItem
            {
                Id = model.ExistingInventoryItemId ?? 0,
                Name = model.Name ?? string.Empty,
                CategoryId = model.CategoryId ?? 0,
                IsBaseline = model.IsBaseline,
                IsAvailable = model.IsAvailable,
                IsActive = model.IsActive
            };

            NormalizeInventoryItem(mappedItem);

            ulong? currentId = string.Equals(model.ActionType, "Update", StringComparison.OrdinalIgnoreCase)
                ? model.ExistingInventoryItemId
                : null;

            await ApplyInventoryItemValidationAsync(mappedItem, currentId);
        }

        /// <summary>
        /// Saves the wizard draft to Session as JSON.
        /// </summary>
        private void SaveInventoryWizardDraftToSession(InventoryWizardStep1ViewModel model)
        {
            var json = JsonSerializer.Serialize(model);
            HttpContext.Session.SetString(InventoryWizardSessionKey, json);
        }

        /// <summary>
        /// Retrieves the wizard draft from Session, or null if it does not exist.
        /// </summary>
        private InventoryWizardStep1ViewModel? GetInventoryWizardDraftFromSession()
        {
            var json = HttpContext.Session.GetString(InventoryWizardSessionKey);

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<InventoryWizardStep1ViewModel>(json);
        }

        /// <summary>
        /// Clears the wizard draft from Session after confirmation.
        /// </summary>
        private void ClearInventoryWizardDraftFromSession()
        {
            HttpContext.Session.Remove(InventoryWizardSessionKey);
        }

        /// <summary>
        /// Trims and normalizes the wizard model values.
        /// </summary>
        private static void NormalizeInventoryWizardDraft(InventoryWizardStep1ViewModel model)
        {
            model.ActionType = model.ActionType?.Trim() ?? "Create";
            model.Name = model.Name?.Trim() ?? string.Empty;

            if (string.Equals(model.ActionType, "Create", StringComparison.OrdinalIgnoreCase))
            {
                model.ExistingInventoryItemId = null;
                model.IsExistingItemLoaded = false;
                model.ExistingInventoryItemDisplayName = null;
            }
        }

        /// <summary>
        /// Returns true if the non-deleted inventory item exists.
        /// </summary>
        private async Task<bool> InventoryItemExists(ulong id)
        {
            return await _context.InventoryItems.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Trims strings and normalizes required values.
        /// </summary>
        private static void NormalizeInventoryItem(InventoryItem model)
        {
            model.Name = model.Name?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyInventoryItemValidationAsync(InventoryItem model, ulong? currentId = null)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(c =>
                    c.Id == model.CategoryId &&
                    c.DeletedAt == null &&
                    c.IsActive &&
                    c.CategoryGroup.DeletedAt == null &&
                    c.CategoryGroup.IsActive);

            if (!categoryExists)
            {
                ModelState.AddModelError(nameof(InventoryItem.CategoryId), "Select a valid category.");
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(InventoryItem.Name), "Inventory item name is required.");
            }

            if (!string.IsNullOrWhiteSpace(model.Name) && !ContainsLetterOrDigit(model.Name))
            {
                ModelState.AddModelError(nameof(InventoryItem.Name), "Inventory item name must contain letters or numbers.");
            }

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

        /// <summary>
        /// Returns true when the value contains at least one letter or digit.
        /// </summary>
        private static bool ContainsLetterOrDigit(string value)
        {
            return value.Any(char.IsLetterOrDigit);
        }
    }
}