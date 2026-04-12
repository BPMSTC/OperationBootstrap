using System.ComponentModel.DataAnnotations;
using A_New_Hope.Models.Enums;

namespace A_New_Hope.Models.Inputs
{
    /// <summary>
    /// Client entry data captured during Referral Entry.
    /// </summary>
    public class ClientEntryInput
    {
        [Display(Name = "First Name")]
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        [MaxLength(100)]
        public string? LastName { get; set; }

        [Display(Name = "Email Address")]
        [MaxLength(254)]
        public string? Email { get; set; }

        [Display(Name = "Phone Number")]
        [MaxLength(25)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Address Line 1")]
        [MaxLength(200)]
        public string? AddressLine1 { get; set; }

        [Display(Name = "Address Line 2")]
        [MaxLength(200)]
        public string? AddressLine2 { get; set; }

        [Display(Name = "City")]
        [MaxLength(100)]
        public string? City { get; set; }

        [Display(Name = "State")]
        [MaxLength(2)]
        public string? State { get; set; }

        [Display(Name = "ZIP Code")]
        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateOnly? DateOfBirth { get; set; }

        [Display(Name = "Employment Status")]
        public EmploymentStatus? EmploymentStatus { get; set; }

        [Display(Name = "Currently Unhoused")]
        public bool IsUnhoused { get; set; }

        /// <summary>
        /// Income rows captured for the client during referral entry.
        /// </summary>
        public List<ClientIncomeEntryInput> Incomes { get; set; } = new();

        public bool HasStarted =>
            !string.IsNullOrWhiteSpace(FirstName) ||
            !string.IsNullOrWhiteSpace(LastName) ||
            !string.IsNullOrWhiteSpace(Email) ||
            !string.IsNullOrWhiteSpace(PhoneNumber) ||
            !string.IsNullOrWhiteSpace(AddressLine1) ||
            !string.IsNullOrWhiteSpace(AddressLine2) ||
            !string.IsNullOrWhiteSpace(City) ||
            !string.IsNullOrWhiteSpace(State) ||
            !string.IsNullOrWhiteSpace(PostalCode) ||
            DateOfBirth.HasValue ||
            EmploymentStatus.HasValue ||
            Incomes.Any(i => i.HasStarted) ||
            IsUnhoused;
    }
}