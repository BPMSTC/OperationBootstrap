using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models
{
    public class Referral
    {
        public ulong Id { get; set; }

        [Required]
        [Display(Name = "Client")]
        public ulong ClientUserId { get; set; }

        [Required]
        [Display(Name = "Referring Organization")]
        public ulong ReferringOrganizationId { get; set; }

        [Required]
        [Display(Name = "Referral Date")]
        [DataType(DataType.Date)]
        public DateTime ReferredOn { get; set; }

        [Display(Name = "Status")]
        public ReferralStatus Status { get; set; } = ReferralStatus.Pending;

        [Display(Name = "Valid From")]
        [DataType(DataType.Date)]
        public DateTime? ValidFrom { get; set; }

        [Display(Name = "Valid To")]
        [DataType(DataType.Date)]
        public DateTime? ValidTo { get; set; }

        [Display(Name = "Referrer Name")]
        [StringLength(200, ErrorMessage = "Referrer name cannot exceed 200 characters.")]
        public string? ReferredByName { get; set; }

        [Display(Name = "Referrer Phone")]
        [StringLength(25)]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        public string? ReferredByPhoneNumber { get; set; }

        [Display(Name = "Referrer Email")]
        [StringLength(254)]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string? ReferredByEmail { get; set; }

        [Display(Name = "Notes")]
        [StringLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters.")]
        public string? Notes { get; set; }

        public ulong? CreatedByUserId { get; set; }

        public ulong? UpdatedByUserId { get; set; }

        [Display(Name = "Created At")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Updated At")]
        [DataType(DataType.DateTime)]
        public DateTime UpdatedAt { get; set; }

        [Display(Name = "Deleted At")]
        [DataType(DataType.DateTime)]
        public DateTime? DeletedAt { get; set; }

        public DomainUser ClientUser { get; set; } = null!;

        public ReferringOrganization ReferringOrganization { get; set; } = null!;

        public DomainUser? CreatedByUser { get; set; }

        public DomainUser? UpdatedByUser { get; set; }
    }
}