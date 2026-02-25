namespace A_New_Hope.Models.ViewModels
{
    public class DomainUserIndexRowViewModel
    {
        public ulong Id { get; set; }
        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        public PreferenceOption DefaultPreference { get; set; }
        public UserType UserType { get; set; }

        public bool IsActive { get; set; }

        public bool HasLoginAccount { get; set; }
        public string? IdentityUserId { get; set; }
    }
}