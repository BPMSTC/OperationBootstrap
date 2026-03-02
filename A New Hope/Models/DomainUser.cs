using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models
{
    /// <summary>
    /// DomainUser
    /// ----------
    /// This is the application's primary "business user" record.
    ///
    /// IMPORTANT DISTINCTION IN YOUR PROJECT:
    /// - DomainUser: represents the real-world person/user in your business domain (clients, staff, admins).
    /// - ApplicationUser (Identity): represents a login account used for authentication/authorization.
    ///
    /// Not every DomainUser must have a login account (Identity user).
    /// For example:
    /// - Clients may exist as DomainUsers but not have logins.
    /// - Staff/Admin users typically have logins.
    ///
    /// Key concepts in this model:
    /// - Soft delete via DeletedAt (records are not physically removed).
    /// - Business classification via UserType (Client/Staff/Admin).
    /// - DefaultPreference provides a fallback preference when a per-item preference does not exist.
    /// - Audit fields track who created/updated a record and when (once wired to auth).
    ///
    /// Note:
    /// - In ApplicationDbContext, this entity is mapped to a table named "Users".
    ///   This is separate from Identity’s AspNetUsers table.
    /// </summary>
    public class DomainUser
    {
        /// <summary>
        /// Primary key for the DomainUser record.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Contact email address for the user.
        /// - In your domain, this is not inherently "login credentials" (Identity handles that).
        /// - Email is typically unique and indexed (configured in ApplicationDbContext).
        /// </summary>
        [MaxLength(254)] // Keeps the column as VARCHAR and supports indexing/uniqueness cleanly (Email is commonly capped at 254)
        public string Email { get; set; } = null!; // Contact email (not login credentials)

        // -----------------------------------------------------------------
        // Profile / contact information
        // -----------------------------------------------------------------

        /// <summary>
        /// Optional phone number for the user.
        /// MaxLength prevents LONGTEXT and keeps the column predictable for MySQL.
        /// </summary>
        [MaxLength(25)]
        public string? PhoneNumber { get; set; } // Avoid LONGTEXT and keep data reasonable

        /// <summary>
        /// Optional first name.
        /// </summary>
        [MaxLength(100)]
        public string? FirstName { get; set; }

        /// <summary>
        /// Optional last name.
        /// </summary>
        [MaxLength(100)]
        public string? LastName { get; set; }

        /// <summary>
        /// Optional address line 1 (street address).
        /// </summary>
        [MaxLength(200)]
        public string? AddressLine1 { get; set; }

        /// <summary>
        /// Optional address line 2 (unit/apartment/suite).
        /// </summary>
        [MaxLength(200)]
        public string? AddressLine2 { get; set; }

        /// <summary>
        /// Optional city portion of address.
        /// </summary>
        [MaxLength(100)]
        public string? City { get; set; }

        /// <summary>
        /// Optional state/region portion of address.
        /// - If you were strictly US-only, you might cap at 2 for state abbreviations.
        /// - 50 allows broader region formats if needed.
        /// </summary>
        [MaxLength(50)]
        public string? State { get; set; } // Use 2 if strictly US states; 50 allows other regions

        /// <summary>
        /// Optional postal/zip code.
        /// </summary>
        [MaxLength(20)]
        public string? PostalCode { get; set; }

        /// <summary>
        /// Optional date of birth.
        /// Using DateOnly indicates you care about date without time-of-day.
        /// (Whether this maps cleanly depends on your EF provider / database support.)
        /// </summary>
        public DateOnly? DateOfBirth { get; set; } // Fine if your MySQL EF provider supports it; otherwise switch to DateTime?

        // -----------------------------------------------------------------
        // Business rules / classification
        // -----------------------------------------------------------------

        /// <summary>
        /// Default preference behavior (Always / Ask / Never) used when no per-item preference exists.
        /// Storing this as an enum prevents invalid values.
        /// </summary>
        public PreferenceOption DefaultPreference { get; set; } = PreferenceOption.Ask; // Enum prevents invalid values like "aks" or "maybe"

        /// <summary>
        /// Business-level user type classification:
        /// - Client
        /// - Staff
        /// - Admin
        /// This is used to drive access rules and UI behavior, and is also mapped to Identity roles in your controllers.
        /// </summary>
        public UserType UserType { get; set; } = UserType.Client;

        /// <summary>
        /// Whether the user is currently active in the system.
        /// Often used to enable/disable actions, and can be synced to Identity lockout when a login exists.
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
        /// Navigation to the DomainUser who created this record (self-referencing audit relationship).
        /// </summary>
        public DomainUser? CreatedByUser { get; set; }

        /// <summary>
        /// Navigation to the DomainUser who last updated this record (self-referencing audit relationship).
        /// </summary>
        public DomainUser? UpdatedByUser { get; set; }

        /// <summary>
        /// Optional 1:1 navigation to ClientProfile.
        /// - Present when this DomainUser represents a client and has a profile.
        /// - Null for staff/admin users (or clients that haven't had a profile created yet).
        /// </summary>
        public ClientProfile? ClientProfile { get; set; }
    }//a change so that my commit will store so i can push, delete this later
}