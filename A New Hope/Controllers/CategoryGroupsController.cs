using System;
using System.Linq;
using System.Threading.Tasks;
using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace A_New_Hope.Controllers
{
    public class CategoryGroupsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoryGroupsController> _logger;

        public CategoryGroupsController(
            ApplicationDbContext context,
            ILogger<CategoryGroupsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: CategoryGroups
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Fetching category groups list");

            var categoryGroups = await _context.CategoryGroups
                .OrderBy(cg => cg.SortOrder)
                .ThenBy(cg => cg.Name)
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} category groups", categoryGroups.Count);

            return View(categoryGroups);
        }

        // GET: CategoryGroups/Details/5
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("CategoryGroup Details requested with null id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for CategoryGroupId {Id}", id);

            var categoryGroup = await _context.CategoryGroups
                .FirstOrDefaultAsync(m => m.Id == id);

            if (categoryGroup == null)
            {
                _logger.LogWarning("CategoryGroup {Id} not found", id);
                return NotFound();
            }

            return View(categoryGroup);
        }

        // GET: CategoryGroups/Create
        public IActionResult Create()
        {
            _logger.LogInformation("Loading Create CategoryGroup page");
            return View();
        }

        // POST: CategoryGroups/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,SortOrder,IsActive")] CategoryGroup categoryGroup)
        {
            _logger.LogInformation(
                "Attempting to create CategoryGroup {Name}",
                categoryGroup.Name);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create CategoryGroup failed validation");
                return View(categoryGroup);
            }

            var now = DateTime.UtcNow;
            categoryGroup.CreatedAt = now;
            categoryGroup.UpdatedAt = now;
            categoryGroup.CreatedByUserId = null;
            categoryGroup.UpdatedByUserId = null;

            _context.Add(categoryGroup);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "CategoryGroup {Name} created successfully (Id {Id})",
                    categoryGroup.Name,
                    categoryGroup.Id);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating CategoryGroup {Name}",
                    categoryGroup.Name);

                ModelState.AddModelError("", "Unable to save. The category group name may already exist.");
                return View(categoryGroup);
            }
        }

        // GET: CategoryGroups/Edit/5
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit CategoryGroup requested with null id");
                return NotFound();
            }

            var categoryGroup = await _context.CategoryGroups.FindAsync(id);
            if (categoryGroup == null)
            {
                _logger.LogWarning("CategoryGroup {Id} not found for edit", id);
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for CategoryGroupId {Id}", id);

            return View(categoryGroup);
        }

        // POST: CategoryGroups/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Name,SortOrder,IsActive")] CategoryGroup formModel)
        {
            _logger.LogInformation("Attempting to edit CategoryGroupId {Id}", id);

            if (id != formModel.Id)
            {
                _logger.LogWarning(
                    "Edit mismatch: route id {RouteId} vs model id {ModelId}",
                    id,
                    formModel.Id);

                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit CategoryGroupId {Id} failed validation", id);
                return View(formModel);
            }

            var existing = await _context.CategoryGroups.FirstOrDefaultAsync(cg => cg.Id == id);
            if (existing == null)
            {
                _logger.LogWarning("CategoryGroup {Id} not found during edit save", id);
                return NotFound();
            }

            existing.Name = formModel.Name;
            existing.SortOrder = formModel.SortOrder;
            existing.IsActive = formModel.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("CategoryGroup {Id} updated successfully", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating CategoryGroupId {Id}",
                    id);

                ModelState.AddModelError("", "Unable to save changes. The category group name may already exist.");
                return View(formModel);
            }
        }

        // GET: CategoryGroups/Delete/5
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete CategoryGroup requested with null id");
                return NotFound();
            }

            var categoryGroup = await _context.CategoryGroups
                .FirstOrDefaultAsync(m => m.Id == id);

            if (categoryGroup == null)
            {
                _logger.LogWarning("CategoryGroup {Id} not found for delete", id);
                return NotFound();
            }

            _logger.LogWarning("Loading Delete confirmation for CategoryGroupId {Id}", id);

            return View(categoryGroup);
        }

        // POST: CategoryGroups/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting CategoryGroupId {Id}", id);

            var categoryGroup = await _context.CategoryGroups.FirstOrDefaultAsync(cg => cg.Id == id);
            if (categoryGroup == null)
            {
                _logger.LogWarning("CategoryGroup {Id} not found during delete", id);
                return NotFound();
            }

            categoryGroup.DeletedAt = DateTime.UtcNow;
            categoryGroup.UpdatedAt = DateTime.UtcNow;
            categoryGroup.UpdatedByUserId = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("CategoryGroup {Id} soft deleted", id);

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> CategoryGroupExists(ulong id)
        {
            return await _context.CategoryGroups.AnyAsync(e => e.Id == id);
        }
    }
}