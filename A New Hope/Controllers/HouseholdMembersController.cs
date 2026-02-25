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
    public class HouseholdMembersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HouseholdMembersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: HouseholdMembers
        public async Task<IActionResult> Index()
        {
            var householdMembers = await _context.HouseholdMembers
                .Where(h => h.DeletedAt == null)
                .Include(h => h.ClientUser)
                .OrderBy(h => h.LastName)
                .ThenBy(h => h.FirstName)
                .ToListAsync();

            return View(householdMembers);
        }

        // GET: HouseholdMembers/Details/5
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var householdMember = await _context.HouseholdMembers
                .Where(h => h.DeletedAt == null)
                .Include(h => h.ClientUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (householdMember == null)
            {
                return NotFound();
            }

            return View(householdMember);
        }

        // GET: HouseholdMembers/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: HouseholdMembers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientUserId,FirstName,LastName,DateOfBirth,AgeAsOfDate")] HouseholdMember householdMember)
        {
            // Navigation properties are not posted by the form
            ModelState.Remove(nameof(HouseholdMember.ClientUser));
            ModelState.Remove(nameof(HouseholdMember.CreatedByUser));
            ModelState.Remove(nameof(HouseholdMember.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(householdMember.ClientUserId);
                return View(householdMember);
            }

            var now = DateTime.UtcNow;
            householdMember.CreatedAt = now;
            householdMember.UpdatedAt = now;
            householdMember.CreatedByUserId = null; // set later when auth is implemented
            householdMember.UpdatedByUserId = null; // set later when auth is implemented

            _context.Add(householdMember);

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
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
                return NotFound();
            }

            var householdMember = await _context.HouseholdMembers
                .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);

            if (householdMember == null)
            {
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
            if (id != formModel.Id)
            {
                return NotFound();
            }

            // Navigation properties are not posted by the form
            ModelState.Remove(nameof(HouseholdMember.ClientUser));
            ModelState.Remove(nameof(HouseholdMember.CreatedByUser));
            ModelState.Remove(nameof(HouseholdMember.UpdatedByUser));

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(formModel.ClientUserId);
                return View(formModel);
            }

            var existing = await _context.HouseholdMembers
                .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);

            if (existing == null)
            {
                return NotFound();
            }

            // Update editable fields only
            existing.ClientUserId = formModel.ClientUserId;
            existing.FirstName = formModel.FirstName;
            existing.LastName = formModel.LastName;
            existing.DateOfBirth = formModel.DateOfBirth;
            existing.AgeAsOfDate = formModel.AgeAsOfDate;

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
                if (!await HouseholdMemberExists(formModel.Id))
                {
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException)
            {
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
                return NotFound();
            }

            var householdMember = await _context.HouseholdMembers
                .Where(h => h.DeletedAt == null)
                .Include(h => h.ClientUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (householdMember == null)
            {
                return NotFound();
            }

            return View(householdMember);
        }

        // POST: HouseholdMembers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            var householdMember = await _context.HouseholdMembers
                .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);

            if (householdMember == null)
            {
                return NotFound();
            }

            // Soft delete
            householdMember.DeletedAt = DateTime.UtcNow;
            householdMember.UpdatedAt = DateTime.UtcNow;
            householdMember.UpdatedByUserId = null; // set later when auth is implemented

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Unable to delete household member.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(ulong? selectedClientUserId = null)
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

            ViewData["ClientUserId"] = new SelectList(users, "Id", "DisplayName", selectedClientUserId);
        }

        private async Task<bool> HouseholdMemberExists(ulong id)
        {
            return await _context.HouseholdMembers
                .AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}