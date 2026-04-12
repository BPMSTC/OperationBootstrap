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
        /// <summary>
        /// Displays all non-deleted referrals.
        /// </summary>
        public async Task<IActionResult> Index(string? searchTerm)
        {
            try
            {
                _logger.LogInformation("Loading Referrals Index page");

                // Build the base query for active referrals.
                IQueryable<Referral> query = _context.Referrals
                    .Where(r => r.DeletedAt == null)
                    .Include(r => r.ClientUser)
                    .Include(r => r.ReferringOrganization);

                // Apply the search filter when one is provided.
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim();

                    query = query.Where(r =>
                        // Client (name + email)
                        (r.ClientUser != null && (
                            (r.ClientUser.FirstName + " " + r.ClientUser.LastName).Contains(searchTerm) ||
                            (r.ClientUser.LastName + ", " + r.ClientUser.FirstName).Contains(searchTerm) ||
                            (r.ClientUser.Email != null && r.ClientUser.Email.Contains(searchTerm))
                        )) ||

                        // Referring Organization
                        (r.ReferringOrganization != null &&
                            r.ReferringOrganization.Name.Contains(searchTerm)) ||

                        // Status
                        r.Status.ToString().Contains(searchTerm)
                    );
                }

                // Retrieve the ordered referrals for display.
                var referrals = await query
                    .OrderByDescending(r => r.ReferredOn)
                    .ThenBy(r => r.Id)
                    .ToListAsync();

                ViewData["CurrentFilter"] = searchTerm;

                _logger.LogInformation("Loaded {Count} referrals", referrals.Count);

                return View(referrals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load referrals");
                return View("Error");
            }
        }

        // GET: Referrals/Details/5
        /// <summary>
        /// Displays details for a single non-deleted referral.
        /// </summary>
        public async Task<IActionResult> Details(ulong? id)
        {
            try
            {
                // Reject requests with no id.
                if (id == null)
                {
                    _logger.LogWarning("Details requested with null Id");
                    return NotFound();
                }

                _logger.LogInformation("Fetching details for Referral Id {Id}", id);

                // Retrieve the requested active referral with related client and organization data.
                var referral = await _context.Referrals
                    .Where(r => r.DeletedAt == null)
                    .Include(r => r.ClientUser)
                    .Include(r => r.ReferringOrganization)
                    .FirstOrDefaultAsync(m => m.Id == id);

                // Return not found when the referral does not exist.
                if (referral == null)
                {
                    _logger.LogWarning("Referral Id {Id} not found", id);
                    return NotFound();
                }

                return View(referral);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for Referral Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading referral details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Referrals/Create
        /// <summary>
        /// Shows the create form.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            try
            {
                _logger.LogInformation("Loading Create Referral page");

                // Populate dropdown values for the create form.
                await PopulateDropdowns();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Create Referral page");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the create form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Referrals/Create
        /// <summary>
        /// Creates a new referral after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientUserId,ReferringOrganizationId,ReferredOn,Status,ValidFrom,ValidTo,Notes")] Referral referral)
        {
            try
            {
                _logger.LogInformation("Attempting to create Referral for ClientUserId {ClientUserId}", referral.ClientUserId);

                // Remove navigation properties that are not posted by the form.
                ModelState.Remove(nameof(Referral.ClientUser));
                ModelState.Remove(nameof(Referral.ReferringOrganization));
                ModelState.Remove(nameof(Referral.CreatedByUser));
                ModelState.Remove(nameof(Referral.UpdatedByUser));

                // Normalize incoming values before business-rule validation.
                NormalizeReferral(referral);
                await ApplyReferralValidationAsync(referral);

                // Return the form with dropdowns restored when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create Referral failed validation for ClientUserId {ClientUserId}", referral.ClientUserId);
                    await PopulateDropdowns(referral.ClientUserId, referral.ReferringOrganizationId);
                    return View(referral);
                }

                try
                {
                    // Build the referral input for the referral service.
                    var referralInput = new ReferralDetailsInput
                    {
                        ReferredOn = referral.ReferredOn,
                        Status = referral.Status,
                        ValidFrom = referral.ValidFrom,
                        ValidTo = referral.ValidTo,
                        Notes = referral.Notes
                    };

                    // Create the referral and capture the new id.
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating Referral for ClientUserId {ClientUserId}", referral?.ClientUserId);
                ModelState.AddModelError("", "An unexpected error occurred while creating the referral.");

                await PopulateDropdowns(referral?.ClientUserId, referral?.ReferringOrganizationId);
                return View(referral ?? new Referral());
            }
        }

        // =========================================================
        // REFERRAL ENTRY FLOW
        // =========================================================

        // GET: Referrals/StartReferralEntry
        /// <summary>
        /// Starts a new Referral Entry flow and initializes a fresh draft.
        /// </summary>
        public IActionResult StartReferralEntry()
        {
            try
            {
                _logger.LogInformation("Starting Referral Entry flow");

                // Clear any existing referral entry draft.
                ClearReferralEntryDraft();

                // Create a fresh draft for the flow.
                var draft = new ReferralEntryDraft();

                if (!draft.HouseholdMembers.Any())
                {
                    draft.HouseholdMembers.Add(new HouseholdMemberEntryInput());
                }

                // Persist the new draft to session.
                SaveReferralEntryDraft(draft);

                return RedirectToAction(nameof(OrganizationEntry));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting Referral Entry flow");
                TempData["ErrorMessage"] = "An unexpected error occurred while starting the referral entry flow.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Referrals/StartReferralEntryForClient
        /// <summary>
        /// Starts a new Referral Entry flow for an existing client.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> StartReferralEntryForClient(ulong id)
        {
            try
            {
                _logger.LogInformation("Starting Referral Entry flow for existing ClientUserId {ClientUserId}", id);

                // Validate that the selected client exists and is active.
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

                // Clear any existing referral entry draft.
                ClearReferralEntryDraft();

                // Create a fresh draft preloaded with the selected client.
                var draft = new ReferralEntryDraft
                {
                    ExistingClientUserId = id
                };

                if (!draft.HouseholdMembers.Any())
                {
                    draft.HouseholdMembers.Add(new HouseholdMemberEntryInput());
                }

                // Persist the new draft to session.
                SaveReferralEntryDraft(draft);

                return RedirectToAction(nameof(OrganizationEntry));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting Referral Entry flow for ClientUserId {ClientUserId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while starting the referral entry flow.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Referrals/OrganizationEntry
        /// <summary>
        /// Shows the organization step of the Referral Entry flow.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> OrganizationEntry()
        {
            try
            {
                _logger.LogInformation("Loading Organization Entry page");

                // Load the current referral entry draft.
                var draft = LoadReferralEntryDraft() ?? new ReferralEntryDraft();

                // Build the view model from the draft.
                var vm = new OrganizationEntryViewModel
                {
                    SelectedReferringOrganizationId = draft.ExistingReferringOrganizationId,
                    NewOrganization = draft.NewOrganization ?? new ReferringOrganizationEntryInput(),
                    OrganizationMode = draft.HasExistingOrganization ? "existing"
                        : draft.HasNewOrganization ? "new"
                        : null
                };

                // Populate dropdown values for the step.
                await PopulateOrganizationEntryDropdowns(vm);

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Organization Entry page");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the organization step.";
                return RedirectToAction(nameof(StartReferralEntry));
            }
        }

        // POST: Referrals/OrganizationEntry
        /// <summary>
        /// Saves the organization step of the Referral Entry flow.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrganizationEntry(OrganizationEntryViewModel vm)
        {
            try
            {
                _logger.LogInformation("Submitting Organization Entry page");

                // Normalize incoming values before business-rule validation.
                NormalizeOrganizationEntry(vm);
                await ApplyOrganizationEntryValidationAsync(vm);

                // Return the form when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Organization Entry failed validation");
                    await PopulateOrganizationEntryDropdowns(vm);
                    return View(vm);
                }

                // Load the current referral entry draft.
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

                if (draft.HasExistingClient)
                {
                    draft.NewClient = null;
                    draft.HouseholdMembers = new List<HouseholdMemberEntryInput>();
                }

                // Persist the updated draft to session.
                SaveReferralEntryDraft(draft);

                if (draft.HasExistingClient)
                {
                    return RedirectToAction(nameof(ReferralDetails));
                }

                return RedirectToAction(nameof(ClientEntry));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting Organization Entry page");
                TempData["ErrorMessage"] = "An unexpected error occurred while saving the organization step.";
                await PopulateOrganizationEntryDropdowns(vm);
                return View(vm);
            }
        }

        // GET: Referrals/ClientEntry
        /// <summary>
        /// Shows the client step of the Referral Entry flow.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ClientEntry()
        {
            try
            {
                _logger.LogInformation("Loading Client Entry page");

                // Load the current referral entry draft.
                var draft = LoadReferralEntryDraft();

                if (draft == null)
                {
                    _logger.LogWarning("Client Entry requested without Referral Entry draft");
                    TempData["ErrorMessage"] = "Your referral entry draft was not found. Please start again.";
                    return RedirectToAction(nameof(StartReferralEntry));
                }

                draft.NewClient ??= new ClientEntryInput();
                draft.NewClient.Incomes ??= new List<ClientIncomeEntryInput>();

                if (!draft.NewClient.Incomes.Any())
                {
                    draft.NewClient.Incomes.Add(new ClientIncomeEntryInput());
                }

                // Build the view model from the draft.
                var vm = new ClientEntryViewModel
                {
                    SelectedClientUserId = draft.ExistingClientUserId,
                    NewClient = draft.NewClient,
                    ClientMode = draft.HasExistingClient ? "existing"
                        : draft.HasNewClient ? "new"
                        : null
                };

                // Populate dropdown values for the step.
                await PopulateClientEntryDropdowns(vm);

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Client Entry page");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the client step.";
                return RedirectToAction(nameof(StartReferralEntry));
            }
        }

        // POST: Referrals/ClientEntry
        /// <summary>
        /// Saves the client step of the Referral Entry flow.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClientEntry(ClientEntryViewModel vm)
        {
            try
            {
                _logger.LogInformation("Submitting Client Entry page");

                // Normalize incoming values before business-rule validation.
                NormalizeClientEntry(vm);
                await ApplyClientEntryValidationAsync(vm);

                // Return the form when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Client Entry failed validation");

                    vm.NewClient ??= new ClientEntryInput();
                    vm.NewClient.Incomes ??= new List<ClientIncomeEntryInput>();

                    if (!vm.NewClient.Incomes.Any())
                    {
                        vm.NewClient.Incomes.Add(new ClientIncomeEntryInput());
                    }

                    await PopulateClientEntryDropdowns(vm);
                    return View(vm);
                }

                // Load the current referral entry draft.
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

                // Persist the updated draft to session.
                SaveReferralEntryDraft(draft);

                if (draft.HasNewClient)
                {
                    return RedirectToAction(nameof(HouseholdEntry));
                }

                return RedirectToAction(nameof(ReferralDetails));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting Client Entry page");
                TempData["ErrorMessage"] = "An unexpected error occurred while saving the client step.";

                vm.NewClient ??= new ClientEntryInput();
                vm.NewClient.Incomes ??= new List<ClientIncomeEntryInput>();

                if (!vm.NewClient.Incomes.Any())
                {
                    vm.NewClient.Incomes.Add(new ClientIncomeEntryInput());
                }

                await PopulateClientEntryDropdowns(vm);
                return View(vm);
            }
        }

        // GET: Referrals/HouseholdEntry
        /// <summary>
        /// Shows the household step of the Referral Entry flow.
        /// </summary>
        [HttpGet]
        public IActionResult HouseholdEntry()
        {
            try
            {
                _logger.LogInformation("Loading Household Entry page");

                // Load the current referral entry draft.
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

                // Build the view model from the draft.
                var vm = new HouseholdEntryViewModel
                {
                    HouseholdMembers = draft.HouseholdMembers.Any()
                        ? draft.HouseholdMembers
                        : new List<HouseholdMemberEntryInput> { new HouseholdMemberEntryInput() },
                    HasHouseholdMembers = draft.HouseholdMembers.Any(h => h.HasStarted)
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Household Entry page");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the household step.";
                return RedirectToAction(nameof(StartReferralEntry));
            }
        }

        // POST: Referrals/HouseholdEntry
        /// <summary>
        /// Saves the household step of the Referral Entry flow.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HouseholdEntry(HouseholdEntryViewModel vm)
        {
            try
            {
                _logger.LogInformation("Submitting Household Entry page");

                vm.HouseholdMembers ??= new List<HouseholdMemberEntryInput>();

                if (!vm.HasHouseholdMembers)
                {
                    vm.HouseholdMembers = new List<HouseholdMemberEntryInput>();
                }
                else
                {
                    // Normalize incoming values before business-rule validation.
                    NormalizeHouseholdEntry(vm);
                    ApplyHouseholdEntryValidation(vm);
                }

                // Return the form when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Household Entry failed validation");

                    if (!vm.HouseholdMembers.Any())
                    {
                        vm.HouseholdMembers.Add(new HouseholdMemberEntryInput());
                    }

                    return View(vm);
                }

                // Load the current referral entry draft.
                var draft = LoadReferralEntryDraft() ?? new ReferralEntryDraft();
                draft.HouseholdMembers = vm.HouseholdMembers;

                // Persist the updated draft to session.
                SaveReferralEntryDraft(draft);

                return RedirectToAction(nameof(ReferralDetails));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting Household Entry page");
                TempData["ErrorMessage"] = "An unexpected error occurred while saving the household step.";

                if (!vm.HouseholdMembers.Any())
                {
                    vm.HouseholdMembers.Add(new HouseholdMemberEntryInput());
                }

                return View(vm);
            }
        }

        // GET: Referrals/ReferralDetails
        /// <summary>
        /// Shows the referral details step of the Referral Entry flow.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ReferralDetails()
        {
            try
            {
                _logger.LogInformation("Loading Referral Details page");

                // Load the current referral entry draft.
                var draft = LoadReferralEntryDraft();

                if (draft == null)
                {
                    _logger.LogWarning("Referral Details requested without Referral Entry draft");
                    TempData["ErrorMessage"] = "Your referral entry draft was not found. Please start again.";
                    return RedirectToAction(nameof(StartReferralEntry));
                }

                // Build the view model from the draft.
                var vm = new ReferralDetailsViewModel
                {
                    Referral = draft.Referral ?? new ReferralDetailsInput(),
                    BackAction = draft.RequiresHouseholdStep ? nameof(HouseholdEntry) : nameof(ClientEntry)
                };

                // Populate dropdown values for the step.
                await PopulateReferralDetailsDropdowns(vm);

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Referral Details page");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the referral details step.";
                return RedirectToAction(nameof(StartReferralEntry));
            }
        }

        // POST: Referrals/ReferralDetails
        /// <summary>
        /// Saves the referral details step of the Referral Entry flow.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReferralDetails(ReferralDetailsViewModel vm)
        {
            try
            {
                _logger.LogInformation("Submitting Referral Details page");

                // Normalize incoming values before business-rule validation.
                NormalizeReferralDetails(vm);
                await ApplyReferralDetailsValidationAsync(vm);

                // Return the form when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Referral Details failed validation");
                    await PopulateReferralDetailsDropdowns(vm);
                    return View(vm);
                }

                // Load the current referral entry draft.
                var draft = LoadReferralEntryDraft() ?? new ReferralEntryDraft();
                draft.Referral = vm.Referral ?? new ReferralDetailsInput();

                // Persist the updated draft to session.
                SaveReferralEntryDraft(draft);

                return RedirectToAction(nameof(ReviewReferralEntry));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting Referral Details page");
                TempData["ErrorMessage"] = "An unexpected error occurred while saving the referral details step.";
                await PopulateReferralDetailsDropdowns(vm);
                return View(vm);
            }
        }

        // GET: Referrals/ReviewReferralEntry
        /// <summary>
        /// Shows the review step of the Referral Entry flow.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ReviewReferralEntry()
        {
            try
            {
                _logger.LogInformation("Loading Review Referral Entry page");

                // Load the current referral entry draft.
                var draft = LoadReferralEntryDraft();

                if (draft == null)
                {
                    _logger.LogWarning("Review Referral Entry requested without Referral Entry draft");
                    TempData["ErrorMessage"] = "Your referral entry draft was not found. Please start again.";
                    return RedirectToAction(nameof(StartReferralEntry));
                }

                // Build the review view model from the draft.
                var vm = await BuildReferralEntryReviewViewModelAsync(draft);

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Review Referral Entry page");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the review step.";
                return RedirectToAction(nameof(StartReferralEntry));
            }
        }

        // POST: Referrals/ConfirmReferralEntry
        /// <summary>
        /// Confirms the Referral Entry flow and saves all required records.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReferralEntry()
        {
            try
            {
                _logger.LogInformation("Submitting Confirm Referral Entry");

                // Load the current referral entry draft.
                var draft = LoadReferralEntryDraft();

                if (draft == null)
                {
                    _logger.LogWarning("Confirm Referral Entry requested without Referral Entry draft");
                    TempData["ErrorMessage"] = "Your referral entry draft was not found. Please start again.";
                    return RedirectToAction(nameof(StartReferralEntry));
                }

                // Apply final draft validation before saving.
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
                        if (draft.NewClient == null)
                        {
                            throw new InvalidOperationException("Referral Entry is missing new client information.");
                        }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error confirming Referral Entry");

                var draft = LoadReferralEntryDraft();
                if (draft == null)
                {
                    TempData["ErrorMessage"] = "An unexpected error occurred while confirming the referral entry.";
                    return RedirectToAction(nameof(StartReferralEntry));
                }

                ModelState.AddModelError(string.Empty, "An unexpected error occurred while confirming the referral entry.");

                var reviewVm = await BuildReferralEntryReviewViewModelAsync(draft);
                return View(nameof(ReviewReferralEntry), reviewVm);
            }
        }

        // =========================================================
        // EXISTING EDIT / DELETE
        // =========================================================

        // GET: Referrals/Edit/5
        /// <summary>
        /// Shows the edit form for a single non-deleted referral.
        /// </summary>
        public async Task<IActionResult> Edit(ulong? id)
        {
            try
            {
                // Reject requests with no id.
                if (id == null)
                {
                    _logger.LogWarning("Edit requested with null Id");
                    return NotFound();
                }

                _logger.LogInformation("Loading Edit page for Referral Id {Id}", id);

                // Retrieve the requested active referral for editing.
                var referral = await _context.Referrals
                    .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

                // Return not found when the referral does not exist.
                if (referral == null)
                {
                    _logger.LogWarning("Referral Id {Id} not found for edit", id);
                    return NotFound();
                }

                // Populate dropdown values using the current record selections.
                await PopulateDropdowns(referral.ClientUserId, referral.ReferringOrganizationId);
                return View(referral);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit page for Referral Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Referrals/Edit/5
        /// <summary>
        /// Updates an existing referral after server-side validation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ulong id, [Bind("Id,ClientUserId,ReferringOrganizationId,ReferredOn,Status,ValidFrom,ValidTo,Notes")] Referral formModel)
        {
            try
            {
                _logger.LogInformation("Attempting to edit Referral Id {Id}", id);

                // Ensure the route id matches the posted model id.
                if (id != formModel.Id)
                {
                    _logger.LogWarning("Edit mismatch: route Id {RouteId} vs model Id {ModelId}", id, formModel.Id);
                    return NotFound();
                }

                // Remove navigation properties that are not posted by the form.
                ModelState.Remove(nameof(Referral.ClientUser));
                ModelState.Remove(nameof(Referral.ReferringOrganization));
                ModelState.Remove(nameof(Referral.CreatedByUser));
                ModelState.Remove(nameof(Referral.UpdatedByUser));

                // Normalize incoming values before business-rule validation.
                NormalizeReferral(formModel);
                await ApplyReferralValidationAsync(formModel, formModel.Id);

                // Return the form with dropdowns restored when validation fails.
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Edit Referral failed validation for Id {Id}", id);
                    await PopulateDropdowns(formModel.ClientUserId, formModel.ReferringOrganizationId);
                    return View(formModel);
                }

                // Retrieve the existing active referral record.
                var existing = await _context.Referrals
                    .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

                // Return not found when the target record no longer exists.
                if (existing == null)
                {
                    _logger.LogWarning("Referral Id {Id} not found during edit save", id);
                    return NotFound();
                }

                // Copy validated form values into the tracked entity.
                existing.ClientUserId = formModel.ClientUserId;
                existing.ReferringOrganizationId = formModel.ReferringOrganizationId;
                existing.ReferredOn = formModel.ReferredOn;
                existing.Status = formModel.Status;
                existing.ValidFrom = formModel.ValidFrom;
                existing.ValidTo = formModel.ValidTo;
                existing.Notes = formModel.Notes;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = null;

                try
                {
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Referral Id {Id} updated successfully", id);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Check whether the record was deleted during the edit attempt.
                    if (!await ReferralExists(formModel.Id))
                    {
                        _logger.LogWarning("Referral Id {Id} no longer exists during concurrency check", id);
                        return NotFound();
                    }

                    _logger.LogError(ex, "Concurrency error updating Referral Id {Id}", id);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error editing Referral Id {Id}", id);
                ModelState.AddModelError("", "An unexpected error occurred while updating the referral.");
                await PopulateDropdowns(formModel.ClientUserId, formModel.ReferringOrganizationId);
                return View(formModel);
            }
        }

        // GET: Referrals/Delete/5
        /// <summary>
        /// Shows the delete confirmation page for a single non-deleted referral.
        /// </summary>
        public async Task<IActionResult> Delete(ulong? id)
        {
            try
            {
                // Reject requests with no id.
                if (id == null)
                {
                    _logger.LogWarning("Delete requested with null Id");
                    return NotFound();
                }

                _logger.LogInformation("Loading Delete confirmation for Referral Id {Id}", id);

                // Retrieve the requested active referral with related client and organization data.
                var referral = await _context.Referrals
                    .Where(r => r.DeletedAt == null)
                    .Include(r => r.ClientUser)
                    .Include(r => r.ReferringOrganization)
                    .FirstOrDefaultAsync(m => m.Id == id);

                // Return not found when the referral does not exist.
                if (referral == null)
                {
                    _logger.LogWarning("Referral Id {Id} not found for delete", id);
                    return NotFound();
                }

                return View(referral);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete page for Referral Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the delete page.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Referrals/Delete/5
        /// <summary>
        /// Soft deletes a referral.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(ulong id)
        {
            try
            {
                _logger.LogWarning("Soft deleting Referral Id {Id}", id);

                // Retrieve the active referral targeted for soft delete.
                var referral = await _context.Referrals
                    .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

                // Return not found when the referral does not exist.
                if (referral == null)
                {
                    _logger.LogWarning("Referral Id {Id} not found during delete", id);
                    return NotFound();
                }

                // Apply soft-delete and audit values.
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting Referral Id {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the referral.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // =========================================================
        // SESSION HELPERS - NEW REFERRAL ENTRY FLOW
        // =========================================================

        /// <summary>
        /// Saves the current Referral Entry draft to session.
        /// </summary>
        private void SaveReferralEntryDraft(ReferralEntryDraft draft)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(draft);
            HttpContext.Session.SetString(ReferralEntrySessionKey, json);
        }

        /// <summary>
        /// Loads the current Referral Entry draft from session.
        /// </summary>
        private ReferralEntryDraft? LoadReferralEntryDraft()
        {
            var json = HttpContext.Session.GetString(ReferralEntrySessionKey);

            return string.IsNullOrWhiteSpace(json)
                ? null
                : System.Text.Json.JsonSerializer.Deserialize<ReferralEntryDraft>(json);
        }

        /// <summary>
        /// Clears the current Referral Entry draft from session.
        /// </summary>
        private void ClearReferralEntryDraft()
        {
            HttpContext.Session.Remove(ReferralEntrySessionKey);
        }

        // =========================================================
        // DROPDOWNS - STANDARD CRUD
        // =========================================================

        /// <summary>
        /// Populates the standard CRUD dropdowns for clients and referring organizations.
        /// </summary>
        private async Task PopulateDropdowns(ulong? selectedClientUserId = null, ulong? selectedReferringOrganizationId = null)
        {
            _logger.LogDebug("Populating dropdowns for Referrals");

            // Retrieve active users for the client dropdown list.
            var users = await _context.DomainUsers
                .Where(u => u.DeletedAt == null)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            // Build display-friendly client dropdown options.
            var userOptions = users
                .Select(u => new
                {
                    u.Id,
                    DisplayName = $"{u.LastName}, {u.FirstName} ({u.Email})"
                })
                .ToList();

            // Retrieve active referring organizations for the dropdown list.
            var organizations = await _context.ReferringOrganizations
                .Where(o => o.DeletedAt == null)
                .OrderBy(o => o.Name)
                .ToListAsync();

            // Store the dropdown options in ViewData.
            ViewData["ClientUserId"] = new SelectList(userOptions, "Id", "DisplayName", selectedClientUserId);
            ViewData["ReferringOrganizationId"] = new SelectList(organizations, "Id", "Name", selectedReferringOrganizationId);
        }

        // =========================================================
        // DROPDOWNS - REFERRAL ENTRY FLOW
        // =========================================================

        /// <summary>
        /// Populates the organization step dropdowns for the Referral Entry flow.
        /// </summary>
        private async Task PopulateOrganizationEntryDropdowns(OrganizationEntryViewModel vm)
        {
            vm.NewOrganization ??= new ReferringOrganizationEntryInput();
            vm.NewOrganization.SelectedServiceCategoryIds ??= new List<ulong>();

            // Retrieve active referring organizations for the existing organization dropdown.
            vm.ExistingOrganizations = await _context.ReferringOrganizations
                .Where(o => o.DeletedAt == null && o.IsActive)
                .OrderBy(o => o.Name)
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = o.Name
                })
                .ToListAsync();

            // Retrieve active service categories for the service categories list.
            vm.AvailableServiceCategories = await _context.ServiceCategories
                .Where(c => c.DeletedAt == null && c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = vm.NewOrganization.SelectedServiceCategoryIds.Contains(c.Id)
                })
                .ToListAsync();
        }

        /// <summary>
        /// Populates the client step dropdowns for the Referral Entry flow.
        /// </summary>
        private async Task PopulateClientEntryDropdowns(ClientEntryViewModel vm)
        {
            // Retrieve active clients for the existing client dropdown.
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

        /// <summary>
        /// Populates the referral details step dropdowns for the Referral Entry flow.
        /// </summary>
        private Task PopulateReferralDetailsDropdowns(ReferralDetailsViewModel vm)
        {
            // Build the referral status dropdown options.
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

        /// <summary>
        /// Builds the review view model for the Referral Entry flow.
        /// </summary>
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

                var selectedCategoryIds = draft.NewOrganization.SelectedServiceCategoryIds ?? new List<ulong>();

                if (selectedCategoryIds.Any())
                {
                    var categoryNames = await _context.ServiceCategories
                        .Where(c => c.DeletedAt == null && selectedCategoryIds.Contains(c.Id))
                        .OrderBy(c => c.Name)
                        .Select(c => c.Name)
                        .ToListAsync();

                    vm.NewOrganizationServiceCategoriesDisplay = categoryNames.Any()
                        ? string.Join(", ", categoryNames)
                        : null;
                }
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
                var newClient = draft.NewClient;

                if (newClient == null)
                {
                    throw new InvalidOperationException("Referral Entry draft is missing new client information.");
                }

                vm.SelectedClientDisplayName =
                    string.IsNullOrWhiteSpace($"{newClient.LastName}{newClient.FirstName}".Trim())
                        ? newClient.Email
                        : $"{newClient.LastName}, {newClient.FirstName} ({newClient.Email})";
            }

            return vm;
        }

        // =========================================================
        // NORMALIZATION / VALIDATION - REFERRAL ENTRY FLOW
        // PLACEHOLDERS FOR NEXT PHASE
        // =========================================================

        /// <summary>
        /// Normalizes the organization step view model.
        /// </summary>
        private void NormalizeOrganizationEntry(OrganizationEntryViewModel vm)
        {
            vm.NewOrganization ??= new ReferringOrganizationEntryInput();
            vm.NewOrganization.SelectedServiceCategoryIds ??= new List<ulong>();

            vm.NewOrganization.Name = NullIfWhiteSpace(vm.NewOrganization.Name);
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

        /// <summary>
        /// Applies business-rule validation for the organization step.
        /// </summary>
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

            vm.NewOrganization ??= new ReferringOrganizationEntryInput();
            vm.NewOrganization.SelectedServiceCategoryIds ??= new List<ulong>();

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

            if (!vm.NewOrganization.SelectedServiceCategoryIds.Any())
            {
                ModelState.AddModelError("NewOrganization.SelectedServiceCategoryIds", "Select at least one service category.");
            }
            else
            {
                var validCategoryIds = await _context.ServiceCategories
                    .Where(c => c.DeletedAt == null && c.IsActive)
                    .Select(c => c.Id)
                    .ToListAsync();

                var invalidIds = vm.NewOrganization.SelectedServiceCategoryIds
                    .Except(validCategoryIds)
                    .ToList();

                if (invalidIds.Any())
                {
                    ModelState.AddModelError("NewOrganization.SelectedServiceCategoryIds", "One or more selected service categories are invalid.");
                }
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

        /// <summary>
        /// Normalizes the client step view model.
        /// </summary>
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

            vm.NewClient.Incomes ??= new List<ClientIncomeEntryInput>();

            foreach (var income in vm.NewClient.Incomes)
            {
                income.Notes = NullIfWhiteSpace(income.Notes);
            }
        }

        /// <summary>
        /// Applies business-rule validation for the client step.
        /// </summary>
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

            if (!string.IsNullOrWhiteSpace(vm.NewClient.Email))
            {
                if (!IsValidEmail(vm.NewClient.Email))
                {
                    ModelState.AddModelError("NewClient.Email", "Email format is invalid.");
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

            if (!vm.NewClient.EmploymentStatus.HasValue)
            {
                ModelState.AddModelError("NewClient.EmploymentStatus", "Employment status is required.");
            }
            else if (!Enum.IsDefined(vm.NewClient.EmploymentStatus.Value))
            {
                ModelState.AddModelError("NewClient.EmploymentStatus", "Select a valid employment status.");
            }

            vm.NewClient.Incomes ??= new List<ClientIncomeEntryInput>();

            for (int i = 0; i < vm.NewClient.Incomes.Count; i++)
            {
                var income = vm.NewClient.Incomes[i];

                if (!income.HasStarted)
                {
                    continue;
                }

                if (!income.IncomeType.HasValue)
                {
                    ModelState.AddModelError($"NewClient.Incomes[{i}].IncomeType", "Income type is required.");
                }

                if (!income.MonthlyAmount.HasValue)
                {
                    ModelState.AddModelError($"NewClient.Incomes[{i}].MonthlyAmount", "Monthly amount is required.");
                }
                else
                {
                    if (income.MonthlyAmount.Value < 0)
                    {
                        ModelState.AddModelError($"NewClient.Incomes[{i}].MonthlyAmount", "Monthly amount must be 0 or greater.");
                    }

                    if (decimal.Round(income.MonthlyAmount.Value, 2) != income.MonthlyAmount.Value)
                    {
                        ModelState.AddModelError($"NewClient.Incomes[{i}].MonthlyAmount", "Monthly amount cannot have more than 2 decimal places.");
                    }

                    if (income.MonthlyAmount.Value > 99999999.99m)
                    {
                        ModelState.AddModelError($"NewClient.Incomes[{i}].MonthlyAmount", "Monthly amount exceeds the allowed maximum.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(income.Notes) && income.Notes.Length > 250)
                {
                    ModelState.AddModelError($"NewClient.Incomes[{i}].Notes", "Notes cannot exceed 250 characters.");
                }
            }
        }

        /// <summary>
        /// Normalizes the household step view model.
        /// </summary>
        private void NormalizeHouseholdEntry(HouseholdEntryViewModel vm)
        {
            vm.HouseholdMembers ??= new List<HouseholdMemberEntryInput>();

            foreach (var member in vm.HouseholdMembers)
            {
                member.FirstName = NullIfWhiteSpace(member.FirstName);
                member.LastName = NullIfWhiteSpace(member.LastName);
            }
        }

        /// <summary>
        /// Applies business-rule validation for the household step.
        /// </summary>
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

                if (member.ApproximateAge.HasValue)
                {
                    if (member.ApproximateAge.Value < 0)
                    {
                        ModelState.AddModelError($"HouseholdMembers[{i}].ApproximateAge", "Approximate Age cannot be negative.");
                    }
                    else if (member.ApproximateAge.Value > 130)
                    {
                        ModelState.AddModelError($"HouseholdMembers[{i}].ApproximateAge", "Approximate Age cannot be greater than 130.");
                    }
                }
            }
        }

        /// <summary>
        /// Normalizes the referral details step view model.
        /// </summary>
        private void NormalizeReferralDetails(ReferralDetailsViewModel vm)
        {
            vm.Referral ??= new ReferralDetailsInput();
            vm.Referral.Notes = NullIfWhiteSpace(vm.Referral.Notes);
        }

        /// <summary>
        /// Applies business-rule validation for the referral details step.
        /// </summary>
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

            if (!string.IsNullOrWhiteSpace(vm.Referral.Notes) &&
                vm.Referral.Notes.Length > 2000)
            {
                ModelState.AddModelError("Referral.Notes", "Notes cannot exceed 2000 characters.");
            }
        }

        /// <summary>
        /// Applies final validation to the Referral Entry draft before confirmation.
        /// </summary>
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

        /// <summary>
        /// Returns true if the non-deleted referral exists.
        /// </summary>
        private async Task<bool> ReferralExists(ulong id)
        {
            return await _context.Referrals.AnyAsync(e => e.Id == id && e.DeletedAt == null);
        }

        /// <summary>
        /// Trims strings and normalizes required values.
        /// </summary>
        private static void NormalizeReferral(Referral model)
        {
            model.Notes = NullIfWhiteSpace(model.Notes);
        }

        /// <summary>
        /// Applies business-rule validation beyond data annotations.
        /// </summary>
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

            if (!string.IsNullOrWhiteSpace(model.Notes) && model.Notes.Length > 2000)
            {
                ModelState.AddModelError(nameof(Referral.Notes), "Notes cannot exceed 2000 characters.");
            }
        }

        /// <summary>
        /// Returns null when the value is blank; otherwise returns the trimmed value.
        /// </summary>
        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Returns true when the phone number matches the allowed US format rules.
        /// </summary>
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

        /// <summary>
        /// Returns true when the value contains at least one letter or digit.
        /// </summary>
        private static bool ContainsLetterOrDigit(string value)
        {
            return value.Any(char.IsLetterOrDigit);
        }

        /// <summary>
        /// Returns true when the city value matches the allowed character rules.
        /// </summary>
        private static bool IsValidCity(string city)
        {
            return Regex.IsMatch(city, @"^[A-Za-z][A-Za-z\s'.-]*$");
        }

        /// <summary>
        /// Returns true when the state value is a valid 2-letter US state code.
        /// </summary>
        private static bool IsValidUsStateCode(string state)
        {
            return state.Length == 2 && ValidUsStateCodes.Contains(state);
        }

        /// <summary>
        /// Returns true when the postal code matches US ZIP or ZIP+4 format.
        /// </summary>
        private static bool IsValidUsPostalCode(string postalCode)
        {
            return Regex.IsMatch(postalCode, @"^\d{5}(-\d{4})?$");
        }

        /// <summary>
        /// Returns true when the email matches the allowed format rules.
        /// </summary>
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

        /// <summary>
        /// Returns true when the person name matches the allowed character rules.
        /// </summary>
        private static bool IsValidPersonName(string name)
        {
            return Regex.IsMatch(name, @"^[A-Za-z][A-Za-z\s'.-]*$");
        }
    }
}