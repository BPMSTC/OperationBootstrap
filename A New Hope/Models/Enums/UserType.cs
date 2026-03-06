namespace A_New_Hope.Models
{
    /// <summary>
    /// Business-level user classification for DomainUser records (not the Identity role itself).
    /// Stored as a string in the database (see ApplicationDbContext configuration).
    /// </summary>
    public enum UserType
    {
        Client,
        Staff,
        Admin
    }
}