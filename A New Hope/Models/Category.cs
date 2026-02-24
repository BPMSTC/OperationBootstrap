using System.ComponentModel.DataAnnotations; // Added because we're using [MaxLength] attributes for EF + validation

namespace A_New_Hope.Models
{
    public class Category
    {
        public ulong Id { get; set; }

        [Display(Name = "Category Group")]
        public ulong CategoryGroupId { get; set; }

        [Display(Name = "Parent Category")]
        public ulong? ParentId { get; set; }

        [MaxLength(150)] // Prevents EF/MySQL from defaulting to LONGTEXT; makes indexes/unique constraints safer and keeps the column as VARCHAR(150)
        public string Name { get; set; } = null!;

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; } = 0; // Non-null + default gives predictable ordering (no null handling in queries/views) and aligns with common "sort_order" usage

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; }

        [Display(Name = "Deleted At")]
        public DateTime? DeletedAt { get; set; }

        public CategoryGroup CategoryGroup { get; set; } = null!;
        public Category? Parent { get; set; }
        public ICollection<Category> Children { get; set; } = new List<Category>();

        public DomainUser? CreatedByUser { get; set; } // Lets you Include() the creator and enables proper FK mapping without EF creating "shadow" FK columns
        public DomainUser? UpdatedByUser { get; set; } // Same as above; also makes audit display in MVC views straightforward (e.g., "Last updated by")
    }
}