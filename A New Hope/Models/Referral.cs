using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models
{
    /// <summary>
    /// Referral
    /// --------
    /// Represents a referral of a client from a referring organization.
    /// 
    /// How it is used:
    /// - Links a ClientUser to a ReferringOrganization.
    /// - Tracks referral dates, validity, status, and referrer information.
    /// - Audit fields track creation and updates.
    /// </summary>
    public class Referral
    {
        /// <summary>
        /// Primary key for the referral record.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Foreign key for the client being referred.
        /// </summary>
        [Required]
        [Display(Name = "Client")]
        public ulong ClientUserId { get; set; }

        /// <summary>
        /// Foreign key for the referring organization.
        /// </summary>
        [Required]
        [Display(Name = "Referring Organization")]
        public ulong ReferringOrganizationId { get; set; }

        /// <summary>
        /// Date the referral was made.
        /// </summary>
        [Required]
        [Display(Name = "Referral Date")]
        [DataType(DataType.Date)]
        public DateTime ReferredOn { get; set; }

        /// <summary>
        /// Current status of the referral.
        /// </summary>
        [Required]
        [Display(Name = "Status")]
        public ReferralStatus Status { get; set; } = ReferralStatus.Pending;

        /// <summary>
        /// Optional start date for referral validity.
        /// </summary>
        [Display(Name = "Valid From")]
        [DataType(DataType.Date)]
        public DateTime? ValidFrom { get; set; }

        /// <summary>
        /// Optional end date for referral validity.
        /// </summary>
        [Display(Name = "Valid To")]
        [DataType(DataType.Date)]
        public DateTime? ValidTo { get; set; }

        /// <summary>
        /// Name of the person who made the referral.
        /// </summary>
        [Display(Name = "Referrer Name")]
        [StringLength(200, ErrorMessage = "Referrer name cannot exceed 200 characters.")]
        [RegularExpression(@"^[A-Za-z][A-Za-z\s'.-]*$", ErrorMessage = "Referrer name contains invalid characters.")]
        public string? ReferredByName { get; set; }

        /// <summary>
        /// Phone number of the person who made the referral.
        /// </summary>
        [Display(Name = "Referrer Phone")]
        [StringLength(25, ErrorMessage = "Phone number cannot exceed 25 characters.")]
        [RegularExpression(@"^\+?[0-9()\-\s]+$", ErrorMessage = "Phone number contains invalid characters.")]
        public string? ReferredByPhoneNumber { get; set; }

        /// <summary>
        /// Email address of the person who made the referral.
        /// </summary>
        [Display(Name = "Referrer Email")]
        [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [RegularExpression(@"^[A-Za-z0-9._+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$",
            ErrorMessage = "Email format is invalid.")]
        public string? ReferredByEmail { get; set; }

        /// <summary>
        /// Optional notes regarding the referral.
        /// </summary>
        [Display(Name = "Notes")]
        [StringLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters.")]
        public string? Notes { get; set; }

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
        /// Timestamp when the record was created (UTC recommended).
        /// </summary>
        [Display(Name = "Created At")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when the record was last updated (UTC recommended).
        /// </summary>
        [Display(Name = "Updated At")]
        [DataType(DataType.DateTime)]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Soft delete marker:
        /// - null = not deleted
        /// - non-null = deleted (excluded by global query filters in ApplicationDbContext)
        /// </summary>
        [Display(Name = "Deleted At")]
        [DataType(DataType.DateTime)]
        public DateTime? DeletedAt { get; set; }

        // -----------------------------------------------------------------
        // Navigation properties (EF Core relationships)
        // -----------------------------------------------------------------

        /// <summary>
        /// Navigation property for the client user.
        /// </summary>
        public DomainUser ClientUser { get; set; } = null!;

        /// <summary>
        /// Navigation property for the referring organization.
        /// </summary>
        public ReferringOrganization ReferringOrganization { get; set; } = null!;

        /// <summary>
        /// Navigation property to the DomainUser who created this referral.
        /// </summary>
        public DomainUser? CreatedByUser { get; set; }

        /// <summary>
        /// Navigation property to the DomainUser who last updated this referral.
        /// </summary>
        public DomainUser? UpdatedByUser { get; set; }
    }
}