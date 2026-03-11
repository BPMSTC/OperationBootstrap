using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages create, read, update, and soft delete operations for categories.
    /// </summary>
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        /// <summary>
        /// Creates the controller with the required database context and logger.
        /// </summary>
        public CategoriesController(ApplicationDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Categories
        /// <summary>
        /// Displays all non-deleted categories.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Fetching category list");

            // Retrieve active categories with related category group and parent data.
            var categories = await _context.Categories
                .Where(c => c.DeletedAt == null)
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
        /// <summary>
        /// Displays details for a single non-deleted category.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            // Reject requests with no id.
            if (id == null)
            {
                _logger.LogWarning("Category Details requested with null id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for CategoryId {Id}", id);

            // Retrieve the requested active category with related category group and parent data.
            var category = await _context.Categories
                .Where(c => c.DeletedAt == null)
                .Include(c => c.CategoryGroup)
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Return not found when the category does not exist.
            if (category == null)
            {
                _logger.LogWarning("Category {Id} not found", id);
                return NotFound();
            }

            return View(category);
        }

        // GET: Categories/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create Category page");

            // Populate dropdown values for the create form.
            await PopulateDropdowns();
            return View();
        }

        // POST: Categories/Create
        /// <summary>
        /// Creates a new category after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryGroupId,ParentId,Name,SortOrder,IsActive")] Category category)
        {
            _logger.LogInformation(
                "Attempting to create category {Name} in CategoryGroupId {GroupId}",
                category.Name,
                category.CategoryGroupId);

            // Remove navigation properties that are not posted by the form.
            ModelState.Remove(nameof(Category.CategoryGroup));
            ModelState.Remove(nameof(Category.Parent));
            ModelState.Remove(nameof(Category.Children));
            ModelState.Remove(nameof(Category.CreatedByUser));
            ModelState.Remove(nameof(Category.UpdatedByUser));

            // Normalize incoming values before business-rule validation.
            NormalizeCategory(category);
            await ApplyCategoryValidationAsync(category);

            // Return the form with dropdowns restored when validation fails.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create Category failed validation");
                await PopulateDropdowns(category.CategoryGroupId, category.ParentId);
                return View(category);
            }

            // Set audit fields for the new category record.
            var now = DateTime.UtcNow;
            category.CreatedAt = now;
            category.UpdatedAt = now;
            category.CreatedByUserId = null;
            category.UpdatedByUserId = null;

            // Queue the new category for insert.
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
        /// <summary>
        /// Shows the edit form for a single non-deleted category.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            // Reject requests with no id.
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null id");
                return NotFound();
            }

            // Retrieve the requested active category for editing.
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            // Return not found when the category does not exist.
            if (category == null)
            {
                _logger.LogWarning("Category {Id} not found for edit", id);
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for CategoryId {Id}", id);

            // Populate dropdown values using the current record selections.
            await PopulateDropdowns(category.CategoryGroupId, category.ParentId, excludeCategoryId: category.Id);

            return View(category);
        }

        // POST: Categories/Edit/5
        /// <summary>
        /// Updates an existing category after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,CategoryGroupId,ParentId,Name,SortOrder,IsActive")] Category formModel)
        {
            _logger.LogInformation("Attempting to edit CategoryId {Id}", id);

            // Ensure the route id matches the posted model id.
            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route id {RouteId} vs model id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            // Remove navigation properties that are not posted by the form.
            ModelState.Remove(nameof(Category.CategoryGroup));
            ModelState.Remove(nameof(Category.Parent));
            ModelState.Remove(nameof(Category.Children));
            ModelState.Remove(nameof(Category.CreatedByUser));
            ModelState.Remove(nameof(Category.UpdatedByUser));

            // Normalize incoming values before business-rule validation.
            NormalizeCategory(formModel);
            await ApplyCategoryValidationAsync(formModel, formModel.Id);

            // Return the form with dropdowns restored when validation fails.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit CategoryId {Id} failed validation", id);
                await PopulateDropdowns(formModel.CategoryGroupId, formModel.ParentId, excludeCategoryId: formModel.Id);
                return View(formModel);
            }

            // Retrieve the existing active category record.
            var existing = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            // Return not found when the target record no longer exists.
            if (existing == null)
            {
                _logger.LogWarning("Category {Id} not found during edit save", id);
                return NotFound();
            }

            // Copy validated form values into the tracked entity.
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
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted category.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            // Reject requests with no id.
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null id");
                return NotFound();
            }

            // Retrieve the requested active category with related category group and parent data.
            var category = await _context.Categories
                .Where(c => c.DeletedAt == null)
                .Include(c => c.CategoryGroup)
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Return not found when the category does not exist.
            if (category == null)
            {
                _logger.LogWarning("Category {Id} not found for delete", id);
                return NotFound();
            }

            _logger.LogWarning("Loading Delete confirmation for CategoryId {Id}", id);

            return View(category);
        }

        // POST: Categories/Delete/5
        /// <summary>
        /// Soft deletes a category.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting CategoryId {Id}", id);

            // Retrieve the active category targeted for soft delete.
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            // Return not found when the category does not exist.
            if (category == null)
            {
                _logger.LogWarning("Category {Id} not found during delete", id);
                return NotFound();
            }

            // Apply soft-delete and audit values.
            category.DeletedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedByUserId = null;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Category {Id} soft deleted", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error soft deleting CategoryId {Id}", id);

                TempData["ErrorMessage"] = "Unable to delete category.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Populates dropdown lists for the create and edit forms.
        /// </summary>
        private async Task PopulateDropdowns(
            ulong? selectedCategoryGroupId = null,
            ulong? selectedParentId = null,
            ulong? excludeCategoryId = null)
        {
            _logger.LogDebug("Populating category dropdowns");

            // Retrieve active category groups for the category group dropdown.
            var categoryGroups = await _context.CategoryGroups
                .Where(g => g.DeletedAt == null)
                .OrderBy(g => g.SortOrder)
                .ThenBy(g => g.Name)
                .ToListAsync();

            // Build the base query for active categories used in the parent dropdown.
            var categoriesQuery = _context.Categories
                .Where(c => c.DeletedAt == null)
                .OrderBy(c => c.Name)
                .AsQueryable();

            // Exclude the current category from parent options during edit.
            if (excludeCategoryId.HasValue)
            {
                categoriesQuery = categoriesQuery.Where(c => c.Id != excludeCategoryId.Value);
            }

            // Retrieve available parent categories for the parent dropdown.
            var parentCategories = await categoriesQuery.ToListAsync();

            // Store the category group and parent dropdown options in ViewData.
            ViewData["CategoryGroupId"] = new SelectList(categoryGroups, "Id", "Name", selectedCategoryGroupId);
            ViewData["ParentId"] = new SelectList(parentCategories, "Id", "Name", selectedParentId);
        }

        /// <summary>
        /// Returns true if the non-deleted category exists.
        /// </summary>
        private async Task<bool> CategoryExists(ulong id)
        {
            // Check whether the requested active category still exists.
            return await _context.Categories.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Trims strings and normalizes required values.
        /// </summary>
        private static void NormalizeCategory(Category model)
        {
            // Normalize and trim the required category name.
            model.Name = model.Name?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyCategoryValidationAsync(Category model, ulong? currentId = null)
        {
            // Validate that the selected category group exists and is not deleted.
            var categoryGroupExists = await _context.CategoryGroups
                .AnyAsync(g => g.Id == model.CategoryGroupId && g.DeletedAt == null);

            if (!categoryGroupExists)
            {
                ModelState.AddModelError(nameof(Category.CategoryGroupId), "Select a valid category group.");
            }

            // Require a category name.
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(Category.Name), "Category name is required.");
            }

            // Require at least one letter or number in the category name.
            if (!string.IsNullOrWhiteSpace(model.Name) && !ContainsLetterOrDigit(model.Name))
            {
                ModelState.AddModelError(nameof(Category.Name), "Category name must contain letters or numbers.");
            }

            // Prevent negative sort order values.
            if (model.SortOrder < 0)
            {
                ModelState.AddModelError(nameof(Category.SortOrder), "Sort Order cannot be less than 0.");
            }

            // Validate parent category rules when a parent is selected.
            if (model.ParentId.HasValue)
            {
                if (currentId.HasValue && model.ParentId.Value == currentId.Value)
                {
                    ModelState.AddModelError(nameof(Category.ParentId), "A category cannot be its own parent.");
                }
                else
                {
                    var parentCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Id == model.ParentId.Value && c.DeletedAt == null);

                    if (parentCategory == null)
                    {
                        ModelState.AddModelError(nameof(Category.ParentId), "Select a valid parent category.");
                    }
                    else if (parentCategory.CategoryGroupId != model.CategoryGroupId)
                    {
                        ModelState.AddModelError(nameof(Category.ParentId), "Parent category must be in the same category group.");
                    }
                }
            }

            // Prevent duplicate active category names within the same category group.
            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                var normalizedName = model.Name.ToLower();

                var duplicateExists = await _context.Categories
                    .AnyAsync(c =>
                        c.DeletedAt == null &&
                        c.Id != currentId &&
                        c.CategoryGroupId == model.CategoryGroupId &&
                        c.Name.ToLower() == normalizedName);

                if (duplicateExists)
                {
                    ModelState.AddModelError(nameof(Category.Name), "A category with this name already exists in the selected category group.");
                }
            }
        }

        /// <summary>
        /// Returns true when the value contains at least one letter or digit.
        /// </summary>
        private static bool ContainsLetterOrDigit(string value)
        {
            // Require at least one alphanumeric character in the value.
            return value.Any(char.IsLetterOrDigit);
        }
    }
}