namespace A_New_Hope.Models
{
    public class HouseholdMember
    {
        public ulong Id { get; set; }

        public ulong ClientUserId { get; set; }

        public string FullName { get; set; } = null!;
        public DateOnly? DateOfBirth { get; set; }
        public byte? AgeYears { get; set; }
        public DateOnly? AgeAsOfDate { get; set; }

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public User ClientUser { get; set; } = null!;
        public User? CreatedByUser { get; set; }
        public User? UpdatedByUser { get; set; }
    }
}
