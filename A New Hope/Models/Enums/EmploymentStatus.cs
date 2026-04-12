using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models.Enums
{
    public enum EmploymentStatus
    {
        [Display(Name = "Not Specified")]
        NotSpecified = 0,

        [Display(Name = "Full Time")]
        FullTime = 1,

        [Display(Name = "Part Time")]
        PartTime = 2,

        [Display(Name = "Unemployed")]
        Unemployed = 3,

        [Display(Name = "Self-Employed")]
        SelfEmployed = 4,

        [Display(Name = "Retired")]
        Retired = 5,

        [Display(Name = "Student")]
        Student = 6,

        [Display(Name = "Disabled")]
        Disabled = 7
    }
}
