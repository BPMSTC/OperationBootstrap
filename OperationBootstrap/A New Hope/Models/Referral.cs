using System.ComponentModel.DataAnnotations; // Added for [MaxLength], [EmailAddress], [Phone]

namespace A_New_Hope.Models
{
    public class Referral
    {
        public ulong Id { get; set; }

        public ulong ClientUserId { get; set; }
        public ulong ReferringOrganizationId { get; set; }

        public DateOnly ReferredOn { get; set; } // Good fit semantically; keep if your MySQL provider supports DateOnly well

        public ReferralStatus Status { get; set; } = ReferralStatus.Pending;
        // Changed from string -> enum to prevent typos/invalid values and keep status logic consistent

        public DateOnly? ValidFrom { get; set; }
        public DateOnly? ValidTo { get; set; }

        [MaxLength(200)] // Prevents LONGTEXT and keeps referrer names within a practical length
        public string? ReferredByName { get; set; }

        [MaxLength(25)] // Prevents LONGTEXT; allows punctuation/extensions while staying practical
        [Phone] // MVC-level validation (not a DB constraint, but useful for forms)
        public string? ReferredByPhoneNumber { get; set; }

        [MaxLength(254)] // Standard practical max for email addresses
        [EmailAddress] // MVC-level validation for cleaner user input
        public string? ReferredByEmail { get; set; }

        public string? Notes { get; set; } // Leaving uncapped is okay if you want large free-form notes (TEXT/LONGTEXT in MySQL)

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public User ClientUser { get; set; } = null!;
        public ReferringOrganization ReferringOrganization { get; set; } = null!;
        public User? CreatedByUser { get; set; }
        public User? UpdatedByUser { get; set; }
    }
}