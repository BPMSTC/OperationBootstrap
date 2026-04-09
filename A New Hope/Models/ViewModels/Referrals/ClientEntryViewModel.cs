using Microsoft.AspNetCore.Mvc.Rendering;
using A_New_Hope.Models.Inputs;

namespace A_New_Hope.Models.ViewModels.Referrals
{
    public class ClientEntryViewModel
    {

        public string? ClientMode { get; set; }

        public ulong? SelectedClientUserId { get; set; }

        public ClientEntryInput NewClient { get; set; } = new();

        public List<SelectListItem> ExistingClients { get; set; } = new();

        public bool HasSelectedExistingClient =>
            SelectedClientUserId.HasValue && SelectedClientUserId.Value > 0;

        public bool HasStartedNewClient => NewClient.HasStarted;
    }
}