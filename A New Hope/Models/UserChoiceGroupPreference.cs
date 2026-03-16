using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models
{
    /// <summary>
    /// UserChoiceGroupPreference
    /// -------------------------
    /// Represents a user's selected item for an InventoryChoiceGroup.
    ///
    /// Core idea:
    /// - Some stakeholder choices are grouped choices between different real inventory items.
    /// - Example: "Sugar or Flour" where the user selects one item from the group.
    ///
    /// Relationship:
    /// - Each record links one user (UserId) to one InventoryChoiceGroup (InventoryChoiceGroupId)
    ///   and stores the selected InventoryItem (SelectedInventoryItemId).
    ///
    /// Example:
    /// - User: Jamie Client
    /// - Choice Group: Sugar or Flour
    /// - Selected Item: Flour
    ///
    /// Uniqueness/business rule:
    /// - A user should have at most one preference per choice group.
    /// - This should be enforced in ApplicationDbContext via a unique index on (UserId, InventoryChoiceGroupId).
    ///
    /// Soft delete:
    /// - DeletedAt marks the record as deleted without physically removing it.
    /// - ApplicationDbContext should apply a query filter to exclude deleted rows by default.
    ///
    /// Audit fields:
    /// - CreatedByUserId / UpdatedByUserId store which DomainUser set/changed the preference.
    /// - CreatedAt / UpdatedAt store timestamps (UTC recommended).
    /// </summary>
    public class UserChoiceGroupPreference
    {
        /// <summary>
        /// Primary key for the preference record.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Foreign key to the DomainUser this preference belongs to.
        /// </summary>
        public ulong UserId { get; set; }

        /// <summary>
        /// Foreign key to the InventoryChoiceGroup this preference applies to.
        /// </summary>
        public ulong InventoryChoiceGroupId { get; set; }

        /// <summary>
        /// Foreign key to the selected InventoryItem within the choice group.
        /// </summary>
        [Display(Name = "Selected Inventory Item")]
        public ulong SelectedInventoryItemId { get; set; }

        /// <summary>
        /// Audit: DomainUser who initially created/set this preference (nullable until auth is wired).
        /// </summary>
        public ulong? CreatedByUserId { get; set; }

        /// <summary>
        /// Audit: DomainUser who last updated/changed this preference (nullable until auth is wired).
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
        /// Required navigation to the DomainUser this preference belongs to.
        /// </summary>
        public DomainUser User { get; set; } = null!;

        /// <summary>
        /// Required navigation to the InventoryChoiceGroup this preference applies to.
        /// </summary>
        public InventoryChoiceGroup InventoryChoiceGroup { get; set; } = null!;

        /// <summary>
        /// Required navigation to the selected InventoryItem within the choice group.
        /// </summary>
        public InventoryItem SelectedInventoryItem { get; set; } = null!;

        /// <summary>
        /// Navigation to the DomainUser who created the preference.
        /// </summary>
        public DomainUser? CreatedByUser { get; set; }

        /// <summary>
        /// Navigation to the DomainUser who last updated the preference.
        /// </summary>
        public DomainUser? UpdatedByUser { get; set; }
    }
}