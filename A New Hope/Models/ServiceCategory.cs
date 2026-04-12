using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models
{
    /// <summary>
    /// Broad service category used to classify referring organizations
    /// and, later, potentially referrals.
    /// Examples: Food, Medical, Transportation, Clothing.
    /// </summary>
    public class ServiceCategory
    {
        public ulong Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public DomainUser? CreatedByUser { get; set; }
        public DomainUser? UpdatedByUser { get; set; }

        public ICollection<ReferringOrganizationServiceCategory> ReferringOrganizationServiceCategories { get; set; }
            = new List<ReferringOrganizationServiceCategory>();
    }
}