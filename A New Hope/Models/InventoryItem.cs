using System.ComponentModel.DataAnnotations; // Added for [MaxLength]

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
        [MaxLength(200)] // Prevents EF/MySQL from defaulting to LONGTEXT; keeps the column as VARCHAR and supports future indexing/searching
        public string Name { get; set; } = null!;

        /// <summary>
        /// Foreign key to the Category this item belongs to.
        /// </summary>
        public ulong CategoryId { get; set; }

        /// <summary>
        /// True if this item is considered part of a baseline/default catalog.
        /// Useful for initial setup or for differentiating "standard" items from ad-hoc ones.
        /// </summary>
        public bool IsBaseline { get; set; } = false; // Keep if you need a "default catalog" flag; otherwise consider removing later

        /// <summary>
        /// Indicates whether this item is currently available (stock/availability state).
        /// This can change frequently without removing or disabling the item from the catalog.
        /// </summary>
        public bool IsAvailable { get; set; } = true; // Availability can change day-to-day without removing the item from the catalog

        /// <summary>
        /// Indicates whether this item is enabled in the catalog (admin/business toggle).
        /// Different meaning than IsAvailable:
        /// - IsActive=false means "do not use this item at all" (hidden/disabled)
        /// - IsAvailable=false means "item exists but isn't available right now"
        /// </summary>
        public bool IsActive { get; set; } = true; // "Catalog enabled/disabled" flag; different meaning than IsAvailable

        /// <summary>
        /// Audit: DomainUser who created this record (nullable until auth is wired).
        /// </summary>
        public ulong? CreatedByUserId { get; set; }

        /// <summary>
        /// Audit: DomainUser who last updated this record (nullable until auth is wired).
        /// </summary>
        public ulong? UpdatedByUserId { get; set; }

        /// <summary>
        /// Timestamp when the record was created (typically set server-side in UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when the record was last updated (typically set server-side in UTC).
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Soft delete marker:
        /// - null = not deleted
        /// - non-null = deleted (excluded by global query filters in ApplicationDbContext)
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        // -----------------------------------------------------------------
        // Navigation properties (EF Core relationships)
        // -----------------------------------------------------------------

        /// <summary>
        /// Required navigation to the Category this item belongs to.
        /// </summary>
        public Category Category { get; set; } = null!;

        /// <summary>
        /// Navigation to the DomainUser who created this record.
        /// Useful for Include() and admin audit display.
        /// </summary>
        public DomainUser? CreatedByUser { get; set; } // Lets you Include() audit users and avoids relying on FK IDs only

        /// <summary>
        /// Navigation to the DomainUser who last updated this record.
        /// </summary>
        public DomainUser? UpdatedByUser { get; set; } // Same; makes admin auditing/UX much easier

        public ICollection<InventoryItemOption> InventoryItemOptions { get; set; } = new List<InventoryItemOption>();
    }
}