namespace A_New_Hope.Models
{
    /// <summary>
    /// Junction table linking a ReferringOrganization to one or more ServiceCategories.
    /// </summary>
    public class ReferringOrganizationServiceCategory
    {
        public ulong ReferringOrganizationId { get; set; }
        public ReferringOrganization ReferringOrganization { get; set; } = null!;

        public ulong ServiceCategoryId { get; set; }
        public ServiceCategory ServiceCategory { get; set; } = null!;
    }
}