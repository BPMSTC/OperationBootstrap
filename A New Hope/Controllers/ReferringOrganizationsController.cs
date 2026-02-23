using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using A_New_Hope.Data;
using A_New_Hope.Models;

namespace A_New_Hope.Controllers
{
    public class ReferringOrganizationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReferringOrganizationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ReferringOrganizations
        public async Task<IActionResult> Index()
        {
            var referringOrganizations = await _context.ReferringOrganizations
                .Where(r => r.DeletedAt == null)
                .OrderBy(r => r.Name)
                .ToListAsync();

            return View(referringOrganizations);
        }

        // GET: ReferringOrganizations/Details/5
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var referringOrganization = await _context.ReferringOrganizations
                .Where(r => r.DeletedAt == null)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (referringOrganization == null)
            {
                return NotFound();
            }

            return View(referringOrganization);
        }

        // GET: ReferringOrganizations/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ReferringOrganizations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Type,PhoneNumber,Email,AddressLine1,AddressLine2,City,State,PostalCode,PrimaryContactName,Notes,IsActive")] ReferringOrganization referringOrganization)
        {
            // Navigation properties are not posted by the form
            ModelState.Remove(nameof(ReferringOrganization.Referrals));
            ModelState.Remove(nameof(ReferringOrganization.CreatedByUser));
            ModelState.Remove(nameof(ReferringOrganization.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                return View(referringOrganization);
            }

            var now = DateTime.UtcNow;
            referringOrganization.CreatedAt = now;
            referringOrganization.UpdatedAt = now;
            referringOrganization.CreatedByUserId = null; // set later when auth is implemented
            referringOrganization.UpdatedByUserId = null; // set later when auth is implemented

            _context.Add(referringOrganization);

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save referring organization.");
                return View(referringOrganization);
            }
        }

        // GET: ReferringOrganizations/Edit/5
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var referringOrganization = await _context.ReferringOrganizations
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (referringOrganization == null)
            {
                return NotFound();
            }

            return View(referringOrganization);
        }

        // POST: ReferringOrganizations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Name,Type,PhoneNumber,Email,AddressLine1,AddressLine2,City,State,PostalCode,PrimaryContactName,Notes,IsActive")] ReferringOrganization formModel)
        {
            if (id != formModel.Id)
            {
                return NotFound();
            }

            // Navigation properties are not posted by the form
            ModelState.Remove(nameof(ReferringOrganization.Referrals));
            ModelState.Remove(nameof(ReferringOrganization.CreatedByUser));
            ModelState.Remove(nameof(ReferringOrganization.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                return View(formModel);
            }

            var existing = await _context.ReferringOrganizations
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (existing == null)
            {
                return NotFound();
            }

            // Update editable fields only
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

            // Audit
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // set later when auth is implemented

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ReferringOrganizationExists(formModel.Id))
                {
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes.");
                return View(formModel);
            }
        }

        // GET: ReferringOrganizations/Delete/5
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var referringOrganization = await _context.ReferringOrganizations
                .Where(r => r.DeletedAt == null)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (referringOrganization == null)
            {
                return NotFound();
            }

            return View(referringOrganization);
        }

        // POST: ReferringOrganizations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            var referringOrganization = await _context.ReferringOrganizations
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (referringOrganization == null)
            {
                return NotFound();
            }

            // Soft delete
            referringOrganization.DeletedAt = DateTime.UtcNow;
            referringOrganization.UpdatedAt = DateTime.UtcNow;
            referringOrganization.UpdatedByUserId = null; // set later when auth is implemented

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
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