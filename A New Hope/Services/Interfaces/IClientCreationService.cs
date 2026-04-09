using A_New_Hope.Models;
using A_New_Hope.Models.Inputs;

namespace A_New_Hope.Services.Interfaces
{
    public interface IClientCreationService
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