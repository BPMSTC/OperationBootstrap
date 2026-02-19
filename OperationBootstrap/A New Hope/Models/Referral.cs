namespace A_New_Hope.Models
{
    public class Referral
    {
        public ulong Id { get; set; }

        public ulong ClientUserId { get; set; }
        public ulong ReferringOrganizationId { get; set; }

        public DateOnly ReferredOn { get; set; }
        public string Status { get; set; } = null!;

        public DateOnly? ValidFrom { get; set; }
        public DateOnly? ValidTo { get; set; }

        public string? ReferredByName { get; set; }
        public string? ReferredByPhoneNumber { get; set; }
        public string? ReferredByEmail { get; set; }

        public string? Notes { get; set; }

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
