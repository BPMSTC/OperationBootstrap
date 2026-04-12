// DataSeeder.cs
// ------------
// This seeder is designed to be safely re-runnable for local development.
//
// How “safe re-run” works in THIS implementation:
// - Each section checks whether the target table already contains rows (AnyAsync()).
// - If the table is NOT empty, that entire section is skipped.
// - This prevents duplicates on repeated application startup.
//
// What this seeder does NOT do:
// - It does not “upsert” (insert-or-update) records.
// - It does not reconcile changes if you modify seed values later.
// - It does not guarantee partial sections are consistent if you manually delete only some rows.
//   Example: If you delete Categories but leave CategoryGroups, the Categories section will run,
//   but the CategoryGroups section will be skipped (because it’s not empty). That might be okay,
//   but it’s important to understand the behavior.
//
// Typical usage:
// - Call SeedAsync at application startup (often from Program.cs) in dev/local environments.
// - The call to MigrateAsync() ensures the schema is created/updated before inserting seed rows.

using A_New_Hope.Models;
using Microsoft.EntityFrameworkCore;
using A_New_Hope.Models.Enums;

namespace A_New_Hope.Data
{
    public static class DataSeeder
    {
        /// <summary>
        /// Seeds the database with minimal “starter” data for development/testing.
        ///
        /// Important behaviors:
        /// - Ensures migrations are applied before seeding.
        /// - Uses UTC timestamps for all CreatedAt/UpdatedAt fields.
        /// - Uses AnyAsync() checks per table to prevent duplicate inserts on reruns.
        ///
        /// Parameter:
        /// - context: The EF Core DbContext connected to your database.
        /// </summary>
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // ------------------------------------------------------------
            // 0) Ensure database is created and migrations are applied
            // ------------------------------------------------------------
            // This will:
            // - Create the database if it does not exist
            // - Apply any pending EF Core migrations
            //
            // If migrations fail (bad connection string, migration errors), this will throw
            // and the app startup will fail (which is usually what you want in dev).
            await context.Database.MigrateAsync();

            // Use a single "now" timestamp so seeded rows share a consistent CreatedAt/UpdatedAt value.
            // This makes the seed data easy to reason about and avoids small time skews.
            var now = DateTime.UtcNow;

