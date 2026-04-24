using A_New_Hope.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Data
{
    /// <summary>
    /// IdentitySeeder
    /// --------------
    /// This seeder is responsible for initializing ASP.NET Core Identity data:
    /// - Ensuring required Identity roles exist (Admin, Staff)
    /// - Ensuring there is an Identity login account for the seeded Domain admin user
    /// - Ensuring that Identity admin account is assigned to the "Admin" role
    ///
    /// IMPORTANT DISTINCTION IN THIS PROJECT:
    /// - DomainUsers (your "business users") are seeded by DataSeeder.
    /// - Identity users (ApplicationUser logins) and roles are seeded by IdentitySeeder.
    ///
    /// Why separate seeders?
    /// - DomainUsers represent your application’s user records (clients/staff/admins).
    /// - Identity represents authentication/authorization accounts and roles.
    /// - Not every DomainUser necessarily needs a login account.
    ///
    /// Ordering requirement:
    /// - DataSeeder must run BEFORE IdentitySeeder, because IdentitySeeder expects that
    ///   the domain admin user ("admin@anewhope.local") already exists in DomainUsers.
    ///
    /// Behavior summary:
    /// 1) Ensure Identity roles exist:
    ///    - "Admin"
    ///    - "Staff"
    /// 2) Lookup Domain admin by email (seeded by DataSeeder).
    /// 3) Lookup or create Identity admin user with the same email.
    /// 4) Ensure Identity admin user links to the Domain admin (DomainUserId FK-like link).
    /// 5) Ensure Identity admin is in the "Admin" role.
    ///
    /// Notes:
    /// - This seeder throws InvalidOperationException when it cannot safely proceed.
    ///   That is intentional in development: it surfaces misconfigurations early.
    /// - Password used here is a development default and should be changed after first login.
    /// </summary>
    public static class IdentitySeeder
    {
        /// <summary>
        /// Seeds Identity roles and an initial admin Identity account.
        ///
        /// Parameter:
        /// - services: IServiceProvider used to resolve Identity and DbContext services from DI.
        ///
        /// This method:
        /// - Resolves RoleManager, UserManager, and ApplicationDbContext from the service provider.
        /// - Ensures roles exist.
        /// - Ensures the seeded domain admin has a corresponding Identity account.
        /// - Ensures the Identity account is assigned the Admin role.
        /// </summary>
        public static async Task SeedAsync(IServiceProvider services)
        {
            // Resolve Identity and database services from the DI container.
            // These are registered in Program.cs via AddIdentity(...) and AddDbContext(...).
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var db = services.GetRequiredService<ApplicationDbContext>();

            // ------------------------------------------------------------
            // 1) Ensure required Identity roles exist
            // ------------------------------------------------------------
            // These role names are used throughout the app in [Authorize(Roles="...")] attributes.
            // If roles don't exist, role assignment will fail and authorization will not behave correctly.
            var roles = new[] { "Admin", "Staff" };

            foreach (var role in roles)
            {
                // RoleExistsAsync checks Identity tables for a role with this name.
                if (!await roleManager.RoleExistsAsync(role))
                {
                    // Create the role if it doesn't exist.
                    var roleResult = await roleManager.CreateAsync(new IdentityRole(role));

                    // Identity operations return IdentityResult, which contains errors if unsuccessful.
                    if (!roleResult.Succeeded)
                    {
                        var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to create role '{role}': {errors}");
                    }
                }
            }

            // ------------------------------------------------------------
            // 2) Locate the Domain admin user (created by DataSeeder)
            // ------------------------------------------------------------
            // This is your "business user" record. IdentitySeeder requires it so it can
            // link the Identity admin login to the correct DomainUser.
            var domainAdmin = await db.DomainUsers
                .FirstOrDefaultAsync(u => u.Email == "admin@anewhope.local");

            if (domainAdmin is null)
            {
                // If this happens, it usually means:
                // - DataSeeder did not run
                // - Or the seed admin email changed
                // - Or the DomainUsers table is empty/misconfigured
                throw new InvalidOperationException(
                    "Domain admin user 'admin@anewhope.local' was not found. Ensure DataSeeder runs before IdentitySeeder.");
            }

            var domainStaff = await db.DomainUsers
                .FirstOrDefaultAsync(u => u.Email == "staff@anewhope.local");

            if (domainStaff is null)
            {
                throw new InvalidOperationException(
                    "Domain staff user 'staff@anewhope.local' was not found. Ensure DataSeeder runs before IdentitySeeder.");
            }

            // ------------------------------------------------------------
            // 3) Ensure an Identity admin account exists (ApplicationUser)
            // ------------------------------------------------------------
            // These constants are used for the initial Identity login.
            // NOTE: In production you'd usually avoid hard-coded passwords and instead:
            // - generate a random password and deliver it securely
            // - or require password creation via invitation/onboarding flow
            const string adminEmail = "admin@anewhope.local";
            const string adminPassword = "ChangeMe123!"; // Development default; change after first login

            // Find Identity user by email.
            var identityAdmin = await userManager.FindByEmailAsync(adminEmail);

            if (identityAdmin is null)
            {
                // Create Identity admin user if it doesn't exist.
                identityAdmin = new ApplicationUser
                {
                    // Identity uses UserName as the primary login identifier by default.
                    // Many apps set UserName to the email for convenience.
                    UserName = adminEmail,
                    Email = adminEmail,

                    // Setting EmailConfirmed to true avoids confirmation flow in dev environments.
                    EmailConfirmed = true,

                    // Link Identity user to DomainUser record so your app can correlate them.
                    DomainUserId = domainAdmin.Id
                };

                // CreateAsync both inserts the Identity user and hashes/stores the password.
                var createResult = await userManager.CreateAsync(identityAdmin, adminPassword);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create Identity admin user: {errors}");
                }
            }
            else
            {
                // Identity user already exists.
                // Keep the DomainUserId link in sync in case the seeded Domain admin record changed.
                // (Example: if the DB was reset/reseeded and the DomainUser got a new Id.)
                if (identityAdmin.DomainUserId != domainAdmin.Id)
                {
                    identityAdmin.DomainUserId = domainAdmin.Id;

                    var updateResult = await userManager.UpdateAsync(identityAdmin);
                    if (!updateResult.Succeeded)
                    {
                        var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to update Identity admin user: {errors}");
                    }
                }
            }

            const string staffEmail = "staff@anewhope.local";
            const string staffPassword = "ChangeMe123!";

            var identityStaff = await userManager.FindByEmailAsync(staffEmail);

            if (identityStaff is null)
            {
                identityStaff = new ApplicationUser
                {
                    UserName = staffEmail,
                    Email = staffEmail,
                    EmailConfirmed = true,
                    DomainUserId = domainStaff.Id
                };

                var createResult = await userManager.CreateAsync(identityStaff, staffPassword);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create Identity staff user: {errors}");
                }
            }
            else
            {
                if (identityStaff.DomainUserId != domainStaff.Id)
                {
                    identityStaff.DomainUserId = domainStaff.Id;

                    var updateResult = await userManager.UpdateAsync(identityStaff);
                    if (!updateResult.Succeeded)
                    {
                        var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to update Identity staff user: {errors}");
                    }
                }
            }

            if (!await userManager.IsInRoleAsync(identityStaff, "Staff"))
            {
                var addRoleResult = await userManager.AddToRoleAsync(identityStaff, "Staff");
                if (!addRoleResult.Succeeded)
                {
                    var errors = string.Join("; ", addRoleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to assign Staff role: {errors}");
                }
            }

            // ------------------------------------------------------------
            // 4) Ensure the Identity admin user is assigned the "Admin" role
            // ------------------------------------------------------------
            // Even if the user exists, we must ensure their role membership is correct,
            // otherwise authorization filters like [Authorize(Roles = "Admin")] won't grant access.
            if (!await userManager.IsInRoleAsync(identityAdmin, "Admin"))
            {
                var addRoleResult = await userManager.AddToRoleAsync(identityAdmin, "Admin");
                if (!addRoleResult.Succeeded)
                {
                    var errors = string.Join("; ", addRoleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to assign Admin role: {errors}");
                }
            }

            // End of Identity seeding.
            // If the method reaches here without throwing, Identity roles and admin account
            // are properly initialized for local development.
        }
    }
}