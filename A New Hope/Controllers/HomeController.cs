using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace A_New_Hope.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.ClientProfiles
                .Include(cp => cp.User)
                .OrderBy(cp => cp.User.LastName)
                .ThenBy(cp => cp.User.FirstName)
                .Select(cp => new
                {
                    Id = cp.UserId,
                    FullName = (cp.User.FirstName ?? "") + " " + (cp.User.LastName ?? ""),
                    Email = cp.User.Email,
                    Address = (cp.User.AddressLine1 ?? "") + ", " +
                              (cp.User.City ?? "") + ", " +
                              (cp.User.State ?? "")
                })
                .Take(100)
                .ToListAsync();

            ViewBag.Users = users;

            return View();
        }

        // 🔎 AJAX SEARCH ENDPOINT
        //e
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new List<object>());

            query = query.ToLower();

            var results = await _context.ClientProfiles
                .Include(cp => cp.User)
                .Where(cp =>
                    (cp.User.FirstName != null && cp.User.FirstName.ToLower().Contains(query)) ||
                    (cp.User.LastName != null && cp.User.LastName.ToLower().Contains(query)) ||
                    (cp.User.Email != null && cp.User.Email.ToLower().Contains(query)) ||
                    (cp.User.AddressLine1 != null && cp.User.AddressLine1.ToLower().Contains(query)) ||
                    (cp.User.City != null && cp.User.City.ToLower().Contains(query)) ||
                    (cp.User.State != null && cp.User.State.ToLower().Contains(query)) ||
                    (cp.User.PostalCode != null && cp.User.PostalCode.ToLower().Contains(query))
                )
                .Select(cp => new
                {
                    id = cp.UserId,
                    fullName = (cp.User.FirstName ?? "") + " " + (cp.User.LastName ?? ""),
                    email = cp.User.Email,
                    address = (cp.User.AddressLine1 ?? "") + ", " +
                              (cp.User.City ?? "") + ", " +
                              (cp.User.State ?? "")
                })
                .Take(20)
                .ToListAsync();

            return Json(results);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}