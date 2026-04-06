using A_New_Hope.Models;
using A_New_Hope.Models.Inputs;

namespace A_New_Hope.Services.Interfaces
{
    public interface IReferralService
    {
        Task<Referral> CreateAsync(
            ReferralDetailsInput input,
            ulong clientUserId,
            ulong referringOrganizationId,
            ulong? actingUserId = null);

        Task<ulong> CreateAndReturnIdAsync(
            ReferralDetailsInput input,
            ulong clientUserId,
            ulong referringOrganizationId,
            ulong? actingUserId = null);
    }
}