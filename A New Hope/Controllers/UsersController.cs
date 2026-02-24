using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using A_New_Hope.Data;
using A_New_Hope.Models;

namespace A_New_Hope.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Where(u => u.DeletedAt == null)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Email,PhoneNumber,FirstName,LastName,AddressLine1,AddressLine2,City,State,PostalCode,DateOfBirth,DefaultPreference,Role,IsActive")] A_New_Hope.Models.DomainUser user)
        {
            // Navigation properties are not posted by the form
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.CreatedByUser));
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.UpdatedByUser));
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.ClientProfile));

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var now = DateTime.UtcNow;
            user.CreatedAt = now;
            user.UpdatedAt = now;
            user.CreatedByUserId = null; // set later when auth is implemented
            user.UpdatedByUserId = null; // set later when auth is implemented

            _context.Add(user);

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save user.");
                return View(user);
            }
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Email,PhoneNumber,FirstName,LastName,AddressLine1,AddressLine2,City,State,PostalCode,DateOfBirth,DefaultPreference,Role,IsActive")] A_New_Hope.Models.DomainUser formModel)
        {
            if (id != formModel.Id)
            {
                return NotFound();
            }

            // Navigation properties are not posted by the form
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.CreatedByUser));
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.UpdatedByUser));
            ModelState.Remove(nameof(A_New_Hope.Models.DomainUser.ClientProfile));

            if (!ModelState.IsValid)
            {
                return View(formModel);
            }

            var existing = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (existing == null)
            {
                return NotFound();
            }

            // Update editable fields only
            existing.Email = formModel.Email;
            existing.PhoneNumber = formModel.PhoneNumber;
            existing.FirstName = formModel.FirstName;
            existing.LastName = formModel.LastName;
            existing.AddressLine1 = formModel.AddressLine1;
            existing.AddressLine2 = formModel.AddressLine2;
            existing.City = formModel.City;
            existing.State = formModel.State;
            existing.PostalCode = formModel.PostalCode;
            existing.DateOfBirth = formModel.DateOfBirth;
            existing.DefaultPreference = formModel.DefaultPreference;
            existing.UserType = formModel.UserType;
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
                if (!await UserExists(formModel.Id))
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

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
            {
                return NotFound();
            }

            // Soft delete
            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedByUserId = null; // set later when auth is implemented

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Unable to delete user.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> UserExists(ulong id)
        {
            return await _context.Users
                .AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}