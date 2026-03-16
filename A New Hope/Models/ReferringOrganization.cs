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
        [MaxLength(200)] // Prevents LONGTEXT in MySQL; keeps org names index-friendly if you add a unique/search index later
        [RegularExpression(@"^[A-Za-z0-9\s&().,'\-]+$", ErrorMessage = "Organization name contains invalid characters.")]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Optional organization type/category (e.g., "Clinic", "County Agency").
        /// </summary>
        [MaxLength(100)] // Keeps this as VARCHAR and prevents oversized values
        [RegularExpression(@"^[A-Za-z0-9\s&().,'\-]*$", ErrorMessage = "Organization type contains invalid characters.")]
        public string? Type { get; set; }

        /// <summary>
        /// Optional main phone number.
        /// [Phone] provides MVC-level validation for cleaner form input.
        /// </summary>
        [MaxLength(25)] // Prevents LONGTEXT; allows punctuation/extensions
        [Phone(ErrorMessage = "Please enter a valid phone number.")] // MVC validation for cleaner input
        [RegularExpression(@"^\+?[0-9()\-\s]+$", ErrorMessage = "Phone number contains invalid characters.")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Optional main email address.
        /// [EmailAddress] provides MVC-level validation for cleaner form input.
        /// </summary>
        [MaxLength(254)] // Practical max for email addresses
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")] // MVC validation
        [RegularExpression(@"^[A-Za-z0-9._+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$",
            ErrorMessage = "Email format is invalid.")]
        public string? Email { get; set; }

        // -----------------------------------------------------------------
        // Address fields (optional)
        // -----------------------------------------------------------------

        /// <summary>
        /// Optional address line 1.
        /// </summary>
        [MaxLength(200)]
        [RegularExpression(@"^[A-Za-z0-9\s#.,'\-]*$", ErrorMessage = "Address contains invalid characters.")]
        public string? AddressLine1 { get; set; } // Avoid LONGTEXT

        /// <summary>
        /// Optional address line 2.
        /// </summary>
        [MaxLength(200)]
        [RegularExpression(@"^[A-Za-z0-9\s#.,'\-]*$", ErrorMessage = "Address contains invalid characters.")]
        public string? AddressLine2 { get; set; } // Avoid LONGTEXT

        /// <summary>
        /// Optional city.
        /// </summary>
        [MaxLength(100)]
        [RegularExpression(@"^[A-Za-z\s.\-']*$", ErrorMessage = "City contains invalid characters.")]
        public string? City { get; set; } // Avoid LONGTEXT

        /// <summary>
        /// Optional state/region.
        /// </summary>
        [MaxLength(50)]
        [RegularExpression(@"^[A-Za-z\s.\-']*$", ErrorMessage = "State contains invalid characters.")]
        public string? State { get; set; } // Use 2 if strictly US-only

        /// <summary>
        /// Optional postal/zip code.
        /// </summary>
        /// <summary>
        /// Optional postal/zip code.
        /// Accepts 5-digit ZIP, ZIP+4 (US), or alphanumeric for non-US postal codes.
        /// </summary>
        [MaxLength(20)]
        [RegularExpression(@"^\d{5}(-\d{4})?$",
            ErrorMessage = "Enter a valid US ZIP code.")]
        public string? PostalCode { get; set; } // Supports ZIP/ZIP+4 and non-US formats if needed

        /// <summary>
        /// Optional primary contact name at the organization.
        /// </summary>
        [MaxLength(200)] // Keeps contact names reasonable and indexable if needed
        [RegularExpression(@"^[A-Za-z][A-Za-z\s'.-]*$", ErrorMessage = "Contact name contains invalid characters.")]
        public string? PrimaryContactName { get; set; }

        /// <summary>
        /// Optional free-form notes about the organization.
        /// Left uncapped to allow longer descriptions if needed.
        /// </summary>
        [StringLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters.")]
        public string? Notes { get; set; } // Leave uncapped if you want free-form notes (TEXT/LONGTEXT is okay here)

        /// <summary>
        /// Business toggle to enable/disable the organization in selection lists and workflows.
        /// </summary>
        public bool IsActive { get; set; } = true;

        // -----------------------------------------------------------------
        // Audit fields
        // -----------------------------------------------------------------

        /// <summary>
        /// Audit: DomainUser who created this record (nullable until auth is wired).
        /// </summary>
        public ulong? CreatedByUserId { get; set; }

        /// <summary>
        /// Audit: DomainUser who last updated this record (nullable until auth is wired).
        /// </summary>
        public ulong? UpdatedByUserId { get; set; }

        /// <summary>
        /// Timestamp when the record was created (typically set server-side in UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when the record was last updated (typically set server-side in UTC).
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Soft delete marker:
        /// - null = not deleted
        /// - non-null = deleted (excluded by global query filters in ApplicationDbContext)
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        // -----------------------------------------------------------------
        // Navigation properties (EF Core relationships)
        // -----------------------------------------------------------------

        /// <summary>
        /// Navigation collection of referrals created by / associated with this organization.
        /// Initialized to an empty list to avoid null checks.
        /// </summary>
        public ICollection<Referral> Referrals { get; set; } = new List<Referral>();

        /// <summary>
        /// Navigation to the DomainUser who created this record (useful for Include() and admin audit views).
        /// </summary>
        public DomainUser? CreatedByUser { get; set; } // Added to match CreatedByUserId and support Include() in admin/audit views

        /// <summary>
        /// Navigation to the DomainUser who last updated this record.
        /// </summary>
        public DomainUser? UpdatedByUser { get; set; } // Added to match UpdatedByUserId and support Include()
    }
}

