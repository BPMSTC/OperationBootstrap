using A_New_Hope.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var db = services.GetRequiredService<ApplicationDbContext>();

            // Roles
            var roles = new[] { "Admin", "Staff" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!roleResult.Succeeded)
                    {
                        var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to create role '{role}': {errors}");
                    }
                }
            }

            // Domain admin (seeded by DataSeeder)
            var domainAdmin = await db.DomainUsers
                .FirstOrDefaultAsync(u => u.Email == "admin@anewhope.local");

            if (domainAdmin is null)
            {
                throw new InvalidOperationException(
                    "Domain admin user 'admin@anewhope.local' was not found. Ensure DataSeeder runs before IdentitySeeder.");
            }

            // Identity admin account
            const string adminEmail = "admin@anewhope.local";
            const string adminPassword = "ChangeMe123!"; // Change after first login

            var identityAdmin = await userManager.FindByEmailAsync(adminEmail);

            if (identityAdmin is null)
            {
                identityAdmin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    DomainUserId = domainAdmin.Id
                };

                var createResult = await userManager.CreateAsync(identityAdmin, adminPassword);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create Identity admin user: {errors}");
                }
            }
            else
            {
                // Keep link in sync if the seeded domain admin record changed
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

            // Ensure role assignment
            if (!await userManager.IsInRoleAsync(identityAdmin, "Admin"))
            {
                var addRoleResult = await userManager.AddToRoleAsync(identityAdmin, "Admin");
                if (!addRoleResult.Succeeded)
                {
                    var errors = string.Join("; ", addRoleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to assign Admin role: {errors}");
                }
            }
        }
    }
}