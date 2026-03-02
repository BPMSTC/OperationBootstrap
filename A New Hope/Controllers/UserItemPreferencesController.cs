using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using A_New_Hope.Data;
using A_New_Hope.Models;

namespace A_New_Hope.Controllers
{
    public class UserItemPreferencesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserItemPreferencesController> _logger;

        public UserItemPreferencesController(ApplicationDbContext context, ILogger<UserItemPreferencesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: UserItemPreferences
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading UserItemPreferences Index page");

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
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create UserItemPreference page");
            await PopulateDropdowns();
            return View();
        }

        // POST: UserItemPreferences/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,InventoryItemId,Preference")] UserItemPreference userItemPreference)
        {
            _logger.LogInformation("Attempting to create UserItemPreference for UserId {UserId} and InventoryItemId {ItemId}",
                userItemPreference.UserId, userItemPreference.InventoryItemId);

            ModelState.Remove(nameof(UserItemPreference.User));
            ModelState.Remove(nameof(UserItemPreference.InventoryItem));
            ModelState.Remove(nameof(UserItemPreference.CreatedByUser));
            ModelState.Remove(nameof(UserItemPreference.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create UserItemPreference failed validation for UserId {UserId} / ItemId {ItemId}",
                    userItemPreference.UserId, userItemPreference.InventoryItemId);
                await PopulateDropdowns(userItemPreference.UserId, userItemPreference.InventoryItemId, userItemPreference.Preference);
                return View(userItemPreference);
            }

            var duplicateExists = await _context.UserItemPreferences
                .AnyAsync(u =>
                    u.DeletedAt == null &&
                    u.UserId == userItemPreference.UserId &&
                    u.InventoryItemId == userItemPreference.InventoryItemId);

            if (duplicateExists)
            {
                _logger.LogWarning("Duplicate UserItemPreference detected for UserId {UserId} / ItemId {ItemId}",
                    userItemPreference.UserId, userItemPreference.InventoryItemId);
                ModelState.AddModelError("", "A preference for this user and inventory item already exists.");
                await PopulateDropdowns(userItemPreference.UserId, userItemPreference.InventoryItemId, userItemPreference.Preference);
                return View(userItemPreference);
            }

            var now = DateTime.UtcNow;
            userItemPreference.CreatedAt = now;
            userItemPreference.UpdatedAt = now;
            userItemPreference.CreatedByUserId = null;
            userItemPreference.UpdatedByUserId = null;

            _context.Add(userItemPreference);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("UserItemPreference Id {Id} created successfully", userItemPreference.Id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error creating UserItemPreference for UserId {UserId} / ItemId {ItemId}",
                    userItemPreference.UserId, userItemPreference.InventoryItemId);
                ModelState.AddModelError("", "Unable to save preference.");
                await PopulateDropdowns(userItemPreference.UserId, userItemPreference.InventoryItemId, userItemPreference.Preference);
                return View(userItemPreference);
            }
        }

        // GET: UserItemPreferences/Edit/5
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for UserItemPreference Id {Id}", id);

            var userItemPreference = await _context.UserItemPreferences
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (userItemPreference == null)
            {
                _logger.LogWarning("UserItemPreference Id {Id} not found for edit", id);
                return NotFound();
            }

            await PopulateDropdowns(userItemPreference.UserId, userItemPreference.InventoryItemId, userItemPreference.Preference);
            return View(userItemPreference);
        }

        // POST: UserItemPreferences/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,UserId,InventoryItemId,Preference")] UserItemPreference formModel)
        {
            _logger.LogInformation("Attempting to edit UserItemPreference Id {Id}", id);

            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            ModelState.Remove(nameof(UserItemPreference.User));
            ModelState.Remove(nameof(UserItemPreference.InventoryItem));
            ModelState.Remove(nameof(UserItemPreference.CreatedByUser));
            ModelState.Remove(nameof(UserItemPreference.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit UserItemPreference failed validation for Id {Id}", id);
                await PopulateDropdowns(formModel.UserId, formModel.InventoryItemId, formModel.Preference);
                return View(formModel);
            }

            var existing = await _context.UserItemPreferences
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (existing == null)
            {
                _logger.LogWarning("UserItemPreference Id {Id} not found during edit save", id);
                return NotFound();
            }

            var duplicateExists = await _context.UserItemPreferences
                .AnyAsync(u =>
                    u.Id != id &&
                    u.DeletedAt == null &&
                    u.UserId == formModel.UserId &&
                    u.InventoryItemId == formModel.InventoryItemId);

            if (duplicateExists)
            {
                _logger.LogWarning("Duplicate detected on edit for UserId {UserId} / ItemId {ItemId}", formModel.UserId, formModel.InventoryItemId);
                ModelState.AddModelError("", "A preference for this user and inventory item already exists.");
                await PopulateDropdowns(formModel.UserId, formModel.InventoryItemId, formModel.Preference);
                return View(formModel);
            }

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
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for UserItemPreference Id {Id}", id);

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
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting UserItemPreference Id {Id}", id);

            var userItemPreference = await _context.UserItemPreferences
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (userItemPreference == null)
            {
                _logger.LogWarning("UserItemPreference Id {Id} not found during delete", id);
                return NotFound();
            }

            userItemPreference.DeletedAt = DateTime.UtcNow;
            userItemPreference.UpdatedAt = DateTime.UtcNow;
            userItemPreference.UpdatedByUserId = null;

            await _context.SaveChangesAsync();
            _logger.LogInformation("UserItemPreference Id {Id} soft deleted", id);

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(ulong? selectedUserId = null, ulong? selectedInventoryItemId = null, PreferenceOption? selectedPreference = null)
        {
            var users = await _context.DomainUsers
                .Where(u => u.DeletedAt == null && u.IsActive)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            var userOptions = users.Select(u => new { u.Id, DisplayName = BuildUserDisplayName(u) }).ToList();

            var inventoryItems = await _context.InventoryItems
                .Where(i => i.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .OrderBy(i => i.Category.CategoryGroup.Name)
                .ThenBy(i => i.Category.Name)
                .ThenBy(i => i.Name)
                .ToListAsync();

            var inventoryOptions = inventoryItems.Select(i => new { i.Id, DisplayName = $"{i.Category.CategoryGroup.Name} - {i.Category.Name} - {i.Name}" }).ToList();

            ViewData["UserId"] = new SelectList(userOptions, "Id", "DisplayName", selectedUserId);
            ViewData["InventoryItemId"] = new SelectList(inventoryOptions, "Id", "DisplayName", selectedInventoryItemId);
            ViewData["PreferenceOptions"] = new SelectList(
                Enum.GetValues(typeof(PreferenceOption))
                    .Cast<PreferenceOption>()
                    .Select(p => new { Value = p, Text = p.ToString() }),
                "Value",
                "Text",
                selectedPreference);

            _logger.LogInformation("Populated dropdowns: {UserCount} users, {ItemCount} items", userOptions.Count, inventoryOptions.Count);
        }

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

        private async Task<bool> UserItemPreferenceExists(ulong id)
        {
            return await _context.UserItemPreferences.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}