using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models
{
    /// <summary>
    /// HouseholdMember
    /// ---------------
    /// Represents an additional person in a client's household.
    ///
    /// Relationship:
    /// - Each HouseholdMember belongs to a client DomainUser via ClientUserId.
    ///
    /// Notes:
    /// - FirstName/LastName are stored separately to support searching/sorting/filtering.
    /// - Soft delete is supported via DeletedAt.
    /// - Audit fields track who created/updated the record (once auth is wired).
    /// </summary>
    public class HouseholdMember
    {
        /// <summary>
        /// Primary key for the household member record.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Foreign key to the client DomainUser who this household member is associated with.
        /// </summary>
        public ulong ClientUserId { get; set; }

        /// <summary>
        /// Household member first name (required).
        /// </summary>
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
        [RegularExpression(@"^[A-Za-z\s'-]+$", ErrorMessage = "First name can only contain letters, spaces, apostrophes, and hyphens.")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Household member last name (required).
        /// </summary>
        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
        [RegularExpression(@"^[A-Za-z\s'-]+$", ErrorMessage = "Last name can only contain letters, spaces, apostrophes, and hyphens.")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Optional date of birth.
        /// Using DateTime keeps provider compatibility (DateOnly support varies by provider).
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        /// <summary>
        /// Optional snapshot date for "age as of" calculations.
        /// Keep only if you truly need to store historical age snapshots; otherwise age can be derived from DOB.
        /// </summary>
        [Range(0, 120, ErrorMessage = "Approximate age must be between 0 and 120.")]
        public int? ApproximateAge { get; set; }

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
        /// Navigation to the client DomainUser who owns this household member.
        /// </summary>
        public DomainUser ClientUser { get; set; } = null!;

        /// <summary>
        /// Navigation to the DomainUser who created this record.
        /// </summary>
        public DomainUser? CreatedByUser { get; set; }

        /// <summary>
        /// Navigation to the DomainUser who last updated this record.
        /// </summary>
        public DomainUser? UpdatedByUser { get; set; }
    }
}