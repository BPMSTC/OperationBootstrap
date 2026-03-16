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
        [MaxLength(100)]
        [Display(Name = "Option Name")]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Sorting value used for consistent display ordering under the parent item.
        /// </summary>
        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Business toggle that determines whether this option is available for use.
        /// </summary>
        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Audit: DomainUser who created this record.
        /// Nullable until auth wiring is complete.
        /// </summary>
        public ulong? CreatedByUserId { get; set; }

        /// <summary>
        /// Audit: DomainUser who last updated this record.
        /// Nullable until auth wiring is complete.
        /// </summary>
        public ulong? UpdatedByUserId { get; set; }

        /// <summary>
        /// Timestamp when the record was created.
        /// </summary>
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when the record was last updated.
        /// </summary>
        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Soft delete marker.
        /// - null = not deleted
        /// - non-null = deleted
        /// </summary>
        [Display(Name = "Deleted At")]
        public DateTime? DeletedAt { get; set; }

        // -------------------------------------------------------------
        // Navigation properties
        // -------------------------------------------------------------

        /// <summary>
        /// Parent inventory item.
        /// </summary>
        public InventoryItem InventoryItem { get; set; } = null!;

        /// <summary>
        /// Navigation to the DomainUser who created the record.
        /// </summary>
        public DomainUser? CreatedByUser { get; set; }

        /// <summary>
        /// Navigation to the DomainUser who last updated the record.
        /// </summary>
        public DomainUser? UpdatedByUser { get; set; }
    }
}