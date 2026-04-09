using A_New_Hope.Models.Inputs;

namespace A_New_Hope.Models.ViewModels.Referrals
{
    public class HouseholdEntryViewModel
    {
        public bool HasHouseholdMembers { get; set; }

        public List<HouseholdMemberEntryInput> HouseholdMembers { get; set; } = new();
    }
}