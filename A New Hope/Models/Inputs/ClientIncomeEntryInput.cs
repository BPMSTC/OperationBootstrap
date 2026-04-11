using System.ComponentModel.DataAnnotations;
using A_New_Hope.Models;

namespace A_New_Hope.Models.Inputs
{
    /// <summary>
    /// Income entry data captured during Referral Entry.
    /// </summary>
    public class ClientIncomeEntryInput
    {
        [Display(Name = "Income Type")]
        public IncomeType? IncomeType { get; set; }

        [Display(Name = "Monthly Amount")]
        [Range(0, 9999999999.99, ErrorMessage = "Monthly amount must be 0 or greater.")]
        public decimal? MonthlyAmount { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Notes")]
        [MaxLength(250, ErrorMessage = "Notes cannot exceed 250 characters.")]
        public string? Notes { get; set; }

        public bool HasStarted =>
            IncomeType.HasValue ||
            MonthlyAmount.HasValue ||
            !string.IsNullOrWhiteSpace(Notes) ||
            !IsActive;
    }
}