namespace A_New_Hope.Models
{
    /// <summary>
    /// User preference for whether a given inventory item should be included/allowed.
    /// Stored as a string in the database (see ApplicationDbContext configuration).
    /// </summary>
    public enum PreferenceOption
    {
        Always,
        Ask,
        Never
    }
}