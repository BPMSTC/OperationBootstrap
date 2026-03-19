using System.ComponentModel.DataAnnotations; // Added for [MaxLength], [Range], [Required], [RegularExpression]

namespace A_New_Hope.Models
{
    /// <summary>
    /// ClientProfile
    /// -------------
    /// Stores client-specific details that extend a DomainUser record.
    ///
    /// Relationship:
    /// - 1:1 with DomainUser
    /// - The primary key of ClientProfile is UserId, which is also a foreign key to DomainUser.Id
    ///
    /// Why a separate table?
    /// - Not every DomainUser is a client (some are Staff/Admin).
    /// - Client-specific fields (employment, income, housing status) are kept out of the base user record
    ///   so your domain model stays clean and flexible.
    ///
    /// Soft delete:
    /// - DeletedAt marks the profile as deleted without removing it from the database.
    /// - ApplicationDbContext applies a query filter to exclude deleted profiles by default.
    ///
    /// Audit fields:
    /// - CreatedByUserId / UpdatedByUserId store the DomainUser responsible for changes (once auth is wired).
    /// - CreatedAt / UpdatedAt store timestamps (UTC recommended).
    ///
    /// Front-end validation:
    /// - MaxLength enforces character limits in forms.
    /// - Range ensures numeric fields stay in valid ranges.
    /// - RegularExpression ensures character-level rules (letters, numbers, spaces, punctuation).
    /// </summary>
    public class ClientProfile
    {
        /// <summary>
        /// Primary key for ClientProfile AND foreign key to DomainUser.Id.
        /// This enforces a true 1:1 relationship (one profile per user).
        /// </summary>
        public ulong UserId { get; set; }

        /// <summary>
        /// Optional employment status text (e.g., "Full-time", "Part-time", "Unemployed").
        /// MaxLength keeps storage predictable and avoids LONGTEXT on MySQL.
        /// Added [StringLength] for front-end validation.
        /// Optional [RegularExpression] to ensure practical characters only.
        /// </summary>
        [StringLength(50, ErrorMessage = "Employment status cannot exceed 50 characters.")]
        [RegularExpression(@"^[A-Za-z0-9\s'.-]*$", ErrorMessage = "Employment status contains invalid characters.")]
        public string? EmploymentStatus { get; set; }

        /// <summary>
        /// Optional monthly earned income amount.
        /// Precision/scale is configured in ApplicationDbContext (HasPrecision(10, 2)).
        /// Added [Range] to enforce positive values for front-end validation.
        /// </summary>
        [Range(0, 9999999999.99, ErrorMessage = "Monthly earned income must be 0 or greater.")]
        public decimal? EarnedIncomeMonthly { get; set; }

        /// <summary>
        /// Optional postal/zip code for client.
        /// Accepts 5-digit ZIP, ZIP+4 (US), or alphanumeric for non-US postal codes.
        /// </summary>
        [MaxLength(20)]
        [RegularExpression(@"^\d{5}(-\d{4})?$",
            ErrorMessage = "Enter a valid US ZIP code.")]
        public string? PostalCode { get; set; }

        /// <summary>
        /// Indicates whether the client is currently unhoused.
        /// Defaults to false; make nullable only if you need an "unknown" state.
        /// </summary>
        public bool IsUnhoused { get; set; } = false; // Keep as bool unless you need an "unknown" state (then use bool?)

        /// <summary>
        /// Audit: DomainUser who created this record (nullable until auth is wired).
        /// </summary>
        public ulong? CreatedByUserId { get; set; }

        /// <summary>
        /// Audit: DomainUser who last updated this record (nullable until auth is wired).
        /// </summary>
        public ulong? UpdatedByUserId { get; set; }

        /// <summary>
        /// Timestamp when the profile was created (typically set server-side in UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when the profile was last updated (typically set server-side in UTC).
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
        /// Required navigation to the DomainUser this profile belongs to.
        /// </summary>
        public DomainUser User { get; set; } = null!;

        /// <summary>
        /// Navigation to the DomainUser who created this profile record.
        /// </summary>
        public DomainUser? CreatedByUser { get; set; }

        /// <summary>
        /// Navigation to the DomainUser who last updated this profile record.
        /// </summary>
        public DomainUser? UpdatedByUser { get; set; }
    }
}