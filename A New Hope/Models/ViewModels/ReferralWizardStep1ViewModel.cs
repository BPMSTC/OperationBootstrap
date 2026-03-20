using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models.ViewModels
{
    /// <summary>
    /// View model for Referral Wizard Step 1:
    /// select an existing referring organization or add a new one.
    /// </summary>
    public class ReferralWizardStep1ViewModel
    {
        [Display(Name = "Existing Organization")]
        public ulong? SelectedReferringOrganizationId { get; set; }

        [Display(Name = "Organization Name")]
        [MaxLength(200)]
        public string? NewOrganizationName { get; set; }

        [Display(Name = "Primary Type of Service")]
        [MaxLength(100)]
        public string? NewOrganizationType { get; set; }

        [Display(Name = "Contact Person Name")]
        [MaxLength(200)]
        public string? NewPrimaryContactName { get; set; }

        [Display(Name = "Email Address")]
        [MaxLength(254)]
        public string? NewEmail { get; set; }

        [Display(Name = "Phone Number")]
        [MaxLength(25)]
        public string? NewPhoneNumber { get; set; }

        [Display(Name = "Address Line 1")]
        [MaxLength(200)]
        public string? NewAddressLine1 { get; set; }

        [Display(Name = "Address Line 2")]
        [MaxLength(200)]
        public string? NewAddressLine2 { get; set; }

        [Display(Name = "City")]
        [MaxLength(100)]
        public string? NewCity { get; set; }

        [Display(Name = "State")]
        [MaxLength(2)]
        public string? NewState { get; set; }

        [Display(Name = "ZIP Code")]
        [MaxLength(20)]
        public string? NewPostalCode { get; set; }

        [Display(Name = "Notes")]
        [MaxLength(2000)]
        public string? NewNotes { get; set; }

        public List<SelectListItem> ExistingOrganizations { get; set; } = new();

        /// <summary>
        /// True when the user selected an existing organization.
        /// </summary>
        public bool HasSelectedExistingOrganization =>
            SelectedReferringOrganizationId.HasValue && SelectedReferringOrganizationId.Value > 0;

        /// <summary>
        /// True when the user has started entering a new organization.
        /// Used to determine validation path and whether the collapse should stay open.
        /// </summary>
        public bool HasStartedNewOrganization =>
            !string.IsNullOrWhiteSpace(NewOrganizationName) ||
            !string.IsNullOrWhiteSpace(NewOrganizationType) ||
            !string.IsNullOrWhiteSpace(NewPrimaryContactName) ||
            !string.IsNullOrWhiteSpace(NewEmail) ||
            !string.IsNullOrWhiteSpace(NewPhoneNumber) ||
            !string.IsNullOrWhiteSpace(NewAddressLine1) ||
            !string.IsNullOrWhiteSpace(NewAddressLine2) ||
            !string.IsNullOrWhiteSpace(NewCity) ||
            !string.IsNullOrWhiteSpace(NewState) ||
            !string.IsNullOrWhiteSpace(NewPostalCode) ||
            !string.IsNullOrWhiteSpace(NewNotes);
    }
}