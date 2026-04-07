using Microsoft.AspNetCore.Mvc.Rendering;
using A_New_Hope.Models.Inputs;

namespace A_New_Hope.Models.ViewModels.Referrals
{
    public class ReferralDetailsViewModel
    {
        public string BackAction { get; set; } = "ClientEntry";

        public ReferralDetailsInput Referral { get; set; } = new();

        public List<SelectListItem> ReferralStatusOptions { get; set; } = new();
    }
}