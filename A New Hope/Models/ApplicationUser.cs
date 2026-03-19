using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models
{
    /// <summary>
    /// ApplicationUser
    /// ---------------
    /// ASP.NET Core Identity login account.
    /// Links authentication accounts to DomainUser records in the application.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Optional link to the application's DomainUser record.
        /// </summary>
        [Display(Name = "Domain User")]
        public ulong? DomainUserId { get; set; }

        /// <summary>
        /// Navigation property to the linked DomainUser.
        /// </summary>
        public DomainUser? DomainUser { get; set; }

        /// <summary>
        /// Timestamp for the last successful login.
        /// </summary>
        [Display(Name = "Last Login")]
        [DataType(DataType.DateTime)]
        public DateTime? LastLoginAt { get; set; }
    }
}