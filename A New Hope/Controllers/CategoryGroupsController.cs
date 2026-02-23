using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using A_New_Hope.Data;
using A_New_Hope.Models;

namespace A_New_Hope.Controllers
{
    public class CategoryGroupsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryGroupsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CategoryGroups
        public async Task<IActionResult> Index()
        {
            var categoryGroups = await _context.CategoryGroups
                .OrderBy(cg => cg.SortOrder)
                .ThenBy(cg => cg.Name)
                .ToListAsync();

            return View(categoryGroups);
        }

        // GET: CategoryGroups/Details/5
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoryGroup = await _context.CategoryGroups
                .FirstOrDefaultAsync(m => m.Id == id);

            if (categoryGroup == null)
            {
                return NotFound();
            }

            return View(categoryGroup);
        }

        // GET: CategoryGroups/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CategoryGroups/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,SortOrder,IsActive")] CategoryGroup categoryGroup)
        {
            if (!ModelState.IsValid)
            {
                return View(categoryGroup);
            }

            var now = DateTime.UtcNow;

            categoryGroup.CreatedAt = now;
            categoryGroup.UpdatedAt = now;
            categoryGroup.CreatedByUserId = null; // Set later when auth is implemented
            categoryGroup.UpdatedByUserId = null; // Set later when auth is implemented

            _context.Add(categoryGroup);

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save. The category group name may already exist.");
                return View(categoryGroup);
            }
        }

        // GET: CategoryGroups/Edit/5
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoryGroup = await _context.CategoryGroups.FindAsync(id);
            if (categoryGroup == null)
            {
                return NotFound();
            }

            return View(categoryGroup);
        }

        // POST: CategoryGroups/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Name,SortOrder,IsActive")] CategoryGroup formModel)
        {
            if (id != formModel.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(formModel);
            }

            var existing = await _context.CategoryGroups.FirstOrDefaultAsync(cg => cg.Id == id);
            if (existing == null)
            {
                return NotFound();
            }

            // Update editable fields only
            existing.Name = formModel.Name;
            existing.SortOrder = formModel.SortOrder;
            existing.IsActive = formModel.IsActive;

            // Audit fields
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // Set later when auth is implemented

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. The category group name may already exist.");
                return View(formModel);
            }
        }

        // GET: CategoryGroups/Delete/5
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoryGroup = await _context.CategoryGroups
                .FirstOrDefaultAsync(m => m.Id == id);

            if (categoryGroup == null)
            {
                return NotFound();
            }

            return View(categoryGroup);
        }

        // POST: CategoryGroups/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            var categoryGroup = await _context.CategoryGroups.FirstOrDefaultAsync(cg => cg.Id == id);
            if (categoryGroup == null)
            {
                return NotFound();
            }

            // Soft delete
            categoryGroup.DeletedAt = DateTime.UtcNow;
            categoryGroup.UpdatedAt = DateTime.UtcNow;
            categoryGroup.UpdatedByUserId = null; // Set later when auth is implemented

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> CategoryGroupExists(ulong id)
        {
            return await _context.CategoryGroups.AnyAsync(e => e.Id == id);
        }
    }
}