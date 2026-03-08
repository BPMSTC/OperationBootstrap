using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages create, read, update, and soft delete operations for household members.
    /// </summary>
    public class HouseholdMembersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HouseholdMembersController> _logger;

        /// <summary>
        /// Creates the controller with the required database context and logger.
        /// </summary>
        public HouseholdMembersController(ApplicationDbContext context, ILogger<HouseholdMembersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: HouseholdMembers
        /// <summary>
        /// Displays all non-deleted household members.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading HouseholdMembers Index page");

            var householdMembers = await _context.HouseholdMembers
                .Where(h => h.DeletedAt == null)
                .Include(h => h.ClientUser)
                .OrderBy(h => h.LastName)
                .ThenBy(h => h.FirstName)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} household members", householdMembers.Count);

            return View(householdMembers);
        }

        // GET: HouseholdMembers/Details/5
        /// <summary>
        /// Displays details for a single non-deleted household member.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details requested with null id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for HouseholdMember Id {Id}", id);

            var householdMember = await _context.HouseholdMembers
                .Where(h => h.DeletedAt == null)
                .Include(h => h.ClientUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (householdMember == null)
            {
                _logger.LogWarning("HouseholdMember Id {Id} not found", id);
                return NotFound();
            }

            return View(householdMember);
        }

        // GET: HouseholdMembers/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create HouseholdMember page");
            await PopulateDropdowns();
            return View();
        }

        // POST: HouseholdMembers/Create
        /// <summary>
        /// Creates a new household member after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientUserId,FirstName,LastName,DateOfBirth,AgeAsOfDate")] HouseholdMember householdMember)
        {
            _logger.LogInformation("Attempting to create HouseholdMember for ClientUserId {ClientUserId}", householdMember.ClientUserId);

            // Navigation properties are not posted by the form.
            ModelState.Remove(nameof(HouseholdMember.ClientUser));
            ModelState.Remove(nameof(HouseholdMember.CreatedByUser));
            ModelState.Remove(nameof(HouseholdMember.UpdatedByUser));

            NormalizeHouseholdMember(householdMember);
            await ApplyHouseholdMemberValidationAsync(householdMember);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create HouseholdMember failed validation for ClientUserId {ClientUserId}", householdMember.ClientUserId);
                await PopulateDropdowns(householdMember.ClientUserId);
                return View(householdMember);
            }

            var now = DateTime.UtcNow;
            householdMember.CreatedAt = now;
            householdMember.UpdatedAt = now;
            householdMember.CreatedByUserId = null; // Placeholder until auth integration.
            householdMember.UpdatedByUserId = null; // Placeholder until auth integration.

            _context.Add(householdMember);

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("HouseholdMember created successfully for ClientUserId {ClientUserId}", householdMember.ClientUserId);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error creating HouseholdMember for ClientUserId {ClientUserId}", householdMember.ClientUserId);

                ModelState.AddModelError("", "Unable to save household member.");
                await PopulateDropdowns(householdMember.ClientUserId);
                return View(householdMember);
            }
        }

        // GET: HouseholdMembers/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted household member.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for HouseholdMember Id {Id}", id);

            var householdMember = await _context.HouseholdMembers
                .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);

            if (householdMember == null)
            {
                _logger.LogWarning("HouseholdMember Id {Id} not found for edit", id);
                return NotFound();
            }

            await PopulateDropdowns(householdMember.ClientUserId);

            return View(householdMember);
        }

        // POST: HouseholdMembers/Edit/5
        /// <summary>
        /// Updates an existing household member after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,ClientUserId,FirstName,LastName,DateOfBirth,AgeAsOfDate")] HouseholdMember formModel)
        {
            _logger.LogInformation("Attempting to edit HouseholdMember Id {Id}", id);

            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            // Navigation properties are not posted by the form.
            ModelState.Remove(nameof(HouseholdMember.ClientUser));
            ModelState.Remove(nameof(HouseholdMember.CreatedByUser));
            ModelState.Remove(nameof(HouseholdMember.UpdatedByUser));

            NormalizeHouseholdMember(formModel);
            await ApplyHouseholdMemberValidationAsync(formModel, formModel.Id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit HouseholdMember failed validation for Id {Id}", id);
                await PopulateDropdowns(formModel.ClientUserId);
                return View(formModel);
            }

            var existing = await _context.HouseholdMembers
                .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);

            if (existing == null)
            {
                _logger.LogWarning("HouseholdMember Id {Id} not found during edit save", id);
                return NotFound();
            }

            existing.ClientUserId = formModel.ClientUserId;
            existing.FirstName = formModel.FirstName;
            existing.LastName = formModel.LastName;
            existing.DateOfBirth = formModel.DateOfBirth;
            existing.AgeAsOfDate = formModel.AgeAsOfDate;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // Placeholder until auth integration.

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("HouseholdMember Id {Id} updated successfully", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await HouseholdMemberExists(formModel.Id))
                {
                    _logger.LogWarning("HouseholdMember Id {Id} no longer exists during concurrency check", id);
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating HouseholdMember Id {Id}", id);

                ModelState.AddModelError("", "Unable to save changes.");
                await PopulateDropdowns(formModel.ClientUserId);
                return View(formModel);
            }
        }

        // GET: HouseholdMembers/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted household member.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogWarning("Loading Delete confirmation for HouseholdMember Id {Id}", id);

            var householdMember = await _context.HouseholdMembers
                .Where(h => h.DeletedAt == null)
                .Include(h => h.ClientUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (householdMember == null)
            {
                _logger.LogWarning("HouseholdMember Id {Id} not found for delete", id);
                return NotFound();
            }

            return View(householdMember);
        }

        // POST: HouseholdMembers/Delete/5
        /// <summary>
        /// Soft deletes a household member.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting HouseholdMember Id {Id}", id);

            var householdMember = await _context.HouseholdMembers
                .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);

            if (householdMember == null)
            {
                _logger.LogWarning("HouseholdMember Id {Id} not found during delete", id);
                return NotFound();
            }

            householdMember.DeletedAt = DateTime.UtcNow;
            householdMember.UpdatedAt = DateTime.UtcNow;
            householdMember.UpdatedByUserId = null; // Placeholder until auth integration.

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("HouseholdMember Id {Id} soft deleted", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error soft deleting HouseholdMember Id {Id}", id);

                TempData["ErrorMessage"] = "Unable to delete household member.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Populates the client user dropdown for the create and edit forms.
        /// </summary>
        private async Task PopulateDropdowns(ulong? selectedClientUserId = null)
        {
            _logger.LogDebug("Populating ClientUser dropdown for HouseholdMember");

            var users = await _context.DomainUsers
                .Where(u =>
                    u.DeletedAt == null &&
                    u.IsActive &&
                    u.UserType == UserType.Client)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new
                {
                    u.Id,
                    DisplayName = $"{u.LastName}, {u.FirstName} ({u.Email})"
                })
                .ToListAsync();

            ViewData["ClientUserId"] = new SelectList(users, "Id", "DisplayName", selectedClientUserId);
        }

        /// <summary>
        /// Returns true if the non-deleted household member exists.
        /// </summary>
        private async Task<bool> HouseholdMemberExists(ulong id)
        {
            return await _context.HouseholdMembers
                .AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Trims required string values.
        /// </summary>
        private static void NormalizeHouseholdMember(HouseholdMember model)
        {
            model.FirstName = model.FirstName?.Trim() ?? string.Empty;
            model.LastName = model.LastName?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
        private async Task ApplyHouseholdMemberValidationAsync(HouseholdMember model, ulong? currentId = null)
        {
            var validClient = await _context.DomainUsers
                .AnyAsync(u =>
                    u.Id == model.ClientUserId &&
                    u.DeletedAt == null &&
                    u.IsActive &&
                    u.UserType == UserType.Client);

            if (!validClient)
            {
                ModelState.AddModelError(nameof(HouseholdMember.ClientUserId), "Select a valid active client.");
            }

            if (string.IsNullOrWhiteSpace(model.FirstName))
            {
                ModelState.AddModelError(nameof(HouseholdMember.FirstName), "First Name is required.");
            }

            if (string.IsNullOrWhiteSpace(model.LastName))
            {
                ModelState.AddModelError(nameof(HouseholdMember.LastName), "Last Name is required.");
            }

            if (!string.IsNullOrWhiteSpace(model.FirstName) && !IsValidPersonName(model.FirstName))
            {
                ModelState.AddModelError(nameof(HouseholdMember.FirstName), "First Name contains invalid characters.");
            }

            if (!string.IsNullOrWhiteSpace(model.LastName) && !IsValidPersonName(model.LastName))
            {
                ModelState.AddModelError(nameof(HouseholdMember.LastName), "Last Name contains invalid characters.");
            }

            if (model.DateOfBirth.HasValue)
            {
                var minDate = new DateTime(1900, 1, 1);

                if (model.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
                {
                    ModelState.AddModelError(nameof(HouseholdMember.DateOfBirth), "Date of Birth cannot be in the future.");
                }

                if (model.DateOfBirth.Value.Date < minDate)
                {
                    ModelState.AddModelError(nameof(HouseholdMember.DateOfBirth), "Date of Birth is earlier than the allowed minimum.");
                }
            }

            if (model.AgeAsOfDate.HasValue && model.DateOfBirth.HasValue &&
                model.AgeAsOfDate.Value.Date < model.DateOfBirth.Value.Date)
            {
                ModelState.AddModelError(nameof(HouseholdMember.AgeAsOfDate), "Age As Of Date cannot be earlier than Date of Birth.");
            }
        }

        /// <summary>
        /// Validates a person name using a practical character set.
        /// </summary>
        private static bool IsValidPersonName(string name)
        {
            return Regex.IsMatch(name, @"^[A-Za-z][A-Za-z\s'.-]*$");
        }
    }
}