using System.ComponentModel.DataAnnotations; // Needed for [MaxLength], [Required], [StringLength], [Range]

namespace A_New_Hope.Models
{
    /// <summary>
    /// CategoryGroup
    /// -------------
    /// Represents a top-level grouping for Categories (and therefore InventoryItems).
    ///
    /// Examples in your seed data:
    /// - "Food"
    /// - "Non-Food"
    ///
    /// Why groups exist:
    /// - They provide a stable, high-level organization layer.
    /// - They make dropdowns and reporting clearer (Group -> Category -> Item).
    ///
    /// Lifecycle rules used across the project:
    /// - IsActive is a business toggle (disable/hide without deleting).
    /// - DeletedAt supports soft delete (record remains in DB but is typically excluded by query filters).
    ///
    /// Audit fields:
    /// - CreatedByUserId / UpdatedByUserId store the DomainUser responsible (once auth is wired).
    /// - CreatedAt / UpdatedAt store timestamps (UTC recommended).
    /// 
    /// Front-end validation:
    /// - Required fields are marked with [Required] for MVC validation.
    /// - Max lengths are enforced via [StringLength] to prevent overly long input.
    /// - Range validation applied where numeric constraints make sense.
    /// </summary>
    public class CategoryGroup
    {
        /// <summary>
        /// Primary key for the category group record.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Group name (required).
        /// MaxLength prevents EF/MySQL from defaulting to LONGTEXT and keeps indexes/uniqueness constraints reliable.
        /// Added [Required] and [StringLength] for front-end validation.
        /// </summary>
        [Required(ErrorMessage = "Group name is required.")]
        [StringLength(150, ErrorMessage = "Group name cannot exceed 150 characters.")]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Sorting value used for consistent ordering of groups in the UI.
        /// Default 0 keeps ordering predictable even when not explicitly set.
        /// Added [Range] for front-end validation (cannot be negative).
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Sort order must be 0 or greater.")]
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Business toggle that determines whether this group is available for selection/use.
        /// This is not the same as deleting; inactive groups can be re-enabled later.
        /// </summary>
        public bool IsActive { get; set; } = true;

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
        /// - null = active/not deleted
        /// - non-null = deleted (excluded by global query filters in ApplicationDbContext)
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        // -----------------------------------------------------------------
        // Navigation properties (EF Core relationships)
        // -----------------------------------------------------------------

        /// <summary>
        /// Navigation collection of categories that belong to this group.
        /// Initialized to an empty list to avoid null checks.
        /// </summary>
        public ICollection<Category> Categories { get; set; } = new List<Category>();

        /// <summary>
        /// Navigation to the DomainUser who created the record (for audit display and Include()).
        /// Having explicit navs helps avoid EF “shadow FK” issues and simplifies admin UI display.
        /// </summary>
        public DomainUser? CreatedByUser { get; set; }

        /// <summary>
        /// Navigation to the DomainUser who last updated the record.
        /// </summary>
        public DomainUser? UpdatedByUser { get; set; }
    }
}