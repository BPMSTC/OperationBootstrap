using Microsoft.AspNetCore.Mvc.Rendering;

namespace A_New_Hope.Models.ViewModels.Referrals
{
    public class ReferralDetailsViewModel
    {
        public ReferralDetailsInput Referral { get; set; } = new();

        public List<SelectListItem> ReferralStatusOptions { get; set; } = new();
    }
}