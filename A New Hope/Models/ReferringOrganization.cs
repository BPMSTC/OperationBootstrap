using System.ComponentModel.DataAnnotations; // Added for [MaxLength], [EmailAddress], [Phone]

namespace A_New_Hope.Models
{
    /// <summary>
    /// ReferringOrganization
    /// ---------------------
    /// Represents an external organization/agency that can refer clients to your program.
    ///
    /// Examples:
    /// - County social services
    /// - Clinics
    /// - Shelters
    /// - Community partners
    ///
    /// How it is used:
    /// - A ReferringOrganization can create many Referral records.
    /// - Referrals link: (Client DomainUser) <-> (ReferringOrganization)
    ///
    /// Status + lifecycle:
    /// - IsActive is a business toggle (disable/hide without deleting).
    /// - DeletedAt supports soft delete (record remains in DB but is excluded by query filters).
    ///
    /// Audit fields:
    /// - CreatedByUserId / UpdatedByUserId store the DomainUser responsible (once auth is wired).
    /// - CreatedAt / UpdatedAt store timestamps (UTC recommended).
    /// </summary>
    public class ReferringOrganization
    {
        /// <summary>
        /// Primary key for the referring organization record.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Organization name (required).
        /// MaxLength prevents LONGTEXT in MySQL and keeps the column index-friendly.
        /// </summary>
        [Required(ErrorMessage = "Organization name is required.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional organization type/category (e.g., "Clinic", "County Agency").
        /// </summary>
        [MaxLength(100)]
        public string? Type { get; set; }

        /// <summary>
        /// Main phone number.
        /// </summary>
        [Required(ErrorMessage = "Phone number is required.")]
        [MaxLength(25)]
        [RegularExpression(@"^(\+1\s?)?(\([0-9]{3}\)|[0-9]{3})[\s.-]?[0-9]{3}[\s.-]?[0-9]{4}$",
            ErrorMessage = "Please enter a valid phone number.")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Email address.
        /// </summary>
        [Required(ErrorMessage = "Email address is required.")]
        [MaxLength(254)]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        // -----------------------------------------------------------------
        // Address fields
        // -----------------------------------------------------------------

        /// <summary>
        /// Address line 1.
        /// </summary>
        [Required(ErrorMessage = "Address line 1 is required.")]
        [MaxLength(200)]
        public string AddressLine1 { get; set; } = string.Empty;

        /// <summary>
        /// Address line 2.
        /// </summary>
        [MaxLength(200)]
        public string? AddressLine2 { get; set; }

        /// <summary>
        /// City.
        /// </summary>
        [Required(ErrorMessage = "City is required.")]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// State.
        /// </summary>
        [Required(ErrorMessage = "State is required.")]
        [StringLength(2, ErrorMessage = "State must be a 2-letter abbreviation.")]
        [RegularExpression(@"^[A-Za-z]{2}$", ErrorMessage = "State must be a valid 2-letter abbreviation.")]
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// Postal/zip code.
        /// Supports US ZIP or ZIP+4
        /// </summary>
        [Required(ErrorMessage = "ZIP code is required.")]
        [MaxLength(10)]
        [RegularExpression(@"^\d{5}(-\d{4})?$",
            ErrorMessage = "Enter a valid US ZIP code.")]
        public string PostalCode { get; set; } = string.Empty;

        /// <summary>
        /// Optional primary contact name at the organization.
        /// </summary>
        [MaxLength(200)]
        public string? PrimaryContactName { get; set; }

        /// <summary>
        /// Optional free-form notes about the organization.
        /// </summary>
        [MaxLength(2000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Business toggle to enable/disable the organization in selection lists and workflows.
        /// </summary>
        public bool IsActive { get; set; } = true;

        // -----------------------------------------------------------------
        // Audit fields
        // -----------------------------------------------------------------

        /// <summary>
        /// Audit: DomainUser who created this record.
        /// Nullable until authentication/auditing is fully wired.
        /// </summary>
        public ulong? CreatedByUserId { get; set; }

        /// <summary>
        /// Audit: DomainUser who last updated this record.
        /// Nullable until authentication/auditing is fully wired.
        /// </summary>
        public ulong? UpdatedByUserId { get; set; }

        /// <summary>
        /// UTC timestamp when the record was created.
        /// Set server-side when the entity is first saved.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// UTC timestamp when the record was last updated.
        /// Set server-side whenever the entity is modified.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// UTC timestamp for soft deletion.
        /// Null means the record is active; non-null means it is soft deleted.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        // -----------------------------------------------------------------
        // Navigation properties (EF Core relationships)
        // -----------------------------------------------------------------

        /// <summary>
        /// Referrals associated with this organization.
        /// Initialized to an empty collection to avoid null checks.
        /// </summary>
        public ICollection<Referral> Referrals { get; set; } = new List<Referral>();

        public ICollection<ReferringOrganizationServiceCategory> ReferringOrganizationServiceCategories { get; set; }
            = new List<ReferringOrganizationServiceCategory>();

        /// <summary>
        /// DomainUser who created this record.
        /// </summary>
        public DomainUser? CreatedByUser { get; set; }

        /// <summary>
        /// DomainUser who last updated this record.
        /// </summary>
        public DomainUser? UpdatedByUser { get; set; }
    }
}

