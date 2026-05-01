using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models.ViewModels.ReferringOrganizations
{
    /// <summary>
    /// View model used by ReferringOrganizations Create/Edit screens.
    /// Supports selecting multiple service categories.
    /// </summary>
    public class ReferringOrganizationEditViewModel
    {
        public ulong? Id { get; set; }

        [Display(Name = "Organization Name")]
        [MaxLength(200)]
        public string? Name { get; set; }

        [Display(Name = "Service Categories")]
        public List<ulong> SelectedServiceCategoryIds { get; set; } = new();

        public List<SelectListItem> AvailableServiceCategories { get; set; } = new();

        [Display(Name = "Phone Number")]
        [MaxLength(25)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Email Address")]
        [MaxLength(254)]
        public string? Email { get; set; }

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

        [Display(Name = "Contact Person Name")]
        [MaxLength(200)]
        public string? PrimaryContactName { get; set; }

        [Display(Name = "Notes")]
        [MaxLength(2000)]
        public string? Notes { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
