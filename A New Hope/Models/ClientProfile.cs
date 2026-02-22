using System.ComponentModel.DataAnnotations; // Added for [MaxLength]

namespace A_New_Hope.Models
{
    public class ClientProfile
    {
        public ulong UserId { get; set; }

        [MaxLength(50)] // Keeps MySQL from using LONGTEXT and prevents overly-long values; also improves indexing if you ever filter by status
        public string? EmploymentStatus { get; set; }

        public decimal? EarnedIncomeMonthly { get; set; } // Precision should stay configured in DbContext (you already set 10,2 there)
        public bool IsUnhoused { get; set; } = false; // Keep as bool unless you need an "unknown" state (then use bool?)

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public User User { get; set; } = null!;
        public User? CreatedByUser { get; set; }
        public User? UpdatedByUser { get; set; }

        // public ICollection<HouseholdMember> HouseholdMembers { get; set; } = new List<HouseholdMember>();
        // Keep this only if HouseholdMember has a FK that aligns to UserId (e.g., HouseholdMember.ClientUserId == ClientProfile.UserId).
        // Otherwise remove for now and re-add once HouseholdMember is finalized to avoid ambiguous navigation.
    }
}