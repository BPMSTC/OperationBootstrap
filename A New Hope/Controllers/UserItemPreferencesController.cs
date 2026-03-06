using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// UserItemPreferencesController
    /// -----------------------------
    /// This controller manages DIO operations for UserItemPreference records.
    ///
    /// In your domain, a UserItemPreference appears to represent a user's preference for a specific
    /// inventory item (for example: Always / Ask / Never).
    ///
    /// Key relationships visible in this controller:
    /// - UserItemPreference -> User (DomainUser, via UserId)
    /// - UserItemPreference -> InventoryItem (via InventoryItemId)
    /// - InventoryItem -> Category -> CategoryGroup
    ///
    /// Key behaviors implemented here:
    /// - Uses Entity Framework Core (ApplicationDbContext) for database operations.
    /// - Uses dependency-injected ILogger for structured logging (traceability + diagnostics).
    /// - Uses SOFT DELETE semantics: DeleteConfirmed sets DeletedAt rather than physically removing rows.
    /// - Filters out soft-deleted preferences in most queries (DeletedAt == null).
    /// - Enforces a business rule to prevent duplicates:
    ///     - A user can have only one preference per inventory item.
    /// - Populates dropdown lists for:
    ///     - selecting a user
    ///     - selecting an inventory item
    ///     - selecting a preference option (enum)
    ///
    /// Notes on audit fields:
    /// - CreatedAt/UpdatedAt are set using DateTime.UtcNow.
    /// - CreatedByUserId/UpdatedByUserId are currently set to null until auth/user tracking is implemented.
    /// </summary>
    public class UserItemPreferencesController : Controller
    {
        /// <summary>
        /// EF Core DbContext used to query/persist UserItemPreferences, Users, InventoryItems, etc.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Logger for this controller. Where logs go depends on Program.cs logging providers.
        /// </summary>
        private readonly ILogger<UserItemPreferencesController> _logger;

        /// <summary>
        /// Constructor with dependency injection.
        /// </summary>
        public UserItemPreferencesController(ApplicationDbContext context, ILogger<UserItemPreferencesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: UserItemPreferences
        /// <summary>
        /// Displays a list of user item preferences (non-deleted only).
        ///
        /// Query behavior:
        /// - Filters out soft-deleted preferences (DeletedAt == null).
        /// - Eager-loads related entities for display:
        ///     - User
        ///     - InventoryItem (and its Category)
        ///     - CreatedByUser / UpdatedByUser (audit navigation props)
        /// - Orders results by user name, then by inventory item name for a readable listing.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading UserItemPreferences Index page");

            // Load preferences and related entities so the view can display meaningful info without extra queries.
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
        /// Displays details for a single UserItemPreference by its primary key Id.
        ///
        /// Behavior:
        /// - Returns 404 if id is null or preference not found (or soft deleted).
        /// - Eager-loads related entities (User, InventoryItem->Category, audit navigation props).
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for UserItemPreference Id {Id}", id);

            var userItemPreference = await _context.UserItemPreferences
                .Where(u => u.DeletedAt == null)
                .Include(u => u.User)
                .Include(u => u.InventoryItem)
                    .ThenInclude(i => i.Category)
                .Include(u => u.CreatedByUser)
                .Include(u => u.UpdatedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (userItemPreference == null)
            {
                _logger.LogWarning("UserItemPreference Id {Id} not found", id);
                return NotFound();
            }

            return View(userItemPreference);
        }

        // GET: UserItemPreferences/Create
        /// <summary>
        /// Shows the Create form for a new UserItemPreference.
        ///
        /// Important:
        /// - This form needs dropdowns for User, InventoryItem, and Preference options.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create UserItemPreference page");
            await PopulateDropdowns();
            return View();
        }

        // POST: UserItemPreferences/Create
        /// <summary>
        /// Processes form submission to create a new UserItemPreference.
        ///
        /// Security:
        /// - [ValidateAntiForgeryToken] provides CSRF protection.
        ///
        /// Binding:
        /// - [Bind("UserId,InventoryItemId,Preference")] limits which properties are accepted (prevents over-posting).
        ///
        /// Validation:
        /// - Removes navigation properties from ModelState because they are not posted by forms.
        /// - If invalid, repopulates dropdowns and re-renders the Create view.
        ///
        /// Business rule enforcement:
        /// - Prevents duplicates: one preference per (UserId, InventoryItemId) pair.
        ///
        /// Audit:
        /// - Sets CreatedAt/UpdatedAt and placeholder CreatedBy/UpdatedBy user IDs.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,InventoryItemId,Preference")] UserItemPreference userItemPreference)
        {
            _logger.LogInformation(
                "Attempting to create UserItemPreference for UserId {UserId} and InventoryItemId {ItemId}",
                userItemPreference.UserId, userItemPreference.InventoryItemId);

            // Navigation and audit navigation properties aren't posted by forms; remove them from ModelState.
            ModelState.Remove(nameof(UserItemPreference.User));
            ModelState.Remove(nameof(UserItemPreference.InventoryItem));
            ModelState.Remove(nameof(UserItemPreference.CreatedByUser));
            ModelState.Remove(nameof(UserItemPreference.UpdatedByUser));

            // If standard model validation fails, rebuild dropdowns and return the view.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning(
                    "Create UserItemPreference failed validation for UserId {UserId} / ItemId {ItemId}",
                    userItemPreference.UserId, userItemPreference.InventoryItemId);

                await PopulateDropdowns(userItemPreference.UserId, userItemPreference.InventoryItemId, userItemPreference.Preference);
                return View(userItemPreference);
            }

            // Duplicate check:
            // - If a non-deleted record already exists for this (UserId, InventoryItemId), block creation.
            var duplicateExists = await _context.UserItemPreferences
                .AnyAsync(u =>
                    u.DeletedAt == null &&
                    u.UserId == userItemPreference.UserId &&
                    u.InventoryItemId == userItemPreference.InventoryItemId);

            if (duplicateExists)
            {
                _logger.LogWarning(
                    "Duplicate UserItemPreference detected for UserId {UserId} / ItemId {ItemId}",
                    userItemPreference.UserId, userItemPreference.InventoryItemId);

                ModelState.AddModelError("", "A preference for this user and inventory item already exists.");
                await PopulateDropdowns(userItemPreference.UserId, userItemPreference.InventoryItemId, userItemPreference.Preference);
                return View(userItemPreference);
            }

            // Set audit fields.
            var now = DateTime.UtcNow;
            userItemPreference.CreatedAt = now;
            userItemPreference.UpdatedAt = now;
            userItemPreference.CreatedByUserId = null; // Placeholder until auth integration.
            userItemPreference.UpdatedByUserId = null; // Placeholder until auth integration.

            // Stage insert.
            _context.Add(userItemPreference);

            try
            {
                // Persist to database.
                await _context.SaveChangesAsync();

                _logger.LogInformation("UserItemPreference Id {Id} created successfully", userItemPreference.Id);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // Likely causes: FK constraints, DB connectivity, uniqueness constraints if applied at DB level, etc.
                _logger.LogError(
                    ex,
                    "Error creating UserItemPreference for UserId {UserId} / ItemId {ItemId}",
                    userItemPreference.UserId, userItemPreference.InventoryItemId);

                ModelState.AddModelError("", "Unable to save preference.");

                // Repopulate dropdowns so the view can render correctly on error.
                await PopulateDropdowns(userItemPreference.UserId, userItemPreference.InventoryItemId, userItemPreference.Preference);

                return View(userItemPreference);
            }
        }

        // GET: UserItemPreferences/Edit/5
        /// <summary>
        /// Shows the Edit form for an existing UserItemPreference.
        ///
        /// Behavior:
        /// - Returns 404 if id is null or record not found (or soft deleted).
        /// - Populates dropdowns so the form can show current selections.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for UserItemPreference Id {Id}", id);

            // Load the preference being edited (non-deleted only).
            var userItemPreference = await _context.UserItemPreferences
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (userItemPreference == null)
            {
                _logger.LogWarning("UserItemPreference Id {Id} not found for edit", id);
                return NotFound();
            }

            // Populate dropdowns with the current selections.
            await PopulateDropdowns(userItemPreference.UserId, userItemPreference.InventoryItemId, userItemPreference.Preference);

            return View(userItemPreference);
        }

        // POST: UserItemPreferences/Edit/5
        /// <summary>
        /// Processes form submission to update an existing UserItemPreference.
        ///
        /// Parameters:
        /// - id: route Id
        /// - formModel: posted model limited to editable fields
        ///
        /// Safety checks:
        /// - Confirms route id matches posted model id.
        /// - Removes navigation properties from ModelState (not posted by forms).
        ///
        /// Business rule enforcement:
        /// - Prevents duplicates by ensuring no other non-deleted record exists with the same
        ///   (UserId, InventoryItemId) pair.
        ///
        /// Audit:
        /// - Updates UpdatedAt/UpdatedByUserId.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,UserId,InventoryItemId,Preference")] UserItemPreference formModel)
        {
            _logger.LogInformation("Attempting to edit UserItemPreference Id {Id}", id);

            // Prevent mismatched/tampered requests by validating route id equals model id.
            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            // Remove navigation properties from validation.
            ModelState.Remove(nameof(UserItemPreference.User));
            ModelState.Remove(nameof(UserItemPreference.InventoryItem));
            ModelState.Remove(nameof(UserItemPreference.CreatedByUser));
            ModelState.Remove(nameof(UserItemPreference.UpdatedByUser));

            // If invalid, repopulate dropdowns and re-render view with validation messages.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit UserItemPreference failed validation for Id {Id}", id);
                await PopulateDropdowns(formModel.UserId, formModel.InventoryItemId, formModel.Preference);
                return View(formModel);
            }

            // Load existing record to apply updates safely.
            var existing = await _context.UserItemPreferences
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (existing == null)
            {
                _logger.LogWarning("UserItemPreference Id {Id} not found during edit save", id);
                return NotFound();
            }

            // Duplicate check for edit:
            // - Any other record (u.Id != id) with same UserId and InventoryItemId is not allowed.
            var duplicateExists = await _context.UserItemPreferences
                .AnyAsync(u =>
                    u.Id != id &&
                    u.DeletedAt == null &&
                    u.UserId == formModel.UserId &&
                    u.InventoryItemId == formModel.InventoryItemId);

            if (duplicateExists)
            {
                _logger.LogWarning(
                    "Duplicate detected on edit for UserId {UserId} / ItemId {ItemId}",
                    formModel.UserId, formModel.InventoryItemId);

                ModelState.AddModelError("", "A preference for this user and inventory item already exists.");
                await PopulateDropdowns(formModel.UserId, formModel.InventoryItemId, formModel.Preference);
                return View(formModel);
            }

            // Apply updates to tracked entity.
            existing.UserId = formModel.UserId;
            existing.InventoryItemId = formModel.InventoryItemId;
            existing.Preference = formModel.Preference;

            // Audit.
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // Placeholder until auth integration.

            try
            {
                // Persist changes.
                await _context.SaveChangesAsync();

                _logger.LogInformation("UserItemPreference Id {Id} updated successfully", id);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Concurrency exception indicates record changed/deleted between load and save.
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

                // Rebuild dropdowns so the view can render correctly after error.
                await PopulateDropdowns(formModel.UserId, formModel.InventoryItemId, formModel.Preference);

                return View(formModel);
            }
        }

        // GET: UserItemPreferences/Delete/5
        /// <summary>
        /// Shows the Delete confirmation page for a UserItemPreference.
        ///
        /// Notes:
        /// - This GET action does not delete anything.
        /// - It loads the record + related entities so the user can confirm they're deleting the right preference.
        /// - Actual soft delete occurs in DeleteConfirmed.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for UserItemPreference Id {Id}", id);

            // Load record for confirmation view, including related entities for context.
            var userItemPreference = await _context.UserItemPreferences
                .Where(u => u.DeletedAt == null)
                .Include(u => u.User)
                .Include(u => u.InventoryItem)
                    .ThenInclude(i => i.Category)
                .Include(u => u.CreatedByUser)
                .Include(u => u.UpdatedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (userItemPreference == null)
            {
                _logger.LogWarning("UserItemPreference Id {Id} not found for delete", id);
                return NotFound();
            }

            return View(userItemPreference);
        }

        // POST: UserItemPreferences/Delete/5
        /// <summary>
        /// Executes the delete operation (soft delete) for a UserItemPreference.
        ///
        /// Soft delete strategy:
        /// - Sets DeletedAt
        /// - Updates audit fields
        /// - Keeps the row in the database for history and referential integrity
        ///
        /// Note:
        /// - This delete path does not have a try/catch; failures will bubble up to the global error handler.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting UserItemPreference Id {Id}", id);

            // Load the record to soft delete (must not already be deleted).
            var userItemPreference = await _context.UserItemPreferences
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (userItemPreference == null)
            {
                _logger.LogWarning("UserItemPreference Id {Id} not found during delete", id);
                return NotFound();
            }

            // Apply soft delete + audit updates.
            userItemPreference.DeletedAt = DateTime.UtcNow;
            userItemPreference.UpdatedAt = DateTime.UtcNow;
            userItemPreference.UpdatedByUserId = null;

            // Persist changes.
            await _context.SaveChangesAsync();

            _logger.LogInformation("UserItemPreference Id {Id} soft deleted", id);

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Populates dropdown lists for Create/Edit views.
        ///
        /// Dropdowns:
        /// - UserId: active, non-deleted DomainUsers displayed via BuildUserDisplayName
        /// - InventoryItemId: non-deleted InventoryItems displayed as
        ///       "CategoryGroup - Category - Item"
        /// - PreferenceOptions: enum values for PreferenceOption displayed by enum name
        ///
        /// Parameters:
        /// - selectedUserId / selectedInventoryItemId / selectedPreference:
        ///   used to preserve the selected values when re-rendering forms after validation errors.
        ///
        /// Logging:
        /// - Logs the final counts of users/items available for selection.
        /// </summary>
        private async Task PopulateDropdowns(ulong? selectedUserId = null, ulong? selectedInventoryItemId = null, PreferenceOption? selectedPreference = null)
        {
            // Load eligible users for selection: not deleted, active only.
            var users = await _context.DomainUsers
                .Where(u => u.DeletedAt == null && u.IsActive)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            // Create simple option objects for SelectList, using a consistent user-friendly display label.
            var userOptions = users
                .Select(u => new { u.Id, DisplayName = BuildUserDisplayName(u) })
                .ToList();

            // Load eligible inventory items for selection.
            // Includes category hierarchy so the label can provide full context.
            var inventoryItems = await _context.InventoryItems
                .Where(i => i.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .OrderBy(i => i.Category.CategoryGroup.Name)
                .ThenBy(i => i.Category.Name)
                .ThenBy(i => i.Name)
                .ToListAsync();

            // Create a display label that clearly identifies where the item lives in the hierarchy.
            var inventoryOptions = inventoryItems
                .Select(i => new
                {
                    i.Id,
                    DisplayName = $"{i.Category.CategoryGroup.Name} - {i.Category.Name} - {i.Name}"
                })
                .ToList();

            // Store SelectLists in ViewData for Razor views to render <select> elements.
            ViewData["UserId"] = new SelectList(userOptions, "Id", "DisplayName", selectedUserId);
            ViewData["InventoryItemId"] = new SelectList(inventoryOptions, "Id", "DisplayName", selectedInventoryItemId);

            // Build a SelectList for the PreferenceOption enum.
            // Each option has:
            // - Value: enum value
            // - Text: enum name via ToString()
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
        /// Builds a user-friendly display label for a DomainUser.
        ///
        /// Output preference order:
        /// 1) "Last, First (email)" when both name and email exist
        /// 2) "Last, First" when only name exists
        /// 3) "email" when only email exists
        /// 4) "User #Id" fallback when neither name nor email exists
        ///
        /// Notes:
        /// - Trims whitespace from each component.
        /// - Uses string.Join with filtering to avoid ugly commas when one part is missing.
        /// </summary>
        private static string BuildUserDisplayName(DomainUser u)
        {
            var first = (u.FirstName ?? string.Empty).Trim();
            var last = (u.LastName ?? string.Empty).Trim();
            var email = (u.Email ?? string.Empty).Trim();

            // Build "Last, First" using only non-empty pieces.
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
        /// Helper method used by Edit POST concurrency handling.
        /// Confirms whether a non-deleted UserItemPreference exists for the provided Id.
        /// </summary>
        private async Task<bool> UserItemPreferenceExists(ulong id)
        {
            return await _context.UserItemPreferences.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}