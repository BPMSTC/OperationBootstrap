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
using A_New_Hope.Models.Enums;
using Microsoft.EntityFrameworkCore;

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

            // Helper arrays for random Client data generation (used in some seed sections below).
            var clientFirstNames = new[]
            {
                "Jamie", "Taylor", "Morgan", "Riley", "Casey", "Jordan", "Alex", "Cameron", "Drew", "Avery",
                "Parker", "Quinn", "Reese", "Skyler", "Dakota", "Harper", "Rowan", "Sawyer", "Emerson", "Finley",
                "Bailey", "Charlie", "Hayden", "Kendall", "Logan", "Micah", "Payton", "Reagan", "Sage", "Tatum",
                "Addison", "Blake", "Corey", "Devon", "Elliot", "Frankie", "Gray", "Hunter", "Indigo", "Jesse",
                "Kai", "Lane", "Marley", "Nico", "Oakley", "Phoenix", "River", "Shawn", "Terry", "Winter"
            };

            var clientLastNames = new[]
            {
                "Anderson", "Bennett", "Carter", "Dawson", "Ellis", "Foster", "Garcia", "Harris", "Iverson", "Johnson",
                "Keller", "Larson", "Miller", "Nelson", "Owens", "Peterson", "Quincy", "Roberts", "Stevens", "Turner",
                "Underwood", "Valdez", "Walker", "Young", "Zimmerman", "Brooks", "Collins", "Diaz", "Evans", "Flores",
                "Gibson", "Hayes", "Ingram", "Jacobs", "Knight", "Lewis", "Mason", "Norris", "Ortiz", "Price",
                "Reed", "Sullivan", "Thomas", "Vaughn", "Watson", "Xu", "Yates", "Zimmer", "Porter", "Hughes",

                "Adams", "Bishop", "Campbell", "Douglas", "Edwards", "Franklin", "Griffin", "Henderson", "Irwin", "Jennings",
                "Kim", "Lawson", "Mitchell", "Newton", "Olsen", "Powell", "Quinn", "Ramirez", "Sanders", "Thompson",
                "Upton", "Vasquez", "West", "York", "Zane", "Barker", "Chavez", "Duncan", "Erickson", "Fields",
                "Graham", "Holland", "Isaac", "Jefferson", "Kramer", "Long", "Morales", "Newman", "O'Brien", "Parker",
                "Rhodes", "Schmidt", "Tran", "Vega", "Wallace", "Yu", "Ziegler", "Pierce", "Holmes", "Fletcher"
            };

            var cities = new[]
            {
                "Stevens Point", "Plover", "Whiting", "Park Ridge", "Junction City", "Amherst", "Rosholt", "Custer"
            };

            var postalCodes = new[]
            {
                "54481", "54467", "54482", "54423", "54407", "54473", "54475", "54406"
            };

            var employmentStatuses = Enum.GetValues(typeof(EmploymentStatus))
                .Cast<EmploymentStatus>()
                .ToArray();

            var incomeTypes = Enum.GetValues(typeof(IncomeType))
                .Cast<IncomeType>()
                .ToArray();

            var referralStatuses = Enum.GetValues(typeof(ReferralStatus))
                .Cast<ReferralStatus>()
                .ToArray();
            //* End helper arrays for random Client data generation



            // ============================================================
            // USERS (DomainUsers)
            // ============================================================
            if (!await context.DomainUsers.AnyAsync())
            {
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

                var staff1 = new DomainUser
                {
                    Email = "staff@anewhope.local",
                    FirstName = "Sample",
                    LastName = "Staff",
                    UserType = UserType.Staff,
                    DefaultPreference = PreferenceOption.Ask,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                var staff2 = new DomainUser
                {
                    Email = "intake.staff@anewhope.local",
                    FirstName = "Intake",
                    LastName = "Coordinator",
                    UserType = UserType.Staff,
                    DefaultPreference = PreferenceOption.Ask,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                var clients = new List<DomainUser>();

                for (int i = 1; i <= 100; i++)
                {
                    var firstName = clientFirstNames[(i - 1) % clientFirstNames.Length];
                    var lastName = clientLastNames[(i - 1) % clientLastNames.Length];

                    var cityIndex = (i - 1) % cities.Length;

                    var isUnhoused = i % 5 == 0;

                    clients.Add(new DomainUser
                    {
                        Email = $"client{i:000}@anewhope.local",
                        FirstName = firstName,
                        LastName = lastName,
                        UserType = UserType.Client,
                        DefaultPreference = i % 3 == 0
                            ? PreferenceOption.Always
                            : i % 3 == 1
                                ? PreferenceOption.Ask
                                : PreferenceOption.Never,
                        IsActive = true,
                        PhoneNumber = $"555-{100 + i:000}-{2000 + i:0000}",

                        AddressLine1 = isUnhoused ? null : $"{100 + i} Main Street",
                        AddressLine2 = null,
                        City = isUnhoused ? null : cities[cityIndex],
                        State = isUnhoused ? null : "WI",
                        PostalCode = isUnhoused ? null : postalCodes[cityIndex],

                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }

                context.DomainUsers.AddRange(admin, staff1, staff2);
                context.DomainUsers.AddRange(clients);

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

            var seededClients = await context.DomainUsers
                .Where(u => u.UserType == UserType.Client && u.DeletedAt == null)
                .OrderBy(u => u.Id)
                .ToListAsync();

            var clientUser1 = seededClients.First();
            var clientUser2 = seededClients.Skip(1).First();

            // ============================================================
            // CLIENT PROFILES
            // ============================================================
            // ClientProfiles represent additional client-specific data tied 1:1 to a DomainUser (UserId).
            // This section seeds one profile per client user.
            if (!await context.ClientProfiles.AnyAsync())
            {
                var profiles = new List<ClientProfile>();

                for (int i = 0; i < seededClients.Count; i++)
                {
                    var client = seededClients[i];

                    profiles.Add(new ClientProfile
                    {
                        UserId = client.Id,
                        EmploymentStatus = GetSeededEmploymentStatus(i),
                        IsUnhoused = string.IsNullOrWhiteSpace(client.AddressLine1)
                            && string.IsNullOrWhiteSpace(client.City)
                            && string.IsNullOrWhiteSpace(client.State)
                            && string.IsNullOrWhiteSpace(client.PostalCode),
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }

                context.ClientProfiles.AddRange(profiles);
                await context.SaveChangesAsync();
            }

            // ============================================================
            // CLIENT INCOMES
            // ============================================================
            // ClientIncomes store categorized monthly income rows for each client profile.
            if (!await context.ClientIncomes.AnyAsync())
            {
                var incomes = new List<ClientIncome>();

                for (int i = 0; i < seededClients.Count; i++)
                {
                    var client = seededClients[i];
                    var employmentStatus = GetSeededEmploymentStatus(i);

                    var hasEmploymentIncome =
                        employmentStatus == EmploymentStatus.FullTime ||
                        employmentStatus == EmploymentStatus.PartTime ||
                        employmentStatus == EmploymentStatus.SelfEmployed;

                    // Working clients should always have income.
                    // Non-working clients get income records about half the time.
                    if (!hasEmploymentIncome && i % 2 != 0)
                    {
                        continue;
                    }

                    var incomeRowCount = (i % 2) + 1;

                    for (int j = 0; j < incomeRowCount; j++)
                    {
                        incomes.Add(new ClientIncome
                        {
                            ClientProfileUserId = client.Id,
                            IncomeType = GetSeededIncomeType(i, j, employmentStatus),
                            MonthlyAmount = 150m + (i * 10m) + (j * 125m),
                            IsActive = true,
                            Notes = j == 0 ? null : "Additional seeded income source.",
                            CreatedByUserId = adminUser.Id,
                            UpdatedByUserId = adminUser.Id,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                    }
                }

                context.ClientIncomes.AddRange(incomes);
                await context.SaveChangesAsync();
            }

            // ============================================================
            // HOUSEHOLD MEMBERS
            // ============================================================
            // HouseholdMembers represent additional people in a client’s household.
            // Each member is linked to a client user via ClientUserId.
            if (!await context.HouseholdMembers.AnyAsync())
            {
                var householdMembers = new List<HouseholdMember>();

                var householdFirstNames = new[]
                {
                    "Sam", "Chris", "Pat", "Lee", "Robin", "Dana", "Leslie", "Shannon", "Kris", "Jo",
                    "Mackenzie", "Noel", "Remy", "Sidney", "Toni"
                };

                for (int i = 0; i < seededClients.Count; i++)
                {
                    var client = seededClients[i];

                    // Half of clients get household members.
                    if (i % 2 != 0)
                    {
                        continue;
                    }

                    // Each selected client gets 1-3 household members.
                    var memberCount = (i % 3) + 1;

                    for (int j = 0; j < memberCount; j++)
                    {
                        householdMembers.Add(new HouseholdMember
                        {
                            ClientUserId = client.Id,
                            FirstName = householdFirstNames[(i + j) % householdFirstNames.Length],
                            LastName = client.LastName,
                            DateOfBirth = new DateTime(
                                2010 + ((i + j) % 12),
                                ((j + 1) % 12) + 1,
                                ((i + j) % 25) + 1),
                            CreatedByUserId = adminUser.Id,
                            UpdatedByUserId = adminUser.Id,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                    }
                }

                context.HouseholdMembers.AddRange(householdMembers);
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
                    },
                    new ReferringOrganization
                    {
                        Name = "CAP Services",
                        PhoneNumber = "555-210-1100",
                        Email = "community@capservices.local",
                        AddressLine1 = "2900 Hoover Road",
                        City = "Stevens Point",
                        State = "WI",
                        PostalCode = "54481",
                        PrimaryContactName = "Morgan Fields",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new ReferringOrganization
                    {
                        Name = "Stevens Point Area Senior Center",
                        PhoneNumber = "555-220-1100",
                        Email = "seniorreferrals@spasc.local",
                        AddressLine1 = "1200 Maria Drive",
                        City = "Stevens Point",
                        State = "WI",
                        PostalCode = "54481",
                        PrimaryContactName = "Riley Stone",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new ReferringOrganization
                    {
                        Name = "Plover Community Outreach",
                        PhoneNumber = "555-230-1100",
                        Email = "outreach@plovercommunity.local",
                        AddressLine1 = "333 Post Road",
                        City = "Plover",
                        State = "WI",
                        PostalCode = "54467",
                        PrimaryContactName = "Casey Morgan",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new ReferringOrganization
                    {
                        Name = "Central Wisconsin Housing Support",
                        PhoneNumber = "555-240-1100",
                        Email = "housing@centralwihousing.local",
                        AddressLine1 = "702 Division Street",
                        City = "Stevens Point",
                        State = "WI",
                        PostalCode = "54481",
                        PrimaryContactName = "Dakota Reed",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new ReferringOrganization
                    {
                        Name = "Family Crisis Resource Center",
                        PhoneNumber = "555-250-1100",
                        Email = "help@familycrisis.local",
                        AddressLine1 = "1880 Church Street",
                        City = "Stevens Point",
                        State = "WI",
                        PostalCode = "54481",
                        PrimaryContactName = "Taylor Brooks",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new ReferringOrganization
                    {
                        Name = "Veterans Assistance Network",
                        PhoneNumber = "555-260-1100",
                        Email = "veterans@assistnetwork.local",
                        AddressLine1 = "500 Clark Street",
                        City = "Stevens Point",
                        State = "WI",
                        PostalCode = "54481",
                        PrimaryContactName = "Parker Evans",
                        IsActive = true,
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );

                await context.SaveChangesAsync();
            }

            var referringOrganizations = await context.ReferringOrganizations
                .Where(o => o.DeletedAt == null && o.IsActive)
                .OrderBy(o => o.Name)
                .ToListAsync();

            var serviceCategories = await context.ServiceCategories
                .Where(c => c.DeletedAt == null && c.IsActive)
                .ToListAsync();

            var foodServiceCategory = serviceCategories.First(c => c.Name == "Food");
            var medicalServiceCategory = serviceCategories.First(c => c.Name == "Medical");
            var transportationServiceCategory = serviceCategories.First(c => c.Name == "Transportation");
            var clothingServiceCategory = serviceCategories.First(c => c.Name == "Clothing");
            var hygieneServiceCategory = serviceCategories.First(c => c.Name == "Hygiene");
            var babySuppliesServiceCategory = serviceCategories.First(c => c.Name == "Baby Supplies");

            if (!await context.ReferringOrganizationServiceCategories.AnyAsync())
            {
                var organizationServiceCategoryMap = new Dictionary<string, List<ServiceCategory>>
                {
                    ["Portage County Social Services"] = new List<ServiceCategory>
                    {
                        foodServiceCategory,
                        transportationServiceCategory
                    },

                    ["Hope Community Clinic"] = new List<ServiceCategory>
                    {
                        medicalServiceCategory,
                        hygieneServiceCategory
                    },

                    ["CAP Services"] = new List<ServiceCategory>
                    {
                        foodServiceCategory,
                        transportationServiceCategory,
                        clothingServiceCategory
                    },

                    ["Stevens Point Area Senior Center"] = new List<ServiceCategory>
                    {
                        foodServiceCategory,
                        transportationServiceCategory,
                        medicalServiceCategory
                    },

                    ["Plover Community Outreach"] = new List<ServiceCategory>
                    {
                        foodServiceCategory,
                        clothingServiceCategory,
                        hygieneServiceCategory
                    },

                    ["Central Wisconsin Housing Support"] = new List<ServiceCategory>
                    {
                        transportationServiceCategory,
                        hygieneServiceCategory
                    },

                    ["Family Crisis Resource Center"] = new List<ServiceCategory>
                    {
                        foodServiceCategory,
                        clothingServiceCategory,
                        hygieneServiceCategory,
                        babySuppliesServiceCategory
                    },

                    ["Veterans Assistance Network"] = new List<ServiceCategory>
                    {
                        foodServiceCategory,
                        transportationServiceCategory,
                        medicalServiceCategory
                    }
                };
                var referringOrganizationServiceCategories = new List<ReferringOrganizationServiceCategory>();

                foreach (var mapItem in organizationServiceCategoryMap)
                {
                    var organization = referringOrganizations.FirstOrDefault(o => o.Name == mapItem.Key);

                    if (organization == null)
                    {
                        continue;
                    }

                    foreach (var serviceCategory in mapItem.Value)
                    {
                        referringOrganizationServiceCategories.Add(new ReferringOrganizationServiceCategory
                        {
                            ReferringOrganizationId = organization.Id,
                            ServiceCategoryId = serviceCategory.Id
                        });
                    }
                }

                context.ReferringOrganizationServiceCategories.AddRange(referringOrganizationServiceCategories);
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
                var referrals = new List<Referral>();

                for (int i = 0; i < seededClients.Count; i++)
                {
                    var client = seededClients[i];

                    var referredOn = now.Date.AddDays(-(i % 90));
                    var status = referralStatuses[i % referralStatuses.Length];
                    var validTo = GetSeededValidTo(status, referredOn, now.Date);

                    referrals.Add(new Referral
                    {
                        ClientUserId = client.Id,
                        ReferringOrganizationId = referringOrganizations[i % referringOrganizations.Count].Id,
                        ReferredOn = referredOn,
                        Status = status,
                        ValidFrom = referredOn,
                        ValidTo = validTo,
                        Notes = $"Seeded referral #{i + 1} for search and filter testing.",
                        CreatedByUserId = adminUser.Id,
                        UpdatedByUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now
                    });

                    // About 25% of clients get a second referral.
                    if (i % 4 == 0)
                    {
                        var secondReferredOn = now.Date.AddDays(-(i % 120) - 7);
                        var secondStatus = referralStatuses[(i + 2) % referralStatuses.Length];
                        var secondValidTo = GetSeededValidTo(secondStatus, secondReferredOn, now.Date);

                        referrals.Add(new Referral
                        {
                            ClientUserId = client.Id,
                            ReferringOrganizationId = referringOrganizations[(i + 3) % referringOrganizations.Count].Id,
                            ReferredOn = secondReferredOn,
                            Status = secondStatus,
                            ValidFrom = secondReferredOn,
                            ValidTo = secondValidTo,
                            Notes = $"Additional seeded referral for client #{i + 1}.",
                            CreatedByUserId = adminUser.Id,
                            UpdatedByUserId = adminUser.Id,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                    }
                }

                context.Referrals.AddRange(referrals);
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

        // GetSeededValidTo determines the ValidTo date for seeded referrals based on their status.
        private static DateTime? GetSeededValidTo(ReferralStatus status, DateTime validFrom, DateTime today)
        {
            return status switch
            {
                ReferralStatus.Pending => null,
                ReferralStatus.Denied => null,

                // Approved referrals should still be valid for about half a year from the seed date.
                ReferralStatus.Approved => today.AddDays(180),

                // Expired referrals should already be expired.
                ReferralStatus.Expired => today.AddDays(-7),

                // Closed referrals should have a ValidTo value, but it should be in the past.
                ReferralStatus.Closed => validFrom.AddDays(30) < today
                    ? validFrom.AddDays(30)
                    : today.AddDays(-1),

                _ => null
            };
        }

        private static EmploymentStatus GetSeededEmploymentStatus(int index)
        {
            var statuses = new[]
            {
                EmploymentStatus.FullTime,
                EmploymentStatus.PartTime,
                EmploymentStatus.SelfEmployed,
                EmploymentStatus.Unemployed,
                EmploymentStatus.Retired,
                EmploymentStatus.Student,
                EmploymentStatus.Disabled,
                EmploymentStatus.NotSpecified
            };

            return statuses[index % statuses.Length];
        }

        private static IncomeType GetSeededIncomeType(int clientIndex, int incomeIndex, EmploymentStatus employmentStatus)
        {
            var nonEmploymentIncomeTypes = new[]
            {
                IncomeType.SocialSecurity,
                IncomeType.ChildSupport,
                IncomeType.Disability,
                IncomeType.Unemployment,
                IncomeType.Pension,
                IncomeType.GeneralAssistance,
                IncomeType.Other
            };

            if (employmentStatus == EmploymentStatus.FullTime ||
                employmentStatus == EmploymentStatus.PartTime ||
                employmentStatus == EmploymentStatus.SelfEmployed)
            {
                return incomeIndex == 0
                    ? IncomeType.Employment
                    : nonEmploymentIncomeTypes[(clientIndex + incomeIndex) % nonEmploymentIncomeTypes.Length];
            }

            return nonEmploymentIncomeTypes[(clientIndex + incomeIndex) % nonEmploymentIncomeTypes.Length];
        }
    }
}
