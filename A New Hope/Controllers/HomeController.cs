using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace A_New_Hope.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Home/Index
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading Home/Index with first 100 client profiles");

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

            _logger.LogInformation("Loaded {Count} client profiles for Home/Index", users.Count);

            ViewBag.Users = users;
            return View();
        }

        // 🔎 AJAX SEARCH ENDPOINT
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogInformation("Search called with empty query");
                return Json(new List<object>());
            }

            query = query.ToLower();
            _logger.LogInformation("Performing search for query: {Query}", query);

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

            _logger.LogInformation("Search returned {Count} results for query: {Query}", results.Count, query);

            return Json(results);
        }

        // GET: Home/Privacy
        public IActionResult Privacy()
        {
            _logger.LogInformation("Loading Privacy page");
            return View();
        }

        // GET: Home/Error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Error page requested, RequestId: {RequestId}", requestId);

            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}