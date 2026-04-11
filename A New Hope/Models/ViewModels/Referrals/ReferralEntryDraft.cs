using System.ComponentModel.DataAnnotations;
using A_New_Hope.Models;
using A_New_Hope.Models.Inputs;

namespace A_New_Hope.Models.ViewModels.Referrals
{
    /// <summary>
    /// Stores the in-progress Referral Entry flow state in session.
    /// This is not tied to a single page. It represents the overall
    /// draft as the user moves from one page to the next.
    /// </summary>
    public class ReferralEntryDraft
    {
        // =========================================================
        // REFERRING ORGANIZATION
        // =========================================================

        public ulong? ExistingReferringOrganizationId { get; set; }

        public ReferringOrganizationEntryInput NewOrganization { get; set; } = new();

        // =========================================================
        // CLIENT
        // =========================================================

        public ulong? ExistingClientUserId { get; set; }

        public ClientEntryInput NewClient { get; set; } = new();

        // =========================================================
        // HOUSEHOLD MEMBERS
        // =========================================================

        public List<HouseholdMemberEntryInput> HouseholdMembers { get; set; } = new();

        // =========================================================
        // REFERRAL DETAILS
        // =========================================================

        public ReferralDetailsInput Referral { get; set; } = new();

        // =========================================================
        // HELPER FLAGS
        // =========================================================

        public bool HasExistingOrganization =>
            ExistingReferringOrganizationId.HasValue && ExistingReferringOrganizationId.Value > 0;

        public bool HasNewOrganization =>
            NewOrganization != null && NewOrganization.HasStarted;

        public bool HasExistingClient =>
            ExistingClientUserId.HasValue && ExistingClientUserId.Value > 0;

        public bool HasNewClient =>
            NewClient != null && NewClient.HasStarted;

        public bool RequiresHouseholdStep => HasNewClient;

        public bool HasAnyHouseholdMembers =>
            HouseholdMembers.Any(h => h.HasStarted);
    }
}