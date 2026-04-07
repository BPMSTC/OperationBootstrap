using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Models.Inputs;
using A_New_Hope.Models.ViewModels;
using A_New_Hope.Models.ViewModels.Referrals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using A_New_Hope.Services.Interfaces;

namespace A_New_Hope.Controllers
{
    /// <summary>
    /// Manages create, read, update, and soft delete operations for referrals,
    /// plus the new multi-page Referral Entry flow.
    /// </summary>
    public class ReferralsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReferralsController> _logger;
        private readonly IReferringOrganizationService _referringOrganizationService;
        private readonly IClientCreationService _clientEntryService;
        private readonly IReferralService _referralService;

        private const string ReferralWizardSessionKey = "ReferralWizard.Step1";
        private const string ReferralEntrySessionKey = "ReferralEntry.Draft";

        // Store the allowed 2-letter US state codes for validation.
        private static readonly HashSet<string> ValidUsStateCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "AL","AK","AZ","AR","CA","CO","CT","DE","FL","GA",
            "HI","ID","IL","IN","IA","KS","KY","LA","ME","MD",
            "MA","MI","MN","MS","MO","MT","NE","NV","NH","NJ",
            "NM","NY","NC","ND","OH","OK","OR","PA","RI","SC",
            "SD","TN","TX","UT","VT","VA","WA","WV","WI","WY","DC"
        };

        /// <summary>
        /// Creates the controller with the required database context and logger.
        /// </summary>
        public ReferralsController(
            ApplicationDbContext context,
            ILogger<ReferralsController> logger,
            IReferringOrganizationService referringOrganizationService,
            IClientCreationService clientEntryService,
            IReferralService referralService)
        {
            _context = context;
            _logger = logger;
            _referringOrganizationService = referringOrganizationService;
            _clientEntryService = clientEntryService;
            _referralService = referralService;
        }

        // =========================================================
        // STANDARD CRUD
        // =========================================================

        // GET: Referrals
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading Referrals Index page");

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
        public async Task<IActionResult> Details(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Fetching details for Referral Id {Id}", id);

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
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Loading Create Referral page");

            await PopulateDropdowns();
            return View();
        }

        // POST: Referrals/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientUserId,ReferringOrganizationId,ReferredOn,Status,ValidFrom,ValidTo,ReferredByName,ReferredByPhoneNumber,ReferredByEmail,Notes")] Referral referral)
        {
            _logger.LogInformation("Attempting to create Referral for ClientUserId {ClientUserId}", referral.ClientUserId);

            ModelState.Remove(nameof(Referral.ClientUser));
            ModelState.Remove(nameof(Referral.ReferringOrganization));
            ModelState.Remove(nameof(Referral.CreatedByUser));
            ModelState.Remove(nameof(Referral.UpdatedByUser));

            NormalizeReferral(referral);
            await ApplyReferralValidationAsync(referral);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create Referral failed validation for ClientUserId {ClientUserId}", referral.ClientUserId);
                await PopulateDropdowns(referral.ClientUserId, referral.ReferringOrganizationId);
                return View(referral);
            }

            try
            {
                var referralInput = new ReferralDetailsInput
                {
                    ReferredOn = referral.ReferredOn,
                    Status = referral.Status,
                    ValidFrom = referral.ValidFrom,
                    ValidTo = referral.ValidTo,
                    ReferredByName = referral.ReferredByName,
                    ReferredByPhoneNumber = referral.ReferredByPhoneNumber,
                    ReferredByEmail = referral.ReferredByEmail,
                    Notes = referral.Notes
                };

                var referralId = await _referralService.CreateAndReturnIdAsync(
                    referralInput,
                    referral.ClientUserId,
                    referral.ReferringOrganizationId,
                    actingUserId: null);

                _logger.LogInformation("Referral Id {Id} created successfully", referralId);
                return RedirectToAction(nameof(Details), new { id = referralId });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business validation failed while creating Referral for ClientUserId {ClientUserId}", referral.ClientUserId);

                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateDropdowns(referral.ClientUserId, referral.ReferringOrganizationId);
                return View(referral);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argument validation failed while creating Referral for ClientUserId {ClientUserId}", referral.ClientUserId);

                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateDropdowns(referral.ClientUserId, referral.ReferringOrganizationId);
                return View(referral);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error creating Referral for ClientUserId {ClientUserId}", referral.ClientUserId);

                ModelState.AddModelError("", "Unable to save referral.");
                await PopulateDropdowns(referral.ClientUserId, referral.ReferringOrganizationId);
                return View(referral);
            }
        }

        // =========================================================
        // REFERRAL ENTRY FLOW
        // =========================================================

        // GET: Referrals/StartReferralEntry
        public IActionResult StartReferralEntry()
        {
            _logger.LogInformation("Starting Referral Entry flow");

            ClearReferralEntryDraft();

            var draft = new ReferralEntryDraft();

            if (!draft.HouseholdMembers.Any())
            {
                draft.HouseholdMembers.Add(new HouseholdMemberEntryInput());
            }

            SaveReferralEntryDraft(draft);

            return RedirectToAction(nameof(OrganizationEntry));
        }

        // GET: Referrals/StartReferralEntryForClient
        [HttpGet]
        public async Task<IActionResult> StartReferralEntryForClient(ulong id)
        {
            _logger.LogInformation("Starting Referral Entry flow for existing ClientUserId {ClientUserId}", id);

            var clientExists = await _context.DomainUsers.AnyAsync(u =>
                u.Id == id &&
                u.DeletedAt == null &&
                u.UserType == UserType.Client &&
                u.IsActive);

            if (!clientExists)
            {
                _logger.LogWarning("Cannot start Referral Entry. ClientUserId {ClientUserId} was not found or is not an active client.", id);
                TempData["ErrorMessage"] = "The selected client could not be found.";
                return RedirectToAction("Index", "Users");
            }

            ClearReferralEntryDraft();

            var draft = new ReferralEntryDraft
            {
                ExistingClientUserId = id,
                NewClient = new ClientEntryInput()
            };

            if (!draft.HouseholdMembers.Any())
            {
                draft.HouseholdMembers.Add(new HouseholdMemberEntryInput());
            }

            SaveReferralEntryDraft(draft);

            return RedirectToAction(nameof(OrganizationEntry));
        }


        // GET: Referrals/OrganizationEntry
        [HttpGet]
        public async Task<IActionResult> OrganizationEntry()
        {
            _logger.LogInformation("Loading Organization Entry page");

            var draft = LoadReferralEntryDraft() ?? new ReferralEntryDraft();

            var vm = new OrganizationEntryViewModel
            {
                SelectedReferringOrganizationId = draft.ExistingReferringOrganizationId,
                NewOrganization = draft.NewOrganization ?? new ReferringOrganizationEntryInput(),
                OrganizationMode = draft.HasExistingOrganization ? "existing"
                    : draft.HasNewOrganization ? "new"
                    : null
            };

            await PopulateOrganizationEntryDropdowns(vm);

            return View(vm);
        }

        // POST: Referrals/OrganizationEntry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrganizationEntry(OrganizationEntryViewModel vm)
        {
            _logger.LogInformation("Submitting Organization Entry page");

            NormalizeOrganizationEntry(vm);
            await ApplyOrganizationEntryValidationAsync(vm);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Organization Entry failed validation");
                await PopulateOrganizationEntryDropdowns(vm);
                return View(vm);
            }

            var draft = LoadReferralEntryDraft() ?? new ReferralEntryDraft();

            if (string.Equals(vm.OrganizationMode, "existing", StringComparison.OrdinalIgnoreCase))
            {
                draft.ExistingReferringOrganizationId = vm.SelectedReferringOrganizationId;
                draft.NewOrganization = new ReferringOrganizationEntryInput();
            }
            else
            {
                draft.ExistingReferringOrganizationId = null;
                draft.NewOrganization = vm.NewOrganization ?? new ReferringOrganizationEntryInput();
            }

            SaveReferralEntryDraft(draft);

            if (draft.HasExistingClient)
            {
                return RedirectToAction(nameof(ReferralDetails));
            }

            return RedirectToAction(nameof(ClientEntry));
        }

        // GET: Referrals/ClientEntry
        [HttpGet]
        public async Task<IActionResult> ClientEntry()
        {
            _logger.LogInformation("Loading Client Entry page");

            var draft = LoadReferralEntryDraft();

            if (draft == null)
            {
                _logger.LogWarning("Client Entry requested without Referral Entry draft");
                TempData["ErrorMessage"] = "Your referral entry draft was not found. Please start again.";
                return RedirectToAction(nameof(StartReferralEntry));
            }

            var vm = new ClientEntryViewModel
            {
                SelectedClientUserId = draft.ExistingClientUserId,
                NewClient = draft.NewClient ?? new ClientEntryInput(),
                ClientMode = draft.HasExistingClient ? "existing"
                    : draft.HasNewClient ? "new"
                    : null
            };

            await PopulateClientEntryDropdowns(vm);

            return View(vm);
        }

        // POST: Referrals/ClientEntry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClientEntry(ClientEntryViewModel vm)
        {
            _logger.LogInformation("Submitting Client Entry page");

            NormalizeClientEntry(vm);
            await ApplyClientEntryValidationAsync(vm);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Client Entry failed validation");
                await PopulateClientEntryDropdowns(vm);
                return View(vm);
            }

            var draft = LoadReferralEntryDraft() ?? new ReferralEntryDraft();

            if (string.Equals(vm.ClientMode, "existing", StringComparison.OrdinalIgnoreCase))
            {
                draft.ExistingClientUserId = vm.SelectedClientUserId;
                draft.NewClient = new ClientEntryInput();
                draft.HouseholdMembers = new List<HouseholdMemberEntryInput>();
            }
            else
            {
                draft.ExistingClientUserId = null;
                draft.NewClient = vm.NewClient ?? new ClientEntryInput();
            }

            SaveReferralEntryDraft(draft);

            if (draft.HasNewClient)
            {
                return RedirectToAction(nameof(HouseholdEntry));
            }

            return RedirectToAction(nameof(ReferralDetails));
        }

        // GET: Referrals/HouseholdEntry
        [HttpGet]
        public IActionResult HouseholdEntry()
        {
            _logger.LogInformation("Loading Household Entry page");

            var draft = LoadReferralEntryDraft();

            if (draft == null)
            {
                _logger.LogWarning("Household Entry requested without Referral Entry draft");
                TempData["ErrorMessage"] = "Your referral entry draft was not found. Please start again.";
                return RedirectToAction(nameof(StartReferralEntry));
            }

            if (!draft.RequiresHouseholdStep)
            {
                return RedirectToAction(nameof(ReferralDetails));
            }

            var vm = new HouseholdEntryViewModel
            {
                HouseholdMembers = draft.HouseholdMembers.Any()
                    ? draft.HouseholdMembers
                    : new List<HouseholdMemberEntryInput> { new HouseholdMemberEntryInput() },
                HasHouseholdMembers = draft.HouseholdMembers.Any(h => h.HasStarted)
            };

            return View(vm);
        }

        // POST: Referrals/HouseholdEntry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HouseholdEntry(HouseholdEntryViewModel vm)
        {
            _logger.LogInformation("Submitting Household Entry page");

            vm.HouseholdMembers ??= new List<HouseholdMemberEntryInput>();

            if (!vm.HasHouseholdMembers)
            {
                vm.HouseholdMembers = new List<HouseholdMemberEntryInput>();
            }
            else
            {
                NormalizeHouseholdEntry(vm);
                ApplyHouseholdEntryValidation(vm);
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Household Entry failed validation");

                if (!vm.HouseholdMembers.Any())
                {
                    vm.HouseholdMembers.Add(new HouseholdMemberEntryInput());
                }

                return View(vm);
            }

            var draft = LoadReferralEntryDraft() ?? new ReferralEntryDraft();
            draft.HouseholdMembers = vm.HouseholdMembers;

            SaveReferralEntryDraft(draft);

            return RedirectToAction(nameof(ReferralDetails));
        }

        // GET: Referrals/ReferralDetails
        [HttpGet]
        public async Task<IActionResult> ReferralDetails()
        {
            _logger.LogInformation("Loading Referral Details page");

            var draft = LoadReferralEntryDraft();

            if (draft == null)
            {
                _logger.LogWarning("Referral Details requested without Referral Entry draft");
                TempData["ErrorMessage"] = "Your referral entry draft was not found. Please start again.";
                return RedirectToAction(nameof(StartReferralEntry));
            }

            var vm = new ReferralDetailsViewModel
            {
                Referral = draft.Referral ?? new ReferralDetailsInput(),
                BackAction = draft.RequiresHouseholdStep ? nameof(HouseholdEntry) : nameof(ClientEntry)
            };

            await PopulateReferralDetailsDropdowns(vm);

            return View(vm);
        }

        // POST: Referrals/ReferralDetails
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReferralDetails(ReferralDetailsViewModel vm)
        {
            _logger.LogInformation("Submitting Referral Details page");

            NormalizeReferralDetails(vm);
            await ApplyReferralDetailsValidationAsync(vm);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Referral Details failed validation");
                await PopulateReferralDetailsDropdowns(vm);
                return View(vm);
            }

            var draft = LoadReferralEntryDraft() ?? new ReferralEntryDraft();
            draft.Referral = vm.Referral ?? new ReferralDetailsInput();

            SaveReferralEntryDraft(draft);

            return RedirectToAction(nameof(ReviewReferralEntry));
        }

        // GET: Referrals/ReviewReferralEntry
        [HttpGet]
        public async Task<IActionResult> ReviewReferralEntry()
        {
            _logger.LogInformation("Loading Review Referral Entry page");

            var draft = LoadReferralEntryDraft();

            if (draft == null)
            {
                _logger.LogWarning("Review Referral Entry requested without Referral Entry draft");
                TempData["ErrorMessage"] = "Your referral entry draft was not found. Please start again.";
                return RedirectToAction(nameof(StartReferralEntry));
            }

            var vm = await BuildReferralEntryReviewViewModelAsync(draft);

            return View(vm);
        }

        // POST: Referrals/ConfirmReferralEntry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReferralEntry()
        {
            _logger.LogInformation("Submitting Confirm Referral Entry");

            var draft = LoadReferralEntryDraft();

            if (draft == null)
            {
                _logger.LogWarning("Confirm Referral Entry requested without Referral Entry draft");
                TempData["ErrorMessage"] = "Your referral entry draft was not found. Please start again.";
                return RedirectToAction(nameof(StartReferralEntry));
            }

            await ApplyReferralEntryDraftValidationAsync(draft);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Confirm Referral Entry failed final validation");

                var reviewVm = await BuildReferralEntryReviewViewModelAsync(draft);
                return View(nameof(ReviewReferralEntry), reviewVm);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var now = DateTime.UtcNow;

                ulong referringOrganizationId;
                ulong clientUserId;

                // -------------------------------------------------
                // ORGANIZATION
                // -------------------------------------------------
                if (draft.HasExistingOrganization)
                {
                    referringOrganizationId = draft.ExistingReferringOrganizationId!.Value;
                }
                else
                {
                    referringOrganizationId = await _referringOrganizationService.CreateAndReturnIdAsync(
                        draft.NewOrganization,
                        actingUserId: null);
                }

                // -------------------------------------------------
                // CLIENT / PROFILE / HOUSEHOLD
                // -------------------------------------------------
                if (draft.HasExistingClient)
                {
                    clientUserId = draft.ExistingClientUserId!.Value;
                }
                else
                {
                    clientUserId = await _clientEntryService.CreateClientAndReturnIdAsync(
                        draft.NewClient,
                        draft.HouseholdMembers,
                        actingUserId: null);
                }

                // -------------------------------------------------
                // REFERRAL
                // -------------------------------------------------
                var referralId = await _referralService.CreateAndReturnIdAsync(
                    draft.Referral,
                    clientUserId,
                    referringOrganizationId,
                    actingUserId: null);

                await transaction.CommitAsync();

                ClearReferralEntryDraft();

                TempData["SuccessMessage"] = "Referral confirmed successfully.";

                return RedirectToAction(nameof(Details), new { id = referralId });
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "Error confirming Referral Entry");

                ModelState.AddModelError(string.Empty, "Unable to save the referral.");

                var reviewVm = await BuildReferralEntryReviewViewModelAsync(draft);
                return View(nameof(ReviewReferralEntry), reviewVm);
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();

                _logger.LogWarning(ex, "Business validation failed during Confirm Referral Entry");

                ModelState.AddModelError(string.Empty, ex.Message);

                var reviewVm = await BuildReferralEntryReviewViewModelAsync(draft);
                return View(nameof(ReviewReferralEntry), reviewVm);
            }
            catch (ArgumentException ex)
            {
                await transaction.RollbackAsync();

                _logger.LogWarning(ex, "Argument validation failed during Confirm Referral Entry");

                ModelState.AddModelError(string.Empty, ex.Message);

                var reviewVm = await BuildReferralEntryReviewViewModelAsync(draft);
                return View(nameof(ReviewReferralEntry), reviewVm);
            }
        }

        // =========================================================
        // EXISTING EDIT / DELETE
        // =========================================================

        // GET: Referrals/Edit/5
        public async Task<IActionResult> Edit(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Edit page for Referral Id {Id}", id);

            var referral = await _context.Referrals
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (referral == null)
            {
                _logger.LogWarning("Referral Id {Id} not found for edit", id);
                return NotFound();
            }

            await PopulateDropdowns(referral.ClientUserId, referral.ReferringOrganizationId);
            return View(referral);
        }

        // POST: Referrals/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,ClientUserId,ReferringOrganizationId,ReferredOn,Status,ValidFrom,ValidTo,ReferredByName,ReferredByPhoneNumber,ReferredByEmail,Notes")] Referral formModel)
        {
            _logger.LogInformation("Attempting to edit Referral Id {Id}", id);

            if (id != formModel.Id)
            {
                _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                return NotFound();
            }

            ModelState.Remove(nameof(Referral.ClientUser));
            ModelState.Remove(nameof(Referral.ReferringOrganization));
            ModelState.Remove(nameof(Referral.CreatedByUser));
            ModelState.Remove(nameof(Referral.UpdatedByUser));

            NormalizeReferral(formModel);
            await ApplyReferralValidationAsync(formModel, formModel.Id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit Referral failed validation for Id {Id}", id);
                await PopulateDropdowns(formModel.ClientUserId, formModel.ReferringOrganizationId);
                return View(formModel);
            }

            var existing = await _context.Referrals
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (existing == null)
            {
                _logger.LogWarning("Referral Id {Id} not found during edit save", id);
                return NotFound();
            }

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

            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = null;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Referral Id {Id} updated successfully", id);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ReferralExists(formModel.Id))
                {
                    _logger.LogWarning("Referral Id {Id} no longer exists during concurrency check", id);
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating Referral Id {Id}", id);

                ModelState.AddModelError("", "Unable to save changes.");
                await PopulateDropdowns(formModel.ClientUserId, formModel.ReferringOrganizationId);
                return View(formModel);
            }
        }

        // GET: Referrals/Delete/5
        public async Task<IActionResult> Delete(ulong? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete requested with null Id");
                return NotFound();
            }

            _logger.LogInformation("Loading Delete confirmation for Referral Id {Id}", id);

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
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            _logger.LogWarning("Soft deleting Referral Id {Id}", id);

            var referral = await _context.Referrals
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (referral == null)
            {
                _logger.LogWarning("Referral Id {Id} not found during delete", id);
                return NotFound();
            }

            referral.DeletedAt = DateTime.UtcNow;
            referral.UpdatedAt = DateTime.UtcNow;
            referral.UpdatedByUserId = null;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Referral Id {Id} soft deleted", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error soft deleting Referral Id {Id}", id);

                TempData["ErrorMessage"] = "Unable to delete referral.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // SESSION HELPERS - NEW REFERRAL ENTRY FLOW
        // =========================================================

        private void SaveReferralEntryDraft(ReferralEntryDraft draft)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(draft);
            HttpContext.Session.SetString(ReferralEntrySessionKey, json);
        }

        private ReferralEntryDraft? LoadReferralEntryDraft()
        {
            var json = HttpContext.Session.GetString(ReferralEntrySessionKey);

            return string.IsNullOrWhiteSpace(json)
                ? null
                : System.Text.Json.JsonSerializer.Deserialize<ReferralEntryDraft>(json);
        }

        private void ClearReferralEntryDraft()
        {
            HttpContext.Session.Remove(ReferralEntrySessionKey);
        }

        // =========================================================
        // DROPDOWNS - STANDARD CRUD
        // =========================================================

        private async Task PopulateDropdowns(ulong? selectedClientUserId = null, ulong? selectedReferringOrganizationId = null)
        {
            _logger.LogDebug("Populating dropdowns for Referrals");

            var users = await _context.DomainUsers
                .Where(u => u.DeletedAt == null)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            var userOptions = users
                .Select(u => new
                {
                    u.Id,
                    DisplayName = $"{u.LastName}, {u.FirstName} ({u.Email})"
                })
                .ToList();

            var organizations = await _context.ReferringOrganizations
                .Where(o => o.DeletedAt == null)
                .OrderBy(o => o.Name)
                .ToListAsync();

            ViewData["ClientUserId"] = new SelectList(userOptions, "Id", "DisplayName", selectedClientUserId);
            ViewData["ReferringOrganizationId"] = new SelectList(organizations, "Id", "Name", selectedReferringOrganizationId);
        }

        // =========================================================
        // DROPDOWNS - REFERRAL ENTRY FLOW
        // =========================================================

        private async Task PopulateOrganizationEntryDropdowns(OrganizationEntryViewModel vm)
        {
            vm.ExistingOrganizations = await _context.ReferringOrganizations
                .Where(o => o.DeletedAt == null && o.IsActive)
                .OrderBy(o => o.Name)
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = o.Name
                })
                .ToListAsync();
        }

        private async Task PopulateClientEntryDropdowns(ClientEntryViewModel vm)
        {
            vm.ExistingClients = await _context.DomainUsers
                .Where(u => u.DeletedAt == null && u.UserType == UserType.Client && u.IsActive)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Email)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace($"{u.LastName}{u.FirstName}".Trim())
                        ? u.Email!
                        : $"{u.LastName}, {u.FirstName} ({u.Email})"
                })
                .ToListAsync();
        }

        private Task PopulateReferralDetailsDropdowns(ReferralDetailsViewModel vm)
        {
            vm.ReferralStatusOptions = Enum.GetValues(typeof(ReferralStatus))
                .Cast<ReferralStatus>()
                .Select(status => new SelectListItem
                {
                    Value = status.ToString(),
                    Text = status.ToString(),
                    Selected = vm.Referral.Status == status
                })
                .ToList();

            return Task.CompletedTask;
        }

        private async Task<ReferralEntryReviewViewModel> BuildReferralEntryReviewViewModelAsync(ReferralEntryDraft draft)
        {
            var vm = new ReferralEntryReviewViewModel
            {
                Draft = draft
            };

            if (draft.HasExistingOrganization)
            {
                vm.SelectedOrganizationDisplayName = await _context.ReferringOrganizations
                    .Where(o => o.Id == draft.ExistingReferringOrganizationId && o.DeletedAt == null)
                    .Select(o => o.Name)
                    .FirstOrDefaultAsync();
            }
            else if (draft.HasNewOrganization)
            {
                vm.SelectedOrganizationDisplayName = draft.NewOrganization.Name;
            }

            if (draft.HasExistingClient)
            {
                vm.SelectedClientDisplayName = await _context.DomainUsers
                    .Where(u => u.Id == draft.ExistingClientUserId && u.DeletedAt == null)
                    .Select(u => string.IsNullOrWhiteSpace($"{u.LastName}{u.FirstName}".Trim())
                        ? u.Email!
                        : $"{u.LastName}, {u.FirstName} ({u.Email})")
                    .FirstOrDefaultAsync();
            }
            else if (draft.HasNewClient)
            {
                vm.SelectedClientDisplayName =
                    string.IsNullOrWhiteSpace($"{draft.NewClient.LastName}{draft.NewClient.FirstName}".Trim())
                        ? draft.NewClient.Email
                        : $"{draft.NewClient.LastName}, {draft.NewClient.FirstName} ({draft.NewClient.Email})";
            }

            return vm;
        }

        // =========================================================
        // NORMALIZATION / VALIDATION - REFERRAL ENTRY FLOW
        // PLACEHOLDERS FOR NEXT PHASE
        // =========================================================

        private void NormalizeOrganizationEntry(OrganizationEntryViewModel vm)
        {
            vm.NewOrganization ??= new ReferringOrganizationEntryInput();

            vm.NewOrganization.Name = NullIfWhiteSpace(vm.NewOrganization.Name);
            vm.NewOrganization.Type = NullIfWhiteSpace(vm.NewOrganization.Type);
            vm.NewOrganization.PrimaryContactName = NullIfWhiteSpace(vm.NewOrganization.PrimaryContactName);
            vm.NewOrganization.Email = NullIfWhiteSpace(vm.NewOrganization.Email);
            vm.NewOrganization.PhoneNumber = NullIfWhiteSpace(vm.NewOrganization.PhoneNumber);
            vm.NewOrganization.AddressLine1 = NullIfWhiteSpace(vm.NewOrganization.AddressLine1);
            vm.NewOrganization.AddressLine2 = NullIfWhiteSpace(vm.NewOrganization.AddressLine2);
            vm.NewOrganization.City = NullIfWhiteSpace(vm.NewOrganization.City);
            vm.NewOrganization.State = NullIfWhiteSpace(vm.NewOrganization.State)?.ToUpperInvariant();
            vm.NewOrganization.PostalCode = NullIfWhiteSpace(vm.NewOrganization.PostalCode);
            vm.NewOrganization.Notes = NullIfWhiteSpace(vm.NewOrganization.Notes);
        }

        private async Task ApplyOrganizationEntryValidationAsync(OrganizationEntryViewModel vm)
        {
            bool selectedExisting = string.Equals(vm.OrganizationMode, "existing", StringComparison.OrdinalIgnoreCase);
            bool enteredNew = string.Equals(vm.OrganizationMode, "new", StringComparison.OrdinalIgnoreCase);

            if (!selectedExisting && !enteredNew)
            {
                ModelState.AddModelError(string.Empty, "Select Existing Organization or New Organization.");
                return;
            }

            if (selectedExisting)
            {
                var exists = await _context.ReferringOrganizations.AnyAsync(o =>
                    o.Id == vm.SelectedReferringOrganizationId &&
                    o.DeletedAt == null &&
                    o.IsActive);

                if (!exists)
                {
                    ModelState.AddModelError(nameof(vm.SelectedReferringOrganizationId), "Select a valid active referring organization.");
                }

                return;
            }

            // New organization validation
            if (string.IsNullOrWhiteSpace(vm.NewOrganization.Name))
            {
                ModelState.AddModelError("NewOrganization.Name", "Organization name is required.");
            }
            else
            {
                if (!ContainsLetterOrDigit(vm.NewOrganization.Name))
                {
                    ModelState.AddModelError("NewOrganization.Name", "Organization name must contain letters or numbers.");
                }

                var normalizedName = vm.NewOrganization.Name.ToLower();

                var duplicateExists = await _context.ReferringOrganizations.AnyAsync(o =>
                    o.DeletedAt == null &&
                    o.Name.ToLower() == normalizedName);

                if (duplicateExists)
                {
                    ModelState.AddModelError("NewOrganization.Name", "An organization with this name already exists.");
                }
            }

            if (!string.IsNullOrWhiteSpace(vm.NewOrganization.Type) &&
                !ContainsLetterOrDigit(vm.NewOrganization.Type))
            {
                ModelState.AddModelError("NewOrganization.Type", "Primary type of service must contain letters or numbers.");
            }

            if (!string.IsNullOrWhiteSpace(vm.NewOrganization.PrimaryContactName) &&
                !IsValidPersonName(vm.NewOrganization.PrimaryContactName))
            {
                ModelState.AddModelError("NewOrganization.PrimaryContactName", "Contact person name contains invalid characters.");
            }

            if (!string.IsNullOrWhiteSpace(vm.NewOrganization.PhoneNumber) &&
                !IsValidPhoneNumber(vm.NewOrganization.PhoneNumber))
            {
                ModelState.AddModelError("NewOrganization.PhoneNumber", "Enter a valid US phone number with 10 digits, or 11 digits starting with 1.");
            }

            if (!string.IsNullOrWhiteSpace(vm.NewOrganization.Email) &&
                !IsValidEmail(vm.NewOrganization.Email))
            {
                ModelState.AddModelError("NewOrganization.Email", "Email format is invalid.");
            }

            if (!string.IsNullOrWhiteSpace(vm.NewOrganization.AddressLine1) &&
                !ContainsLetterOrDigit(vm.NewOrganization.AddressLine1))
            {
                ModelState.AddModelError("NewOrganization.AddressLine1", "Address Line 1 must contain letters or numbers.");
            }

            if (!string.IsNullOrWhiteSpace(vm.NewOrganization.AddressLine2) &&
                !ContainsLetterOrDigit(vm.NewOrganization.AddressLine2))
            {
                ModelState.AddModelError("NewOrganization.AddressLine2", "Address Line 2 must contain letters or numbers.");
            }

            if (!string.IsNullOrWhiteSpace(vm.NewOrganization.City) &&
                !IsValidCity(vm.NewOrganization.City))
            {
                ModelState.AddModelError("NewOrganization.City", "City contains invalid characters.");
            }

            if (!string.IsNullOrWhiteSpace(vm.NewOrganization.State) &&
                !IsValidUsStateCode(vm.NewOrganization.State))
            {
                ModelState.AddModelError("NewOrganization.State", "Enter a valid 2-letter US state code.");
            }

            if (!string.IsNullOrWhiteSpace(vm.NewOrganization.PostalCode) &&
                !IsValidUsPostalCode(vm.NewOrganization.PostalCode))
            {
                ModelState.AddModelError("NewOrganization.PostalCode", "Enter a valid US ZIP code or ZIP+4.");
            }

            if (!string.IsNullOrWhiteSpace(vm.NewOrganization.Notes) &&
                vm.NewOrganization.Notes.Length > 2000)
            {
                ModelState.AddModelError("NewOrganization.Notes", "Notes cannot exceed 2000 characters.");
            }
        }

        private void NormalizeClientEntry(ClientEntryViewModel vm)
        {
            vm.NewClient ??= new ClientEntryInput();

            vm.NewClient.FirstName = NullIfWhiteSpace(vm.NewClient.FirstName);
            vm.NewClient.LastName = NullIfWhiteSpace(vm.NewClient.LastName);
            vm.NewClient.Email = NullIfWhiteSpace(vm.NewClient.Email);
            vm.NewClient.PhoneNumber = NullIfWhiteSpace(vm.NewClient.PhoneNumber);
            vm.NewClient.AddressLine1 = NullIfWhiteSpace(vm.NewClient.AddressLine1);
            vm.NewClient.AddressLine2 = NullIfWhiteSpace(vm.NewClient.AddressLine2);
            vm.NewClient.City = NullIfWhiteSpace(vm.NewClient.City);
            vm.NewClient.State = NullIfWhiteSpace(vm.NewClient.State)?.ToUpperInvariant();
            vm.NewClient.PostalCode = NullIfWhiteSpace(vm.NewClient.PostalCode);
            vm.NewClient.EmploymentStatus = NullIfWhiteSpace(vm.NewClient.EmploymentStatus);
        }

        private async Task ApplyClientEntryValidationAsync(ClientEntryViewModel vm)
        {
            bool usingExisting = string.Equals(vm.ClientMode, "existing", StringComparison.OrdinalIgnoreCase);
            bool usingNew = string.Equals(vm.ClientMode, "new", StringComparison.OrdinalIgnoreCase);

            if (!usingExisting && !usingNew)
            {
                ModelState.AddModelError(string.Empty, "Select Existing Client or New Client.");
                return;
            }

            if (usingExisting)
            {
                if (!vm.SelectedClientUserId.HasValue)
                {
                    ModelState.AddModelError(nameof(vm.SelectedClientUserId), "Select a client.");
                    return;
                }

                var exists = await _context.DomainUsers.AnyAsync(u =>
                    u.Id == vm.SelectedClientUserId &&
                    u.DeletedAt == null &&
                    u.UserType == UserType.Client &&
                    u.IsActive);

                if (!exists)
                {
                    ModelState.AddModelError(nameof(vm.SelectedClientUserId), "Select a valid active client.");
                }

                return;
            }

            // New client validation
            if (string.IsNullOrWhiteSpace(vm.NewClient.FirstName))
            {
                ModelState.AddModelError("NewClient.FirstName", "First name is required.");
            }
            else if (!IsValidPersonName(vm.NewClient.FirstName))
            {
                ModelState.AddModelError("NewClient.FirstName", "First name contains invalid characters.");
            }

            if (string.IsNullOrWhiteSpace(vm.NewClient.LastName))
            {
                ModelState.AddModelError("NewClient.LastName", "Last name is required.");
            }
            else if (!IsValidPersonName(vm.NewClient.LastName))
            {
                ModelState.AddModelError("NewClient.LastName", "Last name contains invalid characters.");
            }

            if (string.IsNullOrWhiteSpace(vm.NewClient.Email))
            {
                ModelState.AddModelError("NewClient.Email", "Email is required.");
            }
            else if (!IsValidEmail(vm.NewClient.Email))
            {
                ModelState.AddModelError("NewClient.Email", "Email format is invalid.");
            }
            else
            {
                var duplicateExists = await _context.DomainUsers.AnyAsync(u =>
                    u.DeletedAt == null &&
                    u.UserType == UserType.Client &&
                    u.Email.ToLower() == vm.NewClient.Email.ToLower());

                if (duplicateExists)
                {
                    ModelState.AddModelError("NewClient.Email", "A client with this email address already exists.");
                }
            }

            if (!string.IsNullOrWhiteSpace(vm.NewClient.PhoneNumber) &&
                !IsValidPhoneNumber(vm.NewClient.PhoneNumber))
            {
                ModelState.AddModelError("NewClient.PhoneNumber", "Enter a valid US phone number with 10 digits, or 11 digits starting with 1.");
            }

            if (!string.IsNullOrWhiteSpace(vm.NewClient.AddressLine1) &&
                !ContainsLetterOrDigit(vm.NewClient.AddressLine1))
            {
                ModelState.AddModelError("NewClient.AddressLine1", "Address Line 1 must contain letters or numbers.");
            }

            if (!string.IsNullOrWhiteSpace(vm.NewClient.AddressLine2) &&
                !ContainsLetterOrDigit(vm.NewClient.AddressLine2))
            {
                ModelState.AddModelError("NewClient.AddressLine2", "Address Line 2 must contain letters or numbers.");
            }

            if (!string.IsNullOrWhiteSpace(vm.NewClient.City) &&
                !IsValidCity(vm.NewClient.City))
            {
                ModelState.AddModelError("NewClient.City", "City contains invalid characters.");
            }

            if (!string.IsNullOrWhiteSpace(vm.NewClient.State) &&
                !IsValidUsStateCode(vm.NewClient.State))
            {
                ModelState.AddModelError("NewClient.State", "Enter a valid 2-letter US state code.");
            }

            if (!string.IsNullOrWhiteSpace(vm.NewClient.PostalCode) &&
                !IsValidUsPostalCode(vm.NewClient.PostalCode))
            {
                ModelState.AddModelError("NewClient.PostalCode", "Enter a valid US ZIP code or ZIP+4.");
            }

            if (vm.NewClient.DateOfBirth.HasValue &&
                vm.NewClient.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.UtcNow))
            {
                ModelState.AddModelError("NewClient.DateOfBirth", "Date of Birth cannot be in the future.");
            }

            if (!string.IsNullOrWhiteSpace(vm.NewClient.EmploymentStatus) &&
                !Regex.IsMatch(vm.NewClient.EmploymentStatus, @"^[A-Za-z0-9\s'.-]*$"))
            {
                ModelState.AddModelError("NewClient.EmploymentStatus", "Employment status contains invalid characters.");
            }

            if (vm.NewClient.EarnedIncomeMonthly.HasValue &&
                vm.NewClient.EarnedIncomeMonthly.Value < 0)
            {
                ModelState.AddModelError("NewClient.EarnedIncomeMonthly", "Monthly earned income must be 0 or greater.");
            }
        }

        private void NormalizeHouseholdEntry(HouseholdEntryViewModel vm)
        {
            vm.HouseholdMembers ??= new List<HouseholdMemberEntryInput>();

            foreach (var member in vm.HouseholdMembers)
            {
                member.FirstName = NullIfWhiteSpace(member.FirstName);
                member.LastName = NullIfWhiteSpace(member.LastName);
            }
        }

        private void ApplyHouseholdEntryValidation(HouseholdEntryViewModel vm)
        {
            if (vm.HouseholdMembers == null)
            {
                return;
            }

            for (int i = 0; i < vm.HouseholdMembers.Count; i++)
            {
                var member = vm.HouseholdMembers[i];

                if (!member.HasStarted)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(member.FirstName))
                {
                    ModelState.AddModelError($"HouseholdMembers[{i}].FirstName", "First name is required.");
                }
                else if (!IsValidPersonName(member.FirstName))
                {
                    ModelState.AddModelError($"HouseholdMembers[{i}].FirstName", "First name contains invalid characters.");
                }

                if (string.IsNullOrWhiteSpace(member.LastName))
                {
                    ModelState.AddModelError($"HouseholdMembers[{i}].LastName", "Last name is required.");
                }
                else if (!IsValidPersonName(member.LastName))
                {
                    ModelState.AddModelError($"HouseholdMembers[{i}].LastName", "Last name contains invalid characters.");
                }

                if (member.DateOfBirth.HasValue &&
                    member.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
                {
                    ModelState.AddModelError($"HouseholdMembers[{i}].DateOfBirth", "Date of Birth cannot be in the future.");
                }

                if (member.AgeAsOfDate.HasValue &&
                    member.AgeAsOfDate.Value.Date > DateTime.UtcNow.Date)
                {
                    ModelState.AddModelError($"HouseholdMembers[{i}].AgeAsOfDate", "Age As Of Date cannot be in the future.");
                }

                if (member.DateOfBirth.HasValue &&
                    member.AgeAsOfDate.HasValue &&
                    member.AgeAsOfDate.Value.Date < member.DateOfBirth.Value.Date)
                {
                    ModelState.AddModelError($"HouseholdMembers[{i}].AgeAsOfDate", "Age As Of Date cannot be earlier than Date of Birth.");
                }
            }
        }

        private void NormalizeReferralDetails(ReferralDetailsViewModel vm)
        {
            vm.Referral ??= new ReferralDetailsInput();

            vm.Referral.ReferredByName = NullIfWhiteSpace(vm.Referral.ReferredByName);
            vm.Referral.ReferredByPhoneNumber = NullIfWhiteSpace(vm.Referral.ReferredByPhoneNumber);
            vm.Referral.ReferredByEmail = NullIfWhiteSpace(vm.Referral.ReferredByEmail);
            vm.Referral.Notes = NullIfWhiteSpace(vm.Referral.Notes);
        }

        private async Task ApplyReferralDetailsValidationAsync(ReferralDetailsViewModel vm)
        {
            await Task.CompletedTask;

            if (!vm.Referral.ReferredOn.HasValue)
            {
                ModelState.AddModelError("Referral.ReferredOn", "Referral date is required.");
            }
            else
            {
                if (vm.Referral.ReferredOn.Value.Date > DateTime.UtcNow.Date)
                {
                    ModelState.AddModelError("Referral.ReferredOn", "Referral date cannot be in the future.");
                }

                if (vm.Referral.ValidFrom.HasValue &&
                    vm.Referral.ValidFrom.Value.Date < vm.Referral.ReferredOn.Value.Date)
                {
                    ModelState.AddModelError("Referral.ValidFrom", "Valid From cannot be earlier than Referral Date.");
                }

                if (vm.Referral.ValidTo.HasValue &&
                    vm.Referral.ValidTo.Value.Date < vm.Referral.ReferredOn.Value.Date)
                {
                    ModelState.AddModelError("Referral.ValidTo", "Valid To cannot be earlier than Referral Date.");
                }
            }

            if (!vm.Referral.Status.HasValue ||
                !Enum.IsDefined(typeof(ReferralStatus), vm.Referral.Status.Value))
            {
                ModelState.AddModelError("Referral.Status", "Select a valid referral status.");
            }

            if (vm.Referral.ValidFrom.HasValue &&
                vm.Referral.ValidTo.HasValue &&
                vm.Referral.ValidFrom.Value.Date > vm.Referral.ValidTo.Value.Date)
            {
                ModelState.AddModelError("Referral.ValidTo", "Valid To must be on or after Valid From.");
            }

            if (!string.IsNullOrWhiteSpace(vm.Referral.ReferredByName) &&
                !IsValidPersonName(vm.Referral.ReferredByName))
            {
                ModelState.AddModelError("Referral.ReferredByName", "Referrer name contains invalid characters.");
            }

            if (!string.IsNullOrWhiteSpace(vm.Referral.ReferredByPhoneNumber) &&
                !IsValidPhoneNumber(vm.Referral.ReferredByPhoneNumber))
            {
                ModelState.AddModelError("Referral.ReferredByPhoneNumber", "Enter a valid US phone number with 10 digits, or 11 digits starting with 1.");
            }

            if (!string.IsNullOrWhiteSpace(vm.Referral.ReferredByEmail) &&
                !IsValidEmail(vm.Referral.ReferredByEmail))
            {
                ModelState.AddModelError("Referral.ReferredByEmail", "Email format is invalid.");
            }

            if (!string.IsNullOrWhiteSpace(vm.Referral.Notes) &&
                vm.Referral.Notes.Length > 2000)
            {
                ModelState.AddModelError("Referral.Notes", "Notes cannot exceed 2000 characters.");
            }
        }

        private async Task ApplyReferralEntryDraftValidationAsync(ReferralEntryDraft draft)
        {
            // -------------------------------------------------
            // ORGANIZATION
            // -------------------------------------------------
            bool hasExistingOrganization = draft.HasExistingOrganization;
            bool hasNewOrganization = draft.HasNewOrganization;

            if (!hasExistingOrganization && !hasNewOrganization)
            {
                ModelState.AddModelError(string.Empty, "Referral Entry is missing organization information.");
            }
            else if (hasExistingOrganization && hasNewOrganization)
            {
                ModelState.AddModelError(string.Empty, "Referral Entry contains both an existing organization and a new organization draft.");
            }
            else if (hasExistingOrganization)
            {
                var orgExists = await _context.ReferringOrganizations.AnyAsync(o =>
                    o.Id == draft.ExistingReferringOrganizationId &&
                    o.DeletedAt == null &&
                    o.IsActive);

                if (!orgExists)
                {
                    ModelState.AddModelError(string.Empty, "The selected referring organization is no longer valid.");
                }
            }
            else
            {
                var orgVm = new OrganizationEntryViewModel
                {
                    SelectedReferringOrganizationId = draft.ExistingReferringOrganizationId,
                    NewOrganization = draft.NewOrganization ?? new ReferringOrganizationEntryInput(),
                    OrganizationMode = draft.HasExistingOrganization ? "existing"
                        : draft.HasNewOrganization ? "new"
                        : null
                };

                NormalizeOrganizationEntry(orgVm);
                await ApplyOrganizationEntryValidationAsync(orgVm);
            }

            // -------------------------------------------------
            // CLIENT
            // -------------------------------------------------
            bool hasExistingClient = draft.HasExistingClient;
            bool hasNewClient = draft.HasNewClient;

            if (!hasExistingClient && !hasNewClient)
            {
                ModelState.AddModelError(string.Empty, "Referral Entry is missing client information.");
            }
            else if (hasExistingClient && hasNewClient)
            {
                ModelState.AddModelError(string.Empty, "Referral Entry contains both an existing client and a new client draft.");
            }
            else if (hasExistingClient)
            {
                var clientExists = await _context.DomainUsers.AnyAsync(u =>
                    u.Id == draft.ExistingClientUserId &&
                    u.DeletedAt == null &&
                    u.UserType == UserType.Client &&
                    u.IsActive);

                if (!clientExists)
                {
                    ModelState.AddModelError(string.Empty, "The selected client is no longer valid.");
                }

                if (draft.HouseholdMembers.Any(h => h.HasStarted))
                {
                    ModelState.AddModelError(string.Empty, "Household members should only be entered when creating a new client.");
                }
            }
            else
            {
                var clientVm = new ClientEntryViewModel
                {
                    SelectedClientUserId = draft.ExistingClientUserId,
                    NewClient = draft.NewClient ?? new ClientEntryInput(),
                    ClientMode = draft.HasExistingClient ? "existing"
                        : draft.HasNewClient ? "new"
                        : null
                };

                NormalizeClientEntry(clientVm);
                await ApplyClientEntryValidationAsync(clientVm);

                var householdVm = new HouseholdEntryViewModel
                {
                    HouseholdMembers = draft.HouseholdMembers ?? new List<HouseholdMemberEntryInput>()
                };

                NormalizeHouseholdEntry(householdVm);
                ApplyHouseholdEntryValidation(householdVm);
            }

            // -------------------------------------------------
            // REFERRAL DETAILS
            // -------------------------------------------------
            if (draft.Referral == null)
            {
                ModelState.AddModelError(string.Empty, "Referral Entry is missing referral details.");
            }
            else
            {
                var referralVm = new ReferralDetailsViewModel
                {
                    Referral = draft.Referral
                };

                NormalizeReferralDetails(referralVm);
                await ApplyReferralDetailsValidationAsync(referralVm);
            }
        }

        // =========================================================
        // EXISTING HELPERS
        // =========================================================

        private async Task<bool> ReferralExists(ulong id)
        {
            return await _context.Referrals.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        private static void NormalizeReferral(Referral model)
        {
            model.ReferredByName = NullIfWhiteSpace(model.ReferredByName);
            model.ReferredByPhoneNumber = NullIfWhiteSpace(model.ReferredByPhoneNumber);
            model.ReferredByEmail = NullIfWhiteSpace(model.ReferredByEmail);
            model.Notes = NullIfWhiteSpace(model.Notes);
        }

        private async Task ApplyReferralValidationAsync(Referral model, ulong? currentId = null)
        {
            var clientExists = await _context.DomainUsers
                .AnyAsync(u =>
                    u.Id == model.ClientUserId &&
                    u.DeletedAt == null &&
                    u.UserType == UserType.Client);

            if (!clientExists)
            {
                ModelState.AddModelError(nameof(Referral.ClientUserId), "Select a valid client.");
            }

            var organizationExists = await _context.ReferringOrganizations
                .AnyAsync(o =>
                    o.Id == model.ReferringOrganizationId &&
                    o.DeletedAt == null &&
                    o.IsActive);

            if (!organizationExists)
            {
                ModelState.AddModelError(nameof(Referral.ReferringOrganizationId), "Select a valid active referring organization.");
            }

            if (!Enum.IsDefined(typeof(ReferralStatus), model.Status))
            {
                ModelState.AddModelError(nameof(Referral.Status), "Select a valid referral status.");
            }

            if (model.ReferredOn.Date > DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(nameof(Referral.ReferredOn), "Referral date cannot be in the future.");
            }

            if (model.ValidFrom.HasValue && model.ValidTo.HasValue && model.ValidFrom.Value > model.ValidTo.Value)
            {
                ModelState.AddModelError(nameof(Referral.ValidTo), "Valid To must be on or after Valid From.");
            }

            if (model.ValidFrom.HasValue && model.ValidFrom.Value < model.ReferredOn)
            {
                ModelState.AddModelError(nameof(Referral.ValidFrom), "Valid From cannot be earlier than Referred On.");
            }

            if (model.ValidTo.HasValue && model.ValidTo.Value < model.ReferredOn)
            {
                ModelState.AddModelError(nameof(Referral.ValidTo), "Valid To cannot be earlier than Referred On.");
            }

            if (!string.IsNullOrWhiteSpace(model.ReferredByName) && !IsValidPersonName(model.ReferredByName))
            {
                ModelState.AddModelError(nameof(Referral.ReferredByName), "Referred By Name contains invalid characters.");
            }

            if (!string.IsNullOrWhiteSpace(model.ReferredByPhoneNumber) && !IsValidPhoneNumber(model.ReferredByPhoneNumber))
            {
                ModelState.AddModelError(nameof(Referral.ReferredByPhoneNumber), "Enter a valid US phone number with 10 digits, or 11 digits starting with 1.");
            }

            if (!string.IsNullOrWhiteSpace(model.ReferredByEmail) && !IsValidEmail(model.ReferredByEmail))
            {
                ModelState.AddModelError(nameof(Referral.ReferredByEmail), "Email format is invalid.");
            }

            if (!string.IsNullOrWhiteSpace(model.Notes) && model.Notes.Length > 2000)
            {
                ModelState.AddModelError(nameof(Referral.Notes), "Notes cannot exceed 2000 characters.");
            }
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (!Regex.IsMatch(phoneNumber, @"^\+?[0-9()\-\s]+$"))
            {
                return false;
            }

            var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

            if (digitsOnly.Length == 10)
            {
                return true;
            }

            if (digitsOnly.Length == 11 && digitsOnly.StartsWith("1"))
            {
                return true;
            }

            return false;
        }

        private static bool ContainsLetterOrDigit(string value)
        {
            return value.Any(char.IsLetterOrDigit);
        }

        private static bool IsValidCity(string city)
        {
            return Regex.IsMatch(city, @"^[A-Za-z][A-Za-z\s'.-]*$");
        }

        private static bool IsValidUsStateCode(string state)
        {
            return state.Length == 2 && ValidUsStateCodes.Contains(state);
        }

        private static bool IsValidUsPostalCode(string postalCode)
        {
            return Regex.IsMatch(postalCode, @"^\d{5}(-\d{4})?$");
        }

        private static bool IsValidEmail(string email)
        {
            if (email.Contains(' '))
            {
                return false;
            }

            if (email.Count(c => c == '@') != 1)
            {
                return false;
            }

            if (email.Contains(".."))
            {
                return false;
            }

            var parts = email.Split('@');
            if (parts.Length != 2)
            {
                return false;
            }

            var localPart = parts[0];
            var domainPart = parts[1];

            if (string.IsNullOrWhiteSpace(localPart) || string.IsNullOrWhiteSpace(domainPart))
            {
                return false;
            }

            if (localPart.StartsWith('.') || localPart.EndsWith('.'))
            {
                return false;
            }

            if (domainPart.StartsWith('.') || domainPart.EndsWith('.'))
            {
                return false;
            }

            if (!domainPart.Contains('.'))
            {
                return false;
            }

            var domainLabels = domainPart.Split('.');
            if (domainLabels.Any(label => string.IsNullOrWhiteSpace(label)))
            {
                return false;
            }

            return Regex.IsMatch(localPart, @"^[A-Za-z0-9._+\-]+$")
                && Regex.IsMatch(domainPart, @"^[A-Za-z0-9.\-]+$");
        }

        private static bool IsValidPersonName(string name)
        {
            return Regex.IsMatch(name, @"^[A-Za-z][A-Za-z\s'.-]*$");
        }
    }
}