using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using A_New_Hope.Data;
using A_New_Hope.Models;

namespace A_New_Hope.Controllers
{
    public class ClientProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientProfilesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ClientProfiles
        public async Task<IActionResult> Index()
        {
            var clientProfiles = await _context.ClientProfiles
                .Where(c => c.DeletedAt == null)
                .Include(c => c.User)
                .OrderBy(c => c.User.LastName)
                .ThenBy(c => c.User.FirstName)
                .ToListAsync();

            return View(clientProfiles);
        }

        // GET: ClientProfiles/Details/5
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clientProfile = await _context.ClientProfiles
                .Where(c => c.DeletedAt == null)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (clientProfile == null)
            {
                return NotFound();
            }

            return View(clientProfile);
        }

        // GET: ClientProfiles/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: ClientProfiles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,EmploymentStatus,EarnedIncomeMonthly,IsUnhoused")] ClientProfile clientProfile)
        {
            // Navigation properties are not posted by the form
            ModelState.Remove(nameof(ClientProfile.User));
            ModelState.Remove(nameof(ClientProfile.CreatedByUser));
            ModelState.Remove(nameof(ClientProfile.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(clientProfile.UserId);
                return View(clientProfile);
            }

            var now = DateTime.UtcNow;
            clientProfile.CreatedAt = now;
            clientProfile.UpdatedAt = now;
            clientProfile.CreatedByUserId = null; // set later when auth is implemented
            clientProfile.UpdatedByUserId = null; // set later when auth is implemented

            _context.Add(clientProfile);

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
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
                return NotFound();
            }

            var clientProfile = await _context.ClientProfiles
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == id && c.DeletedAt == null);

            if (clientProfile == null)
            {
                return NotFound();
            }

            return View(clientProfile);
        }

        // POST: ClientProfiles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("UserId,EmploymentStatus,EarnedIncomeMonthly,IsUnhoused")] ClientProfile formModel)
        {
            if (id != formModel.UserId)
            {
                return NotFound();
            }

            // Navigation properties are not posted by the form
            ModelState.Remove(nameof(ClientProfile.User));
            ModelState.Remove(nameof(ClientProfile.CreatedByUser));
            ModelState.Remove(nameof(ClientProfile.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                return View(formModel);
            }

            var existing = await _context.ClientProfiles
                .FirstOrDefaultAsync(c => c.UserId == id && c.DeletedAt == null);

            if (existing == null)
            {
                return NotFound();
            }

            // Update editable fields only (UserId is PK and should not change)
            existing.EmploymentStatus = formModel.EmploymentStatus;
            existing.EarnedIncomeMonthly = formModel.EarnedIncomeMonthly;
            existing.IsUnhoused = formModel.IsUnhoused;

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
                if (!await ClientProfileExists(formModel.UserId))
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

        // GET: ClientProfiles/Delete/5
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clientProfile = await _context.ClientProfiles
                .Where(c => c.DeletedAt == null)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (clientProfile == null)
            {
                return NotFound();
            }

            return View(clientProfile);
        }

        // POST: ClientProfiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            var clientProfile = await _context.ClientProfiles
                .FirstOrDefaultAsync(c => c.UserId == id && c.DeletedAt == null);

            if (clientProfile == null)
            {
                return NotFound();
            }

            // Soft delete
            clientProfile.DeletedAt = DateTime.UtcNow;
            clientProfile.UpdatedAt = DateTime.UtcNow;
            clientProfile.UpdatedByUserId = null; // set later when auth is implemented

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Unable to delete client profile.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(ulong? selectedUserId = null)
        {
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