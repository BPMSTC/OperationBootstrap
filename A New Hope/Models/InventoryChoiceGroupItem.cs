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

        /*
        [Display(Name = "Sort Order")]
        [Range(0, int.MaxValue, ErrorMessage = "Sort order must be 0 or greater.")]
        public int SortOrder { get; set; } = 0;
        */

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public InventoryChoiceGroup InventoryChoiceGroup { get; set; } = null!;
        public InventoryItem InventoryItem { get; set; } = null!;
        public DomainUser? CreatedByUser { get; set; }
        public DomainUser? UpdatedByUser { get; set; }
    }
}