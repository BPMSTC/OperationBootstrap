using A_New_Hope.Models;

namespace A_New_Hope.Models.ViewModels.Users
{
    /// <summary>
    /// UserDetailsViewModel
    /// --------------------
    /// View model for the Users/Details page.
    ///
    /// Why this exists:
    /// - The Details page needs more than just the DomainUser entity.
    /// - It may need related client-only data such as ClientProfile and HouseholdMembers.
    /// - It may also need Identity/account info such as whether a linked login exists.
    ///
    /// Design notes:
    /// - DomainUser remains the primary business record shown on the page.
    /// - ClientProfile and HouseholdMembers are included only when the user is a Client.
    /// - HasLoginAccount / IdentityUserId mirror the pattern already used by DomainUserIndexRowViewModel.
    /// </summary>
    public class UserDetailsViewModel
    {
        /// <summary>
        /// The primary DomainUser business record being displayed.
        /// </summary>
        public DomainUser User { get; set; } = null!;

        /// <summary>
        /// Optional client-specific profile record.
        /// Populated when the DomainUser is a Client and a profile exists.
        /// </summary>
        public ClientProfile? ClientProfile { get; set; }

        /// <summary>
        /// Optional collection of household members associated with the client.
        /// Empty for non-client users or when no household members exist.
        /// </summary>
        public List<HouseholdMember> HouseholdMembers { get; set; } = new();

        /// <summary>
        /// Optional collection of monthly income records associated with the client.
        /// Empty for non-client users or when no income records exist.
        /// </summary>
        public List<ClientIncome> ClientIncomes { get; set; } = new();

        /// <summary>
        /// True when the DomainUser has an associated ASP.NET Core Identity login account.
        /// </summary>
        public bool HasLoginAccount { get; set; }

        /// <summary>
        /// The linked Identity user id when a login account exists; otherwise null.
        /// </summary>
        public string? IdentityUserId { get; set; }

        /// <summary>
        /// Convenience flag for client-specific UI sections.
        /// </summary>
        public bool IsClient => User.UserType == UserType.Client;

        public List<Referral> Referrals { get; set; } = new();
    }
}