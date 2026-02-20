using System.ComponentModel.DataAnnotations; // Added for [MaxLength]

namespace A_New_Hope.Models
{
    public class HouseholdMember
    {
        public ulong Id { get; set; }

        public ulong ClientUserId { get; set; }

        [MaxLength(200)] // Prevents LONGTEXT in MySQL and keeps data reasonable; also helps if you ever index/search names
        public string FullName { get; set; } = null!;

        public DateOnly? DateOfBirth { get; set; } // Keep if your provider supports DateOnly; otherwise switch to DateTime?

        // public byte? AgeYears { get; set; } // Recommend removing: age becomes stale; compute from DOB + "as of" date/event date

        public DateOnly? AgeAsOfDate { get; set; } // Keep ONLY if you truly need "age as of <date>" snapshots; otherwise remove too

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