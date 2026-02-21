using System.ComponentModel.DataAnnotations; // Added for [MaxLength], [EmailAddress], [Phone]

namespace A_New_Hope.Models
{
    public class ReferringOrganization
    {
        public ulong Id { get; set; }

        [MaxLength(200)] // Prevents LONGTEXT in MySQL; keeps org names index-friendly if you add a unique/search index later
        public string Name { get; set; } = null!;

        [MaxLength(100)] // Keeps this as VARCHAR and prevents oversized values
        public string? Type { get; set; }

        [MaxLength(25)] // Prevents LONGTEXT; allows punctuation/extensions
        [Phone] // MVC validation for cleaner input
        public string? PhoneNumber { get; set; }

        [MaxLength(254)] // Practical max for email addresses
        [EmailAddress] // MVC validation
        public string? Email { get; set; }

        [MaxLength(200)] public string? AddressLine1 { get; set; } // Avoid LONGTEXT
        [MaxLength(200)] public string? AddressLine2 { get; set; } // Avoid LONGTEXT
        [MaxLength(100)] public string? City { get; set; }         // Avoid LONGTEXT
        [MaxLength(50)] public string? State { get; set; }        // Use 2 if strictly US-only
        [MaxLength(20)] public string? PostalCode { get; set; }   // Supports ZIP/ZIP+4 and non-US postal formats

        [MaxLength(200)] // Keeps contact names reasonable and indexable if needed
        public string? PrimaryContactName { get; set; }

        public string? Notes { get; set; } // Leave uncapped if you want free-form notes (TEXT/LONGTEXT is okay here)

        public bool IsActive { get; set; } = true;

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<Referral> Referrals { get; set; } = new List<Referral>();

        public User? CreatedByUser { get; set; } // Added to match CreatedByUserId and support Include() in admin/audit views
        public User? UpdatedByUser { get; set; } // Added to match UpdatedByUserId and support Include()
    }
}