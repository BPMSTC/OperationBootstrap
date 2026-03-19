using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models
{
    public class Category
    {
        public ulong Id { get; set; }

        [Required]
        [Display(Name = "Category Group")]
        public ulong CategoryGroupId { get; set; }

        [Display(Name = "Parent Category")]
        public ulong? ParentId { get; set; }

        [Required]
        [StringLength(150, ErrorMessage = "Category name cannot exceed 150 characters.")]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = null!;

        [Display(Name = "Sort Order")]
        [Range(0, int.MaxValue, ErrorMessage = "Sort Order must be zero or greater.")]
        public int SortOrder { get; set; } = 0;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public ulong? CreatedByUserId { get; set; }

        public ulong? UpdatedByUserId { get; set; }

        [Display(Name = "Created At")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Updated At")]
        [DataType(DataType.DateTime)]
        public DateTime UpdatedAt { get; set; }

        [Display(Name = "Deleted At")]
        [DataType(DataType.DateTime)]
        public DateTime? DeletedAt { get; set; }

        public CategoryGroup CategoryGroup { get; set; } = null!;

        public Category? Parent { get; set; }

        public ICollection<Category> Children { get; set; } = new List<Category>();

        public DomainUser? CreatedByUser { get; set; }

        public DomainUser? UpdatedByUser { get; set; }
    }
}