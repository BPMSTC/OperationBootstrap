// This seeder is designed to be safely re-runnable for local development.
// Each data section checks whether the target table already contains rows
// (using AnyAsync()) before inserting seed data. That helps prevent duplicate
// records when the app starts multiple times.
//
// Note: This is "safe-ish" for initial/dev seeding because it skips entire
// sections once data exists. It does not upsert or reconcile changed seed values.

using A_New_Hope.Models;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Ensure DB exists / migrations applied
            await context.Database.MigrateAsync();

            var now = DateTime.UtcNow;

            // ------------------------------
            // USERS
            // ------------------------------
            // Seed only if table is empty (AnyAsync prevents duplicates on rerun)

            if (!await context.Users.AnyAsync())
            {
                var admin = new User
                {
                    Email = "admin@anewhope.local",
                    PasswordHash = "TEMP_HASH_REPLACE_LATER",
                    FirstName = "System",
                    LastName = "Admin",
                    Role = UserRole.Admin,
                    DefaultPreference = PreferenceOption.Ask,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                var client1 = new User
                {
                    Email = "client1@anewhope.local",
                    PasswordHash = "TEMP_HASH_REPLACE_LATER",
                    FirstName = "Jamie",
                    LastName = "Client",
                    Role = UserRole.User,
                    DefaultPreference = PreferenceOption.Ask,
                    IsActive = true,
                    PhoneNumber = "555-111-2222",
                    City = "Stevens Point",
                    State = "WI",
                    PostalCode = "54481",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                var client2 = new User
                {
                    Email = "client2@anewhope.local",
                    PasswordHash = "TEMP_HASH_REPLACE_LATER",
                    FirstName = "Taylor",
                    LastName = "Client",
                    Role = UserRole.User,
                    DefaultPreference = PreferenceOption.Always,
                    IsActive = true,
                    PhoneNumber = "555-333-4444",
                    City = "Plover",
                    State = "WI",
                    PostalCode = "54467",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                context.Users.AddRange(admin, client1, client2);
                await context.SaveChangesAsync();
            }

            // Pull seeded users (works whether they were just inserted or already existed)
            var adminUser = await context.Users.FirstAsync(u => u.Email == "admin@anewhope.local");
            var clientUser1 = await context.Users.FirstAsync(u => u.Email == "client1@anewhope.local");
            var clientUser2 = await context.Users.FirstAsync(u => u.Email == "client2@anewhope.local");

            // ------------------------------
            // CLIENT PROFILES
            // ------------------------------
            // Seed only if table is empty (AnyAsync prevents duplicates on rerun)

            if (!await context.ClientProfiles.AnyAsync())
            {
                context.ClientProfiles.AddRange(
                    new ClientProfile
                    {
                        UserId = clientUser1.Id,
                        EmploymentStatus = "Part-time",
                        EarnedIncomeMonthly = 1200.00m,
                        IsUnhoused = false,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new ClientProfile
                    {
                        UserId = clientUser2.Id,
                        EmploymentStatus = "Unemployed",
                        EarnedIncomeMonthly = 0m,
                        IsUnhoused = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }

            // ------------------------------
            // HOUSEHOLD MEMBERS
            // ------------------------------
            // Seed only if table is empty (AnyAsync prevents duplicates on rerun)

            if (!await context.HouseholdMembers.AnyAsync())
            {
                context.HouseholdMembers.AddRange(
                    new HouseholdMember
                    {
                        ClientUserId = clientUser1.Id,
                        FirstName = "Casey",
                        LastName = "Client",
                        DateOfBirth = new DateOnly(2015, 6, 12),
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new HouseholdMember
                    {
                        ClientUserId = clientUser2.Id,
                        FirstName = "Morgan",
                        LastName = "Client",
                        DateOfBirth = new DateOnly(2012, 11, 3),
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }

            // ------------------------------
            // CATEGORY GROUPS
            // ------------------------------
            // Seed only if table is empty (AnyAsync prevents duplicates on rerun)

            if (!await context.CategoryGroups.AnyAsync())
            {
                context.CategoryGroups.AddRange(
                    new CategoryGroup
                    {
                        Name = "Food",
                        SortOrder = 1,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new CategoryGroup
                    {
                        Name = "Non-Food",
                        SortOrder = 2,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }

            var foodGroup = await context.CategoryGroups.FirstAsync(g => g.Name == "Food");
            var nonFoodGroup = await context.CategoryGroups.FirstAsync(g => g.Name == "Non-Food");

            // ------------------------------
            // CATEGORIES
            // ------------------------------
            // Seed only if table is empty (AnyAsync prevents duplicates on rerun)
            
            if (!await context.Categories.AnyAsync())
            {
                context.Categories.AddRange(
                    new Category
                    {
                        CategoryGroupId = foodGroup.Id,
                        Name = "Canned Goods",
                        SortOrder = 1,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Category
                    {
                        CategoryGroupId = foodGroup.Id,
                        Name = "Produce",
                        SortOrder = 2,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Category
                    {
                        CategoryGroupId = nonFoodGroup.Id,
                        Name = "Hygiene",
                        SortOrder = 1,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Category
                    {
                        CategoryGroupId = nonFoodGroup.Id,
                        Name = "Household Supplies",
                        SortOrder = 2,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }

            var cannedGoods = await context.Categories.FirstAsync(c => c.Name == "Canned Goods");
            var produce = await context.Categories.FirstAsync(c => c.Name == "Produce");
            var hygiene = await context.Categories.FirstAsync(c => c.Name == "Hygiene");
            var household = await context.Categories.FirstAsync(c => c.Name == "Household Supplies");

            // ------------------------------
            // REFERRING ORGANIZATIONS
            // ------------------------------
            // Seed only if table is empty (AnyAsync prevents duplicates on rerun)

            if (!await context.ReferringOrganizations.AnyAsync())
            {
                context.ReferringOrganizations.AddRange(
                    new ReferringOrganization
                    {
                        Name = "Portage County Social Services",
                        Type = "County Agency",
                        PhoneNumber = "555-100-2000",
                        Email = "referrals@portagecounty.local",
                        City = "Stevens Point",
                        State = "WI",
                        PostalCode = "54481",
                        PrimaryContactName = "Alex Rivera",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new ReferringOrganization
                    {
                        Name = "Hope Community Clinic",
                        Type = "Clinic",
                        PhoneNumber = "555-300-4000",
                        Email = "intake@hopeclinic.local",
                        City = "Plover",
                        State = "WI",
                        PostalCode = "54467",
                        PrimaryContactName = "Jordan Lee",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }

            var org1 = await context.ReferringOrganizations.FirstAsync(o => o.Name == "Portage County Social Services");

            // ------------------------------
            // INVENTORY ITEMS
            // ------------------------------
            // Seed only if table is empty (AnyAsync prevents duplicates on rerun)

            if (!await context.InventoryItems.AnyAsync())
            {
                context.InventoryItems.AddRange(
                    new InventoryItem
                    {
                        Name = "Canned Soup",
                        CategoryId = cannedGoods.Id,
                        IsBaseline = true,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Apples",
                        CategoryId = produce.Id,
                        IsBaseline = true,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Toothpaste",
                        CategoryId = hygiene.Id,
                        IsBaseline = true,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Laundry Detergent",
                        CategoryId = household.Id,
                        IsBaseline = true,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }

            // ------------------------------
            // REFERRALS
            // ------------------------------
            // Seed only if table is empty (AnyAsync prevents duplicates on rerun)

            if (!await context.Referrals.AnyAsync())
            {
                context.Referrals.Add(
                    new Referral
                    {
                        ClientUserId = clientUser1.Id,
                        ReferringOrganizationId = org1.Id,
                        ReferredOn = DateOnly.FromDateTime(DateTime.UtcNow),
                        Status = ReferralStatus.Pending,
                        ReferredByName = "Alex Rivera",
                        ReferredByPhoneNumber = "555-100-2000",
                        ReferredByEmail = "referrals@portagecounty.local",
                        Notes = "Initial seeded referral.",
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }

            // ------------------------------
            // USER ITEM PREFERENCES
            // ------------------------------
            // Seed only if table is empty (AnyAsync prevents duplicates on rerun)

            if (!await context.UserItemPreferences.AnyAsync())
            {
                var toothpaste = await context.InventoryItems.FirstAsync(i => i.Name == "Toothpaste");
                var applesItem = await context.InventoryItems.FirstAsync(i => i.Name == "Apples");

                context.UserItemPreferences.AddRange(
                    new UserItemPreference
                    {
                        UserId = clientUser1.Id,
                        InventoryItemId = toothpaste.Id,
                        Preference = PreferenceOption.Always,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new UserItemPreference
                    {
                        UserId = clientUser1.Id,
                        InventoryItemId = applesItem.Id,
                        Preference = PreferenceOption.Ask,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }
        }
    }
}