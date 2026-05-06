using A_New_Hope.Models;
using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models.ViewModels
{
    public class StaffCreateViewModel
    {
        [Required]
        public UserType UserType { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone(ErrorMessage = "Enter a valid phone number")]
        [RegularExpression(@"^\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}$",
            ErrorMessage = "Enter a valid 10-digit phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string AddressLine1 { get; set; } = string.Empty;

        public string? AddressLine2 { get; set; } // optional

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[A-Za-z]{2}$",
            ErrorMessage = "Use a 2-letter state code (e.g., IL)")]
        public string State { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{5}(-\d{4})?$",
            ErrorMessage = "Enter a valid ZIP code (e.g., 12345 or 12345-6789)")]
        public string PostalCode { get; set; } = string.Empty;

        public string? Password { get; set; } // optional
    }
}