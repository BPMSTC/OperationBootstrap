using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models
{
    /// <summary>
    /// InventoryChoiceGroupItem
    /// ------------------------
    /// Join table linking an InventoryChoiceGroup to the InventoryItems
    /// that belong to that grouped choice.
    /// </summary>
    public class InventoryChoiceGroupItem
    {
        public ulong Id { get; set; }

        [Display(Name = "Choice Group")]
        public ulong InventoryChoiceGroupId { get; set; }

        [Display(Name = "Inventory Item")]
        public ulong InventoryItemId { get; set; }

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; } = 0;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; }

        [Display(Name = "Deleted At")]
        public DateTime? DeletedAt { get; set; }

        public InventoryChoiceGroup InventoryChoiceGroup { get; set; } = null!;
        public InventoryItem InventoryItem { get; set; } = null!;

        public DomainUser? CreatedByUser { get; set; }
        public DomainUser? UpdatedByUser { get; set; }
    }
}