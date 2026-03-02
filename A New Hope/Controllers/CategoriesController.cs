using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(
            ApplicationDbContext context,
            ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Fetching category list");

            var categories = await _context.Categories
                .Include(c => c.CategoryGroup)
                .Include(c => c.Parent)
                .OrderBy(c => c.CategoryGroup.Name)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} categories", categories.Count);

            return View(categories);
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Category Details requested with null id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for CategoryId {Id}", id);

            var category = await _context.Categories
                .Include(c => c.CategoryGroup)
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                _logger.LogWarning("Category {Id} not found", id);
                return NotFound();
            }

            return View(category);
        }

        // GET: Categories/Create
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create Category page");
            await PopulateDropdowns();
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryGroupId,ParentId,Name,SortOrder,IsActive")] Category category)
        {
            _logger.LogInformation(
                "Attempting to create category {Name} in CategoryGroupId {GroupId}",
                category.Name,
                category.CategoryGroupId);

            ModelState.Remove(nameof(Category.CategoryGroup));
            ModelState.Remove(nameof(Category.Parent));

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create Category failed validation");
                await PopulateDropdowns(category.CategoryGroupId, category.ParentId);
                return View(category);
            }

            var now = DateTime.UtcNow;
            category.CreatedAt = now;
            category.UpdatedAt = now;
            category.CreatedByUserId = null;
            category.UpdatedByUserId = null;

            _context.Add(category);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Category {Name} created successfully (Id {Id})", category.Name, category.Id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error creating category {Name}", category.Name);
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
                _logger.LogWarning("Edit requested with null id");
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category {Id} not found for edit", id);
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for CategoryId {Id}", id);

            await PopulateDropdowns(category.CategoryGroupId, category.ParentId, excludeCategoryId: category.Id);
            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,CategoryGroupId,ParentId,Name,SortOrder,IsActive")] Category formModel)
        {
            _logger.LogInformation("Attempting to edit CategoryId {Id}", id);

            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route id {RouteId} vs model id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            ModelState.Remove(nameof(Category.CategoryGroup));
            ModelState.Remove(nameof(Category.Parent));

            if (formModel.ParentId == formModel.Id)
            {
                _logger.LogWarning("Category {Id} attempted to be its own parent", id);
                ModelState.AddModelError("ParentId", "A category cannot be its own parent.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit CategoryId {Id} failed validation", id);
                await PopulateDropdowns(formModel.CategoryGroupId, formModel.ParentId, excludeCategoryId: formModel.Id);
                return View(formModel);
            }

            var existing = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (existing == null)
            {
                _logger.LogWarning("Category {Id} not found during edit save", id);
                return NotFound();
            }

            existing.CategoryGroupId = formModel.CategoryGroupId;
            existing.ParentId = formModel.ParentId;
            existing.Name = formModel.Name;
            existing.SortOrder = formModel.SortOrder;
            existing.IsActive = formModel.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Category {Id} updated successfully", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating CategoryId {Id}", id);
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
                _logger.LogWarning("Delete requested with null id");
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.CategoryGroup)
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                _logger.LogWarning("Category {Id} not found for delete", id);
                return NotFound();
            }

            _logger.LogWarning("Loading Delete confirmation for CategoryId {Id}", id);

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting CategoryId {Id}", id);

            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                _logger.LogWarning("Category {Id} not found during delete", id);
                return NotFound();
            }

            category.DeletedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedByUserId = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Category {Id} soft deleted", id);

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(
            ulong? selectedCategoryGroupId = null,
            ulong? selectedParentId = null,
            ulong? excludeCategoryId = null)
        {
            _logger.LogDebug("Populating category dropdowns");

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