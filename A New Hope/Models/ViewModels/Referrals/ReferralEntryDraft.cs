using System.ComponentModel.DataAnnotations;
using A_New_Hope.Models;

namespace A_New_Hope.Models.ViewModels.Referrals
{
    /// <summary>
    /// Stores the in-progress Referral Entry flow state in session.
    /// This is not tied to a single page. It represents the overall
    /// draft as the user moves from one page to the next.
    /// </summary>
    public class ReferralEntryDraft
    {
        // =========================================================
        // REFERRING ORGANIZATION
        // =========================================================

        public ulong? ExistingReferringOrganizationId { get; set; }

        public ReferringOrganizationEntryInput NewOrganization { get; set; } = new();

        // =========================================================
        // CLIENT
        // =========================================================

        public ulong? ExistingClientUserId { get; set; }

        public ClientEntryInput NewClient { get; set; } = new();

        // =========================================================
        // HOUSEHOLD MEMBERS
        // =========================================================

        public List<HouseholdMemberEntryInput> HouseholdMembers { get; set; } = new();

        // =========================================================
        // REFERRAL DETAILS
        // =========================================================

        public ReferralDetailsInput Referral { get; set; } = new();

        // =========================================================
        // HELPER FLAGS
        // =========================================================

        public bool HasExistingOrganization =>
            ExistingReferringOrganizationId.HasValue && ExistingReferringOrganizationId.Value > 0;

        public bool HasNewOrganization => NewOrganization.HasStarted;

        public bool HasExistingClient =>
            ExistingClientUserId.HasValue && ExistingClientUserId.Value > 0;

        public bool HasNewClient => NewClient.HasStarted;

        public bool RequiresHouseholdStep => HasNewClient;

        public bool HasAnyHouseholdMembers =>
            HouseholdMembers.Any(h => h.HasStarted);
    }

    /// <summary>
    /// Organization entry data captured during Referral Entry.
    /// </summary>
    public class ReferringOrganizationEntryInput
    {
        [Display(Name = "Organization Name")]
        [MaxLength(200)]
        public string? Name { get; set; }

        [Display(Name = "Primary Type of Service")]
        [MaxLength(100)]
        public string? Type { get; set; }

        [Display(Name = "Contact Person Name")]
        [MaxLength(200)]
        public string? PrimaryContactName { get; set; }

        [Display(Name = "Email Address")]
        [MaxLength(254)]
        public string? Email { get; set; }

        [Display(Name = "Phone Number")]
        [MaxLength(25)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Address Line 1")]
        [MaxLength(200)]
        public string? AddressLine1 { get; set; }

        [Display(Name = "Address Line 2")]
        [MaxLength(200)]
        public string? AddressLine2 { get; set; }

        [Display(Name = "City")]
        [MaxLength(100)]
        public string? City { get; set; }

        [Display(Name = "State")]
        [MaxLength(2)]
        public string? State { get; set; }

        [Display(Name = "ZIP Code")]
        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [Display(Name = "Notes")]
        [MaxLength(2000)]
        public string? Notes { get; set; }

        public bool HasStarted =>
            !string.IsNullOrWhiteSpace(Name) ||
            !string.IsNullOrWhiteSpace(Type) ||
            !string.IsNullOrWhiteSpace(PrimaryContactName) ||
            !string.IsNullOrWhiteSpace(Email) ||
            !string.IsNullOrWhiteSpace(PhoneNumber) ||
            !string.IsNullOrWhiteSpace(AddressLine1) ||
            !string.IsNullOrWhiteSpace(AddressLine2) ||
            !string.IsNullOrWhiteSpace(City) ||
            !string.IsNullOrWhiteSpace(State) ||
            !string.IsNullOrWhiteSpace(PostalCode) ||
            !string.IsNullOrWhiteSpace(Notes);
    }

    /// <summary>
    /// Client entry data captured during Referral Entry.
    /// </summary>
    public class ClientEntryInput
    {
        [Display(Name = "First Name")]
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        [MaxLength(100)]
        public string? LastName { get; set; }

        [Display(Name = "Email Address")]
        [MaxLength(254)]
        public string? Email { get; set; }

        [Display(Name = "Phone Number")]
        [MaxLength(25)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Address Line 1")]
        [MaxLength(200)]
        public string? AddressLine1 { get; set; }

        [Display(Name = "Address Line 2")]
        [MaxLength(200)]
        public string? AddressLine2 { get; set; }

        [Display(Name = "City")]
        [MaxLength(100)]
        public string? City { get; set; }

        [Display(Name = "State")]
        [MaxLength(2)]
        public string? State { get; set; }

        [Display(Name = "ZIP Code")]
        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateOnly? DateOfBirth { get; set; }

        [Display(Name = "Employment Status")]
        [MaxLength(50)]
        public string? EmploymentStatus { get; set; }

        [Display(Name = "Monthly Earned Income")]
        [Range(0, 9999999999.99, ErrorMessage = "Monthly earned income must be 0 or greater.")]
        public decimal? EarnedIncomeMonthly { get; set; }

        [Display(Name = "Currently Unhoused")]
        public bool IsUnhoused { get; set; }

        public bool HasStarted =>
            !string.IsNullOrWhiteSpace(FirstName) ||
            !string.IsNullOrWhiteSpace(LastName) ||
            !string.IsNullOrWhiteSpace(Email) ||
            !string.IsNullOrWhiteSpace(PhoneNumber) ||
            !string.IsNullOrWhiteSpace(AddressLine1) ||
            !string.IsNullOrWhiteSpace(AddressLine2) ||
            !string.IsNullOrWhiteSpace(City) ||
            !string.IsNullOrWhiteSpace(State) ||
            !string.IsNullOrWhiteSpace(PostalCode) ||
            DateOfBirth.HasValue ||
            !string.IsNullOrWhiteSpace(EmploymentStatus) ||
            EarnedIncomeMonthly.HasValue ||
            IsUnhoused;
    }

    /// <summary>
    /// Household member entry row captured during Referral Entry.
    /// </summary>
    public class HouseholdMemberEntryInput
    {
        [Display(Name = "First Name")]
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        [MaxLength(100)]
        public string? LastName { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Age As Of Date")]
        [DataType(DataType.Date)]
        public DateTime? AgeAsOfDate { get; set; }

        public bool HasStarted =>
            !string.IsNullOrWhiteSpace(FirstName) ||
            !string.IsNullOrWhiteSpace(LastName) ||
            DateOfBirth.HasValue ||
            AgeAsOfDate.HasValue;
    }

    /// <summary>
    /// Referral-specific data captured during Referral Entry.
    /// </summary>
    public class ReferralDetailsInput
    {
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
        public string? ReferredByEmail { get; set; }

        [Display(Name = "Referral Notes")]
        [MaxLength(2000)]
        public string? Notes { get; set; }
    }
}