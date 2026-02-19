namespace A_New_Hope.Models
{
    public class ClientProfile
    {
        public ulong UserId { get; set; }

        public string? EmploymentStatus { get; set; }
        public decimal? EarnedIncomeMonthly { get; set; }
        public bool IsUnhoused { get; set; } = false;

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public User User { get; set; } = null!;
        public User? CreatedByUser { get; set; }
        public User? UpdatedByUser { get; set; }

        public ICollection<HouseholdMember> HouseholdMembers { get; set; } = new List<HouseholdMember>();
    }
}
