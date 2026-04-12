using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models
{
    /// <summary>
    /// InventoryItemOption
    /// -------------------
    /// Represents a selectable option/variant for an InventoryItem.
    ///
    /// Examples:
    /// - Milk -> 1%, 2%
    /// - Rice -> White, Brown
    /// - Oatmeal -> Instant, Quick
    /// - Pinto Beans -> Canned, Dry
    /// - Bread -> White, Wheat, Both
    ///
    /// Notes:
    /// - Not every InventoryItem has options.
    /// - Use this only for true variants/sub-selections of the same item.
    /// - Mutually exclusive groups of different real items (for example, Ketchup or Mustard)
    ///   should be handled separately later if needed.
    /// </summary>
    public class InventoryItemOption
    {
        /// <summary>
        /// Primary key for the inventory item option record.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Foreign key to the parent InventoryItem.
        /// </summary>
        [Display(Name = "Inventory Item")]
        public ulong InventoryItemId { get; set; }

        /// <summary>
        /// Option name shown to staff/users.
        /// Examples: 1%, 2%, White, Brown, Instant, Dry, Both
        /// </summary>
        [Required(ErrorMessage = "Option name is required.")]
        [MaxLength(100, ErrorMessage = "Option name cannot exceed 100 characters.")]
        [Display(Name = "Option Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Sorting value used for consistent display ordering under the parent item.
        /// </summary>
        [Display(Name = "Sort Order")]
        [Range(0, int.MaxValue, ErrorMessage = "Sort order must be 0 or greater.")]
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Business toggle that determines whether this option is available for use.
        /// </summary>
        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public InventoryItem InventoryItem { get; set; } = null!;
        public DomainUser? CreatedByUser { get; set; }
        public DomainUser? UpdatedByUser { get; set; }
    }
}