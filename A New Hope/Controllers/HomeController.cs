using System.Diagnostics;
using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// HomeController
    /// --------------
    /// This controller is typically used for "top-level" pages such as:
    /// - The landing/home page (Index)
    /// - A Privacy page (often template-generated)
    /// - A generic Error page
    ///
    /// In your current implementation, Home/Index also acts as a simple "directory"
    /// of the first 100 client profiles, and the controller exposes a Search endpoint
    /// meant to be called via AJAX to find users by name/email/address fields.
    ///
    /// Key behaviors:
    /// - Uses Entity Framework Core (ApplicationDbContext) to query ClientProfiles + related User data.
    /// - Uses dependency-injected ILogger for structured logs that help trace requests and outcomes.
    /// - Returns JSON from Search for client-side consumption.
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// EF Core DbContext used to query the database.
        /// In this controller, it's primarily used to read ClientProfiles and related User records.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Logger for this controller.
        /// What you see in logs depends on the logging providers configured in Program.cs.
        /// </summary>
        private readonly ILogger<HomeController> _logger;

        /// <summary>
        /// Constructor with dependency injection.
        /// - ApplicationDbContext is injected from DI container.
        /// - ILogger<HomeController> is injected from ASP.NET Core logging infrastructure.
        /// </summary>
        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Home/Index
        /// <summary>
        /// Home landing page.
        ///
        /// Current behavior:
        /// - Loads the first 100 ClientProfiles (including the related User entity).
        /// - Sorts by last name then first name.
        /// - Projects the results into an anonymous object that contains display-friendly fields:
        ///     - Id (UserId)
        ///     - FullName
        ///     - Email
        ///     - Address (a concatenated string from user address parts)
        /// - Stores the projected list in ViewBag.Users for the Razor view to display.
        ///
        /// Notes:
        /// - The projection prevents sending full entity objects to the view.
        ///   This is often desirable to keep views lightweight and avoid accidental coupling
        ///   to the full entity structure.
        /// - The address string concatenation is "best effort" using null coalescing.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading Home/Index with first 100 client profiles");

            // Query ClientProfiles, bring in the related User record, order results,
            // and project into a lightweight anonymous type for UI display.
            var users = await _context.ClientProfiles
                .Include(cp => cp.User)
                .OrderBy(cp => cp.User.LastName)
                .ThenBy(cp => cp.User.FirstName)
                .Select(cp => new
                {
                    // Using UserId as the identifier in this UI list.
                    Id = cp.UserId,

                    // Build a display name; null coalescing avoids null reference exceptions.
                    FullName = (cp.User.FirstName ?? "") + " " + (cp.User.LastName ?? ""),

                    // Email is pulled directly from the related User.
                    Email = cp.User.Email,

                    // Combine address elements into a single display string.
                    // This is intended for simple display rather than structured address formatting.
                    Address = (cp.User.AddressLine1 ?? "") + ", " +
                              (cp.User.City ?? "") + ", " +
                              (cp.User.State ?? "")
                })
                // Hard cap to avoid loading too much data for a home/landing page.
                .Take(100)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} client profiles for Home/Index", users.Count);

            // ViewBag is a dynamic container for passing data to the view without a strongly typed model.
            // This can be convenient for quick prototypes or lightweight pages.
            ViewBag.Users = users;

            // Render Views/Home/Index.cshtml
            return View();
        }

        // 🔎 AJAX SEARCH ENDPOINT
        /// <summary>
        /// Search endpoint intended to be called asynchronously (AJAX) from the UI.
        ///
        /// Inputs:
        /// - query: the user's search string from the UI (name, email, address fields, etc.)
        ///
        /// Behavior:
        /// - If the query is blank/whitespace, returns an empty JSON array.
        /// - Converts the query to lowercase and performs a case-insensitive search by lowercasing fields.
        /// - Searches multiple User fields:
        ///     - FirstName, LastName, Email
        ///     - AddressLine1, City, State, PostalCode
        /// - Projects results into a small anonymous object with fields appropriate for UI display.
        /// - Limits results to 20 to keep the endpoint fast and payload small.
        ///
        /// Notes (important for future):
        /// - Calling ToLower() on DB fields inside the query can impact index usage and performance.
        ///   It's fine for now, but if the dataset grows, you may want to use a normalized field,
        ///   a full-text search approach, or provider-specific case-insensitive comparisons.
        /// - No DeletedAt filtering is currently present here; it relies on whatever the underlying
        ///   dataset contains. If your ClientProfiles support soft delete, you may later want to
        ///   add cp.DeletedAt == null in the Where clause (not doing it here since you said no logic changes).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            // If the UI calls search without a meaningful query, return an empty result set.
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogInformation("Search called with empty query");
                return Json(new List<object>());
            }

            // Normalize the query so comparisons are case-insensitive (by lowercasing both sides).
            query = query.ToLower();
            _logger.LogInformation("Performing search for query: {Query}", query);

            // Build query:
            // - Include the related User entity.
            // - Filter where ANY of the relevant fields contain the query.
            // - Project only display data fields needed by the client UI.
            // - Limit to 20 results for responsiveness.
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
                    // Lowercase property names here suggest the frontend expects these keys specifically.
                    // (This is not required in general, but often done to match JS conventions.)
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

            // Return JSON for client-side rendering (autocomplete, search results list, etc.).
            return Json(results);
        }

        // GET: Home/Privacy
        /// <summary>
        /// Displays a Privacy page.
        /// This is typically included in default MVC templates.
        /// </summary>
        public IActionResult Privacy()
        {
            _logger.LogInformation("Loading Privacy page");
            return View();
        }

        // GET: Home/Error
        /// <summary>
        /// Displays a generic error page.
        ///
        /// Response caching:
        /// - Disabled (Duration=0, NoStore=true) so error pages aren't cached and shown incorrectly.
        ///
        /// RequestId:
        /// - Uses Activity.Current?.Id when available (distributed tracing / diagnostics)
        /// - Falls back to HttpContext.TraceIdentifier
        ///
        /// This RequestId is useful for correlating UI error screens with server logs.
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Attempt to retrieve an activity id (for tracing), otherwise use ASP.NET's trace identifier.
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            // Log as error because this endpoint indicates an exception occurred somewhere in the pipeline.
            _logger.LogError("Error page requested, RequestId: {RequestId}", requestId);

            // Render Views/Home/Error.cshtml with a simple view model containing the RequestId.
            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}