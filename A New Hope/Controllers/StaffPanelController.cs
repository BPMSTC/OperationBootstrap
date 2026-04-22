using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Models.ViewModels;
using A_New_Hope.Models.ViewModels.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace A_New_Hope.Controllers
{
    public class StaffPanelController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StaffPanelController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================
        // LIST
        // =========================
        public async Task<IActionResult> StaffListIndex(string searchTerm)
        {
            var query = _context.DomainUsers
                .Where(u =>
                    u.DeletedAt == null &&
                    (u.UserType == UserType.Admin || u.UserType == UserType.Staff));

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();

                query = query.Where(u =>
                    (u.Email != null && u.Email.Contains(searchTerm)) ||
                    (u.FirstName != null && u.FirstName.Contains(searchTerm)) ||
                    (u.LastName != null && u.LastName.Contains(searchTerm)) ||
                    (u.FirstName + " " + u.LastName).Contains(searchTerm)
                );
            }

            var domainUsers = await query
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            var identityLinks = await _context.Users
                .Where(iu => iu.DomainUserId != null)
                .Select(iu => new { iu.Id, iu.DomainUserId })
                .ToListAsync();

            var identityByDomainUserId = identityLinks
                .Where(x => x.DomainUserId.HasValue)
                .ToDictionary(x => x.DomainUserId!.Value, x => x.Id);

            var users = domainUsers.Select(u => new DomainUserIndexRowViewModel
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber,
                FirstName = u.FirstName,
                LastName = u.LastName,
                City = u.City,
                State = u.State,
                PostalCode = u.PostalCode,
                DateOfBirth = u.DateOfBirth,
                DefaultPreference = u.DefaultPreference,
                UserType = u.UserType,
                IsActive = u.IsActive,
                HasLoginAccount = identityByDomainUserId.ContainsKey(u.Id),
                IdentityUserId = identityByDomainUserId.TryGetValue(u.Id, out var identityId)
                    ? identityId
                    : null
            }).ToList();

            ViewData["CurrentFilter"] = searchTerm;
            return View(users);
        }

        // =========================
        // DETAILS
        // =========================
        public async Task<IActionResult> StaffDetails(ulong id)
        {
            var user = await _context.DomainUsers.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StaffDetails(ulong id, DomainUser model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.DomainUsers.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

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
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            var user = await _context.DomainUsers.FindAsync(id);
            if (user == null) return NotFound();

            _context.DomainUsers.Remove(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(StaffListIndex));
        }

        // =========================
        // CREATE STEP 1
        // =========================
        public IActionResult StaffCreate()
        {
            if (TempData["StaffCreateData"] is string json &&
                !string.IsNullOrWhiteSpace(json))
            {
                var vm = JsonSerializer.Deserialize<StaffCreateViewModel>(json);
                if (vm != null) return View(vm);
            }

            return View(new StaffCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StaffCreate(StaffCreateViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            TempData["StaffCreateData"] = JsonSerializer.Serialize(vm);
            return RedirectToAction(nameof(StaffReview));
        }

        // =========================
        // REVIEW
        // =========================
        public IActionResult StaffReview()
        {
            var json = TempData.Peek("StaffCreateData") as string;

            if (string.IsNullOrWhiteSpace(json))
                return RedirectToAction(nameof(StaffCreate));

            var vm = JsonSerializer.Deserialize<StaffCreateViewModel>(json);

            if (vm == null)
                return RedirectToAction(nameof(StaffCreate));

            return View(vm);
        }

        // =========================
        // CONFIRM CREATE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCreateStaff()
        {
            if (TempData["StaffCreateData"] is not string json ||
                string.IsNullOrWhiteSpace(json))
            {
                return RedirectToAction(nameof(StaffCreate));
            }

            var vm = JsonSerializer.Deserialize<StaffCreateViewModel>(json);

            if (vm == null)
                return RedirectToAction(nameof(StaffCreate));

            var password = string.IsNullOrWhiteSpace(vm.Password)
                ? "ChangeMe123"
                : vm.Password;

            // CREATE IDENTITY USER
            var identityUser = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email
            };

            var result = await _userManager.CreateAsync(identityUser, password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return RedirectToAction(nameof(StaffCreate));
            }

            // CREATE DOMAIN USER
            var domainUser = new DomainUser
            {
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Email = vm.Email,
                UserType = vm.UserType
            };

            _context.DomainUsers.Add(domainUser);
            await _context.SaveChangesAsync();

            // LINK USERS
            identityUser.DomainUserId = domainUser.Id;
            await _userManager.UpdateAsync(identityUser);

            // ROLE
            await _userManager.AddToRoleAsync(identityUser, vm.UserType.ToString());

            TempData.Remove("StaffCreateData");

            return RedirectToAction(nameof(StaffDetails), new { id = domainUser.Id });
        }
    }
}