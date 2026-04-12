using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models.Inputs
{
    /// <summary>
    /// Household member entry row captured during Referral Entry.
    /// </summary>
    public class HouseholdMemberEntryInput
    {
        [Display(Name = "First Name")]
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        [MaxLength(100)]
        public string? LastName { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Approximate Age")]
        [Range(0, 120)]
        public int? ApproximateAge { get; set; }

        public bool HasStarted =>
            !string.IsNullOrWhiteSpace(FirstName) ||
            !string.IsNullOrWhiteSpace(LastName) ||
            DateOfBirth.HasValue ||
            ApproximateAge.HasValue;
    }
}
