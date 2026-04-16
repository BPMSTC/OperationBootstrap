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
        [MaxLength(254)]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string? Email { get; set; }

        // -----------------------------------------------------------------
        // Profile / contact information
        // -----------------------------------------------------------------

        /// <summary>
        /// Optional phone number for the user.
        /// Added Regex for common characters: digits, spaces, parentheses, hyphens, plus sign.
        /// </summary>
        [MaxLength(25)]
        [RegularExpression(@"^(\+1\s?)?(\([0-9]{3}\)|[0-9]{3})[\s.-]?[0-9]{3}[\s.-]?[0-9]{4}$",
            ErrorMessage = "Enter a valid phone number.")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Optional first name.
        /// </summary>
        [Required]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Invalid characters in name.")]
        public string FirstName { get; set; }

        /// <summary>
        /// Optional last name.
        /// </summary>
        [Required]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Invalid characters in name.")]
        public string LastName { get; set; }

        /// <summary>
        /// Optional address line 1.
        /// </summary>
        [MaxLength(200)]
        public string? AddressLine1 { get; set; }
        /// <summary>
        /// Optional address line 2.
        /// </summary>
        [MaxLength(200)]
        public string? AddressLine2 { get; set; }

        /// <summary>
        /// Optional city.
        /// </summary>
        [MaxLength(100)]
        public string? City { get; set; }

        /// <summary>
        /// Optional state/region portion of address.
        /// </summary>
        [StringLength(2, ErrorMessage = "State must be a 2-letter abbreviation.")]
        [RegularExpression(@"^[A-Za-z]{2}$", ErrorMessage = "State must be a valid 2-letter abbreviation.")]
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