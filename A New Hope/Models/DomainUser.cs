using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models
{
    public class DomainUser
    {
        public ulong Id { get; set; }

        [MaxLength(254)] // Keeps the column as VARCHAR and supports indexing/uniqueness cleanly (Email is commonly capped at 254)
        public string Email { get; set; } = null!; // Contact email (not login credentials)

        // Profile
        [MaxLength(25)] public string? PhoneNumber { get; set; } // Avoid LONGTEXT and keep data reasonable
        [MaxLength(100)] public string? FirstName { get; set; }
        [MaxLength(100)] public string? LastName { get; set; }

        [MaxLength(200)] public string? AddressLine1 { get; set; }
        [MaxLength(200)] public string? AddressLine2 { get; set; }
        [MaxLength(100)] public string? City { get; set; }
        [MaxLength(50)] public string? State { get; set; } // Use 2 if strictly US states; 50 allows other regions
        [MaxLength(20)] public string? PostalCode { get; set; }

        public DateOnly? DateOfBirth { get; set; } // Fine if your MySQL EF provider supports it; otherwise switch to DateTime?

        public PreferenceOption DefaultPreference { get; set; } = PreferenceOption.Ask; // Enum prevents invalid values like "aks" or "maybe"
        public UserType UserType { get; set; } = UserType.Client;
        public bool IsActive { get; set; } = true;

        // Audit
        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public DomainUser? CreatedByUser { get; set; }
        public DomainUser? UpdatedByUser { get; set; }

        public ClientProfile? ClientProfile { get; set; }
    }
}