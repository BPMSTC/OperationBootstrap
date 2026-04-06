using A_New_Hope.Models;
using A_New_Hope.Models.ViewModels.Referrals;

namespace A_New_Hope.Services.Interfaces
{
    public interface IClientEntryService
    {
        Task<DomainUser> CreateClientWithProfileAndHouseholdAsync(
            ClientEntryInput clientInput,
            List<HouseholdMemberEntryInput>? householdInputs = null,
            ulong? actingUserId = null);

        Task<ulong> CreateClientAndReturnIdAsync(
            ClientEntryInput clientInput,
            List<HouseholdMemberEntryInput>? householdInputs = null,
            ulong? actingUserId = null);
    }
}