using A_New_Hope.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Data
{
    /// <summary>
    /// ApplicationDbContext
    /// --------------------
    /// This is the EF Core DbContext for the A_New_Hope application.
    ///
    /// IMPORTANT: This DbContext inherits from IdentityDbContext<ApplicationUser>,
    /// which means it includes:
    /// - All standard ASP.NET Core Identity tables (AspNetUsers, AspNetRoles, etc.)
    /// - Your custom ApplicationUser entity (which extends IdentityUser)
    ///
    /// In addition, it defines DbSets for your application "domain" entities:
    /// - DomainUser (your business users table; mapped to "Users")
    /// - ClientProfile (1:1 with DomainUser)
    /// - HouseholdMember (1:M with DomainUser as the client)
    /// - CategoryGroup, Category, InventoryItem (inventory taxonomy and items)
    /// - UserItemPreference (linking users to item preferences)
    /// - ReferringOrganization and Referral (referral tracking)
    ///
    /// Soft Delete Strategy
    /// --------------------
    /// Many of your entities use soft deletes via a nullable DeletedAt column.
    /// This DbContext applies global query filters on those entities:
    ///     entity.HasQueryFilter(e => e.DeletedAt == null);
    ///
    /// That means:
    /// - Any standard query like context.Categories.ToListAsync() automatically excludes deleted rows.
    /// - If you need to include deleted rows, you must explicitly opt out per query using:
    ///     context.Categories.IgnoreQueryFilters()
    ///
    /// Indexing + MySQL Notes
    /// ----------------------
    /// You are using MySQL. With MySQL (and many other DBs), indexing long/unbounded string columns can be problematic.
    /// This DbContext sets explicit max lengths on certain string columns that are indexed (e.g., Email, Name, enum strings).
    /// That improves:
    /// - Schema compatibility
    /// - Index creation reliability
    /// - Query performance
    ///
    /// Relationships Overview
    /// ----------------------
    /// - DomainUser -> ClientProfile: 1:1 (ClientProfile PK = UserId)
    /// - DomainUser -> HouseholdMembers: 1:M
    /// - CategoryGroup -> Categories: 1:M
    /// - Category -> Category(Parent/Children): self-referencing (optional parent)
    /// - Category -> InventoryItems: 1:M
    /// - DomainUser -> UserItemPreferences: 1:M
    /// - InventoryItem -> UserItemPreferences: 1:M
    /// - ReferringOrganization -> Referrals: 1:M
    /// - DomainUser (client) -> Referrals: 1:M
    /// - ApplicationUser (Identity) -> DomainUser: optional 1:1 link via DomainUserId
    ///
    /// IMPORTANT NOTE ABOUT DELETE BEHAVIORS
    /// ------------------------------------
    /// Delete behaviors here control what the DB/EF does when a *parent record is physically deleted*.
    /// Since you primarily soft delete, these may not be triggered often, but they are still important:
    /// - Restrict: prevents deleting parent if children exist
    /// - Cascade: deleting parent deletes children
    /// - SetNull: deleting parent sets FK to null in child
    ///
    /// (No logic changes were made in this commented version.)
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        /// <summary>
        /// Standard DbContext constructor used by dependency injection.
        /// The options (connection string/provider/etc.) are configured in Program.cs.
        /// </summary>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ---------------------------------------------------------------------
        // Domain Entity DbSets
        // ---------------------------------------------------------------------
        // These DbSet<> properties give EF Core access to query and save your domain tables.
        // Note: The expression-bodied members (=> Set<T>()) are equivalent to a standard auto-property.
        public DbSet<DomainUser> DomainUsers => Set<DomainUser>();
        public DbSet<ClientProfile> ClientProfiles => Set<ClientProfile>();
        public DbSet<HouseholdMember> HouseholdMembers => Set<HouseholdMember>();
        public DbSet<CategoryGroup> CategoryGroups => Set<CategoryGroup>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
        public DbSet<InventoryItemOption> InventoryItemOptions => Set<InventoryItemOption>();
        public DbSet<UserItemPreference> UserItemPreferences => Set<UserItemPreference>();
        public DbSet<UserChoiceGroupPreference> UserChoiceGroupPreferences => Set<UserChoiceGroupPreference>();
        public DbSet<ReferringOrganization> ReferringOrganizations => Set<ReferringOrganization>();
        public DbSet<Referral> Referrals => Set<Referral>();
        public DbSet<InventoryChoiceGroup> InventoryChoiceGroups => Set<InventoryChoiceGroup>();
        public DbSet<InventoryChoiceGroupItem> InventoryChoiceGroupItems => Set<InventoryChoiceGroupItem>();
        public DbSet<ClientIncome> ClientIncomes => Set<ClientIncome>();
        public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
        public DbSet<ReferringOrganizationServiceCategory> ReferringOrganizationServiceCategories => Set<ReferringOrganizationServiceCategory>();

        /// <summary>
        /// OnModelCreating is where you configure EF Core mapping rules:
        /// - Table names
        /// - Keys (PK/FK)
        /// - Indexes
        /// - Conversions (enums stored as strings)
        /// - Precision (decimal columns)
        /// - Query filters (soft delete)
        /// - Delete behaviors
        ///
        /// IMPORTANT:
        /// - base.OnModelCreating(modelBuilder) must be called to configure Identity tables.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // IdentityDbContext sets up the Identity schema (AspNetUsers, AspNetRoles, etc.).
            base.OnModelCreating(modelBuilder);

            // =============================================================
            // DOMAIN USERS (your business users table)
            // =============================================================
            modelBuilder.Entity<DomainUser>(entity =>
            {
                // Primary Key
                entity.HasKey(e => e.Id);
                // Map DomainUser to a table named "Users".
                // (Identity users remain in AspNetUsers.)
                entity.ToTable("Users");
                // MySQL-friendly explicit lengths for string columns.
                // Email has an explicit max length to avoid long text issues.
                entity.Property(e => e.Email)
                    .HasMaxLength(255);
                // Useful indexes for common filters and “soft delete” queries.
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.DeletedAt);

                entity.Property(e => e.DateOfBirth)
                    .HasConversion(
                        v => v.HasValue ? v.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                        v => v.HasValue ? DateOnly.FromDateTime(v.Value) : (DateOnly?)null)
                    .HasColumnType("date");

                // Enum-to-string conversions
                // Store the enum as a short string rather than an int for readability and stability.
                entity.Property(e => e.DefaultPreference)
                    .HasConversion<string>()
                    .HasMaxLength(20);
                entity.Property(e => e.UserType)
                    .HasConversion<string>()
                    .HasMaxLength(20);
                // Global soft-delete filter:
                // Any query against DomainUsers automatically excludes rows where DeletedAt is not null.
                entity.HasQueryFilter(e => e.DeletedAt == null);
                // Audit relationships:
                // CreatedByUserId / UpdatedByUserId both point to DomainUsers.
                // DeleteBehavior.SetNull prevents cascading deletes and keeps historical audit data.
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // =============================================================
            // AUTHENTICATION USERS (Identity ApplicationUser)
            // =============================================================
            // ApplicationUser is your Identity login record.
            // It optionally links to exactly one DomainUser via DomainUserId.
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                // Keep default Identity table names (AspNetUsers, AspNetRoles, etc.).
                // Here we only configure the additional DomainUserId link.

                // Unique index on DomainUserId enforces:
                // - A DomainUser can be linked to at most one Identity account.
                // NOTE: DomainUserId is optional (nullable), so users without a link are allowed.
                entity.HasIndex(e => e.DomainUserId).IsUnique();

                // 1:1 optional relationship:
                // ApplicationUser.DomainUserId -> DomainUser.Id
                // OnDelete(SetNull) means:
                // - If the DomainUser were physically deleted, the Identity link is cleared.
                entity.HasOne(e => e.DomainUser)
                    .WithOne()
                    .HasForeignKey<ApplicationUser>(e => e.DomainUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // =============================================================
            // CLIENT PROFILE (1:1 with DomainUser)
            // =============================================================
            modelBuilder.Entity<ClientProfile>(entity =>
            {
                entity.HasKey(e => e.UserId);

                entity.HasIndex(e => e.DeletedAt);

                entity.HasQueryFilter(e => e.DeletedAt == null);

                entity.HasOne(e => e.User)
                    .WithOne(u => u.ClientProfile)
                    .HasForeignKey<ClientProfile>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });


            // =============================================================
            // CLIENT INCOME
            // =============================================================

            modelBuilder.Entity<ClientIncome>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.ClientProfileUserId);
                entity.HasIndex(e => e.IncomeType);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.DeletedAt);

                entity.Property(e => e.IncomeType)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(e => e.MonthlyAmount)
                    .HasPrecision(10, 2);

                entity.HasQueryFilter(e => e.DeletedAt == null);

                entity.HasOne(e => e.ClientProfile)
                    .WithMany(p => p.ClientIncomes)
                    .HasForeignKey(e => e.ClientProfileUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });



            // =============================================================
            // HOUSEHOLD MEMBERS (1:M from DomainUser)
            // =============================================================
            modelBuilder.Entity<HouseholdMember>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Helpful indexes:
                // - ClientUserId for querying household members for a given client
                // - DateOfBirth for any reporting/age-related filtering
                // - DeletedAt to support soft delete filter efficiently
                entity.HasIndex(e => e.ClientUserId);
                entity.HasIndex(e => e.DateOfBirth);
                entity.HasIndex(e => e.DeletedAt);

                // Global soft-delete filter.
                entity.HasQueryFilter(e => e.DeletedAt == null);

                // Relationship:
                // A DomainUser (client) has many household members.
                // Cascade on physical delete of the client user.
                entity.HasOne(e => e.ClientUser)
                    .WithMany()
                    .HasForeignKey(e => e.ClientUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Audit relationships.
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // =============================================================
            // CATEGORY GROUPS
            // =============================================================
            modelBuilder.Entity<CategoryGroup>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Explicit length to keep MySQL indexes friendly and avoid LONGTEXT.
                entity.Property(e => e.Name)
                    .HasMaxLength(150);

                // Unique group names.
                entity.HasIndex(e => e.Name).IsUnique();

                // Common filter indexes.
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.DeletedAt);

                // Soft delete filter.
                entity.HasQueryFilter(e => e.DeletedAt == null);

                // Audit relationships.
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // =============================================================
            // CATEGORIES (belongs to CategoryGroup; optional self-parent)
            // =============================================================
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .HasMaxLength(150);

                // Indexes that support common navigation and uniqueness rules.
                entity.HasIndex(e => e.CategoryGroupId);
                entity.HasIndex(e => e.ParentId);

                // Enforce "unique name within a category group".
                entity.HasIndex(e => new { e.CategoryGroupId, e.Name }).IsUnique();

                entity.HasIndex(e => e.DeletedAt);

                // Soft delete filter.
                entity.HasQueryFilter(e => e.DeletedAt == null);

                // CategoryGroup (principal) -> Categories (dependents)
                // Restrict prevents deleting a group if categories still exist (physical delete scenario).
                entity.HasOne(e => e.CategoryGroup)
                    .WithMany(g => g.Categories)
                    .HasForeignKey(e => e.CategoryGroupId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Self-referencing relationship:
                // Parent category is optional; when a parent is deleted, children are re-parented to null.
                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Audit relationships.
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // =============================================================
            // INVENTORY ITEMS (belongs to Category)
            // =============================================================
            modelBuilder.Entity<InventoryItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Indexes help query common filtering paths (by category, availability, active status).
                entity.HasIndex(e => e.CategoryId);
                entity.HasIndex(e => e.IsAvailable);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.DeletedAt);

                // Soft delete filter.
                entity.HasQueryFilter(e => e.DeletedAt == null);

                // Relationship:
                // InventoryItem belongs to a Category.
                // Restrict prevents deleting a category if inventory items exist (physical delete scenario).
                entity.HasOne(e => e.Category)
                    .WithMany()
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Audit relationships.
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // =============================================================
            // INVENTORY ITEM OPTIONS (belongs to InventoryItem)
            // =============================================================
            modelBuilder.Entity<InventoryItemOption>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .HasMaxLength(100);

                // Helpful indexes for common filtering/sorting
                entity.HasIndex(e => e.InventoryItemId);
                entity.HasIndex(e => new { e.InventoryItemId, e.Name }).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.DeletedAt);

                // Soft delete filter
                entity.HasQueryFilter(e => e.DeletedAt == null);

                // Relationship:
                // InventoryItemOption belongs to one InventoryItem.
                // InventoryItem can have many options.
                // Cascade is reasonable here because options are children of the parent item.
                entity.HasOne(e => e.InventoryItem)
                    .WithMany(i => i.InventoryItemOptions)
                    .HasForeignKey(e => e.InventoryItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Audit relationships
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });


            // =============================================================
            // INVENTORY CHOICE GROUPS
            // =============================================================
            modelBuilder.Entity<InventoryChoiceGroup>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .HasMaxLength(150);

                entity.Property(e => e.DisplayLabel)
                    .HasMaxLength(150);

                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.DeletedAt);

                // Soft delete filter
                entity.HasQueryFilter(e => e.DeletedAt == null);

                // Audit relationships
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // =============================================================
            // INVENTORY CHOICE GROUP ITEMS
            // =============================================================
            modelBuilder.Entity<InventoryChoiceGroupItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.InventoryChoiceGroupId);
                entity.HasIndex(e => e.InventoryItemId);

                // Prevent duplicate membership of the same item in the same group
                entity.HasIndex(e => new { e.InventoryChoiceGroupId, e.InventoryItemId }).IsUnique();

                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.DeletedAt);

                // Soft delete filter
                entity.HasQueryFilter(e => e.DeletedAt == null);

                // Relationship:
                // ChoiceGroupItem belongs to one InventoryChoiceGroup.
                entity.HasOne(e => e.InventoryChoiceGroup)
                    .WithMany(g => g.InventoryChoiceGroupItems)
                    .HasForeignKey(e => e.InventoryChoiceGroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relationship:
                // ChoiceGroupItem belongs to one InventoryItem.
                entity.HasOne(e => e.InventoryItem)
                    .WithMany(i => i.InventoryChoiceGroupItems)
                    .HasForeignKey(e => e.InventoryItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Audit relationships
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // =============================================================
            // USER ITEM PREFERENCES (unique per user + inventory item)
            // =============================================================
            modelBuilder.Entity<UserItemPreference>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Unique constraint prevents duplicates:
                // A given user can have only one preference per inventory item.
                entity.HasIndex(e => new { e.UserId, e.InventoryItemId }).IsUnique();

                entity.HasIndex(e => e.InventoryItemId);
                entity.HasIndex(e => e.InventoryItemOptionId);
                entity.HasIndex(e => e.DeletedAt);

                // Store Preference enum as string for readability.
                entity.Property(e => e.Preference)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                // Soft delete filter.
                entity.HasQueryFilter(e => e.DeletedAt == null);

                // Relationship:
                // Preference belongs to a user.
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relationship:
                // Preference belongs to an inventory item.
                entity.HasOne(e => e.InventoryItem)
                    .WithMany()
                    .HasForeignKey(e => e.InventoryItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relationship:
                // Preference may optionally belong to an inventory item option.
                // SetNull keeps the preference record if the option is physically deleted.
                entity.HasOne(e => e.InventoryItemOption)
                    .WithMany()
                    .HasForeignKey(e => e.InventoryItemOptionId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Audit relationships.
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // =============================================================
            // USER CHOICE GROUP PREFERENCES (unique per user + choice group)
            // =============================================================
            modelBuilder.Entity<UserChoiceGroupPreference>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Unique constraint prevents duplicates:
                // A given user can have only one preference per choice group.
                entity.HasIndex(e => new { e.UserId, e.InventoryChoiceGroupId }).IsUnique();

                entity.HasIndex(e => e.InventoryChoiceGroupId);
                entity.HasIndex(e => e.SelectedInventoryItemId);
                entity.HasIndex(e => e.DeletedAt);

                // Soft delete filter.
                entity.HasQueryFilter(e => e.DeletedAt == null);

                // Relationship:
                // Preference belongs to a user.
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relationship:
                // Preference belongs to a choice group.
                entity.HasOne(e => e.InventoryChoiceGroup)
                    .WithMany()
                    .HasForeignKey(e => e.InventoryChoiceGroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relationship:
                // Preference stores the selected inventory item from that group.
                entity.HasOne(e => e.SelectedInventoryItem)
                    .WithMany()
                    .HasForeignKey(e => e.SelectedInventoryItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Audit relationships.
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // =============================================================
            // SERVICE CATEGORIES
            // =============================================================
            modelBuilder.Entity<ServiceCategory>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.DeletedAt);

                entity.HasQueryFilter(e => e.DeletedAt == null);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // =============================================================
            // REFERRING ORGANIZATION SERVICE CATEGORIES
            // =============================================================
            modelBuilder.Entity<ReferringOrganizationServiceCategory>(entity =>
            {
                entity.HasKey(e => new { e.ReferringOrganizationId, e.ServiceCategoryId });

                entity.HasOne(e => e.ReferringOrganization)
                    .WithMany(o => o.ReferringOrganizationServiceCategories)
                    .HasForeignKey(e => e.ReferringOrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ServiceCategory)
                    .WithMany(c => c.ReferringOrganizationServiceCategories)
                    .HasForeignKey(e => e.ServiceCategoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =============================================================
            // REFERRING ORGANIZATIONS
            // =============================================================
            modelBuilder.Entity<ReferringOrganization>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Indexes for common filters (active status and soft delete).
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.DeletedAt);

                // Soft delete filter.
                entity.HasQueryFilter(e => e.DeletedAt == null);

                // Audit relationships.
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // =============================================================
            // REFERRALS
            // =============================================================
            modelBuilder.Entity<Referral>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Indexes to support common referral queries:
                // - by client
                // - by organization
                // - by date
                // - composite index for client+date timeline queries
                entity.HasIndex(e => e.ClientUserId);
                entity.HasIndex(e => e.ReferringOrganizationId);
                entity.HasIndex(e => e.ReferredOn);
                entity.HasIndex(e => new { e.ClientUserId, e.ReferredOn });
                entity.HasIndex(e => e.DeletedAt);

                // Store ReferralStatus enum as string for readability.
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                // Soft delete filter.
                entity.HasQueryFilter(e => e.DeletedAt == null);

                // Relationship:
                // Referral belongs to a client user (DomainUser).
                // Restrict prevents physical deletion of user if referrals exist.
                entity.HasOne(e => e.ClientUser)
                    .WithMany()
                    .HasForeignKey(e => e.ClientUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relationship:
                // Referral belongs to a ReferringOrganization.
                // Restrict prevents physical deletion of org if referrals exist.
                entity.HasOne(e => e.ReferringOrganization)
                    .WithMany(o => o.Referrals)
                    .HasForeignKey(e => e.ReferringOrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Audit relationships.
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}