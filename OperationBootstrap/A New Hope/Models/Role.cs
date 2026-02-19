namespace A_New_Hope.Models
{
    public class Role
    {
        public ulong Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public User? CreatedByUser { get; set; }
        public User? UpdatedByUser { get; set; }

        public ICollection<RoleUser> RoleUsers { get; set; } = new List<RoleUser>();
    }
}
