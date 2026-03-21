namespace A_New_Hope.Models.ViewModels
{
    /// <summary>
    /// DomainUserIndexRowViewModel
    /// ---------------------------
    /// Lightweight view model used by the Users/Index page to display a list of DomainUsers.
    ///
    /// Why this exists (instead of passing DomainUser entities directly to the view):
    /// - The Index UI typically needs only a subset of fields for display/search/sorting.
    /// - It also needs extra "computed" fields that do not live on DomainUser (ex: HasLoginAccount).
    /// - Using a view model avoids accidental coupling between your Razor view and your EF entities.
    ///
    /// Identity integration notes:
    /// - DomainUser is the "business user" record.
    /// - ApplicationUser is the ASP.NET Core Identity login record.
    /// - Not every DomainUser has a login account.
    /// - HasLoginAccount / IdentityUserId are included so the Index view can show whether a login exists
    ///   and can link to account management actions (Create login, Manage login, etc.).
    /// </summary>
    public class DomainUserIndexRowViewModel
    {
        /// <summary>
        /// Primary key of the DomainUser record (business user).
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// User email address (also commonly used as username for Identity accounts).
        /// Initialized to empty to avoid null checks in the UI.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Optional contact phone number.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Optional first name (some records may be created without names initially).
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Optional last name.
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Optional city portion of the address (for display/filtering).
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// Optional state portion of the address (for display/filtering).
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Optional postal/zip code (for display/filtering).
        /// </summary>
        public string? PostalCode { get; set; }

        /// <summary>
        /// Optional date of birth. DateOnly indicates the app is storing date without time-of-day.
        /// </summary>
        public DateOnly? DateOfBirth { get; set; }

        /// <summary>
        /// The user's default preference behavior (Always / Ask / Never) for inventory items
        /// when no specific per-item preference exists.
        /// </summary>
        public PreferenceOption DefaultPreference { get; set; }

        /// <summary>
        /// Business-level user classification (Client / Staff / Admin).
        /// This is used to drive role/access decisions and UI behavior at the domain level.
        /// </summary>
        public UserType UserType { get; set; }

        /// <summary>
        /// Indicates whether this DomainUser is active in the application.
        /// In many systems this also drives whether login is enabled (if the user has an Identity account).
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// True if there is an associated Identity login account (ApplicationUser) linked to this DomainUser.
        /// </summary>
        public bool HasLoginAccount { get; set; }

        /// <summary>
        /// The Identity user's primary key (ApplicationUser.Id) when a login exists; otherwise null.
        /// Useful for linking to account management actions/views.
        /// </summary>
        public string? IdentityUserId { get; set; }
    }
}