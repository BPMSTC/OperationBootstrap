using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models
{
    /// <summary>
    /// Represents a top-level grouping for categories.
    /// </summary>
    public class CategoryGroup
    {
        /// <summary>
        /// Primary key for the category group record.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Group name.
        /// </summary>
        [Required(ErrorMessage = "Group name is required.")]
        [StringLength(150, ErrorMessage = "Group name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;

        /*
        /// <summary>
        /// Sort order used for display and consistent ordering.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Sort order must be 0 or greater.")]
        public int SortOrder { get; set; } = 0;
        */

        /// <summary>
        /// Indicates whether the group is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        public ulong? CreatedByUserId { get; set; }

        public ulong? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        public ICollection<Category> Categories { get; set; } = new List<Category>();

        public DomainUser? CreatedByUser { get; set; }

        public DomainUser? UpdatedByUser { get; set; }
    }
}