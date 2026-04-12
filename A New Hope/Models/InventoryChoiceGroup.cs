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

        [Required(ErrorMessage = "Group name is required.")]
        [MaxLength(150)]
        [Display(Name = "Group Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Maximum Selections")]
        [Range(1, int.MaxValue, ErrorMessage = "Maximum selections must be at least 1.")]
        public int MaxSelections { get; set; } = 1;

        [Display(Name = "Display Label")]
        [MaxLength(150)]
        public string? DisplayLabel { get; set; }

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

        public ICollection<InventoryChoiceGroupItem> InventoryChoiceGroupItems { get; set; } = new List<InventoryChoiceGroupItem>();

        public DomainUser? CreatedByUser { get; set; }
        public DomainUser? UpdatedByUser { get; set; }
    }
}