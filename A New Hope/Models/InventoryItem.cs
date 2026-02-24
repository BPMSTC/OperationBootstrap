using System.ComponentModel.DataAnnotations; // Added for [MaxLength]

namespace A_New_Hope.Models
{
    public class InventoryItem
    {
        public ulong Id { get; set; }

        [MaxLength(200)] // Prevents EF/MySQL from defaulting to LONGTEXT; keeps the column as VARCHAR and supports future indexing/searching
        public string Name { get; set; } = null!;

        public ulong CategoryId { get; set; }

        public bool IsBaseline { get; set; } = false; // Keep if you need a "default catalog" flag; otherwise consider removing later
        public bool IsAvailable { get; set; } = true; // Availability can change day-to-day without removing the item from the catalog
        public bool IsActive { get; set; } = true; // "Catalog enabled/disabled" flag; different meaning than IsAvailable

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public Category Category { get; set; } = null!;

        public DomainUser? CreatedByUser { get; set; } // Lets you Include() audit users and avoids relying on FK IDs only
        public DomainUser? UpdatedByUser { get; set; } // Same; makes admin auditing/UX much easier
    }
}