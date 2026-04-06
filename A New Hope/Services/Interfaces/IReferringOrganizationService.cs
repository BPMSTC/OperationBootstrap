using A_New_Hope.Models;
using A_New_Hope.Models.ViewModels.Referrals;

namespace A_New_Hope.Services.Interfaces
{
    public interface IReferringOrganizationService
    {
        Task<ReferringOrganization> CreateAsync(
            ReferringOrganizationEntryInput input,
            ulong? actingUserId = null);

        Task<ulong> CreateAndReturnIdAsync(
            ReferringOrganizationEntryInput input,
            ulong? actingUserId = null);
    }
}