using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// ReferringOrganizationsController
    /// --------------------------------
    /// This controller manages DIO operations for ReferringOrganization records.
    ///
    /// In your domain, a ReferringOrganization likely represents an outside entity that can refer clients,
    /// such as:
    /// - shelters
    /// - community agencies
    /// - hospitals
    /// - social workers / programs
    /// - other nonprofit organizations
    ///
    /// A ReferringOrganization can have many Referrals (navigation property: Referrals).
    ///
    /// Key behaviors implemented here:
    /// - Uses Entity Framework Core (ApplicationDbContext) to query and persist organization data.
    /// - Uses dependency-injected ILogger for request tracing and error diagnostics.
    /// - Uses SOFT DELETE semantics: DeleteConfirmed sets DeletedAt rather than physically removing rows.
    /// - Filters out soft-deleted organizations (DeletedAt == null) for Index/Details/Edit/Delete flows.
    ///
    /// Notes on audit fields:
    /// - CreatedAt/UpdatedAt are set to DateTime.UtcNow for consistent server-side timestamps.
    /// - CreatedByUserId/UpdatedByUserId are currently set to null until auth/user tracking is implemented.
    /// </summary>
    public class ReferringOrganizationsController : Controller
    {
        /// <summary>
        /// EF Core DbContext used to read/write ReferringOrganizations.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Logger for this controller. Output depends on logging providers configured in Program.cs.
        /// </summary>
        private readonly ILogger<ReferringOrganizationsController> _logger;

        /// <summary>
        /// Constructor with dependency injection.
        /// </summary>
        public ReferringOrganizationsController(ApplicationDbContext context, ILogger<ReferringOrganizationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: ReferringOrganizations
        /// <summary>
        /// Displays a list of referring organizations (non-deleted only).
        ///
        /// Query behavior:
        /// - Filters out soft-deleted organizations (DeletedAt == null).
        /// - Orders by Name for a simple, user-friendly listing.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading Referring Organizations Index page");

            // Load active organizations and materialize them into a list for the view.
            var referringOrganizations = await _context.ReferringOrganizations
                .Where(r => r.DeletedAt == null)
                .OrderBy(r => r.Name)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} referring organizations", referringOrganizations.Count);

            // Render Views/ReferringOrganizations/Index.cshtml with the list model.
            return View(referringOrganizations);
        }

        // GET: ReferringOrganizations/Details/5
        /// <summary>
        /// Displays details for a single referring organization by primary key Id.
        ///
        /// Behavior:
        /// - Returns 404 if id is null or record not found (or soft deleted).
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            // Missing route id => cannot locate a record.
            if (id == null)
            {
                _logger.LogWarning("Details requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for Referring Organization Id {Id}", id);

            // Load the organization if it exists and is not soft deleted.
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
        /// <summary>
        /// Shows the Create form for a new ReferringOrganization.
        ///
        /// This page does not require dropdown population, so it simply returns the view.
        /// </summary>
        public IActionResult Create()
        {
            _logger.LogInformation("Loading Create Referring Organization page");
            return View();
        }

        // POST: ReferringOrganizations/Create
        /// <summary>
        /// Processes form submission to create a new ReferringOrganization.
        ///
        /// Security:
        /// - [ValidateAntiForgeryToken] provides CSRF protection.
        ///
        /// Binding:
        /// - [Bind(...)] limits which fields are accepted from the form (prevents over-posting).
        ///
        /// Validation:
        /// - Removes navigation properties (Referrals and audit navigation props) from ModelState
        ///   because HTML forms do not post those complex objects.
        /// - If invalid, re-renders the Create view with validation messages.
        ///
        /// Audit:
        /// - Sets CreatedAt/UpdatedAt and placeholder CreatedBy/UpdatedBy user IDs.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Type,PhoneNumber,Email,AddressLine1,AddressLine2,City,State,PostalCode,PrimaryContactName,Notes,IsActive")] ReferringOrganization referringOrganization)
        {
            _logger.LogInformation("Attempting to create Referring Organization '{Name}'", referringOrganization.Name);

            // Navigation properties are not posted by forms.
            // Removing these avoids false ModelState validation errors.
            ModelState.Remove(nameof(ReferringOrganization.Referrals));
            ModelState.Remove(nameof(ReferringOrganization.CreatedByUser));
            ModelState.Remove(nameof(ReferringOrganization.UpdatedByUser));

            // If validation fails, return the view with the user's input and validation messages.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create Referring Organization failed validation for '{Name}'", referringOrganization.Name);
                return View(referringOrganization);
            }

            // Set audit fields using UTC timestamps.
            var now = DateTime.UtcNow;
            referringOrganization.CreatedAt = now;
            referringOrganization.UpdatedAt = now;
            referringOrganization.CreatedByUserId = null; // Placeholder until auth integration.
            referringOrganization.UpdatedByUserId = null; // Placeholder until auth integration.

            // Stage insert.
            _context.Add(referringOrganization);

            try
            {
                // Persist to DB.
                await _context.SaveChangesAsync();

                _logger.LogInformation("Referring Organization Id {Id} created successfully", referringOrganization.Id);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // Likely causes: unique constraint violations (if configured), DB connectivity, etc.
                _logger.LogError(ex, "Error creating Referring Organization '{Name}'", referringOrganization.Name);

                ModelState.AddModelError("", "Unable to save referring organization.");

                return View(referringOrganization);
            }
        }

        // GET: ReferringOrganizations/Edit/5
        /// <summary>
        /// Shows the Edit form for an existing ReferringOrganization.
        ///
        /// Behavior:
        /// - Returns 404 if id is null or record not found (or soft deleted).
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for Referring Organization Id {Id}", id);

            // Load the organization for editing (excluding deleted).
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
        /// <summary>
        /// Processes form submission to update an existing ReferringOrganization.
        ///
        /// Parameters:
        /// - id: route Id
        /// - formModel: posted model (limited by [Bind] to editable properties)
        ///
        /// Safety checks:
        /// - Confirms route Id matches posted model Id.
        /// - Removes navigation properties from ModelState (not posted by forms).
        ///
        /// Update strategy:
        /// - Loads existing record (ensures not soft deleted)
        /// - Copies editable fields from formModel to existing
        /// - Updates audit fields (UpdatedAt/UpdatedByUserId)
        ///
        /// Error handling:
        /// - DbUpdateConcurrencyException: record changed/deleted since load.
        /// - DbUpdateException: general DB update failures.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,Name,Type,PhoneNumber,Email,AddressLine1,AddressLine2,City,State,PostalCode,PrimaryContactName,Notes,IsActive")] ReferringOrganization formModel)
        {
            _logger.LogInformation("Attempting to edit Referring Organization Id {Id}", id);

            // Ensure route id matches the model id to prevent mismatched/tampered posts.
            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            // Navigation properties are not posted back by the form.
            // If they remain in ModelState, the model may appear invalid incorrectly.
            ModelState.Remove(nameof(ReferringOrganization.Referrals));
            ModelState.Remove(nameof(ReferringOrganization.CreatedByUser));
            ModelState.Remove(nameof(ReferringOrganization.UpdatedByUser));

            // If invalid, return view with validation messages.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit Referring Organization failed validation for Id {Id}", id);
                return View(formModel);
            }

            // Load existing record to safely apply updates.
            var existing = await _context.ReferringOrganizations
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (existing == null)
            {
                _logger.LogWarning("Referring Organization Id {Id} not found during edit save", id);
                return NotFound();
            }

            // Update editable fields
            // Note: This "copy onto tracked entity" pattern avoids unintended overwrites.
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

            // Update audit fields.
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // Placeholder until auth integration.

            try
            {
                // Persist updates.
                await _context.SaveChangesAsync();

                _logger.LogInformation("Referring Organization Id {Id} updated successfully", id);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Concurrency exceptions indicate the record was updated or deleted since it was loaded.
                if (!await ReferringOrganizationExists(formModel.Id))
                {
                    _logger.LogWarning("Referring Organization Id {Id} no longer exists during concurrency check", id);
                    return NotFound();
                }

                // If it still exists, rethrow and let global handlers deal with it.
                throw;
            }
            catch (DbUpdateException ex)
            {
                // General DB update failure (constraints, connectivity, etc.).
                _logger.LogError(ex, "Error updating Referring Organization Id {Id}", id);

                ModelState.AddModelError("", "Unable to save changes.");

                return View(formModel);
            }
        }

        // GET: ReferringOrganizations/Delete/5
        /// <summary>
        /// Shows the Delete confirmation page for a ReferringOrganization.
        ///
        /// Notes:
        /// - This GET action does not delete anything.
        /// - It loads the entity so the user can confirm the correct organization.
        /// - Actual soft delete occurs in DeleteConfirmed.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for Referring Organization Id {Id}", id);

            // Load record for confirmation display (excluding soft-deleted).
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
        /// <summary>
        /// Executes the delete operation (soft delete) for a ReferringOrganization.
        ///
        /// Soft delete strategy:
        /// - Sets DeletedAt (marks record as deleted)
        /// - Updates UpdatedAt and UpdatedByUserId
        /// - Keeps the row for history/audit/referential integrity
        ///
        /// Error handling:
        /// - If SaveChanges fails, sets TempData["ErrorMessage"] and redirects back to Delete confirmation.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting Referring Organization Id {Id}", id);

            // Load the record to delete (must not already be soft deleted).
            var referringOrganization = await _context.ReferringOrganizations
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (referringOrganization == null)
            {
                _logger.LogWarning("Referring Organization Id {Id} not found during delete", id);
                return NotFound();
            }

            // Apply soft delete + audit fields.
            referringOrganization.DeletedAt = DateTime.UtcNow;
            referringOrganization.UpdatedAt = DateTime.UtcNow;
            referringOrganization.UpdatedByUserId = null; // Placeholder until auth integration.

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Referring Organization Id {Id} soft deleted", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error soft deleting Referring Organization Id {Id}", id);

                // TempData persists for one redirect so the Delete view can show the message.
                TempData["ErrorMessage"] = "Unable to delete referring organization.";

                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Helper method used by Edit POST concurrency handling.
        /// Confirms whether a non-deleted ReferringOrganization exists for the provided Id.
        /// </summary>
        private async Task<bool> ReferringOrganizationExists(ulong id)
        {
            return await _context.ReferringOrganizations
                .AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}