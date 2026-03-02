using System.ComponentModel.DataAnnotations; // Added for [MaxLength]

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
        /// </summary>
        [MaxLength(50)] // Keeps MySQL from using LONGTEXT and prevents overly-long values; also improves indexing if you ever filter by status
        public string? EmploymentStatus { get; set; }

        /// <summary>
        /// Optional monthly earned income amount.
        /// Precision/scale is configured in ApplicationDbContext (HasPrecision(10, 2)).
        /// </summary>
        public decimal? EarnedIncomeMonthly { get; set; } // Precision should stay configured in DbContext (you already set 10,2 there)

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
        public DateTime? DeletedAt { get; set; }   // <-- add this

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