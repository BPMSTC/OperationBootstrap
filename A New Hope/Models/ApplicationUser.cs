using Microsoft.AspNetCore.Identity;

namespace A_New_Hope.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Link to your domain/business user record (Client/Staff/Admin)
        public ulong? DomainUserId { get; set; }
        public DomainUser? DomainUser { get; set; }

        // Optional account tracking
        public DateTime? LastLoginAt { get; set; }
    }
}