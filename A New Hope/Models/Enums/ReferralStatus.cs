namespace A_New_Hope.Models
{
    /// <summary>
    /// High-level status for a referral record as it moves through review and completion.
    /// Stored as a string in the database (see ApplicationDbContext configuration).
    /// </summary>
    public enum ReferralStatus
    {
        Pending,
        Approved,
        Denied,
        Expired,
        Closed
    }
}