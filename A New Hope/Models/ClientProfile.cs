using System.ComponentModel.DataAnnotations;
using A_New_Hope.Models.Enums;

namespace A_New_Hope.Models
{
    /// <summary>
    /// Stores client-specific details that extend a DomainUser record.
    /// </summary>
    public class ClientProfile
    {
        /// <summary>
        /// Primary key and foreign key to DomainUser.Id.
        /// </summary>
        public ulong UserId { get; set; }

        /// <summary>
        /// Optional employment status.
        /// </summary>
        public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.NotSpecified;

        /// <summary>
        /// Indicates whether the client is currently unhoused.
        /// </summary>
        public bool IsUnhoused { get; set; } = false;

        public ulong? CreatedByUserId { get; set; }
        public ulong? UpdatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public DomainUser User { get; set; } = null!;
        public DomainUser? CreatedByUser { get; set; }
        public DomainUser? UpdatedByUser { get; set; }
        public ICollection<ClientIncome> ClientIncomes { get; set; } = new List<ClientIncome>();
    }
}