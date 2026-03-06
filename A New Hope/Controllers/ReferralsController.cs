using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// ReferralsController
    /// -------------------
    /// This controller manages DIO operations for Referral records.
    ///
    /// In your domain, a Referral appears to represent a record that:
    /// - Links a client (ClientUserId -> DomainUsers/User entity) to a ReferringOrganization
    /// - Captures referral metadata such as:
    ///     - ReferredOn date
    ///     - Status
    ///     - ValidFrom/ValidTo date range
    ///     - ReferredBy contact info (name/phone/email)
    ///     - Notes
    ///
    /// Key behaviors implemented here:
    /// - Uses Entity Framework Core (ApplicationDbContext) for database access.
    /// - Uses dependency-injected ILogger to log reads/writes/errors for troubleshooting and traceability.
    /// - Uses SOFT DELETE semantics: deletes set DeletedAt (and update audit fields) instead of removing rows.
    /// - Filters out soft-deleted referrals in Index/Details/Edit/Delete flows (DeletedAt == null).
    /// - Populates dropdown lists for selecting ClientUser and ReferringOrganization.
    ///
    /// Notes on audit fields:
    /// - CreatedAt/UpdatedAt are set using DateTime.UtcNow.
    /// - CreatedByUserId/UpdatedByUserId are currently set to null until auth/user tracking is implemented.
    /// </summary>
    public class ReferralsController : Controller
    {
        /// <summary>
        /// EF Core DbContext used to query and persist Referral data and related entities.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Logger for this controller. Output destination depends on Program.cs logging providers.
        /// </summary>
        private readonly ILogger<ReferralsController> _logger;

        /// <summary>
        /// Constructor with dependency injection.
        /// </summary>
        public ReferralsController(ApplicationDbContext context, ILogger<ReferralsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Referrals
        /// <summary>
        /// Displays a list of referrals (non-deleted only).
        ///
        /// Query behavior:
        /// - Filters out soft-deleted referrals (DeletedAt == null).
        /// - Eager-loads:
        ///     - ClientUser (so the view can show client info)
        ///     - ReferringOrganization (so the view can show organization name)
        /// - Sorts:
        ///     - Most recent referrals first (OrderByDescending ReferredOn)
        ///     - Then by Id for stable ordering when dates match
        /// </summary>
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading Referrals Index page");

            // Load active referrals with necessary related entities for display.
            var referrals = await _context.Referrals
                .Where(r => r.DeletedAt == null)
                .Include(r => r.ClientUser)
                .Include(r => r.ReferringOrganization)
                .OrderByDescending(r => r.ReferredOn)
                .ThenBy(r => r.Id)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} referrals", referrals.Count);

            return View(referrals);
        }

        // GET: Referrals/Details/5
        /// <summary>
        /// Displays details for a single Referral by primary key Id.
        ///
        /// Behavior:
        /// - Returns 404 if id is null or referral not found (or soft deleted).
        /// - Eager-loads ClientUser and ReferringOrganization for display context.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for Referral Id {Id}", id);

            // Load the referral record with related entities.
            var referral = await _context.Referrals
                .Where(r => r.DeletedAt == null)
                .Include(r => r.ClientUser)
                .Include(r => r.ReferringOrganization)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (referral == null)
            {
                _logger.LogWarning("Referral Id {Id} not found", id);
                return NotFound();
            }

            return View(referral);
        }

        // GET: Referrals/Create
        /// <summary>
        /// Shows the Create form for a new Referral.
        ///
        /// Important:
        /// - Referral requires both a ClientUserId and ReferringOrganizationId,
        ///   so dropdown lists must be populated before rendering the view.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create Referral page");
            await PopulateDropdowns();
            return View();
        }

        // POST: Referrals/Create
        /// <summary>
        /// Processes form submission to create a new Referral.
        ///
        /// Security:
        /// - [ValidateAntiForgeryToken] provides CSRF protection.
        ///
        /// Binding:
        /// - [Bind(...)] limits which fields are accepted from the form (prevents over-posting).
        ///
        /// Validation:
        /// - Removes navigation properties from ModelState because forms do not post navigation objects.
        /// - If invalid, repopulates dropdowns and re-renders the view with validation messages.
        ///
        /// Audit:
        /// - Sets CreatedAt/UpdatedAt and placeholder CreatedBy/UpdatedBy user IDs.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientUserId,ReferringOrganizationId,ReferredOn,Status,ValidFrom,ValidTo,ReferredByName,ReferredByPhoneNumber,ReferredByEmail,Notes")] Referral referral)
        {
            _logger.LogInformation("Attempting to create Referral for ClientUserId {ClientUserId}", referral.ClientUserId);

            // Navigation properties are not included in form posts. Remove them from ModelState to avoid false invalidation.
            ModelState.Remove(nameof(Referral.ClientUser));
            ModelState.Remove(nameof(Referral.ReferringOrganization));
            ModelState.Remove(nameof(Referral.CreatedByUser));
            ModelState.Remove(nameof(Referral.UpdatedByUser));

            // If validation fails, repopulate dropdowns so the view renders correctly.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create Referral failed validation for ClientUserId {ClientUserId}", referral.ClientUserId);
                await PopulateDropdowns(referral.ClientUserId, referral.ReferringOrganizationId);
                return View(referral);
            }

            // Set audit fields (UTC for consistency).
            var now = DateTime.UtcNow;
            referral.CreatedAt = now;
            referral.UpdatedAt = now;
            referral.CreatedByUserId = null; // Placeholder until auth integration.
            referral.UpdatedByUserId = null; // Placeholder until auth integration.

            // Stage insert.
            _context.Add(referral);

            try
            {
                // Persist to database.
                await _context.SaveChangesAsync();

                _logger.LogInformation("Referral Id {Id} created successfully", referral.Id);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // Common causes: FK constraints, unique constraints (if present), DB connectivity, etc.
                _logger.LogError(ex, "Error creating Referral for ClientUserId {ClientUserId}", referral.ClientUserId);

                ModelState.AddModelError("", "Unable to save referral.");

                // Rebuild dropdowns so view can re-render correctly with same selections.
                await PopulateDropdowns(referral.ClientUserId, referral.ReferringOrganizationId);

                return View(referral);
            }
        }

        // GET: Referrals/Edit/5
        /// <summary>
        /// Shows the Edit form for an existing Referral.
        ///
        /// Behavior:
        /// - Returns 404 if id is null or referral not found (or soft deleted).
        /// - Populates dropdowns so the form can display current selections.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for Referral Id {Id}", id);

            // Load the referral record being edited (excluding deleted).
            var referral = await _context.Referrals
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (referral == null)
            {
                _logger.LogWarning("Referral Id {Id} not found for edit", id);
                return NotFound();
            }

            // Populate dropdowns with current values so the form remains consistent.
            await PopulateDropdowns(referral.ClientUserId, referral.ReferringOrganizationId);

            return View(referral);
        }

        // POST: Referrals/Edit/5
        /// <summary>
        /// Processes form submission to update an existing Referral.
        ///
        /// Parameters:
        /// - id: route Id (primary key of referral)
        /// - formModel: posted model (limited by [Bind] to editable properties)
        ///
        /// Safety checks:
        /// - Confirms route Id matches posted model Id.
        /// - Removes navigation properties from ModelState (not posted by forms).
        ///
        /// Update strategy:
        /// - Loads existing record (ensures not soft deleted)
        /// - Copies all editable fields from formModel to existing
        /// - Updates audit fields (UpdatedAt/UpdatedByUserId)
        ///
        /// Error handling:
        /// - DbUpdateConcurrencyException: record changed/deleted since it was loaded.
        /// - DbUpdateException: general DB update failures.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,ClientUserId,ReferringOrganizationId,ReferredOn,Status,ValidFrom,ValidTo,ReferredByName,ReferredByPhoneNumber,ReferredByEmail,Notes")] Referral formModel)
        {
            _logger.LogInformation("Attempting to edit Referral Id {Id}", id);

            // Prevent tampering/mismatched posts by enforcing route/model id equality.
            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            // Remove navigation props from validation, as they are not posted by the form.
            ModelState.Remove(nameof(Referral.ClientUser));
            ModelState.Remove(nameof(Referral.ReferringOrganization));
            ModelState.Remove(nameof(Referral.CreatedByUser));
            ModelState.Remove(nameof(Referral.UpdatedByUser));

            // If invalid, repopulate dropdowns and re-render view with validation messages.
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit Referral failed validation for Id {Id}", id);
                await PopulateDropdowns(formModel.ClientUserId, formModel.ReferringOrganizationId);
                return View(formModel);
            }

            // Load the existing record to apply updates safely.
            var existing = await _context.Referrals
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (existing == null)
            {
                _logger.LogWarning("Referral Id {Id} not found during edit save", id);
                return NotFound();
            }

            // Update editable fields
            // Note: This is a "copy into tracked entity" approach (rather than attaching formModel),
            // which helps avoid unintended overwrites.
            existing.ClientUserId = formModel.ClientUserId;
            existing.ReferringOrganizationId = formModel.ReferringOrganizationId;
            existing.ReferredOn = formModel.ReferredOn;
            existing.Status = formModel.Status;
            existing.ValidFrom = formModel.ValidFrom;
            existing.ValidTo = formModel.ValidTo;
            existing.ReferredByName = formModel.ReferredByName;
            existing.ReferredByPhoneNumber = formModel.ReferredByPhoneNumber;
            existing.ReferredByEmail = formModel.ReferredByEmail;
            existing.Notes = formModel.Notes;

            // Audit fields.
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null; // Placeholder until auth integration.

            try
            {
                // Persist updates.
                await _context.SaveChangesAsync();

                _logger.LogInformation("Referral Id {Id} updated successfully", id);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Concurrency exception indicates record may have been deleted/changed since load.
                if (!await ReferralExists(formModel.Id))
                {
                    _logger.LogWarning("Referral Id {Id} no longer exists during concurrency check", id);
                    return NotFound();
                }

                // If it still exists, rethrow to be handled by global exception handling.
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating Referral Id {Id}", id);

                ModelState.AddModelError("", "Unable to save changes.");

                // Repopulate dropdowns so the view renders correctly on error.
                await PopulateDropdowns(formModel.ClientUserId, formModel.ReferringOrganizationId);

                return View(formModel);
            }
        }

        // GET: Referrals/Delete/5
        /// <summary>
        /// Shows the Delete confirmation page for a Referral.
        ///
        /// Notes:
        /// - This GET action does not delete anything.
        /// - It loads the referral and related entities so the user can confirm the correct record.
        /// - Actual soft delete is performed in DeleteConfirmed.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for Referral Id {Id}", id);

            // Load record for confirmation view, including related entities for context.
            var referral = await _context.Referrals
                .Where(r => r.DeletedAt == null)
                .Include(r => r.ClientUser)
                .Include(r => r.ReferringOrganization)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (referral == null)
            {
                _logger.LogWarning("Referral Id {Id} not found for delete", id);
                return NotFound();
            }

            return View(referral);
        }

        // POST: Referrals/Delete/5
        /// <summary>
        /// Executes the delete operation (soft delete) for a Referral.
        ///
        /// Soft delete strategy:
        /// - Sets DeletedAt to UTC now
        /// - Updates audit fields
        /// - Keeps the row for history/audit/referential integrity
        ///
        /// Error handling:
        /// - If SaveChanges fails, store a TempData message and redirect back to confirmation page.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting Referral Id {Id}", id);

            // Load the record to be soft deleted.
            var referral = await _context.Referrals
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (referral == null)
            {
                _logger.LogWarning("Referral Id {Id} not found during delete", id);
                return NotFound();
            }

            // Apply soft delete + audit fields.
            referral.DeletedAt = DateTime.UtcNow;
            referral.UpdatedAt = DateTime.UtcNow;
            referral.UpdatedByUserId = null; // Placeholder until auth integration.

            try
            {
                // Persist soft delete changes.
                await _context.SaveChangesAsync();

                _logger.LogInformation("Referral Id {Id} soft deleted", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error soft deleting Referral Id {Id}", id);

                // TempData persists across one redirect to show feedback to the user.
                TempData["ErrorMessage"] = "Unable to delete referral.";

                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Populates dropdown lists needed by the Create/Edit Referral forms.
        ///
        /// Dropdowns:
        /// - ClientUserId: list of users (DomainUsers) displayed as "LastName, FirstName (Email)"
        /// - ReferringOrganizationId: list of organizations displayed by Name
        ///
        /// Parameters:
        /// - selectedClientUserId: pre-select a specific client (useful for redisplay after validation errors)
        /// - selectedReferringOrganizationId: pre-select a specific organization
        ///
        /// Notes:
        /// - This method logs a debug message with counts so you can confirm the dropdowns were populated.
        /// </summary>
        private async Task PopulateDropdowns(ulong? selectedClientUserId = null, ulong? selectedReferringOrganizationId = null)
        {
            _logger.LogDebug("Populating dropdowns for Referrals");

            // Load users eligible for selection (excluding soft-deleted).
            // Sorting includes email as a tertiary key to provide stable ordering.
            var users = await _context.DomainUsers
                .Where(u => u.DeletedAt == null)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            // Create user-friendly dropdown labels.
            var userOptions = users
                .Select(u => new
                {
                    u.Id,
                    DisplayName = $"{u.LastName}, {u.FirstName} ({u.Email})"
                })
                .ToList();

            // Load referring organizations eligible for selection (excluding soft-deleted).
            var organizations = await _context.ReferringOrganizations
                .Where(o => o.DeletedAt == null)
                .OrderBy(o => o.Name)
                .ToListAsync();

            // Place dropdown data into ViewData for Razor views.
            ViewData["ClientUserId"] = new SelectList(userOptions, "Id", "DisplayName", selectedClientUserId);
            ViewData["ReferringOrganizationId"] = new SelectList(organizations, "Id", "Name", selectedReferringOrganizationId);

            _logger.LogDebug("Dropdowns populated: {UsersCount} users, {OrgsCount} organizations", userOptions.Count, organizations.Count);
        }

        /// <summary>
        /// Helper method used by Edit POST concurrency handling.
        /// Confirms whether a non-deleted referral still exists.
        /// </summary>
        private async Task<bool> ReferralExists(ulong id)
        {
            return await _context.Referrals.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }
    }
}