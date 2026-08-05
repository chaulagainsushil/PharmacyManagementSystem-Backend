using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Application.Application.Repository;
using PMS.Application.Application.Service;
using PMS.Common;
using PMS.Repository.Data;

namespace PMS.DependencyHandler;

public static class ServiceExtensions
{
    /// <summary>
    /// Registers ASP.NET Identity, JWT Authentication, and all application services.
    /// Call this from Program.cs: builder.Services.AddApplicationServices(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── ASP.NET Identity ─────────────────────────────────────────────────
        services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength      = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase    = false;
                options.Password.RequireDigit        = false;
                options.User.RequireUniqueEmail      = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // ── JWT Authentication ────────────────────────────────────────────────
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey   = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtSettings["Issuer"],
                    ValidAudience            = jwtSettings["Audience"],
                    IssuerSigningKey         = new SymmetricSecurityKey(
                                                   Encoding.UTF8.GetBytes(secretKey))
                };
            });

        services.AddAuthorization();

        // ── Repository + Service DI ───────────────────────────────────────────
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }

    /// <summary>
    /// Seeds all application roles into the database if they do not already exist.
    /// Call this after <c>app.Build()</c>:
    /// <code>await app.SeedRolesAsync();</code>
    /// </summary>
    public static async Task SeedRolesAsync(this IApplicationBuilder app)
    {
        using var scope       = app.ApplicationServices.CreateScope();
        var roleManager       = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}
