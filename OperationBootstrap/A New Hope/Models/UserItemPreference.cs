namespace A_New_Hope.Models
{
    public class UserItemPreference
    {
        public ulong Id { get; set; }

        public ulong UserId { get; set; }
        public ulong InventoryItemId { get; set; }

        public PreferenceOption Preference { get; set; } = PreferenceOption.Ask;
        // Enum is safer than string (prevents typos/invalid values)

        public ulong? CreatedByUserId { get; set; } // Added so you know who initially set the preference
        public ulong? UpdatedByUserId { get; set; } // Keeps track of who last changed it

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; } // Optional but recommended for soft-delete consistency

        public User User { get; set; } = null!;
        public InventoryItem InventoryItem { get; set; } = null!;

        public User? CreatedByUser { get; set; } // Added to match CreatedByUserId and support Include() in MVC/admin views
        public User? UpdatedByUser { get; set; }
    }
}