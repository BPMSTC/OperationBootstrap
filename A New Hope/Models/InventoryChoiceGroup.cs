using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models
{
    /// <summary>
    /// InventoryChoiceGroup
    /// --------------------
    /// Represents a grouped client choice where one selection is made
    /// from multiple different InventoryItems.
    ///
    /// Examples:
    /// - Vegetable Oil or Mayo
    /// - Ketchup or Mustard
    /// - Sugar or Flour
    /// - Pancake Syrup or Jelly
    /// </summary>
    public class InventoryChoiceGroup
    {
        public ulong Id { get; set; }

        [MaxLength(150)]
        [Display(Name = "Group Name")]
        public string Name { get; set; } = null!;

        [Display(Name = "Maximum Selections")]
        public int MaxSelections { get; set; } = 1;

        [Display(Name = "Display Label")]
        [MaxLength(150)]
        public string? DisplayLabel { get; set; }

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

        public ICollection<InventoryChoiceGroupItem> InventoryChoiceGroupItems { get; set; } = new List<InventoryChoiceGroupItem>();

        public DomainUser? CreatedByUser { get; set; }
        public DomainUser? UpdatedByUser { get; set; }
    }
}