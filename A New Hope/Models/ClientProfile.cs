using System.ComponentModel.DataAnnotations;

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
    /// - Client-specific fields (employment, housing status, income records) are kept out of the base user record
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
        [StringLength(50, ErrorMessage = "Employment status cannot exceed 50 characters.")]
        [RegularExpression(@"^[A-Za-z0-9\s'.-]*$", ErrorMessage = "Employment status contains invalid characters.")]
        public string? EmploymentStatus { get; set; }

        /// <summary>
        /// Indicates whether the client is currently unhoused.
        /// Defaults to false; make nullable only if you need an "unknown" state.
        /// </summary>
        public bool IsUnhoused { get; set; } = false;

        /// <summary>
        /// Audit: DomainUser who created this record.
        /// </summary>
        public ulong? CreatedByUserId { get; set; }

        /// <summary>
        /// Audit: DomainUser who last updated this record.
        /// </summary>
        public ulong? UpdatedByUserId { get; set; }

        /// <summary>
        /// Timestamp when the profile was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when the profile was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Soft delete marker:
        /// - null = not deleted
        /// - non-null = deleted
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        // -----------------------------------------------------------------
        // Navigation properties
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

        /// <summary>
        /// Income records associated with this client profile.
        /// </summary>
        public ICollection<ClientIncome> ClientIncomes { get; set; } = new List<ClientIncome>();
    }
}