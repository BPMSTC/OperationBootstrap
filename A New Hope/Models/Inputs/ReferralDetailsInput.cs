using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models.Inputs
{
    /// <summary>
    /// Referral-specific data captured during Referral Entry.
    /// </summary>
    public class ReferralDetailsInput
    {
        [Required(ErrorMessage = "Referral date is required.")]
        [Display(Name = "Referral Date")]
        [DataType(DataType.Date)]
        public DateTime? ReferredOn { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Referral status is required.")]
        [Display(Name = "Status")]
        public ReferralStatus? Status { get; set; } = ReferralStatus.Pending;

        [Display(Name = "Valid From")]
        [DataType(DataType.Date)]
        public DateTime? ValidFrom { get; set; }

        [Display(Name = "Valid To")]
        [DataType(DataType.Date)]
        public DateTime? ValidTo { get; set; }

        [Display(Name = "Referrer Name")]
        [MaxLength(200)]
        public string? ReferredByName { get; set; }

        [Display(Name = "Referrer Phone Number")]
        [MaxLength(25)]
        public string? ReferredByPhoneNumber { get; set; }

        [Display(Name = "Referrer Email Address")]
        [MaxLength(254)]
        public string? ReferredByEmail { get; set; }

        [Display(Name = "Referral Notes")]
        [MaxLength(2000)]
        public string? Notes { get; set; }
    }
}
