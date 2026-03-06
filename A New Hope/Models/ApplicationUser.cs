using Microsoft.AspNetCore.Identity;

namespace A_New_Hope.Models
{
    /// <summary>
    /// ApplicationUser
    /// ---------------
    /// This is your ASP.NET Core Identity "login account" entity.
    ///
    /// Key concept in your project:
    /// - DomainUser = the application's business user record (Client/Staff/Admin, soft delete, audit fields, etc.)
    /// - ApplicationUser = the authentication/authorization account used to log in (Identity framework)
    ///
    /// Not every DomainUser will have a login account, so the link is optional (nullable DomainUserId).
    ///
    /// In ApplicationDbContext you configure:
    /// - A (optional) 1:1 relationship between ApplicationUser and DomainUser via DomainUserId
    /// - A unique index on DomainUserId so one DomainUser maps to at most one Identity account
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Optional link back to the DomainUser record.
        /// - null means this Identity account is not linked to a domain user (should be rare in your design).
        /// - when set, it allows the app to connect "who is logged in" to your business user data.
        /// </summary>
        public ulong? DomainUserId { get; set; }

        /// <summary>
        /// Navigation property to the linked DomainUser (business user record).
        /// EF Core uses this to enable Include(...) and relationship mapping.
        /// </summary>
        public DomainUser? DomainUser { get; set; }

        /// <summary>
        /// Optional tracking field for auditing / reporting when the account last authenticated.
        /// This is not automatically updated by Identity; you would set it yourself during sign-in events.
        /// </summary>
        public DateTime? LastLoginAt { get; set; }
    }
}