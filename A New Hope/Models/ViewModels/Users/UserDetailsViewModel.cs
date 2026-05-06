using A_New_Hope.Models;

namespace A_New_Hope.Models.ViewModels.Users
{
    /// <summary>
    /// UserDetailsViewModel
    /// --------------------
    /// View model for the Users/Details page.
    /// </summary>
    public class UserDetailsViewModel
    {
        /// <summary>
        /// The primary DomainUser business record being displayed.
        /// </summary>
        public DomainUser User { get; set; } = null!;

        /// <summary>
        /// Optional client-specific profile record.
        /// </summary>
        public ClientProfile? ClientProfile { get; set; }

        /// <summary>
        /// Household members associated with the client.
        /// </summary>
        public List<HouseholdMember> HouseholdMembers { get; set; } = new();

        /// <summary>
        /// Monthly income records associated with the client.
        /// </summary>
        public List<ClientIncome> ClientIncomes { get; set; } = new();

        /// <summary>
        /// True when the DomainUser has an associated ASP.NET Core Identity login account.
        /// </summary>
        public bool HasLoginAccount { get; set; }

        /// <summary>
        /// The linked Identity user id when a login account exists.
        /// </summary>
        public string? IdentityUserId { get; set; }

        /// <summary>
        /// Convenience flag for client-specific UI sections.
        /// </summary>
        public bool IsClient => User.UserType == UserType.Client;

        /// <summary>
        /// Referrals associated with the client.
        /// </summary>
        public List<Referral> Referrals { get; set; } = new();

        /// <summary>
        /// Controls whether the page is in edit mode.
        /// </summary>
        public bool IsEditMode { get; set; }

        /// <summary>
        /// UI-only flags for marking household members as deleted.
        /// Index must match HouseholdMembers list.
        /// </summary>
        public List<bool> HouseholdDeleteFlags { get; set; } = new();
    }
}