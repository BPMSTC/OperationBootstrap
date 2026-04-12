using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages create, read, update, and soft delete operations for category groups.
    /// </summary>
    public class CategoryGroupsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoryGroupsController> _logger;

        /// <summary>
        /// Creates the controller with the required database context and logger.
        /// </summary>
        public CategoryGroupsController(ApplicationDbContext context, ILogger<CategoryGroupsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: CategoryGroups
        /// <summary>
        /// Displays all non-deleted category groups.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Fetching category groups list");

                // Retrieve active category groups for display.
                var categoryGroups = await _context.CategoryGroups
                    .Where(cg => cg.DeletedAt == null)
                    .OrderBy(cg => cg.Name)
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} category groups", categoryGroups.Count);

                return View(categoryGroups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category groups list");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading category groups.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: CategoryGroups/Details/5
        /// <summary>
        /// Displays details for a single non-deleted category group.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            try
            {
                // Reject requests with no id.
                if (id == null)
                {
                    _logger.LogWarning("CategoryGroup Details requested with null id");
                    return NotFound();
                }

                _logger.LogInformation("Fetching details for CategoryGroupId {Id}", id);

                // Retrieve the requested active category group.
                var categoryGroup = await _context.CategoryGroups
                    .FirstOrDefaultAsync(m => m.Id == id && m.DeletedAt == null);

                // Return not found when the category group does not exist.
                if (categoryGroup == null)
                {
                    _logger.LogWarning("CategoryGroup {Id} not found", id);
                    return NotFound();
                }

                return View(categoryGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for CategoryGroupId {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading category group details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: CategoryGroups/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public IActionResult Create()
        {
            try
            {
                _logger.LogInformation("Loading Create CategoryGroup page");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Create CategoryGroup page");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the create form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CategoryGroups/Create
        /// <summary>
        /// Creates a new category group after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,IsActive")] CategoryGroup categoryGroup)
        {
            try
            {
                _logger.LogInformation("Attempting to create CategoryGroup {Name}", categoryGroup.Name);

                // Remove navigation properties that are not posted by the form.
                ModelState.Remove(nameof(CategoryGroup.Categories));
                ModelState.Remove(nameof(CategoryGroup.CreatedByUser));
                ModelState.Remove(nameof(CategoryGroup.UpdatedByUser));

                // Normalize incoming values before business-rule validation.
                NormalizeCategoryGroup(categoryGroup);
                await ApplyCategoryGroupValidationAsync(categoryGroup);

                // Return the form when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create CategoryGroup failed validation");
                    return View(categoryGroup);
                }

                // Set audit fields for the new category group record.
                var now = DateTime.UtcNow;
                categoryGroup.CreatedAt = now;
                categoryGroup.UpdatedAt = now;
                categoryGroup.CreatedByUserId = null; // Replace when auth/user tracking is added.
                categoryGroup.UpdatedByUserId = null; // Replace when auth/user tracking is added.

                // Queue the new category group for insert.
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
                    _logger.LogError(ex, "Error creating CategoryGroup {Name}", categoryGroup.Name);

                    ModelState.AddModelError("", "Unable to save. The category group name may already exist.");
                    return View(categoryGroup);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating CategoryGroup {Name}", categoryGroup?.Name);
                ModelState.AddModelError("", "An unexpected error occurred while creating the category group.");
                return View(categoryGroup);
            }
        }

        // GET: CategoryGroups/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted category group.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            try
            {
                // Reject requests with no id.
                if (id == null)
                {
                    _logger.LogWarning("Edit CategoryGroup requested with null id");
                    return NotFound();
                }

                // Retrieve the requested active category group for editing.
                var categoryGroup = await _context.CategoryGroups
                    .FirstOrDefaultAsync(cg => cg.Id == id && cg.DeletedAt == null);

                // Return not found when the category group does not exist.
                if (categoryGroup == null)
                {
                    _logger.LogWarning("CategoryGroup {Id} not found for edit", id);
                    return NotFound();
                }

                _logger.LogInformation("Loading Edit page for CategoryGroupId {Id}", id);

                return View(categoryGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit page for CategoryGroupId {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CategoryGroups/Edit/5
        /// <summary>
        /// Updates an existing category group after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Name,IsActive")] CategoryGroup formModel)
        {
            try
            {
                _logger.LogInformation("Attempting to edit CategoryGroupId {Id}", id);

                // Ensure the route id matches the posted model id.
                if (id != formModel.Id)
                {
                    _logger.LogWarning(
                        "Edit mismatch: route id {RouteId} vs model id {ModelId}",
                        id,
                        formModel.Id);

                    return NotFound();
                }

                // Remove navigation properties that are not posted by the form.
                ModelState.Remove(nameof(CategoryGroup.Categories));
                ModelState.Remove(nameof(CategoryGroup.CreatedByUser));
                ModelState.Remove(nameof(CategoryGroup.UpdatedByUser));

                // Normalize incoming values before business-rule validation.
                NormalizeCategoryGroup(formModel);
                await ApplyCategoryGroupValidationAsync(formModel, formModel.Id);

                // Return the form when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Edit CategoryGroupId {Id} failed validation", id);
                    return View(formModel);
                }

                // Retrieve the existing active category group record.
                var existing = await _context.CategoryGroups
                    .FirstOrDefaultAsync(cg => cg.Id == id && cg.DeletedAt == null);

                // Return not found when the target record no longer exists.
                if (existing == null)
                {
                    _logger.LogWarning("CategoryGroup {Id} not found during edit save", id);
                    return NotFound();
                }

                // Copy validated form values into the tracked entity.
                existing.Name = formModel.Name;
                existing.IsActive = formModel.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = null; // Placeholder until auth is implemented.

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("CategoryGroup {Id} updated successfully", id);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error updating CategoryGroupId {Id}", id);

                    ModelState.AddModelError("", "Unable to save changes. The category group name may already exist.");
                    return View(formModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error editing CategoryGroupId {Id}", id);
                ModelState.AddModelError("", "An unexpected error occurred while updating the category group.");
                return View(formModel);
            }
        }

        // GET: CategoryGroups/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted category group.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            try
            {
                // Reject requests with no id.
                if (id == null)
                {
                    _logger.LogWarning("Delete CategoryGroup requested with null id");
                    return NotFound();
                }

                // Retrieve the requested active category group for delete confirmation.
                var categoryGroup = await _context.CategoryGroups
                    .FirstOrDefaultAsync(m => m.Id == id && m.DeletedAt == null);

                // Return not found when the category group does not exist.
                if (categoryGroup == null)
                {
                    _logger.LogWarning("CategoryGroup {Id} not found for delete", id);
                    return NotFound();
                }

                _logger.LogWarning("Loading Delete confirmation for CategoryGroupId {Id}", id);

                return View(categoryGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete page for CategoryGroupId {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the delete page.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CategoryGroups/Delete/5
        /// <summary>
        /// Soft deletes a category group.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            try
            {
                _logger.LogWarning("Soft deleting CategoryGroupId {Id}", id);

                // Retrieve the active category group targeted for soft delete.
                var categoryGroup = await _context.CategoryGroups
                    .FirstOrDefaultAsync(cg => cg.Id == id && cg.DeletedAt == null);

                // Return not found when the category group does not exist.
                if (categoryGroup == null)
                {
                    _logger.LogWarning("CategoryGroup {Id} not found during delete", id);
                    return NotFound();
                }

                // Apply soft-delete and audit values.
                categoryGroup.DeletedAt = DateTime.UtcNow;
                categoryGroup.UpdatedAt = DateTime.UtcNow;
                categoryGroup.UpdatedByUserId = null; // Placeholder until auth is implemented.

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("CategoryGroup {Id} soft deleted", id);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error soft deleting CategoryGroupId {Id}", id);

                    TempData["ErrorMessage"] = "Unable to delete category group.";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting CategoryGroupId {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the category group.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        /// <summary>
        /// Returns true if the non-deleted category group exists.
        /// </summary>
        private async Task<bool> CategoryGroupExists(ulong id)
        {
            // Check whether the requested active category group still exists.
            return await _context.CategoryGroups.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Trims strings and normalizes required values.
        /// </summary>
        private static void NormalizeCategoryGroup(CategoryGroup model)
        {
            // Normalize and trim the required category group name.
            model.Name = model.Name?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyCategoryGroupValidationAsync(CategoryGroup model, ulong? currentId = null)
        {
            // Require a category group name.
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(CategoryGroup.Name), "Category group name is required.");
            }

            // Require at least one letter or number in the category group name.
            if (!string.IsNullOrWhiteSpace(model.Name) && !ContainsLetterOrDigit(model.Name))
            {
                ModelState.AddModelError(nameof(CategoryGroup.Name), "Category group name must contain letters or numbers.");
            }

            // Prevent duplicate active category group names.
            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                var normalizedName = model.Name.ToLower();

                var duplicateExists = await _context.CategoryGroups
                    .AnyAsync(cg =>
                        cg.DeletedAt == null &&
                        cg.Id != currentId &&
                        cg.Name.ToLower() == normalizedName);

                if (duplicateExists)
                {
                    ModelState.AddModelError(nameof(CategoryGroup.Name), "A category group with this name already exists.");
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