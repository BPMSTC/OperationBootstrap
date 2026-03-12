using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models
{
    /// <summary>
    /// DomainUser
    /// ----------
    /// This is the application's primary "business user" record.
    /// Front-end validation attributes added where reasonable for forms.
    /// </summary>
    public class DomainUser
    {
        /// <summary>
        /// Primary key for the DomainUser record.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Contact email address for the user.
        /// Front-end validation ensures proper email format and length.
        /// </summary>
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(254)]
        public string Email { get; set; } = null!;

        // -----------------------------------------------------------------
        // Profile / contact information
        // -----------------------------------------------------------------

        /// <summary>
        /// Optional phone number for the user.
        /// </summary>
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [MaxLength(25)]
        public string? PhoneNumber { get; set; }

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
        /// </summary>
        [MaxLength(50)]
        public string? State { get; set; }

        /// <summary>
        /// Optional postal/zip code.
        /// </summary>
        [MaxLength(20)]
        public string? PostalCode { get; set; }

        /// <summary>
        /// Optional date of birth.
        /// </summary>
        public DateOnly? DateOfBirth { get; set; }

        // -----------------------------------------------------------------
        // Business rules / classification
        // -----------------------------------------------------------------

        /// <summary>
        /// Default preference behavior (Always / Ask / Never).
        /// </summary>
        public PreferenceOption DefaultPreference { get; set; } = PreferenceOption.Ask;

        /// <summary>
        /// Business-level user type classification.
        /// </summary>
        public UserType UserType { get; set; } = UserType.Client;

        /// <summary>
        /// Whether the user is currently active in the system.
        /// </summary>
        public bool IsActive { get; set; } = true;

        // -----------------------------------------------------------------
        // Audit fields
        // -----------------------------------------------------------------

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // -----------------------------------------------------------------
        // Navigation properties (EF Core relationships)
        // -----------------------------------------------------------------

        public DomainUser? CreatedByUser { get; set; }
        public DomainUser? UpdatedByUser { get; set; }
        public ClientProfile? ClientProfile { get; set; }
    }
}