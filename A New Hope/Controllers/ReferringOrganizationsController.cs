using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using A_New_Hope.Data;
using A_New_Hope.Models;

namespace A_New_Hope.Controllers
{
    public class ReferringOrganizationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReferringOrganizationsController> _logger;

        public ReferringOrganizationsController(ApplicationDbContext context, ILogger<ReferringOrganizationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: ReferringOrganizations
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading Referring Organizations Index page");

            var referringOrganizations = await _context.ReferringOrganizations
                .Where(r => r.DeletedAt == null)
                .OrderBy(r => r.Name)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} referring organizations", referringOrganizations.Count);
            return View(referringOrganizations);
        }

        // GET: ReferringOrganizations/Details/5
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for Referring Organization Id {Id}", id);

            var referringOrganization = await _context.ReferringOrganizations
                .Where(r => r.DeletedAt == null)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (referringOrganization == null)
            {
                _logger.LogWarning("Referring Organization Id {Id} not found", id);
                return NotFound();
            }

            return View(referringOrganization);
        }

        // GET: ReferringOrganizations/Create
        public IActionResult Create()
        {
            _logger.LogInformation("Loading Create Referring Organization page");
            return View();
        }

        // POST: ReferringOrganizations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Type,PhoneNumber,Email,AddressLine1,AddressLine2,City,State,PostalCode,PrimaryContactName,Notes,IsActive")] ReferringOrganization referringOrganization)
        {
            _logger.LogInformation("Attempting to create Referring Organization '{Name}'", referringOrganization.Name);

            ModelState.Remove(nameof(ReferringOrganization.Referrals));
            ModelState.Remove(nameof(ReferringOrganization.CreatedByUser));
            ModelState.Remove(nameof(ReferringOrganization.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create Referring Organization failed validation for '{Name}'", referringOrganization.Name);
                return View(referringOrganization);
            }

            var now = DateTime.UtcNow;
            referringOrganization.CreatedAt = now;
            referringOrganization.UpdatedAt = now;
            referringOrganization.CreatedByUserId = null;
            referringOrganization.UpdatedByUserId = null;

            _context.Add(referringOrganization);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Referring Organization Id {Id} created successfully", referringOrganization.Id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error creating Referring Organization '{Name}'", referringOrganization.Name);
                ModelState.AddModelError("", "Unable to save referring organization.");
                return View(referringOrganization);
            }
        }

        // GET: ReferringOrganizations/Edit/5
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for Referring Organization Id {Id}", id);

            var referringOrganization = await _context.ReferringOrganizations
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (referringOrganization == null)
            {
                _logger.LogWarning("Referring Organization Id {Id} not found for edit", id);
                return NotFound();
            }

            return View(referringOrganization);
        }

        // POST: ReferringOrganizations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Name,Type,PhoneNumber,Email,AddressLine1,AddressLine2,City,State,PostalCode,PrimaryContactName,Notes,IsActive")] ReferringOrganization formModel)
        {
            _logger.LogInformation("Attempting to edit Referring Organization Id {Id}", id);

            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            ModelState.Remove(nameof(ReferringOrganization.Referrals));
            ModelState.Remove(nameof(ReferringOrganization.CreatedByUser));
            ModelState.Remove(nameof(ReferringOrganization.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit Referring Organization failed validation for Id {Id}", id);
                return View(formModel);
            }

            var existing = await _context.ReferringOrganizations
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (existing == null)
            {
                _logger.LogWarning("Referring Organization Id {Id} not found during edit save", id);
                return NotFound();
            }

            // Update editable fields
            existing.Name = formModel.Name;
            existing.Type = formModel.Type;
            existing.PhoneNumber = formModel.PhoneNumber;
            existing.Email = formModel.Email;
            existing.AddressLine1 = formModel.AddressLine1;
            existing.AddressLine2 = formModel.AddressLine2;
            existing.City = formModel.City;
            existing.State = formModel.State;
            existing.PostalCode = formModel.PostalCode;
            existing.PrimaryContactName = formModel.PrimaryContactName;
            existing.Notes = formModel.Notes;
            existing.IsActive = formModel.IsActive;

            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Referring Organization Id {Id} updated successfully", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ReferringOrganizationExists(formModel.Id))
                {
                    _logger.LogWarning("Referring Organization Id {Id} no longer exists during concurrency check", id);
                    return NotFound();
                }
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating Referring Organization Id {Id}", id);
                ModelState.AddModelError("", "Unable to save changes.");
                return View(formModel);
            }
        }

        // GET: ReferringOrganizations/Delete/5
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for Referring Organization Id {Id}", id);

            var referringOrganization = await _context.ReferringOrganizations
                .Where(r => r.DeletedAt == null)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (referringOrganization == null)
            {
                _logger.LogWarning("Referring Organization Id {Id} not found for delete", id);
                return NotFound();
            }

            return View(referringOrganization);
        }

        // POST: ReferringOrganizations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting Referring Organization Id {Id}", id);

            var referringOrganization = await _context.ReferringOrganizations
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (referringOrganization == null)
            {
                _logger.LogWarning("Referring Organization Id {Id} not found during delete", id);
                return NotFound();
            }

            referringOrganization.DeletedAt = DateTime.UtcNow;
            referringOrganization.UpdatedAt = DateTime.UtcNow;
            referringOrganization.UpdatedByUserId = null;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Referring Organization Id {Id} soft deleted", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error soft deleting Referring Organization Id {Id}", id);
                TempData["ErrorMessage"] = "Unable to delete referring organization.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ReferringOrganizationExists(ulong id)
        {
            return await _context.ReferringOrganizations
                .AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}