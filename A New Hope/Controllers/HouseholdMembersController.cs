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
    public class HouseholdMembersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HouseholdMembersController> _logger;

        public HouseholdMembersController(ApplicationDbContext context, ILogger<HouseholdMembersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: HouseholdMembers
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
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create HouseholdMember page");
            await PopulateDropdowns();
            return View();
        }

        // POST: HouseholdMembers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientUserId,FirstName,LastName,DateOfBirth,AgeAsOfDate")] HouseholdMember householdMember)
        {
            _logger.LogInformation("Attempting to create HouseholdMember for ClientUserId {ClientUserId}", householdMember.ClientUserId);

            ModelState.Remove(nameof(HouseholdMember.ClientUser));
            ModelState.Remove(nameof(HouseholdMember.CreatedByUser));
            ModelState.Remove(nameof(HouseholdMember.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create HouseholdMember failed validation for ClientUserId {ClientUserId}", householdMember.ClientUserId);
                await PopulateDropdowns(householdMember.ClientUserId);
                return View(householdMember);
            }

            var now = DateTime.UtcNow;
            householdMember.CreatedAt = now;
            householdMember.UpdatedAt = now;
            householdMember.CreatedByUserId = null;
            householdMember.UpdatedByUserId = null;

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

            ModelState.Remove(nameof(HouseholdMember.ClientUser));
            ModelState.Remove(nameof(HouseholdMember.CreatedByUser));
            ModelState.Remove(nameof(HouseholdMember.UpdatedByUser));

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
            existing.UpdatedByUserId = null;

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
            householdMember.UpdatedByUserId = null;

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

        private async Task PopulateDropdowns(ulong? selectedClientUserId = null)
        {
            _logger.LogDebug("Populating ClientUser dropdown for HouseholdMember");

            var users = await _context.DomainUsers
                .Where(u => u.DeletedAt == null && u.IsActive)
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

        private async Task<bool> HouseholdMemberExists(ulong id)
        {
            return await _context.HouseholdMembers
                .AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}