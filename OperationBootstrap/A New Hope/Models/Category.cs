namespace A_New_Hope.Models
{
    public class Category
    {
        public ulong Id { get; set; }

        public ulong CategoryGroupId { get; set; }
        public ulong? ParentId { get; set; }

        public string Name { get; set; } = null!;
        public int? SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public CategoryGroup CategoryGroup { get; set; } = null!;
        public Category? Parent { get; set; }
        public ICollection<Category> Children { get; set; } = new List<Category>();
    }
}
