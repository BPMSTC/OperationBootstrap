using System.ComponentModel.DataAnnotations; // Added for [MaxLength], [EmailAddress], [Phone], [RegularExpression]

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
        /// Added Regex for stricter validation of allowed email characters.
        /// </summary>
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(254)]
        [RegularExpression(@"^[A-Za-z0-9._+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$",
            ErrorMessage = "Email format is invalid.")]
        public string Email { get; set; } = null!;

        // -----------------------------------------------------------------
        // Profile / contact information
        // -----------------------------------------------------------------

        /// <summary>
        /// Optional phone number for the user.
        /// Added Regex for common characters: digits, spaces, parentheses, hyphens, plus sign.
        /// </summary>
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [MaxLength(25)]
        [RegularExpression(@"^\+?[0-9()\-\s]+$", ErrorMessage = "Phone number contains invalid characters.")]
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
        /// Optional address line 1.
        /// </summary>
        [MaxLength(200)]
        [RegularExpression(@"^[A-Za-z0-9\s#.,'\-]*$", ErrorMessage = "Address contains invalid characters.")]
        public string? AddressLine1 { get; set; } // Avoid LONGTEXT

        /// <summary>
        /// Optional address line 2.
        /// </summary>
        [MaxLength(200)]
        [RegularExpression(@"^[A-Za-z0-9\s#.,'\-]*$", ErrorMessage = "Address contains invalid characters.")]
        public string? AddressLine2 { get; set; } // Avoid LONGTEXT

        /// <summary>
        /// Optional city.
        /// </summary>
        [MaxLength(100)]
        [RegularExpression(@"^[A-Za-z\s.\-']*$", ErrorMessage = "City contains invalid characters.")]
        public string? City { get; set; } // Avoid LONGTEXT

        /// <summary>
        /// Optional state/region portion of address.
        /// </summary>
        [MaxLength(50)]
        [RegularExpression(@"^[A-Za-z\s.\-']*$", ErrorMessage = "State contains invalid characters.")]
        public string? State { get; set; }

        /// <summary>
        /// Optional postal/zip code.
        /// Accepts 5-digit ZIP or ZIP+4 (US) format.
        /// </summary>
        [MaxLength(20)]
        [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Enter a valid US ZIP code.")]
        public string? PostalCode { get; set; }

        /// <summary>
        /// Optional date of birth.
        /// </summary>
        public DateTime? DateOfBirth { get; set; }

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