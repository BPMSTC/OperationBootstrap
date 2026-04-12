using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models
{
    /// <summary>
    /// InventoryItem
    /// -------------
    /// Represents an item in your inventory/catalog that clients may request/receive.
    ///
    /// Classification:
    /// - Each InventoryItem belongs to exactly one Category (CategoryId).
    /// - Categories belong to CategoryGroups, which provides the "Group -> Category -> Item" hierarchy.
    ///
    /// Status flags:
    /// - IsBaseline: indicates the item is part of a default/core catalog (useful for seeding or standard lists).
    /// - IsAvailable: indicates whether the item is currently available (can change frequently).
    /// - IsActive: indicates whether the item is enabled in the catalog at all (admin toggle).
    ///
    /// Soft delete:
    /// - DeletedAt marks the record as deleted without physically removing it.
    /// - ApplicationDbContext applies a query filter to exclude deleted items by default.
    ///
    /// Audit fields:
    /// - CreatedByUserId / UpdatedByUserId store the DomainUser responsible (once auth is wired).
    /// - CreatedAt / UpdatedAt store timestamps (UTC recommended).
    /// </summary>
    public class InventoryItem
    {
        /// <summary>
        /// Primary key for the inventory item record.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Item name (required).
        /// MaxLength prevents EF/MySQL from defaulting to LONGTEXT and keeps the column as VARCHAR for indexing/search.
        /// </summary>
        [Required(ErrorMessage = "Item name is required.")]
        [MaxLength(200, ErrorMessage = "Item name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to the Category this item belongs to.
        /// </summary>
        [Display(Name = "Category")]
        public ulong CategoryId { get; set; }

        /// <summary>
        /// True if this item is considered part of a baseline/default catalog.
        /// Useful for initial setup or for differentiating "standard" items from ad-hoc ones.
        /// </summary>
        public bool IsBaseline { get; set; } = false;

        /// <summary>
        /// Indicates whether this item is currently available (stock/availability state).
        /// This can change frequently without removing or disabling the item from the catalog.
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// Indicates whether this item is enabled in the catalog (admin/business toggle).
        /// Different meaning than IsAvailable:
        /// - IsActive=false means "do not use this item at all" (hidden/disabled)
        /// - IsAvailable=false means "item exists but isn't available right now"
        /// </summary>
        public bool IsActive { get; set; } = true;

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public Category Category { get; set; } = null!;
        public DomainUser? CreatedByUser { get; set; }
        public DomainUser? UpdatedByUser { get; set; }

        public ICollection<InventoryItemOption> InventoryItemOptions { get; set; } = new List<InventoryItemOption>();
        public ICollection<InventoryChoiceGroupItem> InventoryChoiceGroupItems { get; set; } = new List<InventoryChoiceGroupItem>();
    }
}