namespace A_New_Hope.Models
{
    public class CategoryGroup
    {
        public ulong Id { get; set; }

        public string Name { get; set; } = null!;
        public int? SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}
