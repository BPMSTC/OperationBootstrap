namespace A_New_Hope.Models
{
    public class RoleUser
    {
        public ulong UserId { get; set; }
        public ulong RoleId { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public User User { get; set; } = null!;
        public Role Role { get; set; } = null!;
    }
}
