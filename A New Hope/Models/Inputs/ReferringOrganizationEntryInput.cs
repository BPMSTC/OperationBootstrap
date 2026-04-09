using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models.Inputs
{
    /// <summary>
    /// Organization entry data captured during Referral Entry.
    /// </summary>
    public class ReferringOrganizationEntryInput
    {
        [Display(Name = "Organization Name")]
        [MaxLength(200)]
        public string? Name { get; set; }

        [Display(Name = "Primary Type of Service")]
        [MaxLength(100)]
        public string? Type { get; set; }

        [Display(Name = "Contact Person Name")]
        [MaxLength(200)]
        public string? PrimaryContactName { get; set; }

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

        [Display(Name = "Notes")]
        [MaxLength(2000)]
        public string? Notes { get; set; }

        public bool HasStarted =>
            !string.IsNullOrWhiteSpace(Name) ||
            !string.IsNullOrWhiteSpace(Type) ||
            !string.IsNullOrWhiteSpace(PrimaryContactName) ||
            !string.IsNullOrWhiteSpace(Email) ||
            !string.IsNullOrWhiteSpace(PhoneNumber) ||
            !string.IsNullOrWhiteSpace(AddressLine1) ||
            !string.IsNullOrWhiteSpace(AddressLine2) ||
            !string.IsNullOrWhiteSpace(City) ||
            !string.IsNullOrWhiteSpace(State) ||
            !string.IsNullOrWhiteSpace(PostalCode) ||
            !string.IsNullOrWhiteSpace(Notes);
    }
}