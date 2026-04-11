namespace A_New_Hope.Models
{
    /// <summary>
    /// IncomeType
    /// ----------
    /// Represents the category of monthly income for a client.
    /// Stored as a string in the database for readability and stability.
    /// </summary>
    public enum IncomeType
    {
        Employment = 0,
        SocialSecurity = 1,
        ChildSupport = 2,
        Disability = 3,
        Unemployment = 4,
        Pension = 5,
        GeneralAssistance = 6,
        Other = 7
    }
}