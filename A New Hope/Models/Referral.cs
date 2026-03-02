using System.ComponentModel.DataAnnotations; // Added for [MaxLength], [EmailAddress], [Phone]

namespace A_New_Hope.Models
{
    /// <summary>
    /// Referral
    /// --------
    /// Represents a referral record that links a client (DomainUser) to a ReferringOrganization.
    ///
    /// Typical real-world meaning:
    /// - An outside organization (clinic/agency/etc.) refers a client to Operation Bootstrap.
    /// - The referral can move through a lifecycle (Pending -> Approved/Denied/etc.).
    /// - The referral may have a validity window (ValidFrom/ValidTo).
    /// - The record can store referrer contact details and notes for staff.
    ///
    /// Key relationships:
    /// - ClientUserId -> DomainUser (the client being referred)
    /// - ReferringOrganizationId -> ReferringOrganization (who referred the client)
    ///
    /// Soft delete:
    /// - DeletedAt marks the record as deleted without physically removing it.
    /// - ApplicationDbContext applies a query filter to exclude deleted referrals by default.
    ///
    /// Audit fields:
    /// - CreatedByUserId / UpdatedByUserId store the DomainUser responsible (once auth is wired).
    /// - CreatedAt / UpdatedAt store timestamps (UTC recommended).
    /// </summary>
    public class Referral
    {
        /// <summary>
        /// Primary key for the referral record.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Foreign key to the client DomainUser being referred.
        /// </summary>
        public ulong ClientUserId { get; set; }

        /// <summary>
        /// Foreign key to the organization that referred the client.
        /// </summary>
        public ulong ReferringOrganizationId { get; set; }

        /// <summary>
        /// Date/time the referral was made/received.
        /// Stored as DateTime for broad provider compatibility and accurate timeline tracking.
        /// </summary>
        public DateTime ReferredOn { get; set; } // Good fit semantically; keep if your MySQL provider supports DateOnly well

        /// <summary>
        /// Current status of the referral (Pending/Approved/Denied/etc.).
        /// Using an enum prevents typos and keeps logic consistent across the app.
        /// </summary>
        public ReferralStatus Status { get; set; } = ReferralStatus.Pending;

        /// <summary>
        /// Optional validity start date/time for the referral.
        /// </summary>
        public DateTime? ValidFrom { get; set; }

        /// <summary>
        /// Optional validity end date/time for the referral.
        /// When set, the referral may be considered expired after this date.
        /// </summary>
        public DateTime? ValidTo { get; set; }

        /// <summary>
        /// Optional name of the person who made the referral at the referring organization.
        /// </summary>
        [MaxLength(200)] // Prevents LONGTEXT and keeps referrer names within a practical length
        public string? ReferredByName { get; set; }

        /// <summary>
        /// Optional phone number for the referrer.
        /// [Phone] is MVC/UI validation (not a DB constraint) and helps catch obviously invalid input.
        /// </summary>
        [MaxLength(25)] // Prevents LONGTEXT; allows punctuation/extensions while staying practical
        [Phone] // MVC-level validation (not a DB constraint, but useful for forms)
        public string? ReferredByPhoneNumber { get; set; }

        /// <summary>
        /// Optional email for the referrer.
        /// [EmailAddress] is MVC/UI validation (not a DB constraint) and helps catch invalid input.
        /// </summary>
        [MaxLength(254)] // Standard practical max for email addresses
        [EmailAddress] // MVC-level validation for cleaner user input
        public string? ReferredByEmail { get; set; }

        /// <summary>
        /// Optional free-form notes about the referral.
        /// MaxLength(2000) keeps storage predictable while still allowing meaningful detail.
        /// </summary>
        [MaxLength(2000)]
        public string? Notes { get; set; } // Leaving uncapped is okay if you want large free-form notes (TEXT/LONGTEXT in MySQL)

        /// <summary>
        /// Audit: DomainUser who created this referral record (nullable until auth is wired).
        /// </summary>
        public ulong? CreatedByUserId { get; set; }

        /// <summary>
        /// Audit: DomainUser who last updated this referral record (nullable until auth is wired).
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
        /// Required navigation to the client DomainUser associated with this referral.
        /// </summary>
        public DomainUser ClientUser { get; set; } = null!;

        /// <summary>
        /// Required navigation to the referring organization associated with this referral.
        /// </summary>
        public ReferringOrganization ReferringOrganization { get; set; } = null!;

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