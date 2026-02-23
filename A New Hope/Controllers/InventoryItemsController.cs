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
    public class InventoryItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: InventoryItems
        public async Task<IActionResult> Index()
        {
            var inventoryItems = await _context.InventoryItems
                .Where(i => i.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .OrderBy(i => i.Category.Name)
                .ThenBy(i => i.Name)
                .ToListAsync();

            return View(inventoryItems);
        }

        // GET: InventoryItems/Details/5
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventoryItem = await _context.InventoryItems
                .Where(i => i.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            return View(inventoryItem);
        }

        // GET: InventoryItems/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: InventoryItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,CategoryId,IsBaseline,IsAvailable,IsActive")] InventoryItem inventoryItem)
        {
            // Navigation properties are not posted by the form
            ModelState.Remove(nameof(InventoryItem.Category));
            ModelState.Remove(nameof(InventoryItem.CreatedByUser));
            ModelState.Remove(nameof(InventoryItem.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(inventoryItem.CategoryId);
                return View(inventoryItem);
            }

            var now = DateTime.UtcNow;
            inventoryItem.CreatedAt = now;
            inventoryItem.UpdatedAt = now;
            inventoryItem.CreatedByUserId = null; // set later when auth is implemented
            inventoryItem.UpdatedByUserId = null; // set later when auth is implemented

            _context.Add(inventoryItem);

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save inventory item.");
                await PopulateDropdowns(inventoryItem.CategoryId);
                return View(inventoryItem);
            }
        }

        // GET: InventoryItems/Edit/5
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            await PopulateDropdowns(inventoryItem.CategoryId);
            return View(inventoryItem);
        }

        // POST: InventoryItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Name,CategoryId,IsBaseline,IsAvailable,IsActive")] InventoryItem formModel)
        {
            if (id != formModel.Id)
            {
                return NotFound();
            }

            // Navigation properties are not posted by the form
            ModelState.Remove(nameof(InventoryItem.Category));
            ModelState.Remove(nameof(InventoryItem.CreatedByUser));
            ModelState.Remove(nameof(InventoryItem.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(formModel.CategoryId);
                return View(formModel);
            }

            var existing = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

            if (existing == null)
            {
                return NotFound();
            }

            // Update editable fields only
            existing.Name = formModel.Name;
            existing.CategoryId = formModel.CategoryId;
            existing.IsBaseline = formModel.IsBaseline;
            existing.IsAvailable = formModel.IsAvailable;
            existing.IsActive = formModel.IsActive;

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
                if (!await InventoryItemExists(formModel.Id))
                {
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes.");
                await PopulateDropdowns(formModel.CategoryId);
                return View(formModel);
            }
        }

        // GET: InventoryItems/Delete/5
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventoryItem = await _context.InventoryItems
                .Where(i => i.DeletedAt == null)
                .Include(i => i.Category)
                    .ThenInclude(c => c.CategoryGroup)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            return View(inventoryItem);
        }

        // POST: InventoryItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            // Soft delete
            inventoryItem.DeletedAt = DateTime.UtcNow;
            inventoryItem.UpdatedAt = DateTime.UtcNow;
            inventoryItem.UpdatedByUserId = null; // set later when auth is implemented

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Unable to delete inventory item.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(ulong? selectedCategoryId = null)
        {
            var categories = await _context.Categories
                .Where(c => c.DeletedAt == null && c.CategoryGroup.DeletedAt == null)
                .Include(c => c.CategoryGroup)
                .OrderBy(c => c.CategoryGroup.Name)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            // Display as: "Food - Canned Goods"
            var categoryOptions = categories
                .Select(c => new
                {
                    c.Id,
                    DisplayName = $"{c.CategoryGroup.Name} - {c.Name}"
                })
                .ToList();

            ViewData["CategoryId"] = new SelectList(categoryOptions, "Id", "DisplayName", selectedCategoryId);
        }

        private async Task<bool> InventoryItemExists(ulong id)
        {
            return await _context.InventoryItems.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}