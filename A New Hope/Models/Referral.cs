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
    /// - Tracks referral dates, validity, status, and notes.
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
        /// Optional notes regarding the referral.
        /// </summary>
        [Display(Name = "Notes")]
        [StringLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters.")]
        public string? Notes { get; set; }

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        [Display(Name = "Created At")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Updated At")]
        [DataType(DataType.DateTime)]
        public DateTime UpdatedAt { get; set; }

        [Display(Name = "Deleted At")]
        [DataType(DataType.DateTime)]
        public DateTime? DeletedAt { get; set; }

        public DomainUser ClientUser { get; set; } = null!;
        public ReferringOrganization ReferringOrganization { get; set; } = null!;
        public DomainUser? CreatedByUser { get; set; }
        public DomainUser? UpdatedByUser { get; set; }
    }
}