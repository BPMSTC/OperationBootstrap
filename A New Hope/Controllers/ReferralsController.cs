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
    public class ReferralsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReferralsController> _logger;

        public ReferralsController(ApplicationDbContext context, ILogger<ReferralsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Referrals
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading Referrals Index page");

            var referrals = await _context.Referrals
                .Where(r => r.DeletedAt == null)
                .Include(r => r.ClientUser)
                .Include(r => r.ReferringOrganization)
                .OrderByDescending(r => r.ReferredOn)
                .ThenBy(r => r.Id)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} referrals", referrals.Count);

            return View(referrals);
        }

        // GET: Referrals/Details/5
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for Referral Id {Id}", id);

            var referral = await _context.Referrals
                .Where(r => r.DeletedAt == null)
                .Include(r => r.ClientUser)
                .Include(r => r.ReferringOrganization)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (referral == null)
            {
                _logger.LogWarning("Referral Id {Id} not found", id);
                return NotFound();
            }

            return View(referral);
        }

        // GET: Referrals/Create
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create Referral page");
            await PopulateDropdowns();
            return View();
        }

        // POST: Referrals/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientUserId,ReferringOrganizationId,ReferredOn,Status,ValidFrom,ValidTo,ReferredByName,ReferredByPhoneNumber,ReferredByEmail,Notes")] Referral referral)
        {
            _logger.LogInformation("Attempting to create Referral for ClientUserId {ClientUserId}", referral.ClientUserId);

            ModelState.Remove(nameof(Referral.ClientUser));
            ModelState.Remove(nameof(Referral.ReferringOrganization));
            ModelState.Remove(nameof(Referral.CreatedByUser));
            ModelState.Remove(nameof(Referral.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create Referral failed validation for ClientUserId {ClientUserId}", referral.ClientUserId);
                await PopulateDropdowns(referral.ClientUserId, referral.ReferringOrganizationId);
                return View(referral);
            }

            var now = DateTime.UtcNow;
            referral.CreatedAt = now;
            referral.UpdatedAt = now;
            referral.CreatedByUserId = null;
            referral.UpdatedByUserId = null;

            _context.Add(referral);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Referral Id {Id} created successfully", referral.Id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error creating Referral for ClientUserId {ClientUserId}", referral.ClientUserId);
                ModelState.AddModelError("", "Unable to save referral.");
                await PopulateDropdowns(referral.ClientUserId, referral.ReferringOrganizationId);
                return View(referral);
            }
        }

        // GET: Referrals/Edit/5
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for Referral Id {Id}", id);

            var referral = await _context.Referrals
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (referral == null)
            {
                _logger.LogWarning("Referral Id {Id} not found for edit", id);
                return NotFound();
            }

            await PopulateDropdowns(referral.ClientUserId, referral.ReferringOrganizationId);
            return View(referral);
        }

        // POST: Referrals/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,ClientUserId,ReferringOrganizationId,ReferredOn,Status,ValidFrom,ValidTo,ReferredByName,ReferredByPhoneNumber,ReferredByEmail,Notes")] Referral formModel)
        {
            _logger.LogInformation("Attempting to edit Referral Id {Id}", id);

            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            ModelState.Remove(nameof(Referral.ClientUser));
            ModelState.Remove(nameof(Referral.ReferringOrganization));
            ModelState.Remove(nameof(Referral.CreatedByUser));
            ModelState.Remove(nameof(Referral.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit Referral failed validation for Id {Id}", id);
                await PopulateDropdowns(formModel.ClientUserId, formModel.ReferringOrganizationId);
                return View(formModel);
            }

            var existing = await _context.Referrals
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (existing == null)
            {
                _logger.LogWarning("Referral Id {Id} not found during edit save", id);
                return NotFound();
            }

            // Update editable fields
            existing.ClientUserId = formModel.ClientUserId;
            existing.ReferringOrganizationId = formModel.ReferringOrganizationId;
            existing.ReferredOn = formModel.ReferredOn;
            existing.Status = formModel.Status;
            existing.ValidFrom = formModel.ValidFrom;
            existing.ValidTo = formModel.ValidTo;
            existing.ReferredByName = formModel.ReferredByName;
            existing.ReferredByPhoneNumber = formModel.ReferredByPhoneNumber;
            existing.ReferredByEmail = formModel.ReferredByEmail;
            existing.Notes = formModel.Notes;

            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Referral Id {Id} updated successfully", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ReferralExists(formModel.Id))
                {
                    _logger.LogWarning("Referral Id {Id} no longer exists during concurrency check", id);
                    return NotFound();
                }
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating Referral Id {Id}", id);
                ModelState.AddModelError("", "Unable to save changes.");
                await PopulateDropdowns(formModel.ClientUserId, formModel.ReferringOrganizationId);
                return View(formModel);
            }
        }

        // GET: Referrals/Delete/5
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for Referral Id {Id}", id);

            var referral = await _context.Referrals
                .Where(r => r.DeletedAt == null)
                .Include(r => r.ClientUser)
                .Include(r => r.ReferringOrganization)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (referral == null)
            {
                _logger.LogWarning("Referral Id {Id} not found for delete", id);
                return NotFound();
            }

            return View(referral);
        }

        // POST: Referrals/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting Referral Id {Id}", id);

            var referral = await _context.Referrals
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (referral == null)
            {
                _logger.LogWarning("Referral Id {Id} not found during delete", id);
                return NotFound();
            }

            referral.DeletedAt = DateTime.UtcNow;
            referral.UpdatedAt = DateTime.UtcNow;
            referral.UpdatedByUserId = null;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Referral Id {Id} soft deleted", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error soft deleting Referral Id {Id}", id);
                TempData["ErrorMessage"] = "Unable to delete referral.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(ulong? selectedClientUserId = null, ulong? selectedReferringOrganizationId = null)
        {
            _logger.LogDebug("Populating dropdowns for Referrals");

            var users = await _context.DomainUsers
                .Where(u => u.DeletedAt == null)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            var userOptions = users
                .Select(u => new
                {
                    u.Id,
                    DisplayName = $"{u.LastName}, {u.FirstName} ({u.Email})"
                })
                .ToList();

            var organizations = await _context.ReferringOrganizations
                .Where(o => o.DeletedAt == null)
                .OrderBy(o => o.Name)
                .ToListAsync();

            ViewData["ClientUserId"] = new SelectList(userOptions, "Id", "DisplayName", selectedClientUserId);
            ViewData["ReferringOrganizationId"] = new SelectList(organizations, "Id", "Name", selectedReferringOrganizationId);

            _logger.LogDebug("Dropdowns populated: {UsersCount} users, {OrgsCount} organizations", userOptions.Count, organizations.Count);
        }

        private async Task<bool> ReferralExists(ulong id)
        {
            return await _context.Referrals.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}