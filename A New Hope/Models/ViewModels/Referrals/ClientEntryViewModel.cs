using Microsoft.AspNetCore.Mvc.Rendering;

namespace A_New_Hope.Models.ViewModels.Referrals
{
    public class ClientEntryViewModel
    {
        public ulong? SelectedClientUserId { get; set; }

        public ClientEntryInput NewClient { get; set; } = new();

        public List<SelectListItem> ExistingClients { get; set; } = new();

        public bool HasSelectedExistingClient =>
            SelectedClientUserId.HasValue && SelectedClientUserId.Value > 0;

        public bool HasStartedNewClient => NewClient.HasStarted;
    }
}