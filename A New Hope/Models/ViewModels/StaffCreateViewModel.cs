using A_New_Hope.Models;
using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models.ViewModels
{
    public class StaffCreateViewModel
    {
        public UserType UserType { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string? Password { get; set; }
    }
}