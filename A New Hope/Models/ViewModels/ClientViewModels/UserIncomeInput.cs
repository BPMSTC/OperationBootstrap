using A_New_Hope.Models;
using A_New_Hope.Models.Enums;

public class UserIncomeInput
{
    public IncomeType? IncomeType { get; set; }
    public decimal? MonthlyAmount { get; set; }
}