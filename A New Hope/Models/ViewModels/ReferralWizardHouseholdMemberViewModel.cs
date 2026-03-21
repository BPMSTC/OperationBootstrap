using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models.ViewModels
{
    /// <summary>
    /// View model for one optional household member row
    /// in Referral Wizard Step 1.
    /// </summary>
    public class ReferralWizardHouseholdMemberViewModel
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

        [Display(Name = "Age As Of Date")]
        [DataType(DataType.Date)]
        public DateTime? AgeAsOfDate { get; set; }

        public bool HasStarted =>
            !string.IsNullOrWhiteSpace(FirstName) ||
            !string.IsNullOrWhiteSpace(LastName) ||
            DateOfBirth.HasValue ||
            AgeAsOfDate.HasValue;
    }
}