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
            _logger.LogInformation("Loading UserItemPreferences Index page");

            // Retrieve active user item preferences with related display data.
            var userItemPreferences = await _context.UserItemPreferences
                .Where(u => u.DeletedAt == null)
                .Include(u => u.User)
                .Include(u => u.InventoryItem)
                    .ThenInclude(i => i.Category)
                .Include(u => u.CreatedByUser)
                .Include(u => u.UpdatedByUser)
                .OrderBy(u => u.User.LastName)
                .ThenBy(u => u.User.FirstName)
                .ThenBy(u => u.InventoryItem.Name)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} user item preferences", userItemPreferences.Count);

            return View(userItemPreferences);
        }

        // GET: UserItemPreferences/Details/5
        /// <summary>
        /// Displays details for a single non-deleted user item preference.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            // Reject requests with no id.
            if (id == null)
            {
                _logger.LogWarning("Details requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for UserItemPreference Id {Id}", id);

            // Retrieve the requested active user item preference with related display data.
            var userItemPreference = await _context.UserItemPreferences
                .Where(u => u.DeletedAt == null)
                .Include(u => u.User)
                .Include(u => u.InventoryItem)
                    .ThenInclude(i => i.Category)
                .Include(u => u.CreatedByUser)
                .Include(u => u.UpdatedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Return not found when the preference does not exist.
            if (userItemPreference == null)
            {
                _logger.LogWarning("UserItemPreference Id {Id} not found", id);
                return NotFound();
            }

            return View(userItemPreference);
        }

        // GET: UserItemPreferences/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create UserItemPreference page");

            // Populate dropdown values for the create form.
            await PopulateDropdowns();
            return View();
        }

        // POST: UserItemPreferences/Create
        /// <summary>
        /// Creates a new user item preference after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,InventoryItemId,Preference")] UserItemPreference userItemPreference)
        {
            _logger.LogInformation(
                "Attempting to create UserItemPreference for UserId {UserId} and InventoryItemId {ItemId}",
                userItemPreference.UserId,
                userItemPreference.InventoryItemId);

            // Remove navigation properties that are not posted by the form.
            ModelState.Remove(nameof(UserItemPreference.User));
            ModelState.Remove(nameof(UserItemPreference.InventoryItem));
            ModelState.Remove(nameof(UserItemPreference.CreatedByUser));
            ModelState.Remove(nameof(UserItemPreference.UpdatedByUser));

            // Apply business-rule validation for the submitted preference.
            await ApplyUserItemPreferenceValidationAsync(userItemPreference);

            // Return the form with dropdowns restored when validation fails.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning(
                    "Create UserItemPreference failed validation for UserId {UserId} / ItemId {ItemId}",
                    userItemPreference.UserId,
                    userItemPreference.InventoryItemId);

                await PopulateDropdowns(userItemPreference.UserId, userItemPreference.InventoryItemId, userItemPreference.Preference);
                return View(userItemPreference);
            }

            // Set audit fields for the new user item preference.
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
                await PopulateDropdowns(userItemPreference.UserId, userItemPreference.InventoryItemId, userItemPreference.Preference);
                return View(userItemPreference);
            }
        }

        // GET: UserItemPreferences/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted user item preference.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
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

            // Return not found when the preference does not exist.
            if (userItemPreference == null)
            {
                _logger.LogWarning("UserItemPreference Id {Id} not found for edit", id);
                return NotFound();
            }

            // Populate dropdown values using the current record selections.
            await PopulateDropdowns(userItemPreference.UserId, userItemPreference.InventoryItemId, userItemPreference.Preference);

            return View(userItemPreference);
        }

        // POST: UserItemPreferences/Edit/5
        /// <summary>
        /// Updates an existing user item preference after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,UserId,InventoryItemId,Preference")] UserItemPreference formModel)
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
            ModelState.Remove(nameof(UserItemPreference.CreatedByUser));
            ModelState.Remove(nameof(UserItemPreference.UpdatedByUser));

            // Apply business-rule validation for the submitted changes.
            await ApplyUserItemPreferenceValidationAsync(formModel, formModel.Id);

            // Return the form with dropdowns restored when validation fails.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit UserItemPreference failed validation for Id {Id}", id);
                await PopulateDropdowns(formModel.UserId, formModel.InventoryItemId, formModel.Preference);
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
            existing.Preference = formModel.Preference;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("UserItemPreference Id {Id} updated successfully", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Check whether the record was deleted during the edit attempt.
                if (!await UserItemPreferenceExists(formModel.Id))
                {
                    _logger.LogWarning("UserItemPreference Id {Id} no longer exists during concurrency check", id);
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating UserItemPreference Id {Id}", id);

                ModelState.AddModelError("", "Unable to save changes.");
                await PopulateDropdowns(formModel.UserId, formModel.InventoryItemId, formModel.Preference);
                return View(formModel);
            }
        }

        // GET: UserItemPreferences/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted user item preference.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            // Reject requests with no id.
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for UserItemPreference Id {Id}", id);

            // Retrieve the requested active user item preference with related display data.
            var userItemPreference = await _context.UserItemPreferences
                .Where(u => u.DeletedAt == null)
                .Include(u => u.User)
                .Include(u => u.InventoryItem)
                    .ThenInclude(i => i.Category)
                .Include(u => u.CreatedByUser)
                .Include(u => u.UpdatedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Return not found when the preference does not exist.
            if (userItemPreference == null)
            {
                _logger.LogWarning("UserItemPreference Id {Id} not found for delete", id);
                return NotFound();
            }

            return View(userItemPreference);
        }

        // POST: UserItemPreferences/Delete/5
        /// <summary>
        /// Soft deletes a user item preference.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting UserItemPreference Id {Id}", id);

            // Retrieve the active user item preference targeted for soft delete.
            var userItemPreference = await _context.UserItemPreferences
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(u => u.Id == id);

            // Return not found when the preference does not exist.
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

        /// <summary>
        /// Populates dropdown lists for the create and edit forms.
        /// </summary>
        private async Task PopulateDropdowns(
            ulong? selectedUserId = null,
            ulong? selectedInventoryItemId = null,
            PreferenceOption? selectedPreference = null)
        {
            // Retrieve active domain users for the user dropdown.
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

            // Retrieve active inventory items with category hierarchy for display.
            var inventoryItems = await _context.InventoryItems
                .Where(i => i.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .OrderBy(i => i.Category.CategoryGroup.Name)
                .ThenBy(i => i.Category.Name)
                .ThenBy(i => i.Name)
                .ToListAsync();

            // Build display-friendly inventory item dropdown options.
            var inventoryOptions = inventoryItems
                .Select(i => new
                {
                    i.Id,
                    DisplayName = $"{i.Category.CategoryGroup.Name} - {i.Category.Name} - {i.Name}"
                })
                .ToList();

            // Store the user and inventory item dropdowns in ViewData.
            ViewData["UserId"] = new SelectList(userOptions, "Id", "DisplayName", selectedUserId);
            ViewData["InventoryItemId"] = new SelectList(inventoryOptions, "Id", "DisplayName", selectedInventoryItemId);

            // Store the preference enum options in ViewData.
            ViewData["PreferenceOptions"] = new SelectList(
                Enum.GetValues(typeof(PreferenceOption))
                    .Cast<PreferenceOption>()
                    .Select(p => new { Value = p, Text = p.ToString() }),
                "Value",
                "Text",
                selectedPreference);

            _logger.LogInformation("Populated dropdowns: {UserCount} users, {ItemCount} items", userOptions.Count, inventoryOptions.Count);
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyUserItemPreferenceValidationAsync(UserItemPreference model, ulong? currentId = null)
        {
            // Validate that the selected user exists, is active, and is not deleted.
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

            // Validate that the selected preference value is defined.
            if (!Enum.IsDefined(typeof(PreferenceOption), model.Preference))
            {
                ModelState.AddModelError(nameof(UserItemPreference.Preference), "Select a valid preference.");
            }

            // Prevent duplicate preferences for the same user and inventory item.
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
            // Normalize display parts before building the label.
            var first = (u.FirstName ?? string.Empty).Trim();
            var last = (u.LastName ?? string.Empty).Trim();
            var email = (u.Email ?? string.Empty).Trim();

            // Build the display name in Last, First format when available.
            var namePart = string.Join(", ", new[] { last, first }.Where(s => !string.IsNullOrWhiteSpace(s)));

            // Prefer name plus email when both are available.
            if (!string.IsNullOrWhiteSpace(namePart) && !string.IsNullOrWhiteSpace(email))
                return $"{namePart} ({email})";

            // Fall back to name only when email is not available.
            if (!string.IsNullOrWhiteSpace(namePart))
                return namePart;

            // Fall back to email only when name is not available.
            if (!string.IsNullOrWhiteSpace(email))
                return email;

            // Fall back to a generic identifier when no other display value exists.
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