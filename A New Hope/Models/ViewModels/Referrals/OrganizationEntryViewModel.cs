using Microsoft.AspNetCore.Mvc.Rendering;
using A_New_Hope.Models.Inputs;

namespace A_New_Hope.Models.ViewModels.Referrals
{
    public class OrganizationEntryViewModel
    {
        public string? OrganizationMode { get; set; }

        public ulong? SelectedReferringOrganizationId { get; set; }

        public ReferringOrganizationEntryInput NewOrganization { get; set; } = new();

        public List<SelectListItem> ExistingOrganizations { get; set; } = new();

        public List<SelectListItem> AvailableServiceCategories { get; set; } = new();

        public bool HasSelectedExistingOrganization =>
            SelectedReferringOrganizationId.HasValue && SelectedReferringOrganizationId.Value > 0;

        public bool HasStartedNewOrganization => NewOrganization.HasStarted;
    }
}