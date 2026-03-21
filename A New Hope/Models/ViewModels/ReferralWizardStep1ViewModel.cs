using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models.ViewModels
{
    /// <summary>
    /// View model for Referral Wizard Step 1:
    /// select an existing referring organization or add a new one,
    /// then select an existing client or add a new one,
    /// then enter the referral details themselves.
    /// 
    /// When adding a new client:
    /// - core DomainUser fields are collected
    /// - ClientProfile fields are collected as a static section
    /// - HouseholdMembers can be added optionally as a 1:M collection
    /// </summary>
    public class ReferralWizardStep1ViewModel
    {
        // =========================================================
        // REFERRING ORGANIZATION
        // =========================================================

        [Display(Name = "Existing Organization")]
        public ulong? SelectedReferringOrganizationId { get; set; }

        [Display(Name = "Organization Name")]
        [MaxLength(200)]
        public string? NewOrganizationName { get; set; }

        [Display(Name = "Primary Type of Service")]
        [MaxLength(100)]
        public string? NewOrganizationType { get; set; }

        [Display(Name = "Contact Person Name")]
        [MaxLength(200)]
        public string? NewPrimaryContactName { get; set; }

        [Display(Name = "Email Address")]
        [MaxLength(254)]
        public string? NewEmail { get; set; }

        [Display(Name = "Phone Number")]
        [MaxLength(25)]
        public string? NewPhoneNumber { get; set; }

        [Display(Name = "Address Line 1")]
        [MaxLength(200)]
        public string? NewAddressLine1 { get; set; }

        [Display(Name = "Address Line 2")]
        [MaxLength(200)]
        public string? NewAddressLine2 { get; set; }

        [Display(Name = "City")]
        [MaxLength(100)]
        public string? NewCity { get; set; }

        [Display(Name = "State")]
        [MaxLength(2)]
        public string? NewState { get; set; }

        [Display(Name = "ZIP Code")]
        [MaxLength(20)]
        public string? NewPostalCode { get; set; }

        [Display(Name = "Notes")]
        [MaxLength(2000)]
        public string? NewNotes { get; set; }

        public List<SelectListItem> ExistingOrganizations { get; set; } = new();

        /// <summary>
        /// True when the user selected an existing organization.
        /// </summary>
        public bool HasSelectedExistingOrganization =>
            SelectedReferringOrganizationId.HasValue && SelectedReferringOrganizationId.Value > 0;

        /// <summary>
        /// True when the user has started entering a new organization.
        /// Used to determine validation path and whether the collapse should stay open.
        /// </summary>
        public bool HasStartedNewOrganization =>
            !string.IsNullOrWhiteSpace(NewOrganizationName) ||
            !string.IsNullOrWhiteSpace(NewOrganizationType) ||
            !string.IsNullOrWhiteSpace(NewPrimaryContactName) ||
            !string.IsNullOrWhiteSpace(NewEmail) ||
            !string.IsNullOrWhiteSpace(NewPhoneNumber) ||
            !string.IsNullOrWhiteSpace(NewAddressLine1) ||
            !string.IsNullOrWhiteSpace(NewAddressLine2) ||
            !string.IsNullOrWhiteSpace(NewCity) ||
            !string.IsNullOrWhiteSpace(NewState) ||
            !string.IsNullOrWhiteSpace(NewPostalCode) ||
            !string.IsNullOrWhiteSpace(NewNotes);

        // =========================================================
        // CLIENT
        // =========================================================

        [Display(Name = "Existing Client")]
        public ulong? SelectedClientUserId { get; set; }

        [Display(Name = "First Name")]
        [MaxLength(100)]
        public string? NewClientFirstName { get; set; }

        [Display(Name = "Last Name")]
        [MaxLength(100)]
        public string? NewClientLastName { get; set; }

        [Display(Name = "Email Address")]
        [MaxLength(254)]
        public string? NewClientEmail { get; set; }

        [Display(Name = "Phone Number")]
        [MaxLength(25)]
        public string? NewClientPhoneNumber { get; set; }

        [Display(Name = "Address Line 1")]
        [MaxLength(200)]
        public string? NewClientAddressLine1 { get; set; }

        [Display(Name = "Address Line 2")]
        [MaxLength(200)]
        public string? NewClientAddressLine2 { get; set; }

        [Display(Name = "City")]
        [MaxLength(100)]
        public string? NewClientCity { get; set; }

        [Display(Name = "State")]
        [MaxLength(2)]
        public string? NewClientState { get; set; }

        [Display(Name = "ZIP Code")]
        [MaxLength(20)]
        public string? NewClientPostalCode { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateOnly? NewClientDateOfBirth { get; set; }

        // =========================================================
        // CLIENT PROFILE (static when adding a new client)
        // =========================================================

        [Display(Name = "Employment Status")]
        [MaxLength(50)]
        public string? NewClientEmploymentStatus { get; set; }

        [Display(Name = "Monthly Earned Income")]
        [Range(0, 9999999999.99, ErrorMessage = "Monthly earned income must be 0 or greater.")]
        public decimal? NewClientEarnedIncomeMonthly { get; set; }

        [Display(Name = "Currently Unhoused")]
        public bool NewClientIsUnhoused { get; set; }

        // =========================================================
        // HOUSEHOLD MEMBERS (optional 1:M when adding a new client)
        // =========================================================

        public List<ReferralWizardHouseholdMemberViewModel> HouseholdMembers { get; set; } = new();

        public List<SelectListItem> ExistingClients { get; set; } = new();

        /// <summary>
        /// True when the user selected an existing client.
        /// </summary>
        public bool HasSelectedExistingClient =>
            SelectedClientUserId.HasValue && SelectedClientUserId.Value > 0;

        /// <summary>
        /// True when the user has started entering a new client,
        /// including any static ClientProfile fields.
        /// Used to determine validation path and whether the collapse should stay open.
        /// </summary>
        public bool HasStartedNewClient =>
            !string.IsNullOrWhiteSpace(NewClientFirstName) ||
            !string.IsNullOrWhiteSpace(NewClientLastName) ||
            !string.IsNullOrWhiteSpace(NewClientEmail) ||
            !string.IsNullOrWhiteSpace(NewClientPhoneNumber) ||
            !string.IsNullOrWhiteSpace(NewClientAddressLine1) ||
            !string.IsNullOrWhiteSpace(NewClientAddressLine2) ||
            !string.IsNullOrWhiteSpace(NewClientCity) ||
            !string.IsNullOrWhiteSpace(NewClientState) ||
            !string.IsNullOrWhiteSpace(NewClientPostalCode) ||
            NewClientDateOfBirth.HasValue ||
            !string.IsNullOrWhiteSpace(NewClientEmploymentStatus) ||
            NewClientEarnedIncomeMonthly.HasValue ||
            NewClientIsUnhoused ||
            (HouseholdMembers?.Any(h => h.HasStarted) ?? false);

        // =========================================================
        // REFERRAL DETAILS
        // =========================================================

        [Required(ErrorMessage = "Referral date is required.")]
        [Display(Name = "Referral Date")]
        [DataType(DataType.Date)]
        public DateTime? ReferredOn { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Referral status is required.")]
        [Display(Name = "Status")]
        public ReferralStatus? Status { get; set; } = ReferralStatus.Pending;

        [Display(Name = "Valid From")]
        [DataType(DataType.Date)]
        public DateTime? ValidFrom { get; set; }

        [Display(Name = "Valid To")]
        [DataType(DataType.Date)]
        public DateTime? ValidTo { get; set; }

        [Display(Name = "Referrer Name")]
        [MaxLength(200)]
        public string? ReferredByName { get; set; }

        [Display(Name = "Referrer Phone Number")]
        [MaxLength(25)]
        public string? ReferredByPhoneNumber { get; set; }

        [Display(Name = "Referrer Email Address")]
        [MaxLength(254)]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string? ReferredByEmail { get; set; }

        [Display(Name = "Referral Notes")]
        [MaxLength(2000)]
        public string? ReferralNotes { get; set; }

        public List<SelectListItem> ReferralStatusOptions { get; set; } = new();
    }
}