namespace A_New_Hope.Models.ViewModels.Referrals
{
    public class ReferralEntryReviewViewModel
    {
        public ReferralEntryDraft Draft { get; set; } = new();

        public string? SelectedOrganizationDisplayName { get; set; }
        public string? SelectedClientDisplayName { get; set; }

        public string? NewOrganizationServiceCategoriesDisplay { get; set; }
    }
}