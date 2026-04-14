using A_New_Hope.Models;
using A_New_Hope.Models.Enums;
using A_New_Hope.Models.Inputs;

namespace A_New_Hope.Models.ViewModels.ClientViewModels
{
    public class UserWizardViewModel
    {
       
            public DomainUser User { get; set; } = new();

            public bool IsUnhoused { get; set; }

            public EmploymentStatus? EmploymentStatus { get; set; }

            public List<UserIncomeInput> Incomes { get; set; } = new();

        // HELPER PASSTHROUGH (optional convenience)
        public UserType UserType
        {
            get => User.UserType;
            set => User.UserType = value;
        }
    }
}