using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models
{
    /// <summary>
    /// ClientIncome
    /// ------------
    /// Stores one monthly income source for a client profile.
    ///
    /// Relationship:
    /// - Many ClientIncome records can belong to one ClientProfile.
    /// - Linked to ClientProfile through ClientProfileUserId, which points to ClientProfile.UserId.
    ///
    /// Purpose:
    /// - Allows income to be tracked by category instead of one total field.
    /// - Supports separate benefit/income types such as Social Security, child support, disability, etc.
    /// - Allows overall monthly income to be calculated from active income rows.
    ///
    /// Soft delete:
    /// - DeletedAt marks the record as deleted without removing it from the database.
    ///
    /// Audit fields:
    /// - CreatedByUserId / UpdatedByUserId store the DomainUser responsible for changes.
    /// - CreatedAt / UpdatedAt store timestamps (UTC recommended).
    /// </summary>
    public class ClientIncome
    {
        /// <summary>
        /// Primary key for the income record.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Foreign key to ClientProfile.UserId.
        /// Since ClientProfile uses UserId as its primary key, this links the income row to that profile.
        /// </summary>
        public ulong ClientProfileUserId { get; set; }

        /// <summary>
        /// Type/category of income.
        /// Examples: Employment, SocialSecurity, ChildSupport, Disability.
        /// </summary>
        [Required(ErrorMessage = "Income type is required.")]
        public IncomeType IncomeType { get; set; }

        /// <summary>
        /// Monthly amount for this income source.
        /// Precision/scale is configured in ApplicationDbContext.
        /// </summary>
        [Range(0, 9999999999.99, ErrorMessage = "Monthly amount must be 0 or greater.")]
        public decimal MonthlyAmount { get; set; }

        /// <summary>
        /// Indicates whether this income source is currently active.
        /// Totals can be calculated using only active income rows.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Optional notes about this income source.
        /// </summary>
        [StringLength(250, ErrorMessage = "Notes cannot exceed 250 characters.")]
        public string? Notes { get; set; }

        /// <summary>
        /// Audit: DomainUser who created this record.
        /// </summary>
        public ulong? CreatedByUserId { get; set; }

        /// <summary>
        /// Audit: DomainUser who last updated this record.
        /// </summary>
        public ulong? UpdatedByUserId { get; set; }

        /// <summary>
        /// Timestamp when the record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when the record was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Soft delete marker.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        // -----------------------------------------------------------------
        // Navigation properties
        // -----------------------------------------------------------------

        /// <summary>
        /// Navigation to the ClientProfile this income row belongs to.
        /// </summary>
        public ClientProfile ClientProfile { get; set; } = null!;

        /// <summary>
        /// Navigation to the DomainUser who created this income row.
        /// </summary>
        public DomainUser? CreatedByUser { get; set; }

        /// <summary>
        /// Navigation to the DomainUser who last updated this income row.
        /// </summary>
        public DomainUser? UpdatedByUser { get; set; }

    }
}