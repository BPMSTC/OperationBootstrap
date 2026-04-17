using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    public class StaffPanelController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StaffPanelController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // INDEX (UNCHANGED LOGIC)
        // =========================
        public async Task<IActionResult> StaffListIndex()
        {
            var users = await _context.DomainUsers
                .Where(u => u.UserType == UserType.Admin || u.UserType == UserType.Staff)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();

            return View(users); // expects IEnumerable<DomainUser>
        }

        // =========================
        // DETAILS (GET)
        // =========================
        public async Task<IActionResult> StaffDetails(ulong id)
        {
            var user = await _context.DomainUsers
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            return View(user);
        }

        // =========================
        // DETAILS (POST EDIT)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StaffDetails(ulong id, DomainUser model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.DomainUsers
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(StaffDetails), new { id });
        }

        // =========================
        // DELETE
        // =========================
        public async Task<IActionResult> Delete(ulong id)
        {
            var user = await _context.DomainUsers.FindAsync(id);

            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            var user = await _context.DomainUsers.FindAsync(id);

            if (user == null)
                return NotFound();

            _context.DomainUsers.Remove(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(StaffListIndex));
        }
    }
}