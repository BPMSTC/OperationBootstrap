using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages create, read, update, and soft delete operations for user item preferences.
    /// </summary>
    public class UserItemPreferencesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserItemPreferencesController> _logger;

        /// <summary>
        /// Creates the controller with the required database context and logger.
        /// </summary>
        public UserItemPreferencesController(ApplicationDbContext context, ILogger<UserItemPreferencesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: UserItemPreferences
        /// <summary>
        /// Displays all non-deleted user item preferences.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Loading UserItemPreferences Index page");

                // Retrieve active user item preferences for display.
                var userItemPreferences = await _context.UserItemPreferences
                    .Where(u => u.DeletedAt == null)
                    .Include(u => u.User)
                    .Include(u => u.InventoryItem)
                        .ThenInclude(i => i.Category)
                            .ThenInclude(c => c.CategoryGroup)
                    .Include(u => u.InventoryItemOption)
                    .OrderBy(u => u.User.LastName)
                    .ThenBy(u => u.User.FirstName)
                    .ThenBy(u => u.InventoryItem.Name)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} user item preferences", userItemPreferences.Count);

                return View(userItemPreferences);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user item preferences list");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading user item preferences.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: UserItemPreferences/Details/5
        /// <summary>
        /// Displays details for a single non-deleted user item preference.
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

                _logger.LogInformation("Fetching details for UserItemPreference Id {Id}", id);

                // Retrieve the requested active user item preference.
                var userItemPreference = await _context.UserItemPreferences
                    .Where(u => u.DeletedAt == null)
                    .Include(u => u.User)
                    .Include(u => u.InventoryItem)
                        .ThenInclude(i => i.Category)
                            .ThenInclude(c => c.CategoryGroup)
                    .Include(u => u.InventoryItemOption)
                    .FirstOrDefaultAsync(m => m.Id == id);

                // Return not found when the user item preference does not exist.
                if (userItemPreference == null)
                {
                    _logger.LogWarning("UserItemPreference Id {Id} not found", id);
                    return NotFound();
                }

                return View(userItemPreference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for UserItemPreference Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading user item preference details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: UserItemPreferences/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            try
            {
                _logger.LogInformation("Loading Create UserItemPreference page");

                // Populate dropdown values for the create form.
                await PopulateDropdowns();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Create UserItemPreference page");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the create form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: UserItemPreferences/Create
        /// <summary>
        /// Creates a new user item preference after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,InventoryItemId,InventoryItemOptionId,Preference")] UserItemPreference userItemPreference)
        {
            try
            {
                _logger.LogInformation(
                    "Attempting to create UserItemPreference for UserId {UserId}, InventoryItemId {ItemId}, InventoryItemOptionId {OptionId}",
                    userItemPreference.UserId,
                    userItemPreference.InventoryItemId,
                    userItemPreference.InventoryItemOptionId);

                // Remove navigation properties that are not posted by the form.
                ModelState.Remove(nameof(UserItemPreference.User));
                ModelState.Remove(nameof(UserItemPreference.InventoryItem));
                ModelState.Remove(nameof(UserItemPreference.InventoryItemOption));
                ModelState.Remove(nameof(UserItemPreference.CreatedByUser));
                ModelState.Remove(nameof(UserItemPreference.UpdatedByUser));

                // Apply business-rule validation before saving.
                await ApplyUserItemPreferenceValidationAsync(userItemPreference);

                // Return the form with dropdowns restored when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning(
                        "Create UserItemPreference failed validation for UserId {UserId} / ItemId {ItemId}",
                        userItemPreference.UserId,
                        userItemPreference.InventoryItemId);

                    await PopulateDropdowns(
                        userItemPreference.UserId,
                        userItemPreference.InventoryItemId,
                        userItemPreference.InventoryItemOptionId,
                        userItemPreference.Preference);

                    return View(userItemPreference);
                }

                // Set audit fields for the new user item preference record.
                var now = DateTime.UtcNow;
                userItemPreference.CreatedAt = now;
                userItemPreference.UpdatedAt = now;
                userItemPreference.CreatedByUserId = null;
                userItemPreference.UpdatedByUserId = null;

                // Queue the new user item preference for insert.
                _context.Add(userItemPreference);

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("UserItemPreference Id {Id} created successfully", userItemPreference.Id);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(
                        ex,
                        "Error creating UserItemPreference for UserId {UserId} / ItemId {ItemId}",
                        userItemPreference.UserId,
                        userItemPreference.InventoryItemId);

                    ModelState.AddModelError("", "Unable to save preference.");
                    await PopulateDropdowns(
                        userItemPreference.UserId,
                        userItemPreference.InventoryItemId,
                        userItemPreference.InventoryItemOptionId,
                        userItemPreference.Preference);

                    return View(userItemPreference);
                }
            }
            catch(Exception ex)
{
                _logger.LogError(ex, "Unexpected error creating UserItemPreference for UserId {UserId}", userItemPreference?.UserId);
                ModelState.AddModelError("", "An unexpected error occurred while creating the user item preference.");

                await PopulateDropdowns(
                    userItemPreference?.UserId,
                    userItemPreference?.InventoryItemId,
                    userItemPreference?.InventoryItemOptionId);

                return View(userItemPreference ?? new UserItemPreference());
            }
        }

        // GET: UserItemPreferences/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted user item preference.
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

                _logger.LogInformation("Loading Edit page for UserItemPreference Id {Id}", id);

                // Retrieve the requested active user item preference for editing.
                var userItemPreference = await _context.UserItemPreferences
                    .Where(u => u.DeletedAt == null)
                    .FirstOrDefaultAsync(u => u.Id == id);

                // Return not found when the user item preference does not exist.
                if (userItemPreference == null)
                {
                    _logger.LogWarning("UserItemPreference Id {Id} not found for edit", id);
                    return NotFound();
                }

                // Populate dropdown values using the current record selections.
                await PopulateDropdowns(
                    userItemPreference.UserId,
                    userItemPreference.InventoryItemId,
                    userItemPreference.InventoryItemOptionId,
                    userItemPreference.Preference);

                return View(userItemPreference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit page for UserItemPreference Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: UserItemPreferences/Edit/5
        /// <summary>
        /// Updates an existing user item preference after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,UserId,InventoryItemId,InventoryItemOptionId,Preference")] UserItemPreference formModel)
        {
            try
            {
                _logger.LogInformation("Attempting to edit UserItemPreference Id {Id}", id);

                // Ensure the route id matches the posted model id.
                if (id != formModel.Id)
                {
                    _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                    return NotFound();
                }

                // Remove navigation properties that are not posted by the form.
                ModelState.Remove(nameof(UserItemPreference.User));
                ModelState.Remove(nameof(UserItemPreference.InventoryItem));
                ModelState.Remove(nameof(UserItemPreference.InventoryItemOption));
                ModelState.Remove(nameof(UserItemPreference.CreatedByUser));
                ModelState.Remove(nameof(UserItemPreference.UpdatedByUser));

                // Apply business-rule validation before saving.
                await ApplyUserItemPreferenceValidationAsync(formModel, formModel.Id);

                // Return the form when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Edit UserItemPreference failed validation for Id {Id}", id);

                    await PopulateDropdowns(
                        formModel.UserId,
                        formModel.InventoryItemId,
                        formModel.InventoryItemOptionId,
                        formModel.Preference);

                    return View(formModel);
                }

                // Retrieve the existing active user item preference record.
                var existing = await _context.UserItemPreferences
                    .Where(u => u.DeletedAt == null)
                    .FirstOrDefaultAsync(u => u.Id == id);

                // Return not found when the target record no longer exists.
                if (existing == null)
                {
                    _logger.LogWarning("UserItemPreference Id {Id} not found during edit save", id);
                    return NotFound();
                }

                // Copy validated form values into the tracked entity.
                existing.UserId = formModel.UserId;
                existing.InventoryItemId = formModel.InventoryItemId;
                existing.InventoryItemOptionId = formModel.InventoryItemOptionId;
                existing.Preference = formModel.Preference;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = null;

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("UserItemPreference Id {Id} updated successfully", id);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Check whether the record was deleted during the edit attempt.
                    if (!await UserItemPreferenceExists(formModel.Id))
                    {
                        _logger.LogWarning("UserItemPreference Id {Id} no longer exists during concurrency check", id);
                        return NotFound();
                    }

                    _logger.LogError(ex, "Concurrency error updating UserItemPreference Id {Id}", id);
                    throw;
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error updating UserItemPreference Id {Id}", id);

                    ModelState.AddModelError("", "Unable to save changes.");
                    await PopulateDropdowns(
                        formModel.UserId,
                        formModel.InventoryItemId,
                        formModel.InventoryItemOptionId,
                        formModel.Preference);

                    return View(formModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error editing UserItemPreference Id {Id}", id);
                ModelState.AddModelError("", "An unexpected error occurred while updating the user item preference.");
                await PopulateDropdowns(
                    formModel.UserId,
                    formModel.InventoryItemId,
                    formModel.InventoryItemOptionId,
                    formModel.Preference);

                return View(formModel);
            }
        }

        // GET: UserItemPreferences/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted user item preference.
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

                _logger.LogInformation("Loading Delete confirmation for UserItemPreference Id {Id}", id);

                // Retrieve the requested active user item preference.
                var userItemPreference = await _context.UserItemPreferences
                    .Where(u => u.DeletedAt == null)
                    .Include(u => u.User)
                    .Include(u => u.InventoryItem)
                        .ThenInclude(i => i.Category)
                            .ThenInclude(c => c.CategoryGroup)
                    .Include(u => u.InventoryItemOption)
                    .FirstOrDefaultAsync(m => m.Id == id);

                // Return not found when the user item preference does not exist.
                if (userItemPreference == null)
                {
                    _logger.LogWarning("UserItemPreference Id {Id} not found for delete", id);
                    return NotFound();
                }

                return View(userItemPreference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete page for UserItemPreference Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the delete page.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: UserItemPreferences/Delete/5
        /// <summary>
        /// Soft deletes a user item preference.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            try
            {
                _logger.LogWarning("Soft deleting UserItemPreference Id {Id}", id);

                // Retrieve the active user item preference targeted for soft delete.
                var userItemPreference = await _context.UserItemPreferences
                    .Where(u => u.DeletedAt == null)
                    .FirstOrDefaultAsync(u => u.Id == id);

                // Return not found when the user item preference does not exist.
                if (userItemPreference == null)
                {
                    _logger.LogWarning("UserItemPreference Id {Id} not found during delete", id);
                    return NotFound();
                }

                // Apply soft-delete and audit values.
                userItemPreference.DeletedAt = DateTime.UtcNow;
                userItemPreference.UpdatedAt = DateTime.UtcNow;
                userItemPreference.UpdatedByUserId = null;

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("UserItemPreference Id {Id} soft deleted", id);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error soft deleting UserItemPreference Id {Id}", id);

                    TempData["ErrorMessage"] = "Unable to delete user item preference.";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting UserItemPreference Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the user item preference.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        /// <summary>
        /// Populates dropdown lists for the create and edit forms.
        /// </summary>
        private async Task PopulateDropdowns(
            ulong? selectedUserId = null,
            ulong? selectedInventoryItemId = null,
            ulong? selectedInventoryItemOptionId = null,
            PreferenceOption? selectedPreference = null)
        {
            // Retrieve active users for the dropdown list.
            var users = await _context.DomainUsers
                .Where(u => u.DeletedAt == null && u.IsActive)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            // Build display-friendly user dropdown options.
            var userOptions = users
                .Select(u => new { u.Id, DisplayName = BuildUserDisplayName(u) })
                .ToList();

            // Retrieve active inventory items for the dropdown list.
            var inventoryItems = await _context.InventoryItems
                .Where(i => i.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .OrderBy(i => i.Category.CategoryGroup.Name)
                .ThenBy(i => i.Category.Name)
                .ThenBy(i => i.Name)
                .ToListAsync();

            // Build inventory item dropdown options.
            var inventoryOptions = inventoryItems
                .Select(i => new
                {
                    i.Id,
                    DisplayName = $"{i.Category.CategoryGroup.Name} - {i.Category.Name} - {i.Name}"
                })
                .ToList();

            // Retrieve active inventory item options for the dropdown list.
            var allInventoryItemOptions = await _context.InventoryItemOptions
                .Where(o => o.DeletedAt == null)
                .Include(o => o.InventoryItem)
                    .ThenInclude(i => i.Category)
                        .ThenInclude(c => c.CategoryGroup)
                .OrderBy(o => o.InventoryItem.Category.CategoryGroup.Name)
                .ThenBy(o => o.InventoryItem.Category.Name)
                .ThenBy(o => o.InventoryItem.Name)
                .ThenBy(o => o.SortOrder)
                .ThenBy(o => o.Name)
                .ToListAsync();

            // Build all inventory item option metadata used by the view.
            var inventoryItemOptionData = allInventoryItemOptions
                .Select(o => new
                {
                    o.Id,
                    o.InventoryItemId,
                    DisplayName = $"{o.InventoryItem.Category.CategoryGroup.Name} - {o.InventoryItem.Category.Name} - {o.InventoryItem.Name} - {o.Name}"
                })
                .ToList();

            // Filter inventory item option dropdown options to the selected inventory item.
            var filteredInventoryItemOptionOptions = selectedInventoryItemId.HasValue
                ? inventoryItemOptionData
                    .Where(o => o.InventoryItemId == selectedInventoryItemId.Value)
                    .ToList()
                : inventoryItemOptionData
                    .Where(o => false)
                    .ToList();

            // Store the dropdown options in ViewData.
            ViewData["UserId"] = new SelectList(userOptions, "Id", "DisplayName", selectedUserId);
            ViewData["InventoryItemId"] = new SelectList(inventoryOptions, "Id", "DisplayName", selectedInventoryItemId);
            ViewData["InventoryItemOptionId"] = new SelectList(filteredInventoryItemOptionOptions, "Id", "DisplayName", selectedInventoryItemOptionId);

            ViewData["PreferenceOptions"] = new SelectList(
                Enum.GetValues(typeof(PreferenceOption))
                    .Cast<PreferenceOption>()
                    .Select(p => new { Value = p, Text = p.ToString() }),
                "Value",
                "Text",
                selectedPreference);

            ViewBag.InventoryItemOptionData = inventoryItemOptionData;

            _logger.LogInformation(
                "Populated dropdowns: {UserCount} users, {ItemCount} items, {OptionCount} item options",
                userOptions.Count,
                inventoryOptions.Count,
                inventoryItemOptionData.Count);
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyUserItemPreferenceValidationAsync(UserItemPreference model, ulong? currentId = null)
        {
            // Validate that the selected user exists and is active.
            var validUser = await _context.DomainUsers
                .AnyAsync(u =>
                    u.Id == model.UserId &&
                    u.DeletedAt == null &&
                    u.IsActive);

            if (!validUser)
            {
                ModelState.AddModelError(nameof(UserItemPreference.UserId), "Select a valid active user.");
            }

            // Validate that the selected inventory item exists and is not deleted.
            var validInventoryItem = await _context.InventoryItems
                .AnyAsync(i =>
                    i.Id == model.InventoryItemId &&
                    i.DeletedAt == null);

            if (!validInventoryItem)
            {
                ModelState.AddModelError(nameof(UserItemPreference.InventoryItemId), "Select a valid inventory item.");
            }

            // Validate that the selected option belongs to the selected inventory item.
            if (model.InventoryItemOptionId.HasValue)
            {
                var validInventoryItemOption = await _context.InventoryItemOptions
                    .AnyAsync(o =>
                        o.Id == model.InventoryItemOptionId.Value &&
                        o.DeletedAt == null &&
                        o.InventoryItemId == model.InventoryItemId);

                if (!validInventoryItemOption)
                {
                    ModelState.AddModelError(nameof(UserItemPreference.InventoryItemOptionId), "Select a valid option for the selected inventory item.");
                }
            }

            // Validate that the selected preference is a defined enum value.
            if (!Enum.IsDefined(typeof(PreferenceOption), model.Preference))
            {
                ModelState.AddModelError(nameof(UserItemPreference.Preference), "Select a valid preference.");
            }

            // Prevent duplicate active preferences for the same user and inventory item.
            var duplicateExists = await _context.UserItemPreferences
                .AnyAsync(u =>
                    u.DeletedAt == null &&
                    u.Id != currentId &&
                    u.UserId == model.UserId &&
                    u.InventoryItemId == model.InventoryItemId);

            if (duplicateExists)
            {
                ModelState.AddModelError("", "A preference for this user and inventory item already exists.");
            }
        }

        /// <summary>
        /// Builds a readable display label for a domain user.
        /// </summary>
        private static string BuildUserDisplayName(DomainUser u)
        {
            var first = (u.FirstName ?? string.Empty).Trim();
            var last = (u.LastName ?? string.Empty).Trim();
            var email = (u.Email ?? string.Empty).Trim();

            var namePart = string.Join(", ", new[] { last, first }.Where(s => !string.IsNullOrWhiteSpace(s)));

            if (!string.IsNullOrWhiteSpace(namePart) && !string.IsNullOrWhiteSpace(email))
                return $"{namePart} ({email})";

            if (!string.IsNullOrWhiteSpace(namePart))
                return namePart;

            if (!string.IsNullOrWhiteSpace(email))
                return email;

            return $"User #{u.Id}";
        }

        /// <summary>
        /// Returns true if the non-deleted user item preference exists.
        /// </summary>
        private async Task<bool> UserItemPreferenceExists(ulong id)
        {
            // Check whether the requested active user item preference still exists.
            return await _context.UserItemPreferences.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}