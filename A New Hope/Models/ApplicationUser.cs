using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models
{
    /// <summary>
    /// Identity login account linked to a DomainUser record.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Optional foreign key to the linked DomainUser.
        /// </summary>
        public ulong? DomainUserId { get; set; }

        /// <summary>
        /// Linked DomainUser record.
        /// </summary>
        public DomainUser? DomainUser { get; set; }

        /// <summary>
        /// UTC timestamp of the last successful login.
        /// </summary>
        public DateTime? LastLoginAt { get; set; }
    }
}