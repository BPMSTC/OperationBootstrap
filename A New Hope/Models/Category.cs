using System.ComponentModel.DataAnnotations; // Added because we're using [MaxLength] / [Display] attributes for EF + MVC validation/UI labels

namespace A_New_Hope.Models
{
    /// <summary>
    /// Category
    /// --------
    /// Represents a classification bucket used to organize InventoryItems.
    ///
    /// Hierarchy rules in this model:
    /// - Every Category belongs to exactly one CategoryGroup (CategoryGroupId).
    /// - A Category may optionally have a Parent Category (ParentId) to support sub-categories.
    /// - A Category may have many Children categories (self-referencing tree).
    ///
    /// Lifecycle rules used across the project:
    /// - IsActive is a business toggle (hide/disable from selection without deleting).
    /// - DeletedAt supports soft delete (records remain in DB but are excluded by query filters).
    ///
    /// Audit fields:
    /// - CreatedByUserId / UpdatedByUserId store the DomainUser responsible for the change (when implemented).
    /// - CreatedAt / UpdatedAt store timestamps (UTC recommended).
    /// </summary>
    public class Category
    {
        /// <summary>
        /// Primary key for the Category record.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Foreign key to CategoryGroup. Every category must belong to a group (e.g., "Food", "Non-Food").
        /// </summary>
        [Display(Name = "Category Group")]
        public ulong CategoryGroupId { get; set; }

        /// <summary>
        /// Optional self-referencing foreign key to the parent Category.
        /// When null, this category is a "top-level" category within its group.
        /// </summary>
        [Display(Name = "Parent Category")]
        public ulong? ParentId { get; set; }

        /// <summary>
        /// Category name (required).
        /// MaxLength prevents EF/MySQL from defaulting to LONGTEXT, improves indexing, and keeps schema predictable.
        /// </summary>
        [MaxLength(150)] // Prevents EF/MySQL from defaulting to LONGTEXT; makes indexes/unique constraints safer and keeps the column as VARCHAR(150)
        public string Name { get; set; } = null!;

        /// <summary>
        /// Sorting value used for consistent display ordering within a CategoryGroup (and potentially under a parent).
        /// Default 0 means categories sort predictably even if not explicitly ordered yet.
        /// </summary>
        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; } = 0; // Non-null + default gives predictable ordering and aligns with common "sort_order" usage

        /// <summary>
        /// Business toggle that determines whether this category is available for selection/use.
        /// This is not the same as deleting; inactive categories can be re-enabled later.
        /// </summary>
        [Display(Name = "Active")]
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
        /// Timestamp when the record was created.
        /// (Typically set server-side using UTC.)
        /// </summary>
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when the record was last updated.
        /// (Typically set server-side using UTC.)
        /// </summary>
        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Soft delete marker.
        /// - null means "not deleted"
        /// - non-null means the record is considered deleted and is usually excluded by query filters
        /// </summary>
        [Display(Name = "Deleted At")]
        public DateTime? DeletedAt { get; set; }

        // -----------------------------------------------------------------
        // Navigation properties (EF Core relationships)
        // -----------------------------------------------------------------

        /// <summary>
        /// Navigation to the CategoryGroup this Category belongs to.
        /// Required relationship in your model.
        /// </summary>
        public CategoryGroup CategoryGroup { get; set; } = null!;

        /// <summary>
        /// Navigation to the parent Category (self-referencing relationship).
        /// Null when this category has no parent.
        /// </summary>
        public Category? Parent { get; set; }

        /// <summary>
        /// Navigation to child categories (self-referencing relationship).
        /// Initialized to an empty list to avoid null checks.
        /// </summary>
        public ICollection<Category> Children { get; set; } = new List<Category>();

        /// <summary>
        /// Navigation to the DomainUser who created the record.
        /// Including this avoids EF creating shadow properties and makes it easier to display audit info in views.
        /// </summary>
        public DomainUser? CreatedByUser { get; set; } // Lets you Include() the creator and enables proper FK mapping without EF creating "shadow" FK columns

        /// <summary>
        /// Navigation to the DomainUser who last updated the record.
        /// </summary>
        public DomainUser? UpdatedByUser { get; set; } // Also makes audit display in MVC views straightforward (e.g., "Last updated by")
    }
}