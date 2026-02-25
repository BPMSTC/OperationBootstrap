using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using A_New_Hope.Data;
using A_New_Hope.Models;

namespace A_New_Hope.Controllers
{
    public class UserItemPreferencesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserItemPreferencesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: UserItemPreferences
        public async Task<IActionResult> Index()
        {
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

            return View(userItemPreferences);
        }

        // GET: UserItemPreferences/Details/5
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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
                return NotFound();
            }

            return View(userItemPreference);
        }

        // GET: UserItemPreferences/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: UserItemPreferences/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,InventoryItemId,Preference")] UserItemPreference userItemPreference)
        {
            // Navigation properties are not posted by the form
            ModelState.Remove(nameof(UserItemPreference.User));
            ModelState.Remove(nameof(UserItemPreference.InventoryItem));
            ModelState.Remove(nameof(UserItemPreference.CreatedByUser));
            ModelState.Remove(nameof(UserItemPreference.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(userItemPreference.UserId, userItemPreference.InventoryItemId, userItemPreference.Preference);
                return View(userItemPreference);
            }

            // Prevent duplicate (UserId, InventoryItemId)
            var duplicateExists = await _context.UserItemPreferences
                .AnyAsync(u =>
                    u.DeletedAt == null &&
                    u.UserId == userItemPreference.UserId &&
                    u.InventoryItemId == userItemPreference.InventoryItemId);

            if (duplicateExists)
            {
                ModelState.AddModelError("", "A preference for this user and inventory item already exists.");
                await PopulateDropdowns(userItemPreference.UserId, userItemPreference.InventoryItemId, userItemPreference.Preference);
                return View(userItemPreference);
            }

            var now = DateTime.UtcNow;
            userItemPreference.CreatedAt = now;
            userItemPreference.UpdatedAt = now;
            userItemPreference.CreatedByUserId = null; // set later when auth is implemented
            userItemPreference.UpdatedByUserId = null; // set later when auth is implemented

            _context.Add(userItemPreference);

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
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
                return NotFound();
            }

            var userItemPreference = await _context.UserItemPreferences
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (userItemPreference == null)
            {
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
            if (id != formModel.Id)
            {
                return NotFound();
            }

            // Navigation properties are not posted by the form
            ModelState.Remove(nameof(UserItemPreference.User));
            ModelState.Remove(nameof(UserItemPreference.InventoryItem));
            ModelState.Remove(nameof(UserItemPreference.CreatedByUser));
            ModelState.Remove(nameof(UserItemPreference.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(formModel.UserId, formModel.InventoryItemId, formModel.Preference);
                return View(formModel);
            }

            var existing = await _context.UserItemPreferences
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (existing == null)
            {
                return NotFound();
            }

            // Prevent duplicate (UserId, InventoryItemId) when editing
            var duplicateExists = await _context.UserItemPreferences
                .AnyAsync(u =>
                    u.Id != id &&
                    u.DeletedAt == null &&
                    u.UserId == formModel.UserId &&
                    u.InventoryItemId == formModel.InventoryItemId);

            if (duplicateExists)
            {
                ModelState.AddModelError("", "A preference for this user and inventory item already exists.");
                await PopulateDropdowns(formModel.UserId, formModel.InventoryItemId, formModel.Preference);
                return View(formModel);
            }

            // Update editable fields only
            existing.UserId = formModel.UserId;
            existing.InventoryItemId = formModel.InventoryItemId;
            existing.Preference = formModel.Preference;

            // Audit
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // set later when auth is implemented

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await UserItemPreferenceExists(formModel.Id))
                {
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException)
            {
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
                return NotFound();
            }

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
                return NotFound();
            }

            return View(userItemPreference);
        }

        // POST: UserItemPreferences/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            var userItemPreference = await _context.UserItemPreferences
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (userItemPreference == null)
            {
                return NotFound();
            }

            // Soft delete
            userItemPreference.DeletedAt = DateTime.UtcNow;
            userItemPreference.UpdatedAt = DateTime.UtcNow;
            userItemPreference.UpdatedByUserId = null; // set later when auth is implemented

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(
            ulong? selectedUserId = null,
            ulong? selectedInventoryItemId = null,
            PreferenceOption? selectedPreference = null)
        {
            var users = await _context.DomainUsers
                .Where(u => u.DeletedAt == null && u.IsActive)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            var userOptions = users.Select(u => new
            {
                u.Id,
                DisplayName = BuildUserDisplayName(u)
            }).ToList();

            var inventoryItems = await _context.InventoryItems
                .Where(i => i.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .OrderBy(i => i.Category.CategoryGroup.Name)
                .ThenBy(i => i.Category.Name)
                .ThenBy(i => i.Name)
                .ToListAsync();

            var inventoryOptions = inventoryItems.Select(i => new
            {
                i.Id,
                DisplayName = $"{i.Category.CategoryGroup.Name} - {i.Category.Name} - {i.Name}"
            }).ToList();

            ViewData["UserId"] = new SelectList(userOptions, "Id", "DisplayName", selectedUserId);
            ViewData["InventoryItemId"] = new SelectList(inventoryOptions, "Id", "DisplayName", selectedInventoryItemId);

            // Enum dropdown for PreferenceOption
            ViewData["PreferenceOptions"] = new SelectList(
                Enum.GetValues(typeof(PreferenceOption))
                    .Cast<PreferenceOption>()
                    .Select(p => new { Value = p, Text = p.ToString() }),
                "Value",
                "Text",
                selectedPreference);
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
            return await _context.UserItemPreferences.AnyAsync(e => e.Id == id);
        }
    }
}