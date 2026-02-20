using System.ComponentModel.DataAnnotations; // Added for [MaxLength]

namespace A_New_Hope.Models
{
    public class HouseholdMember
    {
        public ulong Id { get; set; }

        public ulong ClientUserId { get; set; }

        [MaxLength(100)] // Split from FullName so first name can be stored/searched/sorted independently
        public string FirstName { get; set; } = null!;

        [MaxLength(100)] // Split from FullName so last name can be stored/searched/sorted independently
        public string LastName { get; set; } = null!;

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