using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages create, read, update, and soft delete operations for user choice group preferences.
    /// </summary>
    public class UserChoiceGroupPreferencesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserChoiceGroupPreferencesController> _logger;

        /// <summary>
        /// Creates the controller with the required database context and logger.
        /// </summary>
        public UserChoiceGroupPreferencesController(ApplicationDbContext context, ILogger<UserChoiceGroupPreferencesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: UserChoiceGroupPreferences
        /// <summary>
        /// Displays all non-deleted user choice group preferences.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading UserChoiceGroupPreferences Index page");

            var userChoiceGroupPreferences = await _context.UserChoiceGroupPreferences
                .Where(p => p.DeletedAt == null)
                .Include(p => p.User)
                .Include(p => p.InventoryChoiceGroup)
                .Include(p => p.SelectedInventoryItem)
                    .ThenInclude(i => i.Category)
                        .ThenInclude(c => c.CategoryGroup)
                .OrderBy(p => p.User.LastName)
                .ThenBy(p => p.User.FirstName)
                .ThenBy(p => p.InventoryChoiceGroup.Name)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} user choice group preferences", userChoiceGroupPreferences.Count);

            return View(userChoiceGroupPreferences);
        }

        // GET: UserChoiceGroupPreferences/Details/5
        /// <summary>
        /// Displays details for a single non-deleted user choice group preference.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for UserChoiceGroupPreference Id {Id}", id);

            var userChoiceGroupPreference = await _context.UserChoiceGroupPreferences
                .Where(p => p.DeletedAt == null)
                .Include(p => p.User)
                .Include(p => p.InventoryChoiceGroup)
                .Include(p => p.SelectedInventoryItem)
                    .ThenInclude(i => i.Category)
                        .ThenInclude(c => c.CategoryGroup)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (userChoiceGroupPreference == null)
            {
                _logger.LogWarning("UserChoiceGroupPreference Id {Id} not found", id);
                return NotFound();
            }

            return View(userChoiceGroupPreference);
        }

        // GET: UserChoiceGroupPreferences/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create UserChoiceGroupPreference page");

            await PopulateDropdowns();
            return View();
        }

        // POST: UserChoiceGroupPreferences/Create
        /// <summary>
        /// Creates a new user choice group preference after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,InventoryChoiceGroupId,SelectedInventoryItemId")] UserChoiceGroupPreference userChoiceGroupPreference)
        {
            _logger.LogInformation("Attempting to create UserChoiceGroupPreference for UserId {UserId}", userChoiceGroupPreference.UserId);

            ModelState.Remove(nameof(UserChoiceGroupPreference.User));
            ModelState.Remove(nameof(UserChoiceGroupPreference.InventoryChoiceGroup));
            ModelState.Remove(nameof(UserChoiceGroupPreference.SelectedInventoryItem));
            ModelState.Remove(nameof(UserChoiceGroupPreference.CreatedByUser));
            ModelState.Remove(nameof(UserChoiceGroupPreference.UpdatedByUser));

            await ApplyUserChoiceGroupPreferenceValidationAsync(userChoiceGroupPreference);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create UserChoiceGroupPreference failed validation for UserId {UserId}", userChoiceGroupPreference.UserId);
                await PopulateDropdowns(
                    userChoiceGroupPreference.UserId,
                    userChoiceGroupPreference.InventoryChoiceGroupId,
                    userChoiceGroupPreference.SelectedInventoryItemId);
                return View(userChoiceGroupPreference);
            }

            var now = DateTime.UtcNow;
            userChoiceGroupPreference.CreatedAt = now;
            userChoiceGroupPreference.UpdatedAt = now;
            userChoiceGroupPreference.CreatedByUserId = null; // Replace when auth integration is added.
            userChoiceGroupPreference.UpdatedByUserId = null; // Replace when auth integration is added.

            _context.Add(userChoiceGroupPreference);

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("UserChoiceGroupPreference created successfully");
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error creating UserChoiceGroupPreference");

                ModelState.AddModelError("", "Unable to save user choice group preference.");
                await PopulateDropdowns(
                    userChoiceGroupPreference.UserId,
                    userChoiceGroupPreference.InventoryChoiceGroupId,
                    userChoiceGroupPreference.SelectedInventoryItemId);
                return View(userChoiceGroupPreference);
            }
        }

        // GET: UserChoiceGroupPreferences/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted user choice group preference.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for UserChoiceGroupPreference Id {Id}", id);

            var userChoiceGroupPreference = await _context.UserChoiceGroupPreferences
                .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

            if (userChoiceGroupPreference == null)
            {
                _logger.LogWarning("UserChoiceGroupPreference Id {Id} not found for edit", id);
                return NotFound();
            }

            await PopulateDropdowns(
                userChoiceGroupPreference.UserId,
                userChoiceGroupPreference.InventoryChoiceGroupId,
                userChoiceGroupPreference.SelectedInventoryItemId);

            return View(userChoiceGroupPreference);
        }

        // POST: UserChoiceGroupPreferences/Edit/5
        /// <summary>
        /// Updates an existing user choice group preference after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,UserId,InventoryChoiceGroupId,SelectedInventoryItemId")] UserChoiceGroupPreference formModel)
        {
            _logger.LogInformation("Attempting to edit UserChoiceGroupPreference Id {Id}", id);

            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            ModelState.Remove(nameof(UserChoiceGroupPreference.User));
            ModelState.Remove(nameof(UserChoiceGroupPreference.InventoryChoiceGroup));
            ModelState.Remove(nameof(UserChoiceGroupPreference.SelectedInventoryItem));
            ModelState.Remove(nameof(UserChoiceGroupPreference.CreatedByUser));
            ModelState.Remove(nameof(UserChoiceGroupPreference.UpdatedByUser));

            await ApplyUserChoiceGroupPreferenceValidationAsync(formModel, formModel.Id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit UserChoiceGroupPreference failed validation for Id {Id}", id);
                await PopulateDropdowns(
                    formModel.UserId,
                    formModel.InventoryChoiceGroupId,
                    formModel.SelectedInventoryItemId);
                return View(formModel);
            }

            var existing = await _context.UserChoiceGroupPreferences
                .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

            if (existing == null)
            {
                _logger.LogWarning("UserChoiceGroupPreference Id {Id} not found during edit save", id);
                return NotFound();
            }

            existing.UserId = formModel.UserId;
            existing.InventoryChoiceGroupId = formModel.InventoryChoiceGroupId;
            existing.SelectedInventoryItemId = formModel.SelectedInventoryItemId;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // Replace when auth integration is added.

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("UserChoiceGroupPreference Id {Id} updated successfully", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await UserChoiceGroupPreferenceExists(formModel.Id))
                {
                    _logger.LogWarning("UserChoiceGroupPreference Id {Id} no longer exists during concurrency check", id);
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating UserChoiceGroupPreference Id {Id}", id);

                ModelState.AddModelError("", "Unable to save changes.");
                await PopulateDropdowns(
                    formModel.UserId,
                    formModel.InventoryChoiceGroupId,
                    formModel.SelectedInventoryItemId);
                return View(formModel);
            }
        }

        // GET: UserChoiceGroupPreferences/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted user choice group preference.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for UserChoiceGroupPreference Id {Id}", id);

            var userChoiceGroupPreference = await _context.UserChoiceGroupPreferences
                .Where(p => p.DeletedAt == null)
                .Include(p => p.User)
                .Include(p => p.InventoryChoiceGroup)
                .Include(p => p.SelectedInventoryItem)
                    .ThenInclude(i => i.Category)
                        .ThenInclude(c => c.CategoryGroup)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (userChoiceGroupPreference == null)
            {
                _logger.LogWarning("UserChoiceGroupPreference Id {Id} not found for delete", id);
                return NotFound();
            }

            return View(userChoiceGroupPreference);
        }

        // POST: UserChoiceGroupPreferences/Delete/5
        /// <summary>
        /// Soft deletes a user choice group preference.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting UserChoiceGroupPreference Id {Id}", id);

            var userChoiceGroupPreference = await _context.UserChoiceGroupPreferences
                .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

            if (userChoiceGroupPreference == null)
            {
                _logger.LogWarning("UserChoiceGroupPreference Id {Id} not found during delete", id);
                return NotFound();
            }

            userChoiceGroupPreference.DeletedAt = DateTime.UtcNow;
            userChoiceGroupPreference.UpdatedAt = DateTime.UtcNow;
            userChoiceGroupPreference.UpdatedByUserId = null; // Replace when auth integration is added.

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("UserChoiceGroupPreference Id {Id} soft deleted", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error soft deleting UserChoiceGroupPreference Id {Id}", id);

                TempData["ErrorMessage"] = "Unable to delete user choice group preference.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Populates the dropdowns for user, choice group, and selected inventory item.
        /// </summary>
        private async Task PopulateDropdowns(
            ulong? selectedUserId = null,
            ulong? selectedChoiceGroupId = null,
            ulong? selectedInventoryItemId = null)
        {
            _logger.LogDebug("Populating dropdowns for UserChoiceGroupPreference");

            var users = await _context.DomainUsers
                .Where(u => u.DeletedAt == null && u.UserType == UserType.Client)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();

            var choiceGroups = await _context.InventoryChoiceGroups
                .Where(g => g.DeletedAt == null)
                .OrderBy(g => g.Name)
                .ToListAsync();

            var inventoryItems = await _context.InventoryItems
                .Where(i => i.DeletedAt == null && i.Category.DeletedAt == null && i.Category.CategoryGroup.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .OrderBy(i => i.Category.CategoryGroup.Name)
                .ThenBy(i => i.Category.Name)
                .ThenBy(i => i.Name)
                .ToListAsync();

            var userOptions = users
                .Select(u => new
                {
                    u.Id,
                    DisplayName = $"{u.LastName}, {u.FirstName}"
                })
                .ToList();

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

            ViewData["UserId"] = new SelectList(userOptions, "Id", "DisplayName", selectedUserId);
            ViewData["InventoryChoiceGroupId"] = new SelectList(choiceGroupOptions, "Id", "DisplayName", selectedChoiceGroupId);
            ViewData["SelectedInventoryItemId"] = new SelectList(inventoryItemOptions, "Id", "DisplayName", selectedInventoryItemId);
        }

        /// <summary>
        /// Returns true if the non-deleted user choice group preference exists.
        /// </summary>
        private async Task<bool> UserChoiceGroupPreferenceExists(ulong id)
        {
            return await _context.UserChoiceGroupPreferences.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyUserChoiceGroupPreferenceValidationAsync(UserChoiceGroupPreference model, ulong? currentId = null)
        {
            var userExists = await _context.DomainUsers
                .AnyAsync(u => u.Id == model.UserId && u.DeletedAt == null && u.UserType == UserType.Client);

            if (!userExists)
            {
                ModelState.AddModelError(nameof(UserChoiceGroupPreference.UserId), "Select a valid client user.");
            }

            var choiceGroupExists = await _context.InventoryChoiceGroups
                .AnyAsync(g => g.Id == model.InventoryChoiceGroupId && g.DeletedAt == null);

            if (!choiceGroupExists)
            {
                ModelState.AddModelError(nameof(UserChoiceGroupPreference.InventoryChoiceGroupId), "Select a valid choice group.");
            }

            var selectedItemExists = await _context.InventoryItems
                .AnyAsync(i =>
                    i.Id == model.SelectedInventoryItemId &&
                    i.DeletedAt == null &&
                    i.Category.DeletedAt == null &&
                    i.Category.CategoryGroup.DeletedAt == null);

            if (!selectedItemExists)
            {
                ModelState.AddModelError(nameof(UserChoiceGroupPreference.SelectedInventoryItemId), "Select a valid inventory item.");
            }

            var duplicateExists = await _context.UserChoiceGroupPreferences
                .AnyAsync(p =>
                    p.DeletedAt == null &&
                    p.Id != currentId &&
                    p.UserId == model.UserId &&
                    p.InventoryChoiceGroupId == model.InventoryChoiceGroupId);

            if (duplicateExists)
            {
                ModelState.AddModelError(nameof(UserChoiceGroupPreference.InventoryChoiceGroupId), "This user already has a preference for the selected choice group.");
            }
        }
    }
}