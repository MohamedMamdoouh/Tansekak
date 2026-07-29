using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tansekak.Application;
using Tansekak.Application.Interfaces;
using Tansekak.Infrastructure.Identity;
using Tansekak.Infrastructure.Persistence;
using Tansekak.Infrastructure.Seeding;
using Tansekak.Infrastructure.Services;

namespace Tansekak.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            if (environment.IsEnvironment("Testing"))
                options.UseInMemoryDatabase("TansekakIntegrationTests");
            else
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        });

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        if (!environment.IsDevelopment())
        {
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });
        }

        services.AddScoped<IDataSeeder, JsonSeedService>();
        services.AddScoped<IConfigService, ConfigService>();
        services.AddScoped<IAdmissionPredictionService, AdmissionPredictionService>();
        services.AddScoped<IGovernorateService, GovernorateService>();
        services.AddScoped<IUniversityService, UniversityService>();
        services.AddScoped<IFacultyService, FacultyService>();
        services.AddScoped<IUniversityFacultyService, UniversityFacultyService>();
        services.AddScoped<IAdmissionYearService, AdmissionYearService>();
        services.AddScoped<IAdmissionCutoffService, AdmissionCutoffService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IStudentResultImportService, StudentResultImportService>();
        services.AddScoped<IStudentResultService, StudentResultService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddSingleton<StudentResultImportJobQueue>();
        services.AddSingleton<ChunkedUploadSessionStore>();

        return services;
    }

    public static async Task SeedDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await seeder.SeedAsync();

        await SeedAdminUserAsync(scope.ServiceProvider);
    }

    private static async Task SeedAdminUserAsync(IServiceProvider sp)
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();

        const string role = "Administrator";
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

        var email = config["AdminSeed:Email"] ?? "admin@tansekak.local";
        var password = config["AdminSeed:Password"] ?? "Admin@12345";

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            await userManager.CreateAsync(user, password);
            await userManager.AddToRoleAsync(user, role);
        }
    }
}
