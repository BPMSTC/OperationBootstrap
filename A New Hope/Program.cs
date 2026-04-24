using A_New_Hope.Data;
using A_New_Hope.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog; // ADDED
using A_New_Hope.Services;
using A_New_Hope.Services.Interfaces;
using MySql.Data.MySqlClient;
using System.Net.Sockets;

try
{
    // Create the application builder (reads config, sets up DI, logging, etc.)
    var builder = WebApplication.CreateBuilder(args);

    // ------------------------------
    // Logging
    // ------------------------------
    // Use Serilog for logging (configured via appsettings.json -> "Serilog" section).
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services);
    });

    // ------------------------------
    // MVC + Razor Pages
    // ------------------------------
    // Add MVC controllers/views, and register a global action filter that adds Controller/Action log scope.
    builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.Add<LoggingScopeFilter>();
    });

    // ------------------------------
    // Services/Interfaces
    // ------------------------------

    builder.Services.AddScoped<IReferringOrganizationService, ReferringOrganizationService>();
    builder.Services.AddScoped<IClientCreationService, ClientCreationService>();
    builder.Services.AddScoped<IReferralService, ReferralService>();

    // Identity UI endpoints (e.g., /Identity/Account/Login)
    builder.Services.AddRazorPages();

    // ------------------------------
    // Session
    // ------------------------------
    builder.Services.AddDistributedMemoryCache();

    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    // ------------------------------
    // Authorization
    // ------------------------------
    // Fallback policy: if an endpoint doesn't explicitly allow anonymous access,
    // the user must be authenticated AND in Admin or Staff role.
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole("Admin", "Staff")
            .Build();
    });

    // ------------------------------
    // Database
    // ------------------------------
    // Read connection string once and fail fast if missing.
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

    // Register EF Core DbContext using MySQL provider.
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySQL(connectionString));

    // ------------------------------
    // Identity
    // ------------------------------
    // Configure ASP.NET Core Identity (password rules, unique email, etc.)
    // and store Identity data in the same ApplicationDbContext.
    builder.Services
        .AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;

            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders()
        .AddDefaultUI();

    // Route unauthenticated users to the custom login page instead of default Identity UI login.
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

    var app = builder.Build();

    // Logs HTTP requests (status code + timing). Uses Serilog.
    app.UseSerilogRequestLogging(); // ADDED

    // ------------------------------
    // HTTP request pipeline
    // ------------------------------
    // Production-style error handling + HSTS (only outside development).
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles(); // Serves wwwroot assets (CSS/JS/images)
    app.UseRouting();

    app.UseSession();

    // AuthN first, then AuthZ.
    app.UseAuthentication();
    app.UseAuthorization();

    // Enables static asset mapping for endpoints configured with WithStaticAssets().
    app.MapStaticAssets();

    // Default MVC route: /{controller}/{action=Index}/{id?}
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller}/{action=Index}/{id?}")
        .WithStaticAssets();

    // Razor Pages routes (Identity UI endpoints live here)
    app.MapRazorPages();

    // ------------------------------
    // Seeding (Domain + Identity)
    // ------------------------------
    // Creates a scoped service provider so DbContext/Identity services resolve correctly.
    /*using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Seed domain tables (DomainUsers, Categories, etc.)
        await DataSeeder.SeedAsync(db);

        // Seed Identity roles/users (Admin/Staff roles + initial admin login)
        await IdentitySeeder.SeedAsync(scope.ServiceProvider);
    }*/

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        const int maxAttempts = 12;
        var delay = TimeSpan.FromSeconds(5);

        static bool IsTransientDatabaseStartupException(Exception? ex)
        {
            while (ex != null)
            {
                if (ex is MySqlException || ex is SocketException)
                {
                    return true;
                }

                ex = ex.InnerException;
            }

            return false;
        }

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                logger.LogInformation(
                    "Database startup attempt {Attempt} of {MaxAttempts}",
                    attempt,
                    maxAttempts);

                await dbContext.Database.MigrateAsync();
                await DataSeeder.SeedAsync(dbContext);
                await IdentitySeeder.SeedAsync(services);

                logger.LogInformation("Database migration and seeding completed successfully.");
                break;
            }
            catch (Exception ex)
            {
                var isTransient = IsTransientDatabaseStartupException(ex);

                if (!isTransient || attempt == maxAttempts)
                {
                    logger.LogError(
                        ex,
                        "Database startup tasks failed on attempt {Attempt} of {MaxAttempts}.",
                        attempt,
                        maxAttempts);
                    throw;
                }

                logger.LogWarning(
                    ex,
                    "Transient database startup failure on attempt {Attempt} of {MaxAttempts}. Waiting {DelaySeconds} seconds before retrying.",
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);

                await Task.Delay(delay);
            }
        }
    }

    app.Run(); // Start the web host
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}