namespace A_New_Hope.Models
{
    public class UserItemPreference
    {
        public ulong Id { get; set; }

        public ulong UserId { get; set; }
        public ulong InventoryItemId { get; set; }

        public string Preference { get; set; } = null!;

        public ulong? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User User { get; set; } = null!;
        public InventoryItem InventoryItem { get; set; } = null!;
        public User? UpdatedByUser { get; set; }
    }
}
