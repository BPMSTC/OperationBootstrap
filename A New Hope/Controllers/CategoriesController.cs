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
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.CategoryGroup)
                .Include(c => c.Parent)
                .OrderBy(c => c.CategoryGroup.Name)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.CategoryGroup)
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Categories/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryGroupId,ParentId,Name,SortOrder,IsActive")] Category category)
        {
            // Navigation properties are not posted by the form
            ModelState.Remove(nameof(Category.CategoryGroup));
            ModelState.Remove(nameof(Category.Parent));

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(category.CategoryGroupId, category.ParentId);
                return View(category);
            }

            var now = DateTime.UtcNow;
            category.CreatedAt = now;
            category.UpdatedAt = now;
            category.CreatedByUserId = null; // Set later when auth is implemented
            category.UpdatedByUserId = null; // Set later when auth is implemented

            _context.Add(category);

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save. The category name may already exist in that category group.");
                await PopulateDropdowns(category.CategoryGroupId, category.ParentId);
                return View(category);
            }
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            await PopulateDropdowns(category.CategoryGroupId, category.ParentId, excludeCategoryId: category.Id);
            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,CategoryGroupId,ParentId,Name,SortOrder,IsActive")] Category formModel)
        {
            if (id != formModel.Id)
            {
                return NotFound();
            }

            // Navigation properties are not posted by the form
            ModelState.Remove(nameof(Category.CategoryGroup));
            ModelState.Remove(nameof(Category.Parent));

            if (formModel.ParentId == formModel.Id)
            {
                ModelState.AddModelError("ParentId", "A category cannot be its own parent.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(formModel.CategoryGroupId, formModel.ParentId, excludeCategoryId: formModel.Id);
                return View(formModel);
            }

            var existing = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (existing == null)
            {
                return NotFound();
            }

            // Update editable fields only
            existing.CategoryGroupId = formModel.CategoryGroupId;
            existing.ParentId = formModel.ParentId;
            existing.Name = formModel.Name;
            existing.SortOrder = formModel.SortOrder;
            existing.IsActive = formModel.IsActive;

            // Audit
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // Set later when auth is implemented

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. The category name may already exist in that category group.");
                await PopulateDropdowns(formModel.CategoryGroupId, formModel.ParentId, excludeCategoryId: formModel.Id);
                return View(formModel);
            }
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.CategoryGroup)
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            // Soft delete
            category.DeletedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedByUserId = null; // Set later when auth is implemented

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(ulong? selectedCategoryGroupId = null, ulong? selectedParentId = null, ulong? excludeCategoryId = null)
        {
            var categoryGroups = await _context.CategoryGroups
                .OrderBy(g => g.SortOrder)
                .ThenBy(g => g.Name)
                .ToListAsync();

            var categoriesQuery = _context.Categories
                .OrderBy(c => c.Name)
                .AsQueryable();

            if (excludeCategoryId.HasValue)
            {
                categoriesQuery = categoriesQuery.Where(c => c.Id != excludeCategoryId.Value);
            }

            var parentCategories = await categoriesQuery.ToListAsync();

            ViewData["CategoryGroupId"] = new SelectList(categoryGroups, "Id", "Name", selectedCategoryGroupId);
            ViewData["ParentId"] = new SelectList(parentCategories, "Id", "Name", selectedParentId);
        }

        private async Task<bool> CategoryExists(ulong id)
        {
            return await _context.Categories.AnyAsync(e => e.Id == id);
        }
    }
}