            // ============================================================
            // USERS (DomainUsers)
            // ============================================================
            // DomainUsers represent your application’s "business user" records (clients, staff, admins).
            //
            // This section seeds:
            // - 1 admin user
            // - 2 client users
            //
            // NOTE: This seeds ONLY DomainUsers; Identity (login accounts) is handled separately
            // by your IdentitySeeder.
            if (!await context.DomainUsers.AnyAsync())
            {
                // Admin user (staff/admin login concepts are typically handled via Identity roles)
                var admin = new DomainUser
                {
                    Email = "admin@anewhope.local",
                    FirstName = "System",
                    LastName = "Admin",
                    UserType = UserType.Admin,
                    DefaultPreference = PreferenceOption.Ask,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                // Client #1
                var client1 = new DomainUser
                {
                    Email = "client1@anewhope.local",
                    FirstName = "Jamie",
                    LastName = "Client",
                    UserType = UserType.Client,
                    DefaultPreference = PreferenceOption.Ask,
                    IsActive = true,
                    PhoneNumber = "555-111-2222",
                    City = "Stevens Point",
                    State = "WI",
                    PostalCode = "54481",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                // Client #2
                var client2 = new DomainUser
                {
                    Email = "client2@anewhope.local",
                    FirstName = "Taylor",
                    LastName = "Client",
                    UserType = UserType.Client,
                    DefaultPreference = PreferenceOption.Always,
                    IsActive = true,
                    PhoneNumber = "555-333-4444",
                    City = "Plover",
                    State = "WI",
                    PostalCode = "54467",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                // AddRange stages the inserts; SaveChangesAsync commits them to the DB.
                context.DomainUsers.AddRange(admin, client1, client2);
                await context.SaveChangesAsync();
            }

            // Pull seeded users (works whether they were just inserted OR already existed)
            // -------------------------------------------------------------------------
            // These lookups are used as foreign keys in later sections (profiles, referrals, audit fields, etc.).
            //
            // NOTE: FirstAsync will throw if the email isn't found.
            // In dev seeding, that is usually acceptable because it indicates inconsistent seed state.
            var adminUser = await context.DomainUsers.FirstAsync(u =>
                u.UserType == UserType.Admin &&
                u.FirstName == "System" &&
                u.LastName == "Admin");

            var clientUser1 = await context.DomainUsers.FirstAsync(u =>
                u.UserType == UserType.Client &&
                u.FirstName == "Jamie" &&
                u.LastName == "Client");

            var clientUser2 = await context.DomainUsers.FirstAsync(u =>
                u.UserType == UserType.Client &&
                u.FirstName == "Taylor" &&
                u.LastName == "Client");

            // ============================================================
            // CLIENT PROFILES
            // ============================================================
            // ClientProfiles represent additional client-specific data tied 1:1 to a DomainUser (UserId).
            // This section seeds one profile per client user.
            if (!await context.ClientProfiles.AnyAsync())
            {
                context.ClientProfiles.AddRange(
                    new ClientProfile
                    {
                        UserId = clientUser1.Id,
                        EmploymentStatus = EmploymentStatus.PartTime,
                        IsUnhoused = false,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new ClientProfile
                    {
                        UserId = clientUser2.Id,
                        EmploymentStatus = EmploymentStatus.Unemployed,
                        IsUnhoused = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }

            // ============================================================
            // CLIENT INCOMES
            // ============================================================
            // ClientIncomes store categorized monthly income rows for each client profile.
            if (!await context.ClientIncomes.AnyAsync())
            {
                context.ClientIncomes.AddRange(
                    new ClientIncome
                    {
                        ClientProfileUserId = clientUser1.Id,
                        IncomeType = IncomeType.Employment,
                        MonthlyAmount = 1200.00m,
                        IsActive = true,
                        Notes = null,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new ClientIncome
                    {
                        ClientProfileUserId = clientUser2.Id,
                        IncomeType = IncomeType.Unemployment,
                        MonthlyAmount = 300.00m,
                        IsActive = true,
                        Notes = null,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }

            // ============================================================
            // HOUSEHOLD MEMBERS
            // ============================================================
            // HouseholdMembers represent additional people in a client’s household.
            // Each member is linked to a client user via ClientUserId.
            if (!await context.HouseholdMembers.AnyAsync())
            {
                context.HouseholdMembers.AddRange(
                    new HouseholdMember
                    {
                        ClientUserId = clientUser1.Id,
                        FirstName = "Casey",
                        LastName = "Client",
                        DateOfBirth = new DateTime(2015, 6, 12),
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
                        DateOfBirth = new DateTime(2012, 11, 3),
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }

            // ============================================================
            // CATEGORY GROUPS
            // ============================================================
            // CategoryGroups are high-level groupings such as "Food" and "Non-Food".
            // Categories belong to CategoryGroups.
            if (!await context.CategoryGroups.AnyAsync())
            {
                context.CategoryGroups.AddRange(
                    new CategoryGroup
                    {
                        Name = "Food",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new CategoryGroup
                    {
                        Name = "Non-Food",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }

            // Pull category groups for FK use in Categories seeding.
            var foodGroup = await context.CategoryGroups.FirstAsync(g => g.Name == "Food");
            var nonFoodGroup = await context.CategoryGroups.FirstAsync(g => g.Name == "Non-Food");

            // ============================================================
            // CATEGORIES
            // ============================================================
            // Categories belong to CategoryGroups and are used to classify InventoryItems.
            if (!await context.Categories.AnyAsync())
            {
                context.Categories.AddRange(
                    // ------------------------------
                    // FOOD
                    // ------------------------------
                    new Category
                    {
                        CategoryGroupId = foodGroup.Id,
                        Name = "Canned Goods",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Category
                    {
                        CategoryGroupId = foodGroup.Id,
                        Name = "Pasta / Grains",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Category
                    {
                        CategoryGroupId = foodGroup.Id,
                        Name = "Breakfast / Pantry",
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
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Category
                    {
                        CategoryGroupId = foodGroup.Id,
                        Name = "Refrigerated",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Category
                    {
                        CategoryGroupId = foodGroup.Id,
                        Name = "Frozen Protein",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Category
                    {
                        CategoryGroupId = foodGroup.Id,
                        Name = "Condiments / Baking Staples",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // ------------------------------
                    // NON-FOOD
                    // ------------------------------
                    new Category
                    {
                        CategoryGroupId = nonFoodGroup.Id,
                        Name = "Paper Goods",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Category
                    {
                        CategoryGroupId = nonFoodGroup.Id,
                        Name = "Personal Care",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new Category
                    {
                        CategoryGroupId = nonFoodGroup.Id,
                        Name = "Cleaning Supplies",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }

            // Pull categories for FK use in InventoryItems seeding.
            // Defensive lookups: include CategoryGroupId so the seeder does not become ambiguous
            // if duplicate category names are ever added under different groups.
            var cannedGoods = await context.Categories
                .FirstAsync(c => c.Name == "Canned Goods" && c.CategoryGroupId == foodGroup.Id);

            var pastaGrains = await context.Categories
                .FirstAsync(c => c.Name == "Pasta / Grains" && c.CategoryGroupId == foodGroup.Id);

            var breakfastPantry = await context.Categories
                .FirstAsync(c => c.Name == "Breakfast / Pantry" && c.CategoryGroupId == foodGroup.Id);

            var produce = await context.Categories
                .FirstAsync(c => c.Name == "Produce" && c.CategoryGroupId == foodGroup.Id);

            var refrigerated = await context.Categories
                .FirstAsync(c => c.Name == "Refrigerated" && c.CategoryGroupId == foodGroup.Id);

            var frozenProtein = await context.Categories
                .FirstAsync(c => c.Name == "Frozen Protein" && c.CategoryGroupId == foodGroup.Id);

            var condimentsBaking = await context.Categories
                .FirstAsync(c => c.Name == "Condiments / Baking Staples" && c.CategoryGroupId == foodGroup.Id);

            var paperGoods = await context.Categories
                .FirstAsync(c => c.Name == "Paper Goods" && c.CategoryGroupId == nonFoodGroup.Id);

            var personalCare = await context.Categories
                .FirstAsync(c => c.Name == "Personal Care" && c.CategoryGroupId == nonFoodGroup.Id);

            var cleaningSupplies = await context.Categories
                .FirstAsync(c => c.Name == "Cleaning Supplies" && c.CategoryGroupId == nonFoodGroup.Id);


            // ============================================================
            // SERVICE CATEGORIES
            // ============================================================

            if (!await context.ServiceCategories.AnyAsync())
            {
                context.ServiceCategories.AddRange(
                    new ServiceCategory
                    {
                        Name = "Food",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new ServiceCategory
                    {
                        Name = "Medical",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new ServiceCategory
                    {
                        Name = "Transportation",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new ServiceCategory
                    {
                        Name = "Clothing",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new ServiceCategory
                    {
                        Name = "Hygiene",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new ServiceCategory
                    {
                        Name = "Baby Supplies",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }

            // ============================================================
            // REFERRING ORGANIZATIONS
            // ============================================================
            // ReferringOrganizations represent external agencies/organizations that refer clients.
            // Referrals will link a client to a ReferringOrganization.
            if (!await context.ReferringOrganizations.AnyAsync())
            {
                context.ReferringOrganizations.AddRange(
                new ReferringOrganization
                {
                    Name = "Portage County Social Services",
                    PhoneNumber = "555-100-2000",
                    Email = "referrals@portagecounty.local",
                    AddressLine1 = "1462 Main Street",
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
                    PhoneNumber = "555-300-4000",
                    Email = "intake@hopeclinic.local",
                    AddressLine1 = "825 Clinic Avenue",
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
            var org2 = await context.ReferringOrganizations.FirstAsync(o => o.Name == "Hope Community Clinic");

            var foodServiceCategory = await context.ServiceCategories.FirstAsync(c => c.Name == "Food");
            var medicalServiceCategory = await context.ServiceCategories.FirstAsync(c => c.Name == "Medical");
            var transportationServiceCategory = await context.ServiceCategories.FirstAsync(c => c.Name == "Transportation");

            if (!await context.ReferringOrganizationServiceCategories.AnyAsync())
            {
                context.ReferringOrganizationServiceCategories.AddRange(
                    new ReferringOrganizationServiceCategory
                    {
                        ReferringOrganizationId = org1.Id,
                        ServiceCategoryId = foodServiceCategory.Id
                    },
                    new ReferringOrganizationServiceCategory
                    {
                        ReferringOrganizationId = org1.Id,
                        ServiceCategoryId = transportationServiceCategory.Id
                    },
                    new ReferringOrganizationServiceCategory
                    {
                        ReferringOrganizationId = org2.Id,
                        ServiceCategoryId = medicalServiceCategory.Id
                    }
                );

                await context.SaveChangesAsync();
            }


            // ============================================================
            // INVENTORY ITEMS
            // ============================================================
            // InventoryItems represent the “things” clients can request/receive.
            // Each InventoryItem belongs to a Category (and therefore indirectly to a CategoryGroup).
            if (!await context.InventoryItems.AnyAsync())
            {
                context.InventoryItems.AddRange(
                    // ========================================================
                    // BASELINE ITEMS (from stakeholder "always available" form)
                    // ========================================================

                    // Food -> Canned Goods
                    new InventoryItem
                    {
                        Name = "Diced Tomato",
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
                        Name = "Canned Vegetables",
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
                        Name = "Canned Fruit",
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
                        Name = "Canned Chicken",
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
                        Name = "Canned Tuna",
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
                        Name = "Pork and Beans",
                        CategoryId = cannedGoods.Id,
                        IsBaseline = true,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Food -> Pasta / Grains
                    new InventoryItem
                    {
                        Name = "Pasta Sauce",
                        CategoryId = pastaGrains.Id,
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
                        Name = "Mac n Cheese",
                        CategoryId = pastaGrains.Id,
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
                        Name = "Pasta",
                        CategoryId = pastaGrains.Id,
                        IsBaseline = true,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Food -> Breakfast / Pantry
                    new InventoryItem
                    {
                        Name = "Graham Crackers",
                        CategoryId = breakfastPantry.Id,
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
                        Name = "Peanut Butter",
                        CategoryId = breakfastPantry.Id,
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
                        Name = "Cereal",
                        CategoryId = breakfastPantry.Id,
                        IsBaseline = true,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Food -> Produce
                    new InventoryItem
                    {
                        Name = "Fresh Fruit",
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
                        Name = "Carrots",
                        CategoryId = produce.Id,
                        IsBaseline = true,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Food -> Refrigerated
                    new InventoryItem
                    {
                        Name = "Margarine",
                        CategoryId = refrigerated.Id,
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
                        Name = "Eggs",
                        CategoryId = refrigerated.Id,
                        IsBaseline = true,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Food -> Frozen Protein
                    new InventoryItem
                    {
                        Name = "Hot Dogs",
                        CategoryId = frozenProtein.Id,
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
                        Name = "Ground Meats",
                        CategoryId = frozenProtein.Id,
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
                        Name = "Frozen Chicken",
                        CategoryId = frozenProtein.Id,
                        IsBaseline = true,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Non-Food -> Paper Goods
                    new InventoryItem
                    {
                        Name = "Toilet Paper",
                        CategoryId = paperGoods.Id,
                        IsBaseline = true,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // ========================================================
                    // VARIABLE / NON-BASELINE ITEMS
                    // (from stakeholder "may not have" / on-site form)
                    // ========================================================

                    // Food -> Refrigerated
                    new InventoryItem
                    {
                        Name = "Milk",
                        CategoryId = refrigerated.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Bread",
                        CategoryId = refrigerated.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Food -> Produce
                    new InventoryItem
                    {
                        Name = "Potatoes",
                        CategoryId = produce.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Bananas",
                        CategoryId = produce.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Non-Food -> Cleaning Supplies
                    new InventoryItem
                    {
                        Name = "Laundry Soap",
                        CategoryId = cleaningSupplies.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Dish Soap",
                        CategoryId = cleaningSupplies.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Non-Food -> Personal Care
                    new InventoryItem
                    {
                        Name = "Shampoo",
                        CategoryId = personalCare.Id,
                        IsBaseline = false,
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
                        CategoryId = personalCare.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Bar Soap",
                        CategoryId = personalCare.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Food -> Condiments / Baking Staples
                    new InventoryItem
                    {
                        Name = "Vegetable Oil",
                        CategoryId = condimentsBaking.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Mayo",
                        CategoryId = condimentsBaking.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Ketchup",
                        CategoryId = condimentsBaking.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Mustard",
                        CategoryId = condimentsBaking.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Sugar",
                        CategoryId = condimentsBaking.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Flour",
                        CategoryId = condimentsBaking.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Pancake Syrup",
                        CategoryId = condimentsBaking.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Jelly",
                        CategoryId = condimentsBaking.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Food -> Pasta / Grains
                    new InventoryItem
                    {
                        Name = "Rice",
                        CategoryId = pastaGrains.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Split Peas",
                        CategoryId = pastaGrains.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Lentils",
                        CategoryId = pastaGrains.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Pancake Mix",
                        CategoryId = pastaGrains.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Oatmeal",
                        CategoryId = pastaGrains.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Food -> Canned Goods
                    new InventoryItem
                    {
                        Name = "Pinto Beans",
                        CategoryId = cannedGoods.Id,
                        IsBaseline = false,
                        IsAvailable = true,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItem
                    {
                        Name = "Black Beans",
                        CategoryId = cannedGoods.Id,
                        IsBaseline = false,
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

            // ============================================================
            // INVENTORY ITEM OPTIONS
            // ============================================================
            // InventoryItemOptions represent true variants/sub-selections
            // of the same InventoryItem.
            //
            // Examples:
            // - Milk -> 1%, 2%
            // - Bread -> White, Wheat, Both
            // - Rice -> White, Brown
            if (!await context.InventoryItemOptions.AnyAsync())
            {
                var milk = await context.InventoryItems.FirstAsync(i => i.Name == "Milk");
                var bread = await context.InventoryItems.FirstAsync(i => i.Name == "Bread");
                var rice = await context.InventoryItems.FirstAsync(i => i.Name == "Rice");
                var oatmeal = await context.InventoryItems.FirstAsync(i => i.Name == "Oatmeal");
                var pintoBeans = await context.InventoryItems.FirstAsync(i => i.Name == "Pinto Beans");
                var blackBeans = await context.InventoryItems.FirstAsync(i => i.Name == "Black Beans");

                context.InventoryItemOptions.AddRange(
                    // Milk
                    new InventoryItemOption
                    {
                        InventoryItemId = milk.Id,
                        Name = "1%",
                        SortOrder = 1,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItemOption
                    {
                        InventoryItemId = milk.Id,
                        Name = "2%",
                        SortOrder = 2,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Bread
                    new InventoryItemOption
                    {
                        InventoryItemId = bread.Id,
                        Name = "White",
                        SortOrder = 1,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItemOption
                    {
                        InventoryItemId = bread.Id,
                        Name = "Wheat",
                        SortOrder = 2,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItemOption
                    {
                        InventoryItemId = bread.Id,
                        Name = "Both",
                        SortOrder = 3,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Rice
                    new InventoryItemOption
                    {
                        InventoryItemId = rice.Id,
                        Name = "White",
                        SortOrder = 1,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItemOption
                    {
                        InventoryItemId = rice.Id,
                        Name = "Brown",
                        SortOrder = 2,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Oatmeal
                    new InventoryItemOption
                    {
                        InventoryItemId = oatmeal.Id,
                        Name = "Instant",
                        SortOrder = 1,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItemOption
                    {
                        InventoryItemId = oatmeal.Id,
                        Name = "Quick",
                        SortOrder = 2,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Pinto Beans
                    new InventoryItemOption
                    {
                        InventoryItemId = pintoBeans.Id,
                        Name = "Canned",
                        SortOrder = 1,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItemOption
                    {
                        InventoryItemId = pintoBeans.Id,
                        Name = "Dry",
                        SortOrder = 2,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Black Beans
                    new InventoryItemOption
                    {
                        InventoryItemId = blackBeans.Id,
                        Name = "Canned",
                        SortOrder = 1,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryItemOption
                    {
                        InventoryItemId = blackBeans.Id,
                        Name = "Dry",
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

            // ============================================================
            // INVENTORY CHOICE GROUPS
            // ============================================================
            // InventoryChoiceGroups represent grouped client choices where one selection
            // is made from multiple different InventoryItems.
            // Examples:
            // - Vegetable Oil or Mayo
            // - Ketchup or Mustard
            // - Sugar or Flour
            // - Pancake Syrup or Jelly
            if (!await context.InventoryChoiceGroups.AnyAsync())
            {
                context.InventoryChoiceGroups.AddRange(
                    new InventoryChoiceGroup
                    {
                        Name = "OilOrMayo",
                        DisplayLabel = "Vegetable Oil or Mayo",
                        MaxSelections = 1,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryChoiceGroup
                    {
                        Name = "KetchupOrMustard",
                        DisplayLabel = "Ketchup or Mustard",
                        MaxSelections = 1,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryChoiceGroup
                    {
                        Name = "SugarOrFlour",
                        DisplayLabel = "Sugar or Flour",
                        MaxSelections = 1,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryChoiceGroup
                    {
                        Name = "PancakeSyrupOrJelly",
                        DisplayLabel = "Pancake Syrup or Jelly",
                        MaxSelections = 1,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }

            // ============================================================
            // INVENTORY CHOICE GROUP ITEMS
            // ============================================================
            // InventoryChoiceGroupItems link grouped client choices to the real inventory items
            // that belong to that group.
            if (!await context.InventoryChoiceGroupItems.AnyAsync())
            {
                // Pull choice groups for FK use in InventoryChoiceGroupItems seeding.
                var oilOrMayoGroup = await context.InventoryChoiceGroups.FirstAsync(g => g.Name == "OilOrMayo");
                var ketchupOrMustardGroup = await context.InventoryChoiceGroups.FirstAsync(g => g.Name == "KetchupOrMustard");
                var sugarOrFlourGroup = await context.InventoryChoiceGroups.FirstAsync(g => g.Name == "SugarOrFlour");
                var pancakeSyrupOrJellyGroup = await context.InventoryChoiceGroups.FirstAsync(g => g.Name == "PancakeSyrupOrJelly");

                // Pull inventory items for FK use in InventoryChoiceGroupItems seeding.
                var vegetableOil = await context.InventoryItems.FirstAsync(i => i.Name == "Vegetable Oil");
                var mayo = await context.InventoryItems.FirstAsync(i => i.Name == "Mayo");
                var ketchup = await context.InventoryItems.FirstAsync(i => i.Name == "Ketchup");
                var mustard = await context.InventoryItems.FirstAsync(i => i.Name == "Mustard");
                var sugar = await context.InventoryItems.FirstAsync(i => i.Name == "Sugar");
                var flour = await context.InventoryItems.FirstAsync(i => i.Name == "Flour");
                var pancakeSyrup = await context.InventoryItems.FirstAsync(i => i.Name == "Pancake Syrup");
                var jelly = await context.InventoryItems.FirstAsync(i => i.Name == "Jelly");

                context.InventoryChoiceGroupItems.AddRange(
                    // Vegetable Oil or Mayo
                    new InventoryChoiceGroupItem
                    {
                        InventoryChoiceGroupId = oilOrMayoGroup.Id,
                        InventoryItemId = vegetableOil.Id,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryChoiceGroupItem
                    {
                        InventoryChoiceGroupId = oilOrMayoGroup.Id,
                        InventoryItemId = mayo.Id,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Ketchup or Mustard
                    new InventoryChoiceGroupItem
                    {
                        InventoryChoiceGroupId = ketchupOrMustardGroup.Id,
                        InventoryItemId = ketchup.Id,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryChoiceGroupItem
                    {
                        InventoryChoiceGroupId = ketchupOrMustardGroup.Id,
                        InventoryItemId = mustard.Id,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Sugar or Flour
                    new InventoryChoiceGroupItem
                    {
                        InventoryChoiceGroupId = sugarOrFlourGroup.Id,
                        InventoryItemId = sugar.Id,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryChoiceGroupItem
                    {
                        InventoryChoiceGroupId = sugarOrFlourGroup.Id,
                        InventoryItemId = flour.Id,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },

                    // Pancake Syrup or Jelly
                    new InventoryChoiceGroupItem
                    {
                        InventoryChoiceGroupId = pancakeSyrupOrJellyGroup.Id,
                        InventoryItemId = pancakeSyrup.Id,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new InventoryChoiceGroupItem
                    {
                        InventoryChoiceGroupId = pancakeSyrupOrJellyGroup.Id,
                        InventoryItemId = jelly.Id,
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }


            // ============================================================
            // USER CHOICE GROUP PREFERENCES
            // ============================================================
            // UserChoiceGroupPreferences store a user's selected InventoryItem
            // for a grouped choice such as "Sugar or Flour".
            if (!await context.UserChoiceGroupPreferences.AnyAsync())
            {
                // Pull choice groups for FK use in UserChoiceGroupPreferences seeding.
                var sugarOrFlourGroup = await context.InventoryChoiceGroups
                    .FirstAsync(g => g.Name == "SugarOrFlour");

                var ketchupOrMustardGroup = await context.InventoryChoiceGroups
                    .FirstAsync(g => g.Name == "KetchupOrMustard");

                // Pull inventory items for FK use in UserChoiceGroupPreferences seeding.
                var flour = await context.InventoryItems.FirstAsync(i => i.Name == "Flour");
                var mustard = await context.InventoryItems.FirstAsync(i => i.Name == "Mustard");

                context.UserChoiceGroupPreferences.AddRange(
                    new UserChoiceGroupPreference
                    {
                        UserId = clientUser1.Id,
                        InventoryChoiceGroupId = sugarOrFlourGroup.Id,
                        SelectedInventoryItemId = flour.Id,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new UserChoiceGroupPreference
                    {
                        UserId = clientUser2.Id,
                        InventoryChoiceGroupId = ketchupOrMustardGroup.Id,
                        SelectedInventoryItemId = mustard.Id,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }


            // ============================================================
            // REFERRALS
            // ============================================================
            // Referrals link a client (ClientUserId) to an organization (ReferringOrganizationId)
            // and store metadata such as status, contact info, and notes.
            if (!await context.Referrals.AnyAsync())
            {
                context.Referrals.Add(
                    new Referral
                    {
                        ClientUserId = clientUser1.Id,
                        ReferringOrganizationId = org1.Id,

                        // ReferredOn is set to "now" at seed time.
                        ReferredOn = DateTime.UtcNow,

                        Status = ReferralStatus.Pending,
                        Notes = "Initial seeded referral.",
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }

            // ============================================================
            // USER ITEM PREFERENCES
            // ============================================================
            // UserItemPreferences define how a user wants specific inventory items handled.
            // Example: Always / Ask / Never.
            //
            // NOTE:
            // - This section queries inventory items by name.
            // - If you later add duplicate names, these lookups can become ambiguous.
            if (!await context.UserItemPreferences.AnyAsync())
            {
                // Pull inventory items needed for preference rows.
                var toothpaste = await context.InventoryItems.FirstAsync(i => i.Name == "Toothpaste");
                var freshFruit = await context.InventoryItems.FirstAsync(i => i.Name == "Fresh Fruit");

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
                        InventoryItemId = freshFruit.Id,
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