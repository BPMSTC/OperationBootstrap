namespace A_New_Hope.Models
{
    public class InventoryItem
    {
        public ulong Id { get; set; }

        public string Name { get; set; } = null!;
        public ulong CategoryId { get; set; }

        public bool IsBaseline { get; set; } = false;
        public bool IsAvailable { get; set; } = true;
        public bool IsActive { get; set; } = true;

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public Category Category { get; set; } = null!;
    }
}
