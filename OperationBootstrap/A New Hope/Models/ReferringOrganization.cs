namespace A_New_Hope.Models
{
    public class ReferringOrganization
    {
        public ulong Id { get; set; }

        public string Name { get; set; } = null!;
        public string? Type { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? PrimaryContactName { get; set; }
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<Referral> Referrals { get; set; } = new List<Referral>();
    }
}
