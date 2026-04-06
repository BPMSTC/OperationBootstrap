using Microsoft.AspNetCore.Mvc.Rendering;

namespace A_New_Hope.Models.ViewModels.Referrals
{
    public class OrganizationEntryViewModel
    {
        public ulong? SelectedReferringOrganizationId { get; set; }

        public ReferringOrganizationEntryInput NewOrganization { get; set; } = new();

        public List<SelectListItem> ExistingOrganizations { get; set; } = new();

        public bool HasSelectedExistingOrganization =>
            SelectedReferringOrganizationId.HasValue && SelectedReferringOrganizationId.Value > 0;

        public bool HasStartedNewOrganization => NewOrganization.HasStarted;
    }
}