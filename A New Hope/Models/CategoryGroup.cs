using System.ComponentModel.DataAnnotations; // Needed for [MaxLength] (and optional MVC validation attributes)

namespace A_New_Hope.Models
{
    public class CategoryGroup
    {
        public ulong Id { get; set; }

        [MaxLength(150)] // Prevents EF/MySQL from defaulting to LONGTEXT; supports your unique index on Name cleanly (VARCHAR(150))
        public string Name { get; set; } = null!;

        public int SortOrder { get; set; } = 0; // Non-null + default makes ordering consistent and avoids null checks in queries/views
        public bool IsActive { get; set; } = true;

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<Category> Categories { get; set; } = new List<Category>();

        public DomainUser? CreatedByUser { get; set; } // Enables easy Include() + avoids EF “shadow FK” issues if you later add navs elsewhere
        public DomainUser? UpdatedByUser { get; set; } // Same; useful for admin UI (“Last updated by”)
    }
}