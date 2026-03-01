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
    public class ClientProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClientProfilesController> _logger;

        public ClientProfilesController(ApplicationDbContext context, ILogger<ClientProfilesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: ClientProfiles
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Fetching client profiles list");

            var clientProfiles = await _context.ClientProfiles
                .Where(c => c.DeletedAt == null)
                .Include(c => c.User)
                .OrderBy(c => c.User.LastName)
                .ThenBy(c => c.User.FirstName)
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} client profiles", clientProfiles.Count);

            return View(clientProfiles);
        }

        // GET: ClientProfiles/Details/5
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("ClientProfile Details requested with null id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for ClientProfile UserId {Id}", id);

            var clientProfile = await _context.ClientProfiles
                .Where(c => c.DeletedAt == null)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (clientProfile == null)
            {
                _logger.LogWarning("ClientProfile UserId {Id} not found", id);
                return NotFound();
            }

            return View(clientProfile);
        }

        // GET: ClientProfiles/Create
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create ClientProfile page");
            await PopulateDropdowns();
            return View();
        }

        // POST: ClientProfiles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,EmploymentStatus,EarnedIncomeMonthly,IsUnhoused")] ClientProfile clientProfile)
        {
            _logger.LogInformation("Attempting to create ClientProfile for UserId {UserId}", clientProfile.UserId);

            ModelState.Remove(nameof(ClientProfile.User));
            ModelState.Remove(nameof(ClientProfile.CreatedByUser));
            ModelState.Remove(nameof(ClientProfile.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create ClientProfile failed validation for UserId {UserId}", clientProfile.UserId);
                await PopulateDropdowns(clientProfile.UserId);
                return View(clientProfile);
            }

            var now = DateTime.UtcNow;
            clientProfile.CreatedAt = now;
            clientProfile.UpdatedAt = now;
            clientProfile.CreatedByUserId = null;
            clientProfile.UpdatedByUserId = null;

            _context.Add(clientProfile);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("ClientProfile created successfully for UserId {UserId}", clientProfile.UserId);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error creating ClientProfile for UserId {UserId}", clientProfile.UserId);
                ModelState.AddModelError("", "Unable to save client profile.");
                await PopulateDropdowns(clientProfile.UserId);
                return View(clientProfile);
            }
        }

        // GET: ClientProfiles/Edit/5
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null UserId");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for ClientProfile UserId {UserId}", id);

            var clientProfile = await _context.ClientProfiles
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == id && c.DeletedAt == null);

            if (clientProfile == null)
            {
                _logger.LogWarning("ClientProfile UserId {UserId} not found for edit", id);
                return NotFound();
            }

            return View(clientProfile);
        }

        // POST: ClientProfiles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("UserId,EmploymentStatus,EarnedIncomeMonthly,IsUnhoused")] ClientProfile formModel)
        {
            _logger.LogInformation("Attempting to edit ClientProfile UserId {UserId}", id);

            if (id != formModel.UserId)
            {
                _logger.LogWarning("Edit mismatch: route UserId {RouteId} vs model UserId {ModelId}", id, formModel.UserId);
                return NotFound();
            }

            ModelState.Remove(nameof(ClientProfile.User));
            ModelState.Remove(nameof(ClientProfile.CreatedByUser));
            ModelState.Remove(nameof(ClientProfile.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit ClientProfile failed validation for UserId {UserId}", id);
                return View(formModel);
            }

            var existing = await _context.ClientProfiles
                .FirstOrDefaultAsync(c => c.UserId == id && c.DeletedAt == null);

            if (existing == null)
            {
                _logger.LogWarning("ClientProfile UserId {UserId} not found during edit save", id);
                return NotFound();
            }

            existing.EmploymentStatus = formModel.EmploymentStatus;
            existing.EarnedIncomeMonthly = formModel.EarnedIncomeMonthly;
            existing.IsUnhoused = formModel.IsUnhoused;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("ClientProfile UserId {UserId} updated successfully", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ClientProfileExists(formModel.UserId))
                {
                    _logger.LogWarning("ClientProfile UserId {UserId} no longer exists during concurrency check", id);
                    return NotFound();
                }
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating ClientProfile UserId {UserId}", id);
                ModelState.AddModelError("", "Unable to save changes.");
                return View(formModel);
            }
        }

        // GET: ClientProfiles/Delete/5
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null UserId");
                return NotFound();
            }

            _logger.LogWarning("Loading Delete confirmation for ClientProfile UserId {UserId}", id);

            var clientProfile = await _context.ClientProfiles
                .Where(c => c.DeletedAt == null)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (clientProfile == null)
            {
                _logger.LogWarning("ClientProfile UserId {UserId} not found for delete", id);
                return NotFound();
            }

            return View(clientProfile);
        }

        // POST: ClientProfiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting ClientProfile UserId {UserId}", id);

            var clientProfile = await _context.ClientProfiles
                .FirstOrDefaultAsync(c => c.UserId == id && c.DeletedAt == null);

            if (clientProfile == null)
            {
                _logger.LogWarning("ClientProfile UserId {UserId} not found during delete", id);
                return NotFound();
            }

            clientProfile.DeletedAt = DateTime.UtcNow;
            clientProfile.UpdatedAt = DateTime.UtcNow;
            clientProfile.UpdatedByUserId = null;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("ClientProfile UserId {UserId} soft deleted", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error soft deleting ClientProfile UserId {UserId}", id);
                TempData["ErrorMessage"] = "Unable to delete client profile.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(ulong? selectedUserId = null)
        {
            _logger.LogDebug("Populating dropdown for ClientProfiles");

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

            ViewData["UserId"] = new SelectList(users, "Id", "DisplayName", selectedUserId);
        }

        private async Task<bool> ClientProfileExists(ulong id)
        {
            return await _context.ClientProfiles
                .AnyAsync(e => e.UserId == id && e.DeletedAt == null);
        }
    }
}