using System.ComponentModel.DataAnnotations; // Added for [Required], etc.

namespace A_New_Hope.Models
{
    /// <summary>
    /// UserItemPreference
    /// ------------------
    /// Represents a user's preference for a specific InventoryItem.
    ///
    /// Core idea:
    /// - DomainUser has a DefaultPreference (a fallback).
    /// - UserItemPreference overrides that default for a specific InventoryItem.
    ///
    /// Relationship:
    /// - Each record links one user (UserId) to one inventory item (InventoryItemId).
    /// - Optionally, the preference may also link to an InventoryItemOption
    ///   when the item has a variant/sub-selection.
    ///
    /// Examples:
    /// - Milk + Always + 2%
    /// - Bread + Ask + Wheat
    /// - Rice + Never + Brown
    ///
    /// Uniqueness/business rule:
    /// - A user should have at most one preference per inventory item.
    /// - This is enforced in ApplicationDbContext via a unique index on (UserId, InventoryItemId).
    ///
    /// Soft delete:
    /// - DeletedAt marks the record as deleted without physically removing it.
    /// - ApplicationDbContext applies a query filter to exclude deleted rows by default.
    ///
    /// Audit fields:
    /// - CreatedByUserId / UpdatedByUserId store which DomainUser set/changed the preference.
    /// - CreatedAt / UpdatedAt store timestamps (UTC recommended).
    /// </summary>
    public class UserItemPreference
    {
        /// <summary>
        /// Primary key for the preference record.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Foreign key to the DomainUser this preference belongs to.
        /// Required for validation and form submission.
        /// </summary>
        [Display(Name = "User")]
        public ulong UserId { get; set; }

        /// <summary>
        /// Foreign key to the InventoryItem this preference applies to.
        /// Required for validation and form submission.
        /// </summary>
        [Display(Name = "Inventory Item")]
        public ulong InventoryItemId { get; set; }

        /// <summary>
        /// Optional foreign key to the InventoryItemOption chosen for this preference.
        /// Null when the InventoryItem has no options or no option has been selected.
        /// </summary>
        [Display(Name = "Inventory Item Option")]
        public ulong? InventoryItemOptionId { get; set; }

        /// <summary>
        /// Preference value (Always / Ask / Never).
        /// Using an enum prevents invalid values and keeps preference logic consistent.
        /// Required for front-end forms.
        /// </summary>
        [Display(Name = "Preference")]
        public PreferenceOption Preference { get; set; } = PreferenceOption.Ask;

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public DomainUser User { get; set; } = null!;
        public InventoryItem InventoryItem { get; set; } = null!;
        public InventoryItemOption? InventoryItemOption { get; set; }
        public DomainUser? CreatedByUser { get; set; }
        public DomainUser? UpdatedByUser { get; set; }
    }
}