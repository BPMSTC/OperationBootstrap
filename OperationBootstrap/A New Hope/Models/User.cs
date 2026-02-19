namespace A_New_Hope.Models
{
    public class User
    {
        public ulong Id { get; set; }

        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? RememberToken { get; set; }
        public DateTime? EmailVerifiedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Profile
        public string? PhoneNumber { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Name { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string DefaultPreference { get; set; } = "ask";
        public bool IsActive { get; set; } = true;

        // Audit
        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public User? CreatedByUser { get; set; }
        public User? UpdatedByUser { get; set; }

        public ICollection<RoleUser> RoleUsers { get; set; } = new List<RoleUser>();
        public ClientProfile? ClientProfile { get; set; }
    }
}
