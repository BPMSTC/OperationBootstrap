using A_New_Hope.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RoleUser> RoleUsers => Set<RoleUser>();
    public DbSet<ClientProfile> ClientProfiles => Set<ClientProfile>();
    public DbSet<HouseholdMember> HouseholdMembers => Set<HouseholdMember>();
    public DbSet<CategoryGroup> CategoryGroups => Set<CategoryGroup>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<UserItemPreference> UserItemPreferences => Set<UserItemPreference>();
    public DbSet<ReferringOrganization> ReferringOrganizations => Set<ReferringOrganization>();
    public DbSet<Referral> Referrals => Set<Referral>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==============================
        // USERS
        // ==============================
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.LastLoginAt);
            entity.HasIndex(e => e.DeletedAt);

            entity.Property(e => e.DefaultPreference)
                .HasConversion<string>()
                .HasMaxLength(20);

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

        // ==============================
        // ROLES
        // ==============================
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Name).IsUnique();

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

        // ==============================
        // ROLE_USER (Pivot)
        // ==============================
        modelBuilder.Entity<RoleUser>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.RoleId);

            entity.HasOne(e => e.User)
                .WithMany(u => u.RoleUsers)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.RoleUsers)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ==============================
        // CLIENT PROFILE (1:1)
        // ==============================
        modelBuilder.Entity<ClientProfile>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.Property(e => e.EarnedIncomeMonthly)
                .HasPrecision(10, 2);

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

        // ==============================
        // HOUSEHOLD MEMBERS
        // ==============================
        modelBuilder.Entity<HouseholdMember>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.ClientUserId);
            entity.HasIndex(e => e.DateOfBirth);

            entity.HasQueryFilter(e => e.DeletedAt == null);

            entity.HasOne(e => e.ClientUser)
                .WithMany()
                .HasForeignKey(e => e.ClientUserId)
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

        // ==============================
        // CATEGORY GROUPS
        // ==============================
        modelBuilder.Entity<CategoryGroup>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        // ==============================
        // CATEGORIES
        // ==============================
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.CategoryGroupId);
            entity.HasIndex(e => e.ParentId);
            entity.HasIndex(e => new { e.CategoryGroupId, e.Name }).IsUnique();

            entity.HasQueryFilter(e => e.DeletedAt == null);

            entity.HasOne(e => e.CategoryGroup)
                .WithMany(g => g.Categories)
                .HasForeignKey(e => e.CategoryGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ==============================
        // INVENTORY ITEMS
        // ==============================
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.IsAvailable);
            entity.HasIndex(e => e.IsActive);

            entity.HasQueryFilter(e => e.DeletedAt == null);

            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ==============================
        // USER ITEM PREFERENCES
        // ==============================
        modelBuilder.Entity<UserItemPreference>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.UserId, e.InventoryItemId }).IsUnique();
            entity.HasIndex(e => e.InventoryItemId);

            entity.Property(e => e.Preference)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.InventoryItem)
                .WithMany()
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.UpdatedByUser)
                .WithMany()
                .HasForeignKey(e => e.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ==============================
        // REFERRING ORGANIZATIONS
        // ==============================
        modelBuilder.Entity<ReferringOrganization>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.IsActive);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        // ==============================
        // REFERRALS
        // ==============================
        modelBuilder.Entity<Referral>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.ClientUserId);
            entity.HasIndex(e => e.ReferringOrganizationId);
            entity.HasIndex(e => e.ReferredOn);
            entity.HasIndex(e => new { e.ClientUserId, e.ReferredOn });

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasQueryFilter(e => e.DeletedAt == null);

            entity.HasOne(e => e.ClientUser)
                .WithMany()
                .HasForeignKey(e => e.ClientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReferringOrganization)
                .WithMany(o => o.Referrals)
                .HasForeignKey(e => e.ReferringOrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

